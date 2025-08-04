using Common.DTOs;
using Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using WebApi.Helpers;
using WebApi.Hubs;

[ApiController]
[Route("api/[controller]")]
public class EvaluationController : ControllerBase
{

    [Authorize]
    [AuthorizeRole("Professor")]
    [HttpGet("feedbacks/student/{studentId}")] // Nijesam upotrijebio za sad jer sam filtrirao
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
    public async Task<IActionResult> GetMyFeedbacks() // Nijesam upotrijebio 
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
    public async Task<IActionResult> AddProfessorComment([FromBody] AddProfessorCommentRequest request) // Testirano
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
    public async Task<IActionResult> GetFeedbackByWorkId(Guid workId) // Testirano, 
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
    [AuthorizeRole("Professor")]
    [HttpGet("all-feedbacks")]
    public async Task<IActionResult> GetAllFeedbacks() //Ne treba mi
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
    public async Task<IActionResult> GetStatistics() // Izvlacenje statistike svih feedback-ova za sve studente
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var stats = await evaluationService.GetStatisticsAsync();
        return Ok(stats);
    }

    [Authorize]
    [HttpGet("statistics/student/{studentId}")]
    public async Task<IActionResult> GetStatisticsByStudentId(Guid studentId) // Dobavljanje statistike za Feedback za jednog studenta na osnovu njegovog ID
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var stats = await evaluationService.GetStatisticsByStudentIdAsync(studentId);
        return Ok(stats);
    }

    [HttpPost("notify-progress-change")]
    public async Task<IActionResult> NotifyProgressChange([FromBody] ProgressUpdateDto progress)
    {
        var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<StatusHub>>();
        await hubContext.Clients.All.SendAsync("ProgressUpdated", progress.StudentId);
        return Ok();
    }



    [Authorize]
    [AuthorizeRole("Professor")]
    [HttpPost("statistics/date-range")]
    public async Task<IActionResult> GetStatisticsByDateRange([FromBody] DateRangeRequest request) // Dobavljanje statistike za sve feedback na osnovu date-range
    {
        var evaluationService = ServiceProxy.Create<IEvaluationService>(
            new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
            new ServicePartitionKey(0)
        );

        var stats = await evaluationService.GetStatisticsByDateRangeAsync(request);
        return Ok(stats);
    }
}
