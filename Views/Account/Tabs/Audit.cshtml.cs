using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models.Database;

namespace WeddingShare.Views.Account.Tabs
{
    public class AuditModel : PageModel
    {
        public AuditModel() 
        {
        }

        public IEnumerable<AuditLogModel>? Logs { get; set; }

        public void OnGet()
        {
        }
    }
}