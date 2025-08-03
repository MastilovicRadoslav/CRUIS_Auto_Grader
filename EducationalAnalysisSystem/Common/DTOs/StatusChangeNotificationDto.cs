using Common.Enums;

public class StatusChangeNotificationDto
{
    public Guid WorkId { get; set; }
    public WorkStatus NewStatus { get; set; }
    public string Title { get; set; }
    public TimeSpan EstimatedAnalysisTime { get; set; } // ➕ DODAJ
    public DateTime SubmittedAt { get; set; }            // ➕ DODAJ
}


