using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class UpdateConsentDTO
    {
        [Display(Name = "Marketing Communications")]
        public bool MarketingConsent { get; set; }

        [Display(Name = "Accept Terms and Conditions")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
        public bool HasAcceptedPOPIA { get; set; }
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

    public class DataExportRequestDTO
    {
        [Display(Name = "Export Format")]
        public string Format { get; set; } = "PDF";

        [Display(Name = "Include All Data")]
        public bool IncludeAllData { get; set; } = true;
    }

    public class DeletionRequestDTO
    {
        [Required(ErrorMessage = "Please provide a reason for deletion")]
        [Display(Name = "Reason for Deletion")]
        public string Reason { get; set; }

        [Display(Name = "Additional Information (Optional)")]
        public string? AdditionalInfo { get; set; }
    }
}
