using System.Runtime.Serialization;

namespace Common.DTOs
{
    [DataContract]

    public class SubmitWorkData
    {
        [DataMember]
        public Guid StudentId { get; set; }
        [DataMember]
        public string Title { get; set; } = string.Empty;
        
        [DataMember]
        public string Content { get; set; } = string.Empty;


    }

}
