using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandonApi.Models
{
    public class EditEmailForm
    {
        [Required]
        [Display(Name = "email", Description = "Email address")]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [Display(Name = "emailConfirm", Description = "Email address")]
        [EmailAddress]
        public string EmailConfirm { get; set; }
    }
}
