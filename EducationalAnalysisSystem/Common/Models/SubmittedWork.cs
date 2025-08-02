using Common.DTOs;
using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class SubmittedWork
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [DataMember]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonRepresentation(BsonType.String)]
        [DataMember]
        public Guid StudentId { get; set; }

        [DataMember]
        public string StudentName { get; set; } = string.Empty;

        [DataMember]
        public string Title { get; set; } = string.Empty;
        [DataMember]
        public string Content { get; set; } = string.Empty;

        [DataMember]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [DataMember]
        public WorkStatus Status { get; set; } = WorkStatus.Pending;

        [BsonIgnore]
        [DataMember]
        public AnalysisResultDto Analysis { get; set; } = new();

    }
}
