﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Common.DTOs
{
    public class FeedbackDto
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid WorkId { get; set; }
        public string Title { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)] // 
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;

        public int Grade { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public string? ProfessorComment { get; set; }
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    }
}
