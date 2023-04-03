namespace CIE.AspNetCore.Authentication.Models
{
    public class IdentityProvider
    {
        public string Name { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationDisplayName { get; set; }
        public string OrganizationUrlMetadata { get; set; }
        public string OrganizationUrl { get; set; }
        public string OrganizationLogoUrl { get; set; }
        public string SingleSignOnServiceUrlPost { get; set; }
        public string SingleSignOutServiceUrlPost { get; set; }
        public string SingleSignOnServiceUrlRedirect { get; set; }
        public string SingleSignOutServiceUrlRedirect { get; set; }
        public string DateTimeFormat { get; internal set; }
        public double? NowDelta { get; internal set; }
        public string SubjectNameIdRemoveText { get; set; } = "CIE-";
        public ProviderType ProviderType { get; set; } = ProviderType.IdentityProvider;

        public string GetSingleSignOnServiceUrl(RequestMethod requestMethod)
            => requestMethod == RequestMethod.Post
                ? SingleSignOnServiceUrlPost
                : SingleSignOnServiceUrlRedirect;

        public string GetSingleSignOutServiceUrl(RequestMethod requestMethod)
            => requestMethod == RequestMethod.Post
                ? SingleSignOutServiceUrlPost
                : SingleSignOutServiceUrlRedirect;
    }
}
