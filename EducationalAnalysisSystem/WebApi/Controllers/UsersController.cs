using Common.DTOs;
using Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using WebApi.Helpers;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        // Nema autorizacije svi imaju pristup
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var result = await userService.RegisterUserAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new RegisterResponse
            {
                UserId = result.Data,
                Message = "User registered successfully."
            });
        }


        // Nema autorizacije svi imaju pristup
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var user = await userService.LoginAsync(request);

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(new
            {
                Token = JwtTokenGenerator.GenerateToken(user.Id, user.Role),
                UserId = user.Id,
                Role = user.Role,
                Message = "Login successful."
            });

        }

    }
}
