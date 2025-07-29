using Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

[ApiController]
[Route("api/[controller]")]
public class EvaluationController : ControllerBase
{

    [HttpGet("feedbacks/student/{studentId}")]
    public async Task<IActionResult> GetFeedbacksByStudentId(Guid studentId)
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        try
        {
            var feedbacks = await evaluationService.GetFeedbacksByStudentIdAsync(studentId);
            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving feedbacks: {ex.Message}");
        }
    }
}
