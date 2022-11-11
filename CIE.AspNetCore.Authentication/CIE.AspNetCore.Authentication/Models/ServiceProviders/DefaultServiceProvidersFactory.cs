﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace CIE.AspNetCore.Authentication.Models.ServiceProviders
{
    internal class DefaultServiceProvidersFactory : IServiceProvidersFactory
    {
        public async Task<List<ServiceProvider>> GetServiceProviders() 
            => await Task.FromResult(new List<ServiceProvider>());
    }
}
