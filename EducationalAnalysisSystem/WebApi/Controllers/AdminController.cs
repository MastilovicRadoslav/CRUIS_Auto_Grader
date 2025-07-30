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
        [AuthorizeRole("Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
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
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
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
        public async Task<IActionResult> DeleteUser(Guid id)
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
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
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


        [Authorize]
        [AuthorizeRole("Admin")]
        [HttpPost("settings/max-submissions")]
        public async Task<IActionResult> SetMaxSubmissions([FromBody] MaxSubmissionsSetting request)
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var result = await userService.SetMaxSubmissionsAsync(request.MaxPerStudent);
            return result.Success ? Ok("Setting updated.") : StatusCode(500, "Failed to update setting.");
        }

        [Authorize]
        [AuthorizeRole("Admin")]
        [HttpGet("settings/max-submissions")]
        public async Task<IActionResult> GetMaxSubmissions()
        {
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var value = await userService.GetMaxSubmissionsAsync();
            return Ok(new { maxPerStudent = value });
        }

    }
}
