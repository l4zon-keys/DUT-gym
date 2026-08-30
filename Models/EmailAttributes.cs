using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace LoginFormASPCore6.Models
{
    /// <summary>
    /// Validates that an email address belongs to one of the recognised
    /// DUT institutional domains (student or staff).
    /// </summary>
    public class InstitutionEmailAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var email = value as string;

            if (string.IsNullOrWhiteSpace(email))
            {
                // Let [Required] handle empty values.
                return ValidationResult.Success;
            }

            var isValid = EmailRoleHelper.AllowedDomains
                .Any(domain => email.Trim().EndsWith(domain, StringComparison.OrdinalIgnoreCase));

            return isValid
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? "Only DUT student or staff email addresses are permitted.");
        }
    }

    /// <summary>
    /// Resolves a DUT institutional role from an email address domain.
    /// </summary>
    public static class EmailRoleHelper
    {
        public const string StudentDomain = "@dut4life.ac.za";
        public const string StaffDomain = "@dut.ac.za";

        public static readonly string[] AllowedDomains = { StudentDomain, StaffDomain };

        public const string StudentRole = "Student";
        public const string StaffRole = "Staff";
        // Not assignable via signup/domain - promoted manually (directly in the database).
        // Admin has every Staff permission plus admin-only ones (see AppControllerBase).
        public const string AdminRole = "Admin";
        // Self-applied via the "Become a Trainer" form, not domain-derived - starts
        // ApprovalStatus.Pending until an Admin approves them (same gate as Staff).
        public const string TrainerRole = "Trainer";
        public const string UnknownRole = "Unknown";

        public static string GetRole(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return UnknownRole;
            }

            email = email.Trim();

            if (email.EndsWith(StudentDomain, StringComparison.OrdinalIgnoreCase))
            {
                return StudentRole;
            }

            if (email.EndsWith(StaffDomain, StringComparison.OrdinalIgnoreCase))
            {
                return StaffRole;
            }

            return UnknownRole;
        }
    }
}