using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models.Database;

namespace WeddingShare.Views.Account.Tabs
{
    public class ResourcesModel : PageModel
    {
        public ResourcesModel() 
        {
        }

        public List<CustomResourceModel>? CustomResources { get; set; }

        public void OnGet()
        {
        }
    }
}