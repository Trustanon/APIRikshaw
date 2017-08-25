using LandonApi.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandonApi.Models
{
    public class RegisterForm
    {
        [Required]
        [Display(Name = "email", Description = "Email address")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(90)]
        [Display(Name = "password", Description = "Password")]
        [Secret]
        public string Password { get; set; }

        [Required]
        [MinLength(3)]
        [MaxLength(99)]
        [RegularExpression(@"^[\p{L}]+$", ErrorMessage = "Use letters only please")]
        [Display(Name = "firstName", Description = "First name")]
        public string FirstName { get; set; }

        [Required]
        [MinLength(3)]
        [MaxLength(99)]
        [RegularExpression(@"^[\p{L}]+$", ErrorMessage = "Use letters only please")]
        [Display(Name = "lastName", Description = "Last name")]
        public string LastName { get; set; }
    }
}
