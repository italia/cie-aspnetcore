using CIE.AspNetCore.Authentication.Saml.SP;
using System.Collections.Generic;

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
        private string[] _nace2codes;

        public string Municipality { get; set; }
        public string Province { get { return Country != "IT" ? "EE" : _province; } set { _province = value; } }
        public string Country { get; set; } = "IT";
        public string Company { get; set; }
        public string[] EmailAddress { get; set; }
        public string[] TelephoneNumber { get; set; }
        public ContactTypeType ContactType { get; set; }
        public string VATNumber { get; set; }
        public string FiscalCode { get; set; }
        public string[] NACE2Codes { get { return _nace2codes; } set { _nace2codes = value; } }


        public bool IsItalian()
        {
            return Country == "IT";
        }

        public Saml.SP.ContactType GetContactForXml(ServiceProvider sp)
        {
            //the code order is strange because spid-sp-test require to respect items order 
            var elements = new List<ItemsChoiceType7>();
            var values = new List<string>();
            elements.Add(GetContactKind() == ContactKind.Private ? ItemsChoiceType7.Private : ItemsChoiceType7.Public);
            values.Add(""); //Private and Public have no value
            var (specElements, specValues) = GetSpecificElements();
            elements.AddRange(specElements);
            values.AddRange(specValues);
            if (!string.IsNullOrWhiteSpace(VATNumber))
            {
                elements.Add(ItemsChoiceType7.VATNumber);
                values.Add(this.VATNumber);
            }
            if (!string.IsNullOrWhiteSpace(FiscalCode))
            {
                elements.Add(ItemsChoiceType7.FiscalCode);
                values.Add(this.FiscalCode);
            }
            if (NACE2Codes is not null && NACE2Codes.Length > 0)
                foreach (var code in NACE2Codes)
                {
                    elements.Add(ItemsChoiceType7.NACE2Code);
                    values.Add(code);
                }

            var extensions = new Saml.SP.ContactPersonSPExtensionType()
            {
                Items = values.ToArray(),
                ItemsElementName = elements.ToArray(),
                Municipality = this.Municipality,
                Country = this.Country
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
            if (EmailAddress.Length == 0 || EmailAddress.Length == 1 && string.IsNullOrEmpty(EmailAddress[0]))
                return (false, $"No {nameof(EmailAddress)} are specified");

            return SpecificValidate();
        }

        public abstract (List<ItemsChoiceType7>, List<string>) GetSpecificElements();

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

        public override (List<ItemsChoiceType7>, List<string>) GetSpecificElements()
        {

            return (new List<ItemsChoiceType7>(), new List<string>());
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

        public override (List<ItemsChoiceType7>, List<string>) GetSpecificElements()
        {
            var elements = new List<ItemsChoiceType7>();
            var values = new List<string>();

            elements.Add(ItemsChoiceType7.IPACode);
            values.Add(this.IPACode);
            if (!string.IsNullOrWhiteSpace(IPACategory))
            {
                elements.Add(ItemsChoiceType7.IPACategory);
                values.Add(this.IPACategory);
            }

            return (elements, values);
        }
    }
}