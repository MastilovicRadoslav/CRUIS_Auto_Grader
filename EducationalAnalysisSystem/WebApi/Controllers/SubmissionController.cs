using Common.DTOs;
using Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;


namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionController : ControllerBase
    {
        [Authorize]
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

            return Ok(new WorkResponse
            {
                WorkId = result.Data,
                Message = "Work submitted successfully."
            });
        }

        [Authorize]
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
    }
}
