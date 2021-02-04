using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using CIE.AspNetCore.Authentication.Events;
using CIE.AspNetCore.Authentication.Helpers;
using CIE.AspNetCore.Authentication.Models;
using CIE.AspNetCore.Authentication.Resources;
using CIE.AspNetCore.Authentication.Saml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;

namespace CIE.AspNetCore.Authentication
{
    public class CieHandler : RemoteAuthenticationHandler<CieOptions>, IAuthenticationSignOutHandler
    {
        EventsHandler _eventsHandler;
        RequestGenerator _requestGenerator;
        IHttpClientFactory _httpClientFactory;

        public CieHandler(IOptionsMonitor<CieOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IHttpClientFactory httpClientFactory)
            : base(options, logger, encoder, clock)
        {
            _httpClientFactory = httpClientFactory;
        }

        protected new CieEvents Events
        {
            get { return (CieEvents)base.Events; }
            set { base.Events = value; }
        }

        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new CieEvents());

        /// <summary>
        /// Decides whether this handler should handle request based on request path. If it's true, HandleRequestAsync method is invoked.
        /// </summary>
        /// <returns>value indicating whether the request should be handled or not</returns>
        public override async Task<bool> ShouldHandleRequestAsync()
        {
            var result = await base.ShouldHandleRequestAsync();
            if (!result)
            {
                result = Options.RemoteSignOutPath == Request.Path;
            }
            return result;
        }

        /// <summary>
        /// Handle the request and de
        /// </summary>
        /// <returns></returns>
        public override Task<bool> HandleRequestAsync()
        {
            _eventsHandler = new EventsHandler(Events);
            _requestGenerator = new RequestGenerator(Response, Logger);

            // RemoteSignOutPath and CallbackPath may be the same, fall through if the message doesn't match.
            if (Options.RemoteSignOutPath.HasValue && Options.RemoteSignOutPath == Request.Path)
            {
                // We've received a remote sign-out request
                return HandleRemoteSignOutAsync();
            }

            return base.HandleRequestAsync();
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // Save the original challenge URI so we can redirect back to it when we're done.
            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = OriginalPathBase + OriginalPath + Request.QueryString;
            }

            // Create the CIE request id
            string authenticationRequestId = Guid.NewGuid().ToString();

            // Select the Identity Provider
            var idp = Options.IdentityProvider;


            var securityTokenCreatingContext = await _eventsHandler.HandleSecurityTokenCreatingContext(Context, Scheme, Options, properties, authenticationRequestId);

            // Create the signed SAML request
            var message = SamlHandler.GetAuthnRequest(
                authenticationRequestId,
                securityTokenCreatingContext.TokenOptions.EntityId,
                securityTokenCreatingContext.TokenOptions.AssertionConsumerServiceIndex,
                securityTokenCreatingContext.TokenOptions.AttributeConsumingServiceIndex,
                securityTokenCreatingContext.TokenOptions.Certificate,
                idp);

            GenerateCorrelationId(properties);

            var (redirectHandled, afterRedirectMessage) = await _eventsHandler.HandleRedirectToIdentityProviderForAuthentication(Context, Scheme, Options, properties, message);
            if (redirectHandled)
            {
                return;
            }
            message = afterRedirectMessage;

            properties.SetAuthenticationRequest(message);
            properties.Save(Response, Options.StateDataFormat);

            await _requestGenerator.HandleRequest(message,
                message.ID,
                securityTokenCreatingContext.TokenOptions.Certificate,
                idp.SingleSignOnServiceUrl,
                idp.Method);
        }

        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            AuthenticationProperties properties = new AuthenticationProperties();
            properties.Load(Request, Options.StateDataFormat);

            var (id, message) = await ExtractInfoFromAuthenticationResponse();

            try
            {
                var request = properties.GetAuthenticationRequest();

                var validationMessageResult = await ValidateAuthenticationResponse(message, request, properties);
                if (validationMessageResult != null)
                    return validationMessageResult;

                var responseMessageReceivedResult = await _eventsHandler.HandleAuthenticationResponseMessageReceived(Context, Scheme, Options, properties, message);
                if (responseMessageReceivedResult.Result != null)
                {
                    return responseMessageReceivedResult.Result;
                }
                message = responseMessageReceivedResult.ProtocolMessage;
                properties = responseMessageReceivedResult.Properties;

                var correlationValidationResult = ValidateCorrelation(properties);
                if (correlationValidationResult != null)
                {
                    return correlationValidationResult;
                }

                var (principal, validFrom, validTo) = CreatePrincipal(message);

                AdjustAuthenticationPropertiesDates(properties, validFrom, validTo);

                properties.SetSubjectNameId(message.GetAssertion().Subject?.GetNameID()?.Value);
                properties.SetSessionIndex(message.GetAssertion().GetAuthnStatement().SessionIndex);
                properties.Save(Response, Options.StateDataFormat);

                var ticket = new AuthenticationTicket(principal, properties, Scheme.Name);
                await _eventsHandler.HandleAuthenticationSuccess(Context, Scheme, Options, id, ticket);
                return HandleRequestResult.Success(ticket);
            }
            catch (Exception exception)
            {
                Logger.ExceptionProcessingMessage(exception);

                var authenticationFailedResult = await _eventsHandler.HandleAuthenticationFailed(Context, Scheme, Options, message, exception);
                return authenticationFailedResult.Result ?? HandleRequestResult.Fail(exception, properties);
            }
        }

        public async virtual Task SignOutAsync(AuthenticationProperties properties)
        {
            var target = ResolveTarget(Options.ForwardSignOut);
            if (target != null)
            {
                await Context.SignOutAsync(target, properties);
                return;
            }

            string authenticationRequestId = Guid.NewGuid().ToString();

            var requestProperties = new AuthenticationProperties();
            requestProperties.Load(Request, Options.StateDataFormat);

            // Extract the user state from properties and reset.
            var subjectNameId = requestProperties.GetSubjectNameId();
            var sessionIndex = requestProperties.GetSessionIndex();

            var idp = Options.IdentityProvider;

            var securityTokenCreatingContext = await _eventsHandler.HandleSecurityTokenCreatingContext(Context, Scheme, Options, properties, authenticationRequestId);

            var message = SamlHandler.GetLogoutRequest(
                authenticationRequestId,
                securityTokenCreatingContext.TokenOptions.EntityId,
                securityTokenCreatingContext.TokenOptions.Certificate,
                idp,
                subjectNameId,
                sessionIndex);

            var (redirectHandled, afterRedirectMessage) = await _eventsHandler.HandleRedirectToIdentityProviderForSignOut(Context, Scheme, Options, properties, message);
            if (redirectHandled)
            {
                return;
            }
            message = afterRedirectMessage;

            properties.SetLogoutRequest(message);
            properties.Save(Response, Options.StateDataFormat);

            await _requestGenerator.HandleRequest(message,
                message.ID,
                securityTokenCreatingContext.TokenOptions.Certificate,
                idp.SingleSignOutServiceUrl,
                idp.Method);
        }

        protected virtual async Task<bool> HandleRemoteSignOutAsync()
        {
            var (id, message) = await ExtractInfoFromSignOutResponse();

            AuthenticationProperties requestProperties = new AuthenticationProperties();
            requestProperties.Load(Request, Options.StateDataFormat);

            var logoutRequest = requestProperties.GetLogoutRequest();

            var validSignOut = ValidateSignOutResponse(message, logoutRequest);
            if (!validSignOut)
                return false;

            var remoteSignOutContext = await _eventsHandler.HandleRemoteSignOut(Context, Scheme, Options, message);
            if (remoteSignOutContext.Result != null)
            {
                if (remoteSignOutContext.Result.Handled)
                {
                    Logger.RemoteSignOutHandledResponse();
                    return true;
                }
                if (remoteSignOutContext.Result.Skipped)
                {
                    Logger.RemoteSignOutSkipped();
                    return false;
                }
            }

            Logger.RemoteSignOut();

            await Context.SignOutAsync(Options.SignOutScheme);
            Response.Redirect(requestProperties.RedirectUri);
            return true;
        }

        private async Task<HandleRequestResult> ValidateAuthenticationResponse(ResponseType response, AuthnRequestType request, AuthenticationProperties properties)
        {
            if (response == null)
            {
                if (Options.SkipUnrecognizedRequests)
                {
                    return HandleRequestResult.SkipHandler();
                }

                return HandleRequestResult.Fail("No message.");
            }

            if (properties == null && !Options.AllowUnsolicitedLogins)
            {
                return HandleRequestResult.Fail("Unsolicited logins are not allowed.");
            }

            var idp = Options.IdentityProvider;

            var metadataIdp = await DownloadMetadataIDP(idp.OrganizationUrlMetadata);

            response.ValidateAuthnResponse(request, metadataIdp);
            return null;
        }

        private static readonly XmlSerializer entityDescriptorSerializer = new(typeof(EntityDescriptor));
        private static ConcurrentDictionary<string, string> metadataCache = new ConcurrentDictionary<string, string>();
        private async Task<EntityDescriptor> DownloadMetadataIDP(string urlMetadataIdp)
        {
            string xml = null;
            if (Options.CacheIdpMetadata
                && metadataCache.ContainsKey(urlMetadataIdp))
            {
                xml = metadataCache[urlMetadataIdp];
            }
            if (string.IsNullOrWhiteSpace(xml))
            {
                using var client = _httpClientFactory.CreateClient("cie");
                xml = await client.GetStringAsync(urlMetadataIdp);
            }
            if (Options.CacheIdpMetadata
                && !string.IsNullOrWhiteSpace(xml))
            {
                metadataCache.AddOrUpdate(urlMetadataIdp, xml, (x, y) => xml);
            }
            using var reader = new StringReader(xml);
            return (EntityDescriptor)entityDescriptorSerializer.Deserialize(reader);
        }

        private HandleRequestResult ValidateCorrelation(AuthenticationProperties properties)
        {
            if (properties.GetCorrelationProperty() != null && !ValidateCorrelationId(properties))
            {
                return HandleRequestResult.Fail("Correlation failed.", properties);
            }
            return null;
        }

        private void AdjustAuthenticationPropertiesDates(AuthenticationProperties properties, DateTimeOffset? validFrom, DateTimeOffset? validTo)
        {
            if (Options.UseTokenLifetime && validFrom != null && validTo != null)
            {
                // Override any session persistence to match the token lifetime.
                var issued = validFrom;
                if (issued != DateTimeOffset.MinValue)
                {
                    properties.IssuedUtc = issued.Value.ToUniversalTime();
                }
                var expires = validTo;
                if (expires != DateTimeOffset.MinValue)
                {
                    properties.ExpiresUtc = expires.Value.ToUniversalTime();
                }
                properties.AllowRefresh = false;
            }
        }

        private (ClaimsPrincipal principal, DateTimeOffset? validFrom, DateTimeOffset? validTo) CreatePrincipal(ResponseType idpAuthnResponse)
        {
            var claims = new Claim[]
            {
                new Claim( ClaimTypes.NameIdentifier, idpAuthnResponse.GetAssertion().GetAttributeStatement().GetAttributes().FirstOrDefault(x => SamlConst.fiscalNumber.Equals(x.Name) || SamlConst.fiscalNumber.Equals(x.FriendlyName))?.GetAttributeValue()?.Trim()?.Replace("TINIT-", "") ?? string.Empty),
                new Claim( SamlConst.name, idpAuthnResponse.GetAssertion().GetAttributeStatement().GetAttributes().FirstOrDefault(x => SamlConst.name.Equals(x.Name) || SamlConst.name.Equals(x.FriendlyName))?.GetAttributeValue()?.Trim() ?? string.Empty),
                new Claim( SamlConst.familyName, idpAuthnResponse.GetAssertion().GetAttributeStatement().GetAttributes().FirstOrDefault(x => SamlConst.familyName.Equals(x.Name) || SamlConst.familyName.Equals(x.FriendlyName))?.GetAttributeValue()?.Trim() ?? string.Empty),
                new Claim( SamlConst.fiscalNumber, idpAuthnResponse.GetAssertion().GetAttributeStatement().GetAttributes().FirstOrDefault(x => SamlConst.fiscalNumber.Equals(x.Name) || SamlConst.fiscalNumber.Equals(x.FriendlyName))?.GetAttributeValue()?.Trim()?.Replace("TINIT-", "") ?? string.Empty),
                new Claim( SamlConst.dateOfBirth, idpAuthnResponse.GetAssertion().GetAttributeStatement().GetAttributes().FirstOrDefault(x => SamlConst.dateOfBirth.Equals(x.Name) || SamlConst.dateOfBirth.Equals(x.FriendlyName))?.GetAttributeValue()?.Trim() ?? string.Empty),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name, SamlConst.fiscalNumber, null);

            var returnedPrincipal = new ClaimsPrincipal(identity);
            return (returnedPrincipal, new DateTimeOffset(idpAuthnResponse.IssueInstant), new DateTimeOffset(idpAuthnResponse.GetAssertion().Subject.GetSubjectConfirmation().SubjectConfirmationData.NotOnOrAfter));
        }

        private async Task<(string Id, ResponseType Message)> ExtractInfoFromAuthenticationResponse()
        {
            if (HttpMethods.IsPost(Request.Method)
              && !string.IsNullOrEmpty(Request.ContentType)
              // May have media/type; charset=utf-8, allow partial match.
              && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
              && Request.Body.CanRead)
            {
                var form = await Request.ReadFormAsync();

                return (
                    form["RelayState"].ToString(),
                    SamlHandler.GetAuthnResponse(form["SAMLResponse"][0])
                );
            }
            return (null, null);
        }

        private async Task<(string Id, LogoutResponseType Message)> ExtractInfoFromSignOutResponse()
        {
            if (HttpMethods.IsPost(Request.Method)
              && !string.IsNullOrEmpty(Request.ContentType)
              && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
              && Request.Body.CanRead)
            {
                var form = await Request.ReadFormAsync();

                return (
                    form["RelayState"].ToString(),
                    SamlHandler.GetLogoutResponse(form["SAMLResponse"][0])
                );
            }
            return (null, null);
        }

        private bool ValidateSignOutResponse(LogoutResponseType response, LogoutRequestType request)
        {
            var valid = response.Status.StatusCode.Value == SamlConst.Success && SamlHandler.ValidateLogoutResponse(response, request);
            if (valid)
            {
                return true;
            }

            Logger.RemoteSignOutFailed();
            return false;
        }

        private class EventsHandler
        {
            private CieEvents _events;

            public EventsHandler(CieEvents events)
            {
                _events = events;
            }

            public async Task<SecurityTokenCreatingContext> HandleSecurityTokenCreatingContext(HttpContext context, AuthenticationScheme scheme, CieOptions options, AuthenticationProperties properties, string samlAuthnRequestId)
            {
                var securityTokenCreatingContext = new SecurityTokenCreatingContext(context, scheme, options, properties)
                {
                    SamlAuthnRequestId = samlAuthnRequestId,
                    TokenOptions = new SecurityTokenCreatingOptions
                    {
                        EntityId = options.EntityId,
                        Certificate = options.Certificate,
                        AssertionConsumerServiceIndex = options.AssertionConsumerServiceIndex,
                        AttributeConsumingServiceIndex = options.AttributeConsumingServiceIndex
                    }
                };
                await _events.TokenCreating(securityTokenCreatingContext);
                return securityTokenCreatingContext;
            }

            public async Task<(bool, AuthnRequestType)> HandleRedirectToIdentityProviderForAuthentication(HttpContext context, AuthenticationScheme scheme, CieOptions options, AuthenticationProperties properties, AuthnRequestType message)
            {
                var redirectContext = new RedirectContext(context, scheme, options, properties, message);
                await _events.RedirectToIdentityProvider(redirectContext);
                return (redirectContext.Handled, (AuthnRequestType)redirectContext.SignedProtocolMessage);
            }

            public async Task<(bool, LogoutRequestType)> HandleRedirectToIdentityProviderForSignOut(HttpContext context, AuthenticationScheme scheme, CieOptions options, AuthenticationProperties properties, LogoutRequestType message)
            {
                var redirectContext = new RedirectContext(context, scheme, options, properties, message);
                await _events.RedirectToIdentityProvider(redirectContext);
                return (redirectContext.Handled, (LogoutRequestType)redirectContext.SignedProtocolMessage);
            }

            public async Task<MessageReceivedContext> HandleAuthenticationResponseMessageReceived(HttpContext context, AuthenticationScheme scheme, CieOptions options, AuthenticationProperties properties, ResponseType message)
            {
                var messageReceivedContext = new MessageReceivedContext(context, scheme, options, properties, message);
                await _events.MessageReceived(messageReceivedContext);
                return messageReceivedContext;
            }

            public async Task<AuthenticationSuccessContext> HandleAuthenticationSuccess(HttpContext context, AuthenticationScheme scheme, CieOptions options, string authenticationRequestId, AuthenticationTicket ticket)
            {
                var authenticationSuccessContext = new AuthenticationSuccessContext(context, scheme, options, authenticationRequestId, ticket);
                await _events.AuthenticationSuccess(authenticationSuccessContext);
                return authenticationSuccessContext;
            }

            public async Task<AuthenticationFailedContext> HandleAuthenticationFailed(HttpContext context, AuthenticationScheme scheme, CieOptions options, ResponseType message, Exception exception)
            {
                var authenticationFailedContext = new AuthenticationFailedContext(context, scheme, options, message, exception);
                await _events.AuthenticationFailed(authenticationFailedContext);
                return authenticationFailedContext;
            }

            public async Task<RemoteSignOutContext> HandleRemoteSignOut(HttpContext context, AuthenticationScheme scheme, CieOptions options, LogoutResponseType message)
            {
                var remoteSignOutContext = new RemoteSignOutContext(context, scheme, options, message);
                await _events.RemoteSignOut(remoteSignOutContext);
                return remoteSignOutContext;
            }
        }

        private class RequestGenerator
        {
            HttpResponse _response;
            ILogger _logger;

            public RequestGenerator(HttpResponse response, ILogger logger)
            {
                _response = response;
                _logger = logger;
            }

            public async Task HandleRequest<T>(T message,
                string messageId,
                X509Certificate2 certificate,
                string signOnUrl,
                RequestMethod method)
                where T : class
            {
                var messageGuid = messageId.Replace("_", string.Empty);

                if (method == RequestMethod.Post)
                {
                    var signedSerializedMessage = SamlHandler.SignRequest(message, certificate, messageId);
                    await HandlePostRequest(signedSerializedMessage, signOnUrl, messageGuid);
                }
                else
                {
                    var unsignedSerializedMessage = SamlHandler.SerializeMessage(message);
                    HandleRedirectRequest(unsignedSerializedMessage, certificate, signOnUrl, messageGuid);
                }
            }

            private async Task HandlePostRequest(string signedSerializedMessage, string url, string messageGuid)
            {
                await _response.WriteAsync($"<html><head><title>Login</title></head><body><form id=\"cieform\" action=\"{url}\" method=\"post\">" +
                                          $"<input type=\"hidden\" name=\"SAMLRequest\" value=\"{signedSerializedMessage}\" />" +
                                          $"<input type=\"hidden\" name=\"RelayState\" value=\"{messageGuid}\" />" +
                                          $"<button id=\"btnLogin\" style=\"display: none;\">Login</button>" +
                                          "<script>document.getElementById('btnLogin').click()</script>" +
                                          "</form></body></html>");
            }

            private void HandleRedirectRequest(string unsignedSerializedMessage, X509Certificate2 certificate, string url, string messageGuid)
            {
                string redirectUri = GetRedirectUrl(url, messageGuid, unsignedSerializedMessage, certificate);
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    _logger.MalformedRedirectUri(redirectUri);
                }
                _response.Redirect(redirectUri);
            }

            private string GetRedirectUrl(string signOnSignOutUrl, string samlAuthnRequestId, string unsignedSerializedMessage, X509Certificate2 certificate)
            {
                var samlEndpoint = signOnSignOutUrl;

                var queryStringSeparator = samlEndpoint.Contains("?") ? "&" : "?";

                var dict = new Dictionary<string, StringValues>()
                {
                    { "SAMLRequest", DeflateString(unsignedSerializedMessage) },
                    { "RelayState", samlAuthnRequestId },
                    { "SigAlg", SamlConst.SignatureMethod}
                };

                var queryStringNoSignature = BuildURLParametersString(dict).Substring(1);

                var signatureQuery = queryStringNoSignature.CreateSignature(certificate);

                dict.Add("Signature", signatureQuery);

                return samlEndpoint + queryStringSeparator + BuildURLParametersString(dict).Substring(1);
            }

            private string DeflateString(string value)
            {
                using MemoryStream output = new MemoryStream();
                using (DeflateStream gzip = new DeflateStream(output, CompressionMode.Compress))
                {
                    using StreamWriter writer = new StreamWriter(gzip, Encoding.UTF8);
                    writer.Write(value);
                }

                return Convert.ToBase64String(output.ToArray());
            }

            private string BuildURLParametersString(Dictionary<string, StringValues> parameters)
            {
                UriBuilder uriBuilder = new UriBuilder();
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                foreach (var urlParameter in parameters)
                {
                    query[urlParameter.Key] = urlParameter.Value;
                }
                uriBuilder.Query = query.ToString();
                return uriBuilder.Query;
            }

        }
    }


    internal static class AuthenticationPropertiesExtensions
    {
        public static void SetAuthenticationRequest(this AuthenticationProperties properties, AuthnRequestType request) =>
            properties.Items["AuthenticationRequest"] = SamlHandler.SerializeMessage(request);
        public static AuthnRequestType GetAuthenticationRequest(this AuthenticationProperties properties) =>
            SamlHandler.DeserializeMessage<AuthnRequestType>(properties.Items["AuthenticationRequest"]);

        public static void SetLogoutRequest(this AuthenticationProperties properties, LogoutRequestType request) =>
            properties.Items["LogoutRequest"] = SamlHandler.SerializeMessage(request);
        public static LogoutRequestType GetLogoutRequest(this AuthenticationProperties properties) =>
            SamlHandler.DeserializeMessage<LogoutRequestType>(properties.Items["LogoutRequest"]);

        public static void SetSubjectNameId(this AuthenticationProperties properties, string subjectNameId) => properties.Items["subjectNameId"] = subjectNameId;
        public static string GetSubjectNameId(this AuthenticationProperties properties) => properties.Items["subjectNameId"];

        public static void SetSessionIndex(this AuthenticationProperties properties, string sessionIndex) => properties.Items["SessionIndex"] = sessionIndex;
        public static string GetSessionIndex(this AuthenticationProperties properties) => properties.Items["SessionIndex"];

        public static void SetCorrelationProperty(this AuthenticationProperties properties, string correlationProperty) => properties.Items[".xsrf"] = correlationProperty;
        public static string GetCorrelationProperty(this AuthenticationProperties properties) => properties.Items[".xsrf"];

        public static void Save(this AuthenticationProperties properties, HttpResponse response, ISecureDataFormat<AuthenticationProperties> encryptor)
        {
            response.Cookies.Append(CieDefaults.CookieName, encryptor.Protect(properties), new CookieOptions()
            {
                SameSite = SameSiteMode.None,
                Secure = true
            });
        }

        public static void Load(this AuthenticationProperties properties, HttpRequest request, ISecureDataFormat<AuthenticationProperties> encryptor)
        {
            var cookie = request.Cookies[CieDefaults.CookieName];
            BusinessValidation.ValidationNotNull(cookie, ErrorLocalization.CiePropertiesNotFound);
            AuthenticationProperties cookieProperties = encryptor.Unprotect(cookie);
            BusinessValidation.ValidationNotNull(cookieProperties, ErrorLocalization.CiePropertiesNotFound);
            properties.AllowRefresh = cookieProperties.AllowRefresh;
            properties.ExpiresUtc = cookieProperties.ExpiresUtc;
            properties.IsPersistent = cookieProperties.IsPersistent;
            properties.IssuedUtc = cookieProperties.IssuedUtc;
            foreach (var item in cookieProperties.Items)
            {
                properties.Items.Add(item);
            }
            foreach (var item in cookieProperties.Parameters)
            {
                properties.Parameters.Add(item);
            }
            properties.RedirectUri = cookieProperties.RedirectUri;
        }
    }
}
