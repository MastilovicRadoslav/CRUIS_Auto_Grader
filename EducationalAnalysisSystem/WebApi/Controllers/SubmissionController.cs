using Common.DTOs;
using Common.Enums;
using Common.Interfaces;
using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using WebApi.Helpers;
using WebApi.Hubs;


namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionController : ControllerBase
    {
        [Authorize]
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitWork([FromForm] IFormFile file, [FromForm] string title, [FromForm] Guid studentId) // Testirano
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { error = "Title is required." });

            if (studentId == Guid.Empty)
                return BadRequest(new { error = "Invalid student ID." });

            var content = await FileProcessor.ExtractTextAsync(file);

            var submitData = new SubmitWorkData
            {
                StudentId = studentId,
                Title = title,
                Content = content // // Parsiranje fajla jer se fajl ne moze prenositi preko remoting
            };

            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var result = await submissionService.SubmitWorkAsync(submitData);

            return Ok(new
            {
                WorkId = result.Data,
                Content = content
            });
        }


        [HttpPost("notify-status-change")]
        public async Task<IActionResult> NotifyStatusChange([FromBody] StatusChangeNotificationDto dto) // Testirano
        {
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<StatusHub>>();
            await hubContext.Clients.All.SendAsync("StatusChanged", dto);
            return Ok();
        }


        [Authorize]
        [AuthorizeRole("Student")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyWorks() // Testirano - student
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized();

            var studentId = Guid.Parse(userIdStr);

            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var works = await submissionService.GetWorksByStudentIdAsync(studentId);
            return Ok(works);
        }

        [Authorize]
        [AuthorizeRole("Professor")]
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetWorksByStudentId(Guid studentId) // Zaobisao sam tako sto sam filtrirao po imenu i ona vidim njegove radove bez slanje Endpoint
        {
            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var works = await submissionService.GetWorksByStudentIdAsync(studentId);
            return Ok(works);
        }

        [Authorize]
        [AuthorizeRole("Professor")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllSubmissions() // Testirano
        {
            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var submissions = await submissionService.GetAllSubmissionsAsync();
            return Ok(submissions);
        }

        [Authorize]
        [AuthorizeRole("Professor")]
        [HttpGet("by-status")]
        public async Task<IActionResult> GetByStatus([FromQuery] WorkStatus status) // Zaobisao sam tako sto sam filtrirao po datumu ranga i ona vidim njegove radove bez slanje Endpoint
        {
            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var result = await submissionService.GetSubmissionsByStatusAsync(status);
            return Ok(result);
        }

    }
}