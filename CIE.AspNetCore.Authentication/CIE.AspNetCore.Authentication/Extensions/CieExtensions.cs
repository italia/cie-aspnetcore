using CIE.AspNetCore.Authentication.Events;
using CIE.AspNetCore.Authentication.Models;
using CIE.AspNetCore.Authentication.Models.ServiceProviders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;

namespace CIE.AspNetCore.Authentication.Extensions
{
    public static class CieExtensions
    {
        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the default authentication scheme, display name, and options.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, IConfiguration configuration)
            => builder.AddCie(CieDefaults.AuthenticationScheme, o => { o.LoadFromConfiguration(configuration); });

        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the default authentication scheme, display name, and the given options configuration.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions">A delegate that configures the <see cref="CieOptions"/>.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, Action<CieOptions> configureOptions)
            => builder.AddCie(CieDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the given authentication scheme, default display name, and the given options configuration.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="authenticationScheme"></param>
        /// <param name="configureOptions">A delegate that configures the <see cref="CieOptions"/>.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, string authenticationScheme, Action<CieOptions> configureOptions)
            => builder.AddCie(authenticationScheme, CieDefaults.DisplayName, configureOptions);

        /// <summary>
        /// Registers the <see cref="CieHandler"/> using the given authentication scheme, display name, and options configuration.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="authenticationScheme"></param>
        /// <param name="displayName"></param>
        /// <param name="configureOptions">A delegate that configures the <see cref="CieOptions"/>.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddCie(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<CieOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CieOptions>, CiePostConfigureOptions>());
            builder.Services.TryAdd(ServiceDescriptor.Singleton<IActionContextAccessor, ActionContextAccessor>());
            builder.Services.AddHttpClient("cie");
            builder.Services.TryAddScoped(factory => {
                var actionContext = factory.GetService<IActionContextAccessor>().ActionContext;
                var urlHelperFactory = factory.GetService<IUrlHelperFactory>();
                return urlHelperFactory.GetUrlHelper(actionContext);
            });
            builder.Services.AddOptions<CieOptions>().Configure(configureOptions);
            builder.Services.TryAddScoped<IServiceProvidersFactory, DefaultServiceProvidersFactory>();
            builder.Services.TryAddScoped<ILogHandler, DefaultLogHandler>();
            return builder.AddRemoteScheme<CieOptions, CieHandler>(authenticationScheme, displayName, configureOptions);
        }

        /// <summary>
        /// Adds the service providers factory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddServiceProvidersFactory<T>(this AuthenticationBuilder builder)
            where T : class, IServiceProvidersFactory
        {
            builder.Services.AddScoped<IServiceProvidersFactory, T>();
            return builder;
        }

        /// <summary>
        /// Adds the cie sp metadata endpoints.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IApplicationBuilder AddCieSPMetadataEndpoints(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CieSPMetadataMiddleware>();
        }

        /// <summary>
        /// Adds the custom log handler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddLogHandler<T>(this AuthenticationBuilder builder)
            where T : class, ILogHandler
        {
            builder.Services.AddScoped<ILogHandler, T>();
            return builder;
        }

        /// <summary>
        /// Finds the first value.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <param name="claimType">Type of the claim.</param>
        /// <returns></returns>
        public static string FindFirstValue(this ClaimsPrincipal principal, CieClaimTypes claimType)
        {
            return principal.FindFirstValue(claimType.Value);
        }

        /// <summary>
        /// Finds the first.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <param name="claimType">Type of the claim.</param>
        /// <returns></returns>
        public static Claim FindFirst(this ClaimsPrincipal principal, CieClaimTypes claimType)
        {
            return principal.FindFirst(claimType.Value);
        }
    }
}
