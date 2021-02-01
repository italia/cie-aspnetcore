using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using CIE.AspNetCore.Authentication.Models;
using CIE.AspNetCore.Authentication.Saml;

namespace CIE.AspNetCore.Authentication.Events
{
    public class MessageReceivedContext : RemoteAuthenticationContext<CieOptions>
    {
        /// <summary>
        /// Creates a new context object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        /// <param name="properties"></param>
        public MessageReceivedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CieOptions options,
            AuthenticationProperties properties,
            ResponseType protocolMessage)
            : base(context, scheme, options, properties)
        {
            ProtocolMessage = protocolMessage;
        }

        /// <summary>
        /// The <see cref="Response"/> received on this request.
        /// </summary>
        public ResponseType ProtocolMessage { get; set; }
    }
}
