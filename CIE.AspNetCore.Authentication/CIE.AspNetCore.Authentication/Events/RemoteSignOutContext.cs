using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using CIE.AspNetCore.Authentication.Models;
using CIE.AspNetCore.Authentication.Saml;

namespace CIE.AspNetCore.Authentication.Events
{
    public class RemoteSignOutContext : RemoteAuthenticationContext<CieOptions>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        /// <param name="message"></param>
        public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, CieOptions options, LogoutResponseType message)
            : base(context, scheme, options, new AuthenticationProperties())
            => ProtocolMessage = message;

        /// <summary>
        /// The signout message.
        /// </summary>
        public LogoutResponseType ProtocolMessage { get; set; }
    }
}
