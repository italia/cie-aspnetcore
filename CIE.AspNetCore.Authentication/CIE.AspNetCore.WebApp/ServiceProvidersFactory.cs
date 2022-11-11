using Microsoft.Extensions.Options;
using CIE.AspNetCore.Authentication.Models;
using SPIDSS = CIE.AspNetCore.Authentication.Models.ServiceProviders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIE.AspNetCore.Authentication.Models.ServiceProviders;

namespace CIE.AspNetCore.WebApp
{
  public class ServiceProvidersFactory : IServiceProvidersFactory
    {
        private readonly CieOptions _options;

        public ServiceProvidersFactory(IOptionsMonitor<CieOptions> options)
        {
            _options = options.CurrentValue;
        }

        public Task<List<SPIDSS.ServiceProvider>> GetServiceProviders()
            => Task.FromResult(new List<SPIDSS.ServiceProvider>() {
                new ServiceProviderStandard()
                {
                    FileName = "metadata.xml",
                    Certificate = _options.Certificate,
                    Id = Guid.NewGuid(),
                    EntityId = _options.EntityId,
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
                            EmailAddress = new string[] { "esempio_sp_privato@spp.it" },
                            TelephoneNumber = new string[] { "+39061234567" },
                            IPACode = "codiceIPA_SP",
                            IPACategory = "categoriaIPA_SP",
                            NACE2Codes = new string[] { "CODICE_ATECO" },
                            Municipality = "CODICE_ISTAT_SEDE"
                        }
                    }
                },
                new ServiceProviderStandard()
                {
                    FileName = "metadata2.xml",
                    Certificate = _options.Certificate,
                    Id = Guid.NewGuid(),
                    EntityId = _options.EntityId,
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
                            EmailAddress = new string[] { "esempio_sp_privato@spp.it" },
                            TelephoneNumber = new string[] { "+39061234567" },
                            IPACode = "codiceIPA_SP",
                            IPACategory = "categoriaIPA_SP",
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
                });
    }
}
