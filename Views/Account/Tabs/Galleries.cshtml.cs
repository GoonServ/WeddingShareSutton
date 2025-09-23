using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models.Database;

namespace WeddingShare.Views.Account.Tabs
{
    public class GalleriesModel : PageModel
    {
        public GalleriesModel() 
        {
        }

        public List<GalleryModel>? Galleries { get; set; }

        public void OnGet()
        {
        }
    }
}