using WeddingShare.Helpers.Database;
using WeddingShare.Models.Database;

namespace WeddingShare.Helpers
{
    public interface IAuditHelper
    {
        Task<bool> LogAction(string? user, string? action);
    }

    public class AuditHelper : IAuditHelper
    {
        private readonly IDatabaseHelper _databaseHelper;
        private readonly ILogger _logger;

        public AuditHelper(IDatabaseHelper databaseHelper, ILogger<AuditHelper> logger)
        {
            _databaseHelper = databaseHelper;
            _logger = logger;
        }

        public async Task<bool> LogAction(string? user, string? action)
        {
            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(action))
            { 
                try 
                {
                    return await _databaseHelper.AddAuditLog(new AuditLogModel()
                    {
                        Username = user,
                        Message = action
                    }) != null;
                }
                catch (Exception ex) 
                {
                    _logger.LogError($"Failed to log audit message '{action}' for user '{user}'", ex);
                }
            }

            return false;
        }
    }
}