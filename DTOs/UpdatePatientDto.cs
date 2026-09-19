using System.ComponentModel.DataAnnotations;
using Voice_AI_Agent.Model;

namespace Voice_AI_Agent.DTOs
{
    public class UpdatePatientDto
    {
        [StringLength(50, MinimumLength = 1)]
        [RegularExpression(@"^[A-Za-z'\-]+$")]
        public string? FirstName { get; set; }

        [StringLength(50, MinimumLength = 1)]
        [RegularExpression(@"^[A-Za-z'\-]+$")]
        public string? LastName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public Sex? Sex { get; set; }

        [StringLength(10)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be a 10-digit U.S. number (digits only).")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, MinimumLength = 1)]
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }

        [StringLength(100, MinimumLength = 1)]
        public string? City { get; set; }

        [StringLength(2)]
        [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "State must be a 2-letter U.S. state abbreviation.")]
        public string? State { get; set; }

        [StringLength(10)]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Zip code must be 5 digits or ZIP+4 format.")]
        public string? ZipCode { get; set; }

        public string? Email { get; set; }
        public string? InsuranceProvider { get; set; }
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Insurance member ID must be alphanumeric.")]
        public string? InsuranceMemberId { get; set; }
        public string? PreferredLanguage { get; set; } = "English";
        public string? EmergencyContactName { get; set; }

        [StringLength(10)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Emergency contact phone must be a 10-digit U.S. number (digits only).")]
        public string? EmergencyContactPhone { get; set; }
    }
}
