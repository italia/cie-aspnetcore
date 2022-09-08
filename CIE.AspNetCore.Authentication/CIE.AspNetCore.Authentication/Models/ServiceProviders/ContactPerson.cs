using CIE.AspNetCore.Authentication.Helpers;
using CIE.AspNetCore.Authentication.Saml.SP;
using System.Collections.Generic;
using System.Xml;

namespace CIE.AspNetCore.Authentication.Models.ServiceProviders
{
    public enum ContactKind
    {
        Private,
        Public
    }

    public interface IContactPerson
    {
        ContactKind GetContactKind();
        ContactTypeType ContactType { get; set; }
        (bool, string) Validate();

        Saml.SP.ContactType GetContactForXml(ServiceProvider sp);
    }

    public abstract class BaseContactPerson : IContactPerson
    {
        private string _province;

        public string Municipality { get; set; }
        public string Province { get { return Country != "IT" ? "EE" : _province; } set { _province = value; } }
        public string Country { get; set; } = "IT";
        public string Company { get; set; }
        public string[] EmailAddress { get; set; }
        public string[] TelephoneNumber { get; set; }
        public ContactTypeType ContactType { get; set; }

        private string[] _nace2codes;

        public string VATNumber { get; set; }
        public string FiscalCode { get; set; }
        public string[] NACE2Codes { get { return _nace2codes; } set { _nace2codes = value; } }


        public bool IsItalian()
        {
            return Country == "IT";
        }

        public Saml.SP.ContactType GetContactForXml(ServiceProvider sp)
        {
            var elements = GetSpecificElements();
            elements.Add(XmlHelpers.GetXmlElement(Saml.SamlConst.cie, Saml.SamlConst.cieNamespace, GetContactKind().ToString()));
            if (!string.IsNullOrWhiteSpace(VATNumber))
                elements.Add(XmlHelpers.GetXmlElement(Saml.SamlConst.cie, Saml.SamlConst.cieNamespace, ItemsChoiceType7.VATNumber.ToString(), VATNumber));
            if (!string.IsNullOrWhiteSpace(FiscalCode))
                elements.Add(XmlHelpers.GetXmlElement(Saml.SamlConst.cie, Saml.SamlConst.cieNamespace, ItemsChoiceType7.FiscalCode.ToString(), FiscalCode));
            if (NACE2Codes.Length > 0)
                foreach (var code in NACE2Codes)
                    elements.Add(XmlHelpers.GetXmlElement(Saml.SamlConst.cie, Saml.SamlConst.cieNamespace, ItemsChoiceType7.NACE2Code.ToString(), code));


            var extensions = new Saml.SP.ContactPersonSPExtensionType()
            {
                Municipality = this.Municipality,
                Country = this.Country,
                Any = elements.ToArray()
            };

            if (!string.IsNullOrEmpty(this.Province))
                extensions.Province = Province;

            return new Saml.SP.ContactType()
            {
                contactType = this.ContactType,
                Extensions = extensions,
                Company = this.ContactType == ContactTypeType.administrative ? sp.OrganizationName : this.Company,
                EmailAddress = this.EmailAddress,
                TelephoneNumber = this.TelephoneNumber
            };
        }

        public (bool, string) Validate()
        {
            if (string.IsNullOrWhiteSpace(Municipality))
                return (false, $"No {nameof(Municipality)} are specified");

            return SpecificValidate();
        }

        public abstract List<XmlElement> GetSpecificElements();

        public abstract (bool, string) SpecificValidate();

        public abstract ContactKind GetContactKind();
    }

    public class PrivateContactPerson : BaseContactPerson
    {
        public override ContactKind GetContactKind()
        {
            return ContactKind.Private;
        }

        public override (bool, string) SpecificValidate()
        {
            if (string.IsNullOrWhiteSpace(VATNumber)
               && string.IsNullOrWhiteSpace(FiscalCode))
                return (false, $"No {nameof(VATNumber)} or {nameof(FiscalCode)} were specified");

            if (IsItalian() && (NACE2Codes.Length == 0 || NACE2Codes.Length == 1 && string.IsNullOrEmpty(NACE2Codes[0])))
                return (false, $"No {nameof(NACE2Codes)} are specified, required for Italian company.");

            return (true, "");
        }

        public override List<XmlElement> GetSpecificElements()
        {
            var elements = new List<XmlElement>();

            return elements;
        }
    }

    public class PublicContactPerson : BaseContactPerson
    {

        public string IPACode { get; set; }
        public string IPACategory { get; set; }

        public override ContactKind GetContactKind()
        {
            return ContactKind.Public;
        }

        public override (bool, string) SpecificValidate()
        {
            if (string.IsNullOrWhiteSpace(IPACode))
                return (false, $"No {nameof(IPACode)} are specified");

            return (true, "");
        }

        public override List<XmlElement> GetSpecificElements()
        {
            var elements = new List<XmlElement>();

            elements.Add(XmlHelpers.GetXmlElement(Saml.SamlConst.cie, Saml.SamlConst.cieNamespace, ItemsChoiceType7.IPACode.ToString(), IPACode));
            if (!string.IsNullOrWhiteSpace(IPACategory))
                elements.Add(XmlHelpers.GetXmlElement(Saml.SamlConst.cie, Saml.SamlConst.cieNamespace, ItemsChoiceType7.IPACategory.ToString(), IPACategory));

            return elements;
        }
    }
}