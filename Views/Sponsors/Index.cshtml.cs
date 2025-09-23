using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models;

namespace WeddingShare.Views.Sponsors
{
    public class IndexModel : PageModel
    {
        public IndexModel() 
        {
        }

        public SponsorsList? SponsorsList { get; set; }

        public void OnGet()
        {
        }
    }
}