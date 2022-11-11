using CIE.AspNetCore.Authentication.Helpers;
using CIE.AspNetCore.Authentication.Saml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace CIE.AspNetCore.Authentication.Models.ServiceProviders
{
    public sealed class ServiceProviderStandard : ServiceProvider
    {
        public string EntityId { get; set; }
 
        public override (string result, string contentType) Serialize()
        {
            Saml.SP.EntityDescriptorType metadata = new Saml.SP.EntityDescriptorType()
            {
                entityID = EntityId,
                ID = $"_{Id}",
                SPSSODescriptor = new Saml.SP.SPSSODescriptorType(){
                        KeyDescriptor = new Saml.SP.KeyDescriptorType[]{
                            new Saml.SP.KeyDescriptorType(){
                                use = Saml.SP.KeyTypes.signing,
                                KeyInfo = new Saml.SP.KeyInfoType
                                {
                                    ItemsElementName = new Saml.SP.ItemsChoiceType2[]{ Saml.SP.ItemsChoiceType2.X509Data },
                                    Items = new Saml.SP.X509DataType[]{
                                        new Saml.SP.X509DataType{
                                            ItemsElementName = new Saml.SP.ItemsChoiceType[]{ Saml.SP.ItemsChoiceType.X509Certificate },
                                            Items = new object[]{ Certificate.ExportPublicKey() }
                                        }
                                    }
                                }
                            },
                            new Saml.SP.KeyDescriptorType(){
                                use = Saml.SP.KeyTypes.encryption,
                                KeyInfo = new Saml.SP.KeyInfoType
                                {
                                    ItemsElementName = new Saml.SP.ItemsChoiceType2[]{ Saml.SP.ItemsChoiceType2.X509Data },
                                    Items = new Saml.SP.X509DataType[]{
                                        new Saml.SP.X509DataType{
                                            ItemsElementName = new Saml.SP.ItemsChoiceType[]{ Saml.SP.ItemsChoiceType.X509Certificate },
                                            Items = new object[]{ Certificate.ExportPublicKey() }
                                        }
                                    }
                                }
                            }
                        },
                        AuthnRequestsSigned = true,
                        WantAssertionsSigned = true,
                        protocolSupportEnumeration = new string[]{ SamlConst.Saml2pProtocol },
                        SingleLogoutService = SingleLogoutServiceLocations.Select(s => new Saml.SP.SingleLogoutServiceType(){
                                Binding = s.ProtocolBinding == ProtocolBinding.POST ? Saml.SP.SingleLogoutServiceBindingType.urnoasisnamestcSAML20bindingsHTTPPOST : Saml.SP.SingleLogoutServiceBindingType.urnoasisnamestcSAML20bindingsHTTPRedirect,
                                Location = s.Location
                            }).ToArray(),
                        NameIDFormat = SamlConst.NameIDPolicyFormat ,
                        AssertionConsumerService = AssertionConsumerServices.Select(s => new Saml.SP.AssertionConsumerServiceType(){
                            Binding = s.ProtocolBinding == ProtocolBinding.POST ? SamlConst.ProtocolBindingPOST : SamlConst.ProtocolBindingRedirect,
                            Location = s.Location,
                            index = s.Index,
                            isDefault = s.IsDefault,
                            isDefaultSpecified = true
                        }).ToArray(),
                        AttributeConsumingService = AttributeConsumingServices.Select(s => new Saml.SP.AttributeConsumingServiceType(){
                            index = s.Index,
                            ServiceName = new Saml.SP.UUID[]{ new Saml.SP.UUID(){lang = "", Value = Guid.NewGuid().ToString() } },//TODO: capire se posso rigenerarlo ogni volta o se serve salvarlo in qualche modo
                            ServiceDescription = new Saml.SP.localizedNameType[]{ new Saml.SP.localizedNameType(){lang = Language, Value = s.ServiceDescription } },
                            RequestedAttribute = s.ClaimTypes.Select(c => new Saml.SP.RequestedAttributeType(){
                                NameFormat = SamlConst.RequestedAttributeNameFormat,
                                Name = c.GetSamlAttributeName()
                            }).ToArray()
                        }).ToArray(),
                    },
                Organization = new Saml.SP.OrganizationType()
                {
                    OrganizationDisplayName = new Saml.SP.localizedNameType[] { new Saml.SP.localizedNameType { lang = Language, Value = OrganizationDisplayName } },
                    OrganizationName = new Saml.SP.localizedNameType[] { new Saml.SP.localizedNameType { lang = Language, Value = OrganizationName } },
                    OrganizationURL = new Saml.SP.localizedURIType[] { new Saml.SP.localizedURIType { lang = Language, Value = OrganizationURL } },
                },
                ContactPerson = ContactPersons.Select(s => CheckContactAndGetIt(s)).ToArray()
            };

            var result = SamlHandler.SignSerializedMetadata(SamlHandler.SerializeMetadata(metadata), Certificate, metadata.ID);

            return (result, "application/xml; charset=UTF-8");
        }

        private Saml.SP.ContactType CheckContactAndGetIt(IContactPerson c){
            (var res, var errmsg)= c.Validate();

            if(!res)
                throw new Exception(errmsg);
            
            return c.GetContactForXml(this);
        }
    }
}
