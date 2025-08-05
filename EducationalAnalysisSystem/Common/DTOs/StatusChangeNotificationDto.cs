using Common.Enums;

public class StatusChangeNotificationDto
{
    public Guid WorkId { get; set; }
    public WorkStatus NewStatus { get; set; }
    public string Title { get; set; }
    public string StudentName { get; set; }

    public TimeSpan EstimatedAnalysisTime { get; set; } // 
    public DateTime SubmittedAt { get; set; }            // 
}


