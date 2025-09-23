namespace WeddingShare.Models.Database
{
    public class AuditLogModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public DateTime Timestamp { get; set; }
    }
}