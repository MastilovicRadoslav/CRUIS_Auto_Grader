using Common.DTOs;
using Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using WebApi.Helpers;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        [Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers() // Testirano
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var users = await userService.GetAllUsersAsync();

            if (users == null || !users.Any())
            {
                return NotFound("No users found.");
            }

            return Ok(users);
        }

        [Authorize]
        [AuthorizeRole("Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request) // Testirano radi
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var result = await userService.CreateUserAsync(request);

            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { userId = result.Data, message = "User created successfully." });
        }

        [Authorize]
        [AuthorizeRole("Admin")]
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id) // Testirano radi
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var result = await userService.DeleteUserAsync(id);

            if (!result.Success)
                return NotFound(new { error = result.Error });

            return Ok(new { message = "User deleted successfully." });
        }

        [Authorize]
        [AuthorizeRole("Admin")]
        [HttpPut("user/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request) // Testirano radi
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var result = await userService.UpdateUserAsync(id, request);

            if (!result.Success)
                return NotFound(new { error = result.Error });

            return Ok(new { message = "User updated successfully." });
        }

        [HttpPost("settings/submission-window")]
        public async Task<IActionResult> SetSubmissionWindow([FromBody] SubmissionWindowSetting request)
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var success = await userService.SetSubmissionWindowAsync(request);

            return success ? Ok("Submission window setting updated.") : StatusCode(500, "Failed to update setting.");
        }

        [HttpGet("settings/submission-window")]
        public async Task<ActionResult<SubmissionWindowSetting>> GetSubmissionWindow()
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var setting = await userService.GetSubmissionWindowAsync();
            return Ok(setting);
        }
    }
}
