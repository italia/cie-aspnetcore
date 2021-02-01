using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using CIE.AspNetCore.Authentication.Helpers;
using CIE.AspNetCore.Authentication.Models;
using System;

namespace CIE.AspNetCore.Authentication
{
    public static class CieExtensions
    {
        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the default authentication scheme, display name, and options.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, IConfiguration configuration)
            => builder.AddCie(CieDefaults.AuthenticationScheme, configuration, _ => { });

        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the default authentication scheme, display name, and the given options configuration.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions">A delegate that configures the <see cref="CieOptions"/>.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, IConfiguration configuration, Action<CieOptions> configureOptions)
            => builder.AddCie(CieDefaults.AuthenticationScheme, configuration, configureOptions);

        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the given authentication scheme, default display name, and the given options configuration.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="authenticationScheme"></param>
        /// <param name="configureOptions">A delegate that configures the <see cref="CieOptions"/>.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, string authenticationScheme, IConfiguration configuration, Action<CieOptions> configureOptions)
            => builder.AddCie(authenticationScheme, CieDefaults.DisplayName, configuration, configureOptions);

        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the given authentication scheme, display name, and options configuration.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="authenticationScheme"></param>
        /// <param name="displayName"></param>
        /// <param name="configureOptions">A delegate that configures the <see cref="CieOptions"/>.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, string authenticationScheme, string displayName, IConfiguration configuration, Action<CieOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CieOptions>, CiePostConfigureOptions>());
            builder.Services.TryAdd(ServiceDescriptor.Singleton<IActionContextAccessor, ActionContextAccessor>());
            builder.Services.AddHttpClient("cie");
            builder.Services.TryAddScoped(factory =>
            {
                var actionContext = factory.GetService<IActionContextAccessor>().ActionContext;
                var urlHelperFactory = factory.GetService<IUrlHelperFactory>();
                return urlHelperFactory.GetUrlHelper(actionContext);
            });
            builder.Services.AddOptions<CieConfiguration>().Configure(o => OptionsHelper.LoadFromConfiguration(o, configuration));
            return builder.AddRemoteScheme<CieOptions, CieHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
