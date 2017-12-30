using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.Dtos
{
    public class UserForRegisterDto
    {
        // Annotated validity is for ModelState errors. 

        [Required]
        public string Username { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 4, ErrorMessage = "Must specify password between 4 and 8 characters")]
        public string Password { get; set; }
    }
}
