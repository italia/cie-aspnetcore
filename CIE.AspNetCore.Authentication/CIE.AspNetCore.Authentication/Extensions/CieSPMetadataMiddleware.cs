﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using CIE.AspNetCore.Authentication.Models;
using CIE.AspNetCore.Authentication.Models.ServiceProviders;
using System.Linq;
using System.Threading.Tasks;

namespace CIE.AspNetCore.Authentication.Extensions
{
    internal class CieSPMetadataMiddleware
    {
        private readonly RequestDelegate _next;

        public CieSPMetadataMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IOptionsSnapshot<CieOptions> options, IServiceProvidersFactory serviceProvidersFactory)
        {
            var serviceProviders = options.Value.ServiceProviders;

            serviceProviders.AddRange(await serviceProvidersFactory.GetServiceProviders());

            var serviceProvider = serviceProviders.FirstOrDefault(m =>
                context.Request.Path.Equals($"{options.Value.ServiceProvidersMetadataEndpointsBasePath}/{m.FileName}", System.StringComparison.OrdinalIgnoreCase));
            if (serviceProvider is not null)
            {
                var (result, contentType) = serviceProvider.Serialize();
                context.Response.ContentType = contentType ?? "application/xml; charset=UTF-8";
                await context.Response.WriteAsync(result);
                await context.Response.Body.FlushAsync();
                return;
            }

            await _next(context);
        }
    }
}
