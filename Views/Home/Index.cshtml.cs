using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WeddingShare.Views.Home
{
    public class IndexModel : PageModel
    {
        public IndexModel() 
        {
            this.GalleryNames = new List<string>() { "default" };
        }

        public IEnumerable<string> GalleryNames { get; set; }

        public void OnGet()
        {
        }
    }
}