﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Localization;
using WeddingShare.BackgroundWorkers;
using WeddingShare.Configurations;
using WeddingShare.Constants;
using WeddingShare.Helpers;
using WeddingShare.Middleware;

namespace WeddingShare
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public static bool Ready = false;
        
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Startup>();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = new ConfigHelper(new EnvironmentWrapper(), Configuration, _loggerFactory.CreateLogger<ConfigHelper>());

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            services.AddDependencyInjectionConfiguration();
            services.AddWebClientConfiguration(config);

            var dbHelper = services.AddDatabaseConfiguration(config, _loggerFactory);

            var settings = new SettingsHelper(dbHelper, config, _loggerFactory.CreateLogger<SettingsHelper>());
            services.AddNotificationConfiguration(settings);
            services.AddLocalizationConfiguration(settings);

            services.AddHostedService<DirectoryScanner>();
            services.AddHostedService<NotificationReport>();
            services.AddHostedService<CleanupService>();

            services.AddRazorPages();
            services.AddControllersWithViews().AddRazorRuntimeCompilation();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue;
            });

            services.Configure<FormOptions>(x =>
            {
                x.MultipartHeadersLengthLimit = Int32.MaxValue;
                x.MultipartBoundaryLengthLimit = Int32.MaxValue;
                x.MultipartBodyLengthLimit = Int64.MaxValue;
                x.ValueLengthLimit = Int32.MaxValue;
                x.BufferBodyLengthLimit = Int64.MaxValue;
                x.MemoryBufferThreshold = Int32.MaxValue;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
            {
                options.Cookie.HttpOnly = false;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);

                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = $"/Error?Reason={ErrorCode.Unauthorized}";
                options.SlidingExpiration = true;
            });
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.Name = ".WeddingShare.Session";
                options.Cookie.IsEssential = true;
            });

            var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<Lang.Translations>>();
            var ffmpegPath = config.GetOrDefault(FFMPEG.InstallPath, "/ffmpeg");
            var imageHelper = new ImageHelper(new FileHelper(_loggerFactory.CreateLogger<FileHelper>()), _loggerFactory.CreateLogger<ImageHelper>(), localizer);
            var downloaded = imageHelper.DownloadFFMPEG(ffmpegPath).Result;
            if (!downloaded)
            {
                _logger.LogWarning($"{localizer["FFMPEG_Download_Failed"].Value} '{ffmpegPath}'");
            }

            Ready = true;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler();

            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            var config = new ConfigHelper(new EnvironmentWrapper(), Configuration, _loggerFactory.CreateLogger<ConfigHelper>());
            if (config.GetOrDefault(Settings.Basic.ForceHttps, false))
            { 
                app.UseHttpsRedirection();
            }

            app.UseCookiePolicy();

            if (config.GetOrDefault(Security.Headers.Enabled, true))
            {
                try
                {
                    app.Use(async (context, next) =>
                    {
                        context.Response.Headers.Remove("X-Frame-Options");
                        context.Response.Headers.Append("X-Frame-Options", config.GetOrDefault(Security.Headers.XFrameOptions, "SAMEORIGIN"));

                        context.Response.Headers.Remove("X-Content-Type-Options");
                        context.Response.Headers.Append("X-Content-Type-Options", config.GetOrDefault(Security.Headers.XContentTypeOptions, "nosniff"));

                        context.Response.Headers.Remove("Content-Security-Policy");
                        context.Response.Headers.Append("Content-Security-Policy", config.GetOrDefault(Security.Headers.CSP, $"default-src 'self' http://localhost:* ws://localhost:*; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' https://github.com/ https://avatars.githubusercontent.com/ data:; frame-src 'self'; frame-ancestors 'self';"));

                        await next();
                    });
                }
                catch { }
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRequestLocalization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
                endpoints.MapRazorPages();
            });
        }
    }
}