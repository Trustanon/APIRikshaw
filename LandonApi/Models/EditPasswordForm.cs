using LandonApi.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandonApi.Models
{
    public class EditPasswordForm
    {
        [Required]
        [MinLength(6)]
        [MaxLength(90)]
        [Display(Name = "passwordCurrent", Description = "Password current")]
        [Secret]
        public string PasswordCurrent { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(90)]
        [Display(Name = "password", Description = "Password")]
        [Secret]
        public string Password { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(90)]
        [Display(Name = "passwordConfirm", Description = "Password Confirmation")]
        [Secret]
        public string PasswordConfirm { get; set; }
    }
}
