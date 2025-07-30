using Common.DTOs;
using Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

[ApiController]
[Route("api/[controller]")]
public class EvaluationController : ControllerBase
{

    [Authorize]
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

    [Authorize]
    [HttpPost("professor-comment")]
    public async Task<IActionResult> AddProfessorComment([FromBody] AddProfessorCommentRequest request)
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var success = await evaluationService.AddProfessorCommentAsync(request);

        if (!success)
            return NotFound("Feedback not found for provided WorkId.");

        return Ok("Comment added successfully.");
    }

    [Authorize]
    [HttpGet("feedback/{workId}")]
    public async Task<IActionResult> GetFeedbackByWorkId(Guid workId)
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var feedback = await evaluationService.GetFeedbackByWorkIdAsync(workId);
        if (feedback == null)
            return NotFound("Feedback not found for this work.");

        return Ok(feedback);
    }

    [Authorize]
    [HttpGet("all-feedbacks")]
    public async Task<IActionResult> GetAllFeedbacks()
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var feedbacks = await evaluationService.GetAllFeedbacksAsync();
        return Ok(feedbacks);
    }

    [Authorize]
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var stats = await evaluationService.GetStatisticsAsync();
        return Ok(stats);
    }

}
