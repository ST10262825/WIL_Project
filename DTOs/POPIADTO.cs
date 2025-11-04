namespace TutorConnectAPI.DTOs
{
    public class UpdateConsentDTO
    {
        public bool MarketingConsent { get; set; }
        public bool HasAcceptedPOPIA { get; set; }
    }

    public class DeletionRequestDTO
    {
        public string Reason { get; set; }
        public string? AdditionalInfo { get; set; }
    }

    public class ConsentStatusDTO
    {
        public bool HasAcceptedPOPIA { get; set; }
        public DateTime? POPIAAcceptedDate { get; set; }
        public string POPIAVersion { get; set; }
        public bool MarketingConsent { get; set; }
        public DateTime? LastConsentUpdate { get; set; }
        public string CurrentPOPIAVersion { get; set; }
        public bool NeedsReconsent { get; set; }
    }
}
