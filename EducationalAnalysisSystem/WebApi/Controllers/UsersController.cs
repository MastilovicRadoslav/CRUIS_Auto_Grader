using Common.DTOs;
using Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Pravljenje proxy-ja ka UserService-u (stateful)
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0) // za sada samo jedna partija
            );

            var userId = await userService.RegisterUserAsync(request);

            return Ok(new RegisterResponse
            {
                UserId = userId,
                Message = "User registered successfully."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
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

            return Ok(new LoginResponse
            {
                UserId = user.Id,
                Role = user.Role,
                Message = "Login successful."
            });
        }
    }
}
