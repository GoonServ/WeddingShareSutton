using System.Net.Http.Headers;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Notifications;

namespace WeddingShare.Configurations
{
    internal static class NotificationConfiguration
    {
        private const int CLIENT_DEFAULT_TIMEOUT = 10;

        public static void AddNotificationConfiguration(this IServiceCollection services, SettingsHelper settings)
        {
            services.AddSingleton<INotificationHelper, NotificationBroker>();
            services.AddNtfyConfiguration(settings);
            services.AddGotifyConfiguration(settings);
        }

        public static void AddNtfyConfiguration(this IServiceCollection services, SettingsHelper settings)
        {
            services.AddHttpClient("NtfyClient", (serviceProvider, httpClient) =>
            {
                var endpoint = settings.GetOrDefault(Constants.Notifications.Ntfy.Endpoint, string.Empty).Result;
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    var token = settings.GetOrDefault(Constants.Notifications.Ntfy.Token, string.Empty).Result;
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    httpClient.BaseAddress = new Uri(endpoint);
                    httpClient.Timeout = TimeSpan.FromSeconds(CLIENT_DEFAULT_TIMEOUT);
                }
            });
        }

        public static void AddGotifyConfiguration(this IServiceCollection services, SettingsHelper settings)
        {
            services.AddHttpClient("GotifyClient", (serviceProvider, httpClient) =>
            {
                var endpoint = settings.GetOrDefault(Constants.Notifications.Gotify.Endpoint, string.Empty).Result;
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    httpClient.BaseAddress = new Uri(endpoint);
                    httpClient.Timeout = TimeSpan.FromSeconds(CLIENT_DEFAULT_TIMEOUT);
                }
            });
        }
    }
}