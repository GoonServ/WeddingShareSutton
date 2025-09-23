using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WeddingShare.Constants;
using WeddingShare.Helpers;
using WeddingShare.Models;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class SponsorsController : Controller
    {
        private readonly ISettingsHelper _settings;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<Lang.Translations> _localizer;

        public SponsorsController(ISettingsHelper settings, IHttpClientFactory clientFactory, ILogger<HomeController> logger, IStringLocalizer<Lang.Translations> localizer)
        {
            _settings = settings;
            _clientFactory = clientFactory;
            _logger = logger;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new Views.Sponsors.IndexModel();

            try
            {
                var client = _clientFactory.CreateClient("SponsorsClient");
                var endpoint = await _settings.GetOrDefault(Sponsors.Endpoint, "/sponsors.json");
                model.SponsorsList = await client.GetFromJsonAsync<SponsorsList>(endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Sponsors_Load_Error"].Value} - {ex?.Message}");
            }

            return PartialView(model);
        }
    }
}