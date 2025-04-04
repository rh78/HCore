﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace HCore.Web.Exceptions
{
    public class RequestFailedApiException : ApiException
    {
        private readonly string _errorCode;

        public const string ArgumentInvalid = "argument_invalid";
        public const string ArgumentMissing = "argument_missing";
        public const string LinkInvalid = "link_invalid";
        public const string MaxPagingOffsetExceeded = "max_paging_offset_exceeded";
        public const string MaxPagingLimitExceeded = "max_paging_limit_exceeded";
        public const string PagingOffsetInvalid = "paging_offset_invalid";
        public const string PagingLimitInvalid = "paging_limit_invalid";
        public const string ApiCredentialsMissing = "api_credentials_missing";
        public const string ClientIdMissing = "client_id_missing";
        public const string ClientIdInvalid = "client_id_invalid";
        public const string ClientIdTooLong = "client_id_too_long";
        public const string ClientSecretMissing = "client_secret_missing";
        public const string ClientSecretInvalid = "client_secret_invalid";
        public const string ClientSecretTooLong = "client_secret_too_long";
        public const string Key1Missing = "key_1_missing";
        public const string Key1Invalid = "key_1_invalid";
        public const string Key1TooLong = "key_1_too_long";
        public const string Key2Missing = "key_2_missing";
        public const string Key2Invalid = "key_2_invalid";
        public const string Key2TooLong = "key_2_too_long";
        public const string Key3Missing = "key_3_missing";
        public const string Key3Invalid = "key_3_invalid";
        public const string Key3TooLong = "key_3_too_long";
        public const string Key4Missing = "key_4_missing";
        public const string Key4Invalid = "key_4_invalid";
        public const string Key4TooLong = "key_4_too_long";
        public const string RedirectUrlNotRequired = "redirect_url_not_required";
        public const string RedirectUrlMissing = "redirect_url_missing";
        public const string RedirectUrlInvalid = "redirect_url_invalid";
        public const string RedirectUrlTooLong = "redirect_url_too_long";
        public const string RedirectUrlMustBeAbsolute = "redirect_url_must_be_absolute";
        public const string UuidMissing = "uuid_missing";
        public const string UuidInvalid = "uuid_invalid";
        public const string StateInvalid = "state_invalid";
        public const string CodeMissing = "code_missing";
        public const string CodeInvalid = "code_invalid";
        public const string CodeTooLong = "code_too_long";
        public const string AccessTokenMissing = "access_token_missing";
        public const string AccessTokenTooLong = "access_token_too_long";
        public const string IdentityTokenMissing = "identity_token_missing";
        public const string IdentityTokenTooLong = "identity_token_too_long";
        public const string RefreshTokenMissing = "refresh_token_missing";
        public const string RefreshTokenTooLong = "refresh_token_too_long";
        public const string IssuerMissing = "issuer_missing";
        public const string IssuerInvalid = "issuer_invalid";
        public const string IssuerTooLong = "issuer_too_long";
        public const string SubjectMissing = "subject_missing";
        public const string SubjectInvalid = "subject_invalid";
        public const string SubjectTooLong = "subject_too_long";
        public const string PrivateKeyMissing = "private_key_missing";
        public const string PrivateKeyNoDelimiters = "private_key_no_delimiters";
        public const string PrivateKeyNoPrivateKey = "private_key_no_private_key";
        public const string PrivateKeyInvalid = "private_key_invalid";
        public const string PrivateKeyTooLong = "private_key_too_long";
        public const string UserCredentialsMissing = "user_credentials_missing";
        public const string UserNameMissing = "user_name_missing";
        public const string UserNameInvalid = "user_name_invalid";
        public const string UserNameTooLong = "user_name_too_long";
        public const string UserAlreadyRegistered = "user_already_registered";
        public const string EmailSendingFailed = "email_sending_failed";
        public const string SpfDkimSetupNotFinalized = "spf_dkim_setup_not_finalized";
        public const string PasswordMissing = "password_missing";        
        public const string PasswordTooShort = "password_too_short";
        public const string PasswordTooLong = "password_too_long";
        public const string PasswordConfirmationMissing = "password_confirmation_missing";
        public const string PasswordConfirmationNoMatch = "password_confirmation_no_match";
        public const string NameMissing= "name_missing";
        public const string NameInvalid = "name_invalid";
        public const string NameTooLong = "name_too_long";
        public const string CultureMissing = "culture_missing";
        public const string CulturesMissing = "cultures_missing";
        public const string CultureInvalid = "culture_invalid";
        public const string CultureTooLong = "culture_too_long";
        public const string SubdomainMissing = "subdomain_missing";
        public const string SubdomainInvalid = "subdomain_invalid";
        public const string SubdomainTooLong = "subdomain_too_long";
        public const string LogoSvgUrlMissing = "logo_svg_url_missing";
        public const string LogoSvgUrlTooLong = "logo_svg_url_too_long";
        public const string LogoPngUrlMissing = "logo_png_url_missing";
        public const string LogoPngUrlTooLong = "logo_png_url_too_long";
        public const string IconIcoUrlMissing = "icon_ico_url_missing";
        public const string IconIcoUrlTooLong = "icon_ico_url_too_long";
        public const string AppleTouchIconUrlMissing = "apple_touch_icon_url_missing";
        public const string AppleTouchIconUrlTooLong = "apple_touch_icon_url_too_long";
        public const string TooManyUpdateRecords = "too_many_update_records";
        public const string NoRecordsSpecified = "no_records_specified";
        public const string UserGroupUuidMissing = "user_group_uuid_missing";
        public const string UserGroupUuidInvalid = "user_group_uuid_invalid";
        public const string TenantUuidMissing = "tenant_uuid_missing";
        public const string TenantUuidInvalid = "tenant_uuid_invalid";
        public const string TenantDeletionFailed = "tenant_deletion_failed";
        public const string ConfirmationCodeRequired = "confirmation_code_required";
        public const string UserUuidMissing = "user_uuid_missing";
        public const string UserUuidInvalid = "user_uuid_invalid";
        public const string UserUuidTooLong = "user_uuid_too_long";
        public const string EmailMissing = "email_missing";
        public const string EmailInvalid = "email_invalid";
        public const string EmailNotExisting = "email_not_existing";
        public const string NoDisposableEmailsAllowed = "no_disposable_emails_allowed";
        public const string EmailRequiresBusinessAccount = "email_requires_business_account";
        public const string EmailTooLong = "email_too_long";
        public const string EmailAlreadyExists = "email_already_exists";
        public const string EmailAlreadyConfirmed = "email_already_confirmed";
        public const string EmailsMissing = "emails_missing";
        public const string SupportEmailInvalid = "support_email_invalid";
        public const string SupportEmailTooLong = "support_email_too_long";
        public const string SupportReplyToEmailInvalid = "support_reply_to_email_invalid";
        public const string SupportReplyToEmailTooLong = "support_reply_to_email_too_long";
        public const string SupportEmailDisplayNameInvalid = "support_email_display_name_invalid";
        public const string SupportEmailDisplayNameTooLong = "support_email_display_name_too_long";
        public const string NoreplyEmailInvalid = "noreply_email_invalid";
        public const string NoreplyEmailTooLong = "noreply_email_too_long";
        public const string NoreplyReplyToEmailInvalid = "noreply_reply_to_email_invalid";
        public const string NoreplyReplyToEmailTooLong = "noreply_reply_to_email_too_long";
        public const string NoreplyEmailDisplayNameInvalid = "noreply_email_display_name_invalid";
        public const string NoreplyEmailDisplayNameTooLong = "noreply_email_display_name_too_long";
        public const string OrganizationMissing = "organization_missing";
        public const string OrganizationInvalid = "organization_invalid";
        public const string OrganizationTooLong = "organization_too_long";
        public const string CustomIdentifierMissing = "custom_identifier_missing";
        public const string CustomIdentifierInvalid = "custom_identifier_invalid";
        public const string CustomIdentifierTooLong = "custom_identifier_too_long";
        public const string PhoneNumberMissing = "phone_number_missing";
        public const string PhoneNumberInvalid = "phone_number_invalid";
        public const string PhoneNumberTooLong = "phone_number_too_long";
        public const string ValidationFailed = "validation_failed";
        public const string DuplicateUserName = "duplicate_user_name";
        public const string PasswordRequiresNonAlphanumeric = "password_requires_non_alphanumeric";
        public const string PasswordRequiresDigit = "password_requires_digit";
        public const string PasswordRequiresLower = "password_requires_lower";
        public const string PasswordRequiresUpper = "password_requires_upper";
        public const string SecurityTokenInvalid = "security_token_invalid";
        public const string ScrollUuidInvalid = "scroll_uuid_invalid";
        public const string ScrollUuidTooLong = "scroll_uuid_too_long";
        public const string ContinuationUuidInvalid = "continuation_uuid_invalid";
        public const string FirstNameMissing = "first_name_missing";
        public const string FirstNameInvalid = "first_name_invalid";
        public const string FirstNameTooLong = "first_name_too_long";
        public const string LastNameMissing = "last_name_missing";
        public const string LastNameInvalid = "last_name_invalid";
        public const string LastNameTooLong = "last_name_too_long";
        public const string UserDetailsNotSupported = "user_details_not_supported";
        public const string CannotDeleteLastSystemUserGroupMember = "cannot_delete_last_system_user_group_member";
        public const string SortByInvalid = "sort_by_invalid";
        public const string SortOrderInvalid = "sort_order_invalid";
        public const string FilterValueInvalid = "filter_value_invalid";
        public const string PleaseAcceptPrivacyPolicy = "please_accept_privacy_policy";
        public const string PleaseAcceptTermsAndConditions = "please_accept_terms_and_conditions";
        public const string PleaseConfirmEmailExternal = "please_confirm_email_external";
        public const string NotificationCultureInvalid = "notification_culture_invalid";
        public const string DefaultCultureInvalid = "default_culture_invalid";
        public const string DefaultCultureMissing = "default_culture_missing";
        public const string CurrencyMissing = "currency_missing";
        public const string CurrencyInvalid = "currency_invalid";
        public const string DefaultCurrencyMissing = "default_currency_missing";
        public const string DefaultCurrencyInvalid = "default_currency_invalid";
        public const string CountryMissing = "country_missing";
        public const string CountryInvalid = "country_invalid";
        public const string AddressLine1Missing = "address_line_1_missing";
        public const string AddressLine1TooLong = "address_line_1_too_long";
        public const string AddressLine2TooLong = "address_line_2_too_long";
        public const string PostalCodeMissing = "postal_code_missing";
        public const string PostalCodeTooLong = "postal_code_too_long";
        public const string CityMissing = "city_missing";
        public const string CityTooLong = "city_too_long";
        public const string StateTooLong = "state_too_long";
        public const string VatIdMissing = "vat_id_missing";
        public const string VatIdTooLong = "vat_id_too_long";
        public const string VatIdInvalid = "vat_id_invalid";
        public const string AccountIsExternallyManaged = "account_is_externally_managed";
        public const string AccountsAreExternallyManaged = "accounts_are_externally_managed";
        public const string RecaptchaValidationFailed = "recaptcha_validation_failed";
        public const string DomainMissing = "domain_missing";
        public const string DomainInvalid = "domain_invalid";
        public const string DomainNotFound = "domain_not_found";
        public const string DomainAlreadyUsed = "domain_already_used";
        public const string PrimaryColorInvalid = "primary_color_invalid";
        public const string SecondaryColorInvalid = "secondary_color_invalid";
        public const string TextOnPrimaryColorInvalid = "text_on_primary_color_invalid";
        public const string TextOnSecondaryColorInvalid = "text_on_secondary_color_invalid";
        public const string TenantSubdomainImmutable = "tenant_subdomain_immutable";
        public const string TenantSubdomainAlreadyUsed = "tenant_subdomain_already_used";
        public const string ContactPersonNameMissing = "contact_person_name_missing";
        public const string ContactPersonNameTooLong = "contact_person_name_too_long";
        public const string ScopedTenantUuidMissing = "scoped_tenant_uuid_missing";
        public const string ScopedTenantUuidInvalid = "scoped_tenant_uuid_invalid";
        public const string ExternalUuidTooLong = "external_uuid_too_long";
        public const string DescriptionTooLong = "description_too_long";
        public const string CommentTooLong = "comment_too_long";
        public const string JobIdMissing = "job_id_missing";
        public const string JobIdTooLong = "job_id_too_long";
        public const string OrderIdMissing = "order_id_missing";
        public const string OrderIdTooLong = "order_id_too_long";
        public const string ConfirmationIdMissing = "confirmation_id_missing";
        public const string ConfirmationIdTooLong = "confirmation_id_too_long";
        public const string SubmissionMessageTooLong = "submission_message_too_long";
        public const string RejectionReasonTooLong = "rejection_reason_too_long";
        public const string CancellationReasonMissing = "cancellation_reason_missing";
        public const string CancellationReasonTooLong = "cancellation_reason_too_long";
        public const string ExternalUserGroupIdentifierInvalid = "external_user_group_identifier_invalid";
        public const string ExternalUserGroupIdentifierTooLong = "external_user_group_identifier_too_long";
        public const string SearchTermMissing = "search_term_missing";
        public const string SearchTermInvalid = "search_term_invalid";
        public const string SearchTermTooLong = "search_term_too_long";
        public const string LastUpdatedAtFromInvalid = "last_updated_at_from_invalid";
        public const string LastUpdatedAtToInvalid = "last_updated_at_to_invalid";
        public const string LastUpdatedAtToBeforeLastUpdatedAtFrom = "last_updated_at_to_before_purchased_at_from";
        public const string NotificationUuidMissing = "notification_uuid_missing";
        public const string NotificationUuidInvalid = "notification_uuid_invalid";
        public const string FileUuidMissing = "file_uuid_missing";
        public const string FileUuidsMissing = "file_uuids_missing";
        public const string BillingTypeInvalid = "billing_type_invalid";
        public const string ClaimsPrincipalHasTooManyGroups = "claims_principal_has_too_many_groups";
        public const string CustomCssInvalid = "custom_css_invalid";
        public const string ExternalAuthenticationRequiredForEmailDomain = "external_authentication_required_for_email_domain";

        // internal, not translated

        public const string ProfileMissing = "profile_missing";

        public string UserUuid { get; set; }
        public string RedirectCode { get; set; }

        public RequestFailedApiException(string errorCode, string message) : 
            base(message)
        {
            _errorCode = errorCode;            
        }

        public RequestFailedApiException(string errorCode, string message, string uuid, string name) :
           base(message, uuid, name)
        {
            _errorCode = errorCode;
        }

        public RequestFailedApiException(string errorCode, string message, long? uuid, string name) :
           base(message, uuid, name)
        {
            _errorCode = errorCode;
        }

        public RequestFailedApiException(string errorCode, string message, DateTimeOffset? dateTimeOffset) :
           base(message, dateTimeOffset)
        {
            _errorCode = errorCode;
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status400BadRequest;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }      
    }
}
