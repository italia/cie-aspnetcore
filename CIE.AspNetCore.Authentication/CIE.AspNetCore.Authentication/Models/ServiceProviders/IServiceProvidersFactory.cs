using System.Collections.Generic;
using System.Threading.Tasks;

namespace CIE.AspNetCore.Authentication.Models.ServiceProviders
{
    public interface IServiceProvidersFactory
    {
        Task<List<ServiceProvider>> GetServiceProviders();
    }
}
