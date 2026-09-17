using System;

namespace Voice_AI_Agent.Model
{
    public enum Sex
    {
        Male,
        Female,
        Other,
        DeclineToAnswer
    }

    public class Patient
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required DateOnly DateOfBirth { get; set; }
        public required Sex Sex { get; set; }
        public required string PhoneNumber { get; set; }
        public required string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string ZipCode { get; set; }

        public string? Email { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsuranceMemberId { get; set; }
        public string PreferredLanguage { get; set; } = "English";
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
        public Guid PatientId { get; set; } = Guid.NewGuid();
    }
}
