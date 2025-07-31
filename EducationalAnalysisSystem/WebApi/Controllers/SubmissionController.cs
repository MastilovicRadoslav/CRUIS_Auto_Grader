using Common.DTOs;
using Common.Enums;
using Common.Interfaces;
using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using WebApi.Helpers;


namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionController : ControllerBase
    {
        [Authorize]
        [AuthorizeRole("Student")]
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitWork([FromBody] SubmitWorkRequest request)
        {
            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var result = await submissionService.SubmitWorkAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            //// Evaluacija nakon uspješnog snimanja
            //var newSubmission = new SubmittedWork
            //{
            //    Id = result.Data,
            //    StudentId = request.StudentId,
            //    Title = request.Title,
            //    Content = request.Content, // ili ostavi prazno ako nije bitno
            //    SubmittedAt = DateTime.UtcNow
            //};

            //var evaluationService = ServiceProxy.Create<IEvaluationService>(
            //    new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            //    new ServicePartitionKey(0)
            //);

            //var feedback = await evaluationService.EvaluateAsync(newSubmission);

            //newSubmission.Status = WorkStatus.Completed;

            return Ok(new WorkResponse
            {
                WorkId = result.Data,
                Message = "Work submitted and evaluated successfully.",
            });
        }


        [Authorize]
        [AuthorizeRole("Student")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyWorks()
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
        public async Task<IActionResult> GetWorksByStudentId(Guid studentId)
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
        public async Task<IActionResult> GetAllSubmissions()
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
        public async Task<IActionResult> GetByStatus([FromQuery] WorkStatus status)
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
