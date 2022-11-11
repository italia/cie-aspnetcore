using CIE.AspNetCore.Authentication.Events;
using CIE.AspNetCore.Authentication.Extensions;
using CIE.AspNetCore.Authentication.Models;
using CIE.AspNetCore.Authentication.Models.ServiceProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CIE.AspNetCore.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services
                .AddAuthentication(o => {
                    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = CieDefaults.AuthenticationScheme;
                })
                .AddCie(o => {
                    o.LoadFromConfiguration(Configuration);
                    o.ServiceProviders.AddRange(GetPrivateServiceProviders(o));
                    o.Events.OnTokenCreating = async (s) => await s.HttpContext.RequestServices.GetRequiredService<CustomCieEvents>().TokenCreating(s);
                })
                .AddServiceProvidersFactory<ServiceProvidersFactory>()
                .AddCookie();
            services.AddScoped<CustomCieEvents>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.AddCieSPMetadataEndpoints();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                                    name: "default",
                                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public class CustomCieEvents : CieEvents
        {
            public CustomCieEvents(IServiceProvider serviceProvider)
            {

            }

            public override Task TokenCreating(SecurityTokenCreatingContext context)
            {
                return base.TokenCreating(context);
            }
        }

        private List<Authentication.Models.ServiceProviders.ServiceProvider> GetPrivateServiceProviders(CieOptions o)
        {
            return new List<Authentication.Models.ServiceProviders.ServiceProvider>(){
                new ServiceProviderStandard()
                {
                    FileName = "metadata1.xml",
                    Certificate = o.Certificate,
                    Id = Guid.NewGuid(),
                    EntityId = o.EntityId,
                    SingleLogoutServiceLocations = new List<SingleLogoutService>() {
                        new SingleLogoutService() {
                                Location = "https://localhost:5001/signout-cie",
                                ProtocolBinding = ProtocolBinding.POST
                        }
                    },
                    AssertionConsumerServices = new System.Collections.Generic.List<AssertionConsumerService>() {
                        new AssertionConsumerService(){
                            Index = 0,
                            IsDefault = true,
                            Location = "https://localhost:5001/signin-cie",
                            ProtocolBinding = ProtocolBinding.POST
                        },
                        new AssertionConsumerService() {
                            Index = 1,
                            IsDefault = false,
                            Location = "https://localhost:5001/signin-cie",
                            ProtocolBinding = ProtocolBinding.Redirect
                        }
                    },
                    AttributeConsumingServices = new System.Collections.Generic.List<AttributeConsumingService>() {
                        new AttributeConsumingService() {
                            Index = 0,
                            ServiceDescription = "Service 1 Description",
                            ClaimTypes = new CieClaimTypes[] {
                                CieClaimTypes.Name,
                                CieClaimTypes.FamilyName,
                                CieClaimTypes.FiscalNumber,
                                CieClaimTypes.DateOfBirth
                            }
                        },
                        new AttributeConsumingService() {
                            Index = 1,
                            ServiceDescription = "Service 2 Description",
                            ClaimTypes = new CieClaimTypes[] {
                                CieClaimTypes.Name,
                                CieClaimTypes.FamilyName,
                                CieClaimTypes.FiscalNumber,
                                CieClaimTypes.DateOfBirth
                            }
                        }
                    },
                    OrganizationName = "Organizzazione fittizia per il collaudo",
                    OrganizationDisplayName = "Oganizzazione fittizia per il collaudo",
                    OrganizationURL = "https://www.asfweb.it/",
                    ContactPersons = new System.Collections.Generic.List<IContactPerson>() {
                        new PublicContactPerson() {
                            ContactType = Authentication.Saml.SP.ContactTypeType.administrative,
                            Company = "Organizzazione fittizia per il collaudo",
                            EmailAddress = new string[] { "info.cie@partnertecnologicoidfederata.com" },
                            TelephoneNumber = new string[] { "+390999135792" },
                            VATNumber = "IT01234567890",
                            FiscalCode = "9876543210",
                            NACE2Codes = new string[] { "CODICE_ATECO" },
                            Municipality = "CODICE_ISTAT_SEDE"
                        }
                    }
                },
                new ServiceProviderStandard()
                {
                    FileName = "metadata3.xml",
                    Certificate = o.Certificate,
                    Id = Guid.NewGuid(),
                    EntityId = o.EntityId,
                    SingleLogoutServiceLocations = new List<SingleLogoutService>() {
                        new SingleLogoutService() {
                                Location = "https://localhost:5001/signout-cie",
                                ProtocolBinding = ProtocolBinding.POST
                        }
                    },
                    AssertionConsumerServices = new System.Collections.Generic.List<AssertionConsumerService>() {
                        new AssertionConsumerService(){
                            Index = 0,
                            IsDefault = true,
                            Location = "https://localhost:5001/signin-cie",
                            ProtocolBinding = ProtocolBinding.POST
                        },
                        new AssertionConsumerService() {
                            Index = 1,
                            IsDefault = false,
                            Location = "https://localhost:5001/signin-cie",
                            ProtocolBinding = ProtocolBinding.Redirect
                        }
                    },
                    AttributeConsumingServices = new System.Collections.Generic.List<AttributeConsumingService>() {
                        new AttributeConsumingService() {
                            Index = 0,
                            ServiceDescription = "Service 1 Description",
                            ClaimTypes = new CieClaimTypes[] {
                                CieClaimTypes.Name,
                                CieClaimTypes.FamilyName,
                                CieClaimTypes.FiscalNumber,
                                CieClaimTypes.DateOfBirth
                            }
                        },
                        new AttributeConsumingService() {
                            Index = 1,
                            ServiceDescription = "Service 2 Description",
                            ClaimTypes = new CieClaimTypes[] {
                                CieClaimTypes.Name,
                                CieClaimTypes.FamilyName,
                                CieClaimTypes.FiscalNumber,
                                CieClaimTypes.DateOfBirth
                            }
                        }
                    },
                    OrganizationName = "Organizzazione fittizia per il collaudo",
                    OrganizationDisplayName = "Oganizzazione fittizia per il collaudo",
                    OrganizationURL = "https://www.asfweb.it/",
                    ContactPersons = new System.Collections.Generic.List<IContactPerson>() {
                        new PrivateContactPerson() {
                            ContactType = Authentication.Saml.SP.ContactTypeType.administrative,
                            EmailAddress = new string[] { "esempio_sp_privato@spp.it" },
                            TelephoneNumber = new string[] { "+39061234567" },
                            VATNumber = "IT01234567890",
                            FiscalCode = "9876543210",
                            NACE2Codes = new string[] { "CODICE_ATECO" },
                            Municipality = "CODICE_ISTAT_SEDE"
                        },
                        new PrivateContactPerson() {
                            ContactType = Authentication.Saml.SP.ContactTypeType.technical,
                            Company = "Partner Tecnologico per Soluzioni di Identità Federata s.r.l.",
                            EmailAddress = new string[] { "info.cie@partnertecnologicoidfederata.com" },
                            TelephoneNumber = new string[] { "+390999135792" },
                            VATNumber = "IT01234567890",
                            FiscalCode = "9876543210",
                            NACE2Codes = new string[] { "CODICE_ATECO" },
                            Municipality = "CODICE_ISTAT_SEDE"
                        }
                    }
                }
            };
        }
    }
}
