using System;
using System.Collections.Generic;
using System.Text;

namespace CIE.AspNetCore.Authentication.Models
{
    public sealed class CieClaimTypes
    {
        public static string Name = nameof(Name);
        public static string FamilyName = nameof(FamilyName);
        public static string FiscalNumber = nameof(FiscalNumber);
        public static string DateOfBirth = nameof(DateOfBirth);
    }
}
