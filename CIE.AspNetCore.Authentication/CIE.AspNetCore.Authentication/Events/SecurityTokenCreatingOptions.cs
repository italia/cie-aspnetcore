﻿using CIE.AspNetCore.Authentication.Models;
using System.Security.Cryptography.X509Certificates;

namespace CIE.AspNetCore.Authentication.Events
{
    public sealed class SecurityTokenCreatingOptions
    {
        public string EntityId { get; set; }
        public ushort AssertionConsumerServiceIndex { get; set; }
        public ushort AttributeConsumingServiceIndex { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public int SecurityLevel { get; set; }
        public RequestMethod RequestMethod { get; set; }
    }
}
