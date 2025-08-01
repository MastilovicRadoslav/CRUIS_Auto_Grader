﻿using Common.DTOs;
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
        public async Task<IActionResult> SubmitWork([FromForm] IFormFile file, [FromForm] string title, [FromForm] Guid studentId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { error = "Title is required." });

            if (studentId == Guid.Empty)
                return BadRequest(new { error = "Invalid student ID." });

            var content = await FileProcessor.ExtractTextAsync(file);

            var analysis = await LlmClient.AnalyzeAsync(content);

            var submitData = new SubmitWorkData
            {
                StudentId = studentId,
                Title = title,
                Content = content,
                Analysis = analysis
            };

            //
            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );
            var result = await submissionService.SubmitWorkAsync(submitData);


            // Za sada samo testiramo prijem podataka, ništa više
            return Ok(new
            {
                WorkId = result.Data,
                Analysis = analysis
            });

        }



        [Authorize]
        [AuthorizeRole("Student")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyWorks() //Testirano
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