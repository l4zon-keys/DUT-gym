using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LoginFormASPCore6.Models
{
    public partial class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter your full name.")]
        [DisplayName("Full Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        public string EmpName { get; set; } = null!;

        [Required(ErrorMessage = "Please select a gender.")]
        [DisplayName("Gender")]
        public string Gender { get; set; } = null!;

        [Required(ErrorMessage = "Please enter your DUT email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [InstitutionEmail(ErrorMessage = "Only DUT student (@dut4life.ac.za) or staff (@dut.ac.za) email addresses may be used.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [DisplayName("DUT Email Address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Please enter a password.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "Password must include an uppercase letter, a lowercase letter, a number, and a special character.")]
        [DisplayName("Password")]
        public string Password { get; set; } = null!;

        // Automatically determined server-side from the email domain at signup.
        // Not bound from the form and not user-editable.
        [DisplayName("Role")]
        public string Role { get; set; } = "Unknown";
    }
}