using System.Runtime.Serialization;

namespace WeddingShare.Models
{
    public class UpdateSettingsModel
    {
        [DataMember(Name = "key")]
        public string Key { get; set; }
        
        [DataMember(Name = "value")]
        public string? Value { get; set; }
    }
}