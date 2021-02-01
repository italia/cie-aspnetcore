using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using CIE.AspNetCore.Authentication.Models;

namespace CIE.AspNetCore.Authentication.Events
{
    public class AuthenticationSuccessContext : RemoteAuthenticationContext<CieOptions>
    {
        /// <summary>
        /// Creates a new context object
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        public AuthenticationSuccessContext(HttpContext context, AuthenticationScheme scheme, CieOptions options, string authenticationRequestId, AuthenticationTicket authenticationTicket)
            : base(context, scheme, options, new AuthenticationProperties())
        {
            AuthenticationRequestId = authenticationRequestId;
            AuthenticationTicket = authenticationTicket;
        }

        /// <summary>
        /// Gets or sets the authentication ticket.
        /// </summary>
        /// <value>
        /// The authentication ticket.
        /// </value>
        public AuthenticationTicket AuthenticationTicket { get; internal set; }
        /// <summary>
        /// Gets or sets the saml authn request identifier.
        /// </summary>
        /// <value>
        /// The saml authn request identifier.
        /// </value>
        public string AuthenticationRequestId { get; internal set; }
    }
}
