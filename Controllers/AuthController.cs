using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
  [Route("api/[controller]")]
  public class AuthController : Controller
  {
      private readonly IAuthRepository _repo;
      private readonly IConfiguration _config;
      public AuthController(IAuthRepository repo, IConfiguration config)    // DI.
      {
          _repo = repo;
          _config = config;
      }

      [HttpPost("register")]
      public async Task<IActionResult> Register([FromBody]UserForRegisterDto userForRegisterDto) {
          // Validate request
          if(!string.IsNullOrEmpty(userForRegisterDto.Username))    // Avoid lower-casing of null string causing needless exception.
              userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

          if (await _repo.UserExists(userForRegisterDto.Username)) {
              ModelState.AddModelError("Username", "Username already exists");  // Use the appropriate dto key.
          }

          if (!ModelState.IsValid)
            return BadRequest(ModelState);

          var userToCreate = new User {
              Username = userForRegisterDto.Username
          };

          var createUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

          return StatusCode(201);
      }  

      [HttpPost("login")]
      public async Task<IActionResult> Login([FromBody]UserForLoginDto userForLoginDto) {

        throw new Exception("Computer says no!");

        var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

        if (userFromRepo == null)
            return Unauthorized();

        // Generate the JWT.
        var tokenHandler = new JwtSecurityTokenHandler();                 // Std JWT header here.
        var key = Encoding.ASCII.GetBytes(_config.GetSection("AppSettings:Token").Value);  // App key here.
        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(new Claim[] {                  // Payload here (user data).
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username)
            }),
            Expires = DateTime.Now.AddDays(1),                          // Payload here (timestamps).
            // Note: issuer and audience can be specified in the payloafd as well.
            SigningCredentials = 
                new SigningCredentials(new SymmetricSecurityKey(key),   // Signature here with secret.
                                        SecurityAlgorithms.HmacSha512Signature)
            };
        var token = tokenHandler.CreateToken(tokenDescriptor);        // Encode and connect the pieces.
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new { tokenString } );   // Return the new JWT as JSON object.
      }
  }
}
