namespace CIE.AspNetCore.Authentication.Models
{
    /// <summary>
    /// Default values related to Cie authentication handler
    /// </summary>
    public sealed class CieDefaults
    {
        /// <summary>
        /// The default authentication type used when registering the CieHandler.
        /// </summary>
        public const string AuthenticationScheme = "Cie";

        /// <summary>
        /// The default display name used when registering the CieHandler.
        /// </summary>
        public const string DisplayName = "Cie";

        /// <summary>
        /// Constant used to identify userstate inside AuthenticationProperties that have been serialized in the 'wctx' parameter.
        /// </summary>
        public static readonly string UserstatePropertiesKey = "Cie.Userstate";

        /// <summary>
        /// The cookie name
        /// </summary>
        public static readonly string CookieName = "Cie.Properties";
    }
}
