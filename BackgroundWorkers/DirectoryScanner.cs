﻿using NCrontab;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WeddingShare.Constants;
using WeddingShare.Enums;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models.Database;

namespace WeddingShare.BackgroundWorkers
{
    public sealed class DirectoryScanner(IWebHostEnvironment hostingEnvironment, ISettingsHelper settingsHelper, IDatabaseHelper databaseHelper, IFileHelper fileHelper, IImageHelper imageHelper, ILogger<DirectoryScanner> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = await settingsHelper.GetOrDefault(BackgroundServices.DirectoryScanner.Enabled, true);
            if (enabled)
            {
                var cron = await settingsHelper.GetOrDefault(BackgroundServices.DirectoryScanner.Schedule, "*/30 * * * *");
                var nextExecutionTime = DateTime.Now.AddSeconds(10);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var currentCron = await settingsHelper.GetOrDefault(BackgroundServices.DirectoryScanner.Schedule, "*/30 * * * *");

                    var now = DateTime.Now;
                    if (now >= nextExecutionTime)
                    {
                        await ScanForFiles();

                        var schedule = CrontabSchedule.Parse(cron, new CrontabSchedule.ParseOptions() { IncludingSeconds = cron.Split(new[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length == 6 });
                        nextExecutionTime = schedule.GetNextOccurrence(now);
                    }
                    else
                    {
                        if (!currentCron.Equals(cron))
                        {
                            nextExecutionTime = DateTime.Now;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                    
                    cron = currentCron;
                }
            }
        }

        private async Task ScanForFiles()
        {
            if (Startup.Ready)
            {
                await this.ScanGalleryImages();
                await this.ScanCustomResources();
            }
            else
            {
                logger.LogInformation($"Skipping directory scan, application not ready yet");
            }
        }

        private async Task ScanGalleryImages()
        {
            try
            { 
                var thumbnailsDirectory = Path.Combine(hostingEnvironment.WebRootPath, Directories.Thumbnails);
                fileHelper.CreateDirectoryIfNotExists(thumbnailsDirectory);

                var uploadsDirectory = Path.Combine(hostingEnvironment.WebRootPath, Directories.Uploads);
                if (fileHelper.DirectoryExists(uploadsDirectory))
                {
                    var galleryDirs = fileHelper.GetDirectories(uploadsDirectory, "*", SearchOption.TopDirectoryOnly)?.Where(x => !Path.GetFileName(x).StartsWith("."));
                    if (galleryDirs != null)
                    {
                        foreach (var galleryDir in galleryDirs)
                        {
                            try
                            {
                                var identifier = Path.GetFileName(galleryDir).ToLower();

                                var galleryId = await databaseHelper.GetGalleryId(identifier);
                                if (galleryId != null)
                                {
                                    var galleryItem = await databaseHelper.GetGallery(galleryId.Value);
                                    if (galleryItem == null)
                                    {
                                        if (await databaseHelper.GetGalleryCount() < await settingsHelper.GetOrDefault(Settings.Basic.MaxGalleryCount, 1000000))
                                        {
                                            galleryItem = await databaseHelper.AddGallery(new GalleryModel()
                                            {
                                                Name = identifier,
                                                Owner = 0
                                            });
                                        }
                                    }

                                    if (galleryItem != null)
                                    {
                                        var allowedFileTypes = settingsHelper.GetOrDefault(Settings.Gallery.AllowedFileTypes, ".jpg,.jpeg,.png,.mp4,.mov", galleryItem?.Id).Result.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                        var galleryItems = await databaseHelper.GetAllGalleryItems(galleryItem.Id);

                                        if (Path.Exists(galleryDir))
                                        {
                                            var approvedFiles = fileHelper.GetFiles(galleryDir, "*.*", SearchOption.TopDirectoryOnly).Where(x => allowedFileTypes.Any(y => string.Equals(Path.GetExtension(x).Trim('.'), y.Trim('.'), StringComparison.OrdinalIgnoreCase)));
                                            if (approvedFiles != null)
                                            {
                                                foreach (var file in approvedFiles)
                                                {
                                                    try
                                                    {
                                                        var filename = Path.GetFileName(file);
                                                        var g = galleryItems.FirstOrDefault(x => string.Equals(x.Title, filename, StringComparison.OrdinalIgnoreCase));
                                                        if (g == null)
                                                        {
                                                            g = await databaseHelper.AddGalleryItem(new GalleryItemModel()
                                                            {
                                                                GalleryId = galleryItem.Id,
                                                                Title = filename,
                                                                Checksum = await fileHelper.GetChecksum(file),
                                                                MediaType = imageHelper.GetMediaType(file),
                                                                State = GalleryItemState.Approved,
                                                                UploadedDate = await fileHelper.GetCreationDatetime(file),
                                                                FileSize = fileHelper.FileSize(file)
                                                            });
                                                        }

                                                        var thumbnailDir = Path.Combine(thumbnailsDirectory, galleryItem.Name);
                                                        var thumbnailPath = Path.Combine(thumbnailDir, $"{Path.GetFileNameWithoutExtension(file)}.webp");
                                                        if (!fileHelper.FileExists(thumbnailPath))
                                                        {
                                                            fileHelper.CreateDirectoryIfNotExists(thumbnailDir);
                                                            await imageHelper.GenerateThumbnail(file, thumbnailPath, settingsHelper.GetOrDefault(Settings.Basic.ThumbnailSize, 720).Result);
                                                            fileHelper.DeleteFileIfExists(Path.Combine(thumbnailsDirectory, $"{Path.GetFileNameWithoutExtension(file)}.webp"));
                                                        }
                                                        else
                                                        {
                                                            using (var img = await Image.LoadAsync(thumbnailPath))
                                                            {
                                                                var width = img.Width;

                                                                img.Mutate(x => x.AutoOrient());

                                                                if (width != img.Width)
                                                                {
                                                                    await img.SaveAsWebpAsync(thumbnailPath);
                                                                }
                                                            }
                                                        }

                                                        if (g != null)
                                                        {
                                                            var updated = false;

                                                            if (g.UploadedDate == null)
                                                            {
                                                                g.UploadedDate = new FileInfo(file).CreationTimeUtc;
                                                                updated = true;
                                                            }

                                                            if (g.MediaType == MediaType.Unknown)
                                                            {
                                                                g.MediaType = imageHelper.GetMediaType(file);
                                                                updated = true;
                                                            }

                                                            if (g.Orientation == ImageOrientation.None)
                                                            {
                                                                g.Orientation = await imageHelper.GetOrientation(thumbnailPath);
                                                                updated = true;
                                                            }

                                                            if (g.FileSize == 0)
                                                            {
                                                                g.FileSize = fileHelper.FileSize(file);
                                                                updated = true;
                                                            }

                                                            if (updated)
                                                            {
                                                                await databaseHelper.EditGalleryItem(g);
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        logger.LogError(ex, $"An error occurred while scanning file '{file}'");
                                                    }
                                                }
                                            }

                                            if (Path.Exists(Path.Combine(galleryDir, "Pending")))
                                            {
                                                var pendingFiles = fileHelper.GetFiles(Path.Combine(galleryDir, "Pending"), "*.*", SearchOption.TopDirectoryOnly).Where(x => allowedFileTypes.Any(y => string.Equals(Path.GetExtension(x).Trim('.'), y.Trim('.'), StringComparison.OrdinalIgnoreCase)));
                                                if (pendingFiles != null)
                                                {
                                                    foreach (var file in pendingFiles)
                                                    {
                                                        try
                                                        {
                                                            var filename = Path.GetFileName(file);
                                                            if (!galleryItems.Exists(x => string.Equals(x.Title, filename, StringComparison.OrdinalIgnoreCase)))
                                                            {
                                                                await databaseHelper.AddGalleryItem(new GalleryItemModel()
                                                                {
                                                                    GalleryId = galleryItem.Id,
                                                                    Title = filename,
                                                                    Checksum = await fileHelper.GetChecksum(file),
                                                                    MediaType = imageHelper.GetMediaType(file),
                                                                    State = GalleryItemState.Pending,
                                                                    UploadedDate = await fileHelper.GetCreationDatetime(file),
                                                                    FileSize = new FileInfo(file).Length
                                                                });
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            logger.LogError(ex, $"An error occurred while scanning file '{file}'");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"An error occurred while scanning directory '{galleryDir}'");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"DirectoryScanner - ScanGalleryImages - Failed to scan files - {ex?.Message}");
            }
        }

        private async Task ScanCustomResources()
        {
            try
            {
                var existing = await databaseHelper.GetAllCustomResources();

                var customResourcesDirectory = Path.Combine(hostingEnvironment.WebRootPath, Directories.CustomResources);
                fileHelper.CreateDirectoryIfNotExists(customResourcesDirectory);

                foreach (var resource in fileHelper.GetFiles(customResourcesDirectory))
                {
                    try
                    {
                        var filename = Path.GetFileName(resource);
                        if (!existing.Any(x => filename.Equals(x.FileName, StringComparison.OrdinalIgnoreCase)))
                        { 
                            await databaseHelper.AddCustomResource(new CustomResourceModel()
                            {
                                FileName = filename,
                                UploadedBy = "DirectoryScanner",
                                Owner = 0
                            });
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"DirectoryScanner - ScanCustomResources - Failed to scan files - {ex?.Message}");
            }
        }
    }
}