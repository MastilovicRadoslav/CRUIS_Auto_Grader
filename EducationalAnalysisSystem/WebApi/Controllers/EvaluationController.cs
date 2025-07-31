using Common.DTOs;
using Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using WebApi.Helpers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationController : ControllerBase
{

    [Authorize]
    [AuthorizeRole("Professor")]
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
    [AuthorizeRole("Student")]
    [HttpGet("feedbacks/my")]
    public async Task<IActionResult> GetMyFeedbacks()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var studentId = Guid.Parse(userIdStr);

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
    [AuthorizeRole("Professor")]
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
    [AuthorizeRole("Professor")]
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
    [AuthorizeRole("Student")]
    [HttpGet("feedback/my/{workId}")]
    public async Task<IActionResult> GetMyFeedbackByWorkId(Guid workId)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var studentId = Guid.Parse(userIdStr);

        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var feedback = await evaluationService.GetFeedbackByWorkIdAsync(workId);

        if (feedback == null)
            return NotFound("Feedback not found for this work.");

        if (feedback.StudentId != studentId)
            return Forbid("You are not authorized to view feedback for this work.");

        return Ok(feedback);
    }


    [Authorize]
    [AuthorizeRole("Professor")]
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
    [AuthorizeRole("Professor")]
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

    [Authorize]
    [AuthorizeRole("Professor")]
    [HttpGet("statistics/student/{studentId}")]
    public async Task<IActionResult> GetStatisticsByStudentId(Guid studentId)
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var stats = await evaluationService.GetStatisticsByStudentIdAsync(studentId);
        return Ok(stats);
    }

    [Authorize]
    [AuthorizeRole("Professor")]
    [HttpPost("statistics/date-range")]
    public async Task<IActionResult> GetStatisticsByDateRange([FromBody] DateRangeRequest request)
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var stats = await evaluationService.GetStatisticsByDateRangeAsync(request);
        return Ok(stats);
    }
}
