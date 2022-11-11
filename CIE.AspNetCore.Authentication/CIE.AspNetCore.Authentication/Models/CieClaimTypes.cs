using CIE.AspNetCore.Authentication.Saml;
using System;
using System.Collections.Generic;

namespace CIE.AspNetCore.Authentication.Models
{
    public sealed class CieClaimTypes
    {
        private static Dictionary<string, CieClaimTypes> _types = new Dictionary<string, CieClaimTypes>() {
            { nameof(Name), new CieClaimTypes(nameof(Name)) },
            { nameof(FamilyName), new CieClaimTypes(nameof(FamilyName)) },
            { nameof(FiscalNumber), new CieClaimTypes(nameof(FiscalNumber)) },
            { nameof(DateOfBirth), new CieClaimTypes(nameof(DateOfBirth)) },
            { nameof(RawFiscalNumber), new CieClaimTypes(nameof(RawFiscalNumber)) },
        };

        private CieClaimTypes(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }

        public static CieClaimTypes Name { get { return _types[nameof(Name)]; } }
        public static CieClaimTypes FamilyName { get { return _types[nameof(FamilyName)]; } }
        public static CieClaimTypes FiscalNumber { get { return _types[nameof(FiscalNumber)]; } }
        public static CieClaimTypes DateOfBirth { get { return _types[nameof(DateOfBirth)]; } }
        public static CieClaimTypes RawFiscalNumber { get { return _types[nameof(RawFiscalNumber)]; } }

        internal string GetSamlAttributeName()
        {
            return Value switch
            {
                nameof(Name) => SamlConst.name,
                nameof(FamilyName) => SamlConst.familyName,
                nameof(FiscalNumber) or nameof(RawFiscalNumber) => SamlConst.fiscalNumber,
                nameof(DateOfBirth) => SamlConst.dateOfBirth,
                _ => throw new Exception("Invalid ClaimType"),
            };
        }
    }
}
