﻿using HCore.Tenants.Database.SqlServer.Models.Impl;

namespace HCore.Templating.Templates.ViewModels.Shared
{
    public class TemplateViewModel
    {
        public long? DeveloperUuid { get; set; }

        public string TenantName { get; set; }
        public string TenantLogoSvgUrl { get; set; }
        public string TenantLogoPngUrl { get; set; }
        public string TenantIconIcoUrl { get; set; }
        public string TenantAppleTouchIconUrl { get; set; }
        public string TenantPrimaryColor { get; set; }
        public string TenantSecondaryColor { get; set; }
        public string TenantTextOnPrimaryColor { get; set; }
        public string TenantTextOnSecondaryColor { get; set; }
        public string TenantSupportEmail { get; set; }
        public string TenantWebAddress { get; set; }
        public string TenantPoweredByShort { get; set; }
        public string TenantProductName { get; set; }
        public string TenantDefaultCulture { get; set; }
        public string TenantDefaultCurrency { get; set; }

        public bool TenantHidePoweredBy { get; set; }

        public string TenantCustomEmailCss { get; set; }

        public EmailSettingsModel EmailSettings { get; set; }
    }
}
