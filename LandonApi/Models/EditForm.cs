using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandonApi.Models
{
    public class EditForm
    {
        
        [MinLength(3)]
        [MaxLength(99)]
        [RegularExpression(@"^[\p{L}]+$", ErrorMessage = "Use letters only please")]
        [Display(Name = "firstName", Description = "First name")]
        public string FirstName { get; set; }

       
        [MinLength(3)]
        [MaxLength(99)]
        [RegularExpression(@"^[\p{L}]+$", ErrorMessage = "Use letters only please")]
        [Display(Name = "lastName", Description = "Last name")]
        public string LastName { get; set; }
    }
}
