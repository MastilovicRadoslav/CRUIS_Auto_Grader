using System.Runtime.Serialization;

namespace Common.DTOs
{
    [DataContract]
    public class SubmitWorkRequest
    {
        [DataMember]
        public Guid StudentId { get; set; }

        [DataMember]
        public string Content { get; set; } = string.Empty;

        [DataMember]
        public string Title { get; set; } = string.Empty;
    }
}
