﻿using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using WeddingShare.Constants;
using WeddingShare.Enums;
using WeddingShare.Models.Database;

namespace WeddingShare.Helpers.Database
{
    public class SQLiteDatabaseHelper : IDatabaseHelper
    {
        private readonly string _connString;
        private readonly ILogger _logger;

        public SQLiteDatabaseHelper(IConfigHelper config, ILogger<SQLiteDatabaseHelper> logger)
        {
            _connString = config.GetOrDefault(Settings.Database.ConnectionString, "Data Source=./config/wedding-share.db");
            _logger = logger;

            _logger.LogInformation($"Using SQLite connection string: '{_connString}'");
        }

        #region Setup
        private async Task<SqliteConnection> GetConnection()
        {
            return await GetConnection(_connString);
        }

        private async Task<SqliteConnection> GetConnection(string connString)
        {
            return await Task.Run(() => { return new SqliteConnection(connString); });
        }

        private SqliteCommand CreateCommand(string cmd, SqliteConnection conn)
        {
            return new SqliteCommand(cmd, conn);
        }

        private async Task<SqliteTransaction> CreateTransaction(SqliteConnection conn)
        {
            return (SqliteTransaction)await conn.BeginTransactionAsync();
        }

        private void ClearPool(SqliteConnection conn)
        {
            SqliteConnection.ClearPool(conn);
        }
        #endregion

        #region Gallery
        public async Task<int> GetGalleryCount()
        {
            int? result = 0;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT COUNT(`id`) AS `count` FROM `galleries`;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                result = (int?)(long?)await cmd.ExecuteScalarAsync();
                await conn.CloseAsync();
            }

            return result ?? 0;
        }

        public async Task<IEnumerable<string>> GetGalleryNames()
        {
            List<string> result = new List<string>();

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` FROM `galleries` AS g ORDER BY `name` ASC;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                var name = !await reader.IsDBNullAsync("name") ? reader.GetString("name") : null;
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    result.Add(name);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Failed to parse gallery model from database - {ex?.Message}");
                            }
                        }
                    }
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<GalleryModel>> GetAllGalleries()
        {
            List<GalleryModel> result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending`, SUM(gi.file_size) AS `total_gallery_size` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` GROUP BY g.`id` ORDER BY `name` ASC;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                { 
                    result = await ReadGalleries(reader);
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<int?> GetGalleryIdByName(string? name)
        {
            int? result = null;

            if (!string.IsNullOrWhiteSpace(name))
            {
                using (var conn = await GetConnection())
                {
                    var cmd = CreateCommand($"SELECT g.`id` FROM `galleries` AS g WHERE UPPER(g.`name`)=UPPER(@Name);", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("Name", name);

                    await conn.OpenAsync();
                    result = (int?)(long?)await cmd.ExecuteScalarAsync();
                    await conn.CloseAsync();
                }
            }

            return result;
        }

        public async Task<int?> GetGalleryId(string identifier)
        {
            int? result = null;

            if (!string.IsNullOrWhiteSpace(identifier))
            {
                using (var conn = await GetConnection())
                {
                    var cmd = CreateCommand($"SELECT g.`id` FROM `galleries` AS g WHERE UPPER(g.`identifier`)=UPPER(@Identifier);", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("Identifier", identifier);

                    await conn.OpenAsync();
                    result = (int?)(long?)await cmd.ExecuteScalarAsync();
                    await conn.CloseAsync();
                }
            }

            return result;
        }

        public async Task<string?> GetGalleryIdentifier(int id)
        {
            return (await this.GetGallery(id))?.Identifier;
        }

        public async Task<string?> GetGalleryName(int id)
        {
            return (await this.GetGallery(id))?.Name;
        }

        public async Task<GalleryModel?> GetGallery(int id)
        {
            GalleryModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending`, SUM(gi.file_size) AS `total_gallery_size` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` {(id > 0 ? "WHERE g.`id`=@Id;" : string.Empty)}", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);

                await conn.OpenAsync();
                if (id > 0)
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleries(reader))?.FirstOrDefault();
                    }
                }
                else
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var galleries = await ReadGalleries(reader);
                        result = new GalleryModel()
                        {
                            Id = 0,
                            Identifier = "all",
                            Name = "all",
                            SecretKey = null,
                            TotalItems = galleries?.Sum(x => x.TotalItems) ?? 0,
                            ApprovedItems = galleries?.Sum(x => x.ApprovedItems) ?? 0,
                            PendingItems = galleries?.Sum(x => x.PendingItems) ?? 0,
                            TotalGallerySize =  galleries?.Sum(x => x.TotalGallerySize) ?? 0,
                            Owner = 1
                        };
                    }
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryModel?> AddGallery(GalleryModel model)
        {
            GalleryModel? result = null;

            if (ProtectedValues.GalleryNames.Any(x => x.Equals(model.Name?.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                // Prevent users from creating galleries with the same name as a protected gallery
                return null;
            }

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"INSERT INTO `galleries` (`identifier`, `name`, `secret_key`, `owner`) VALUES (@Identifier, @Name, @SecretKey, @Owner); SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending`, SUM(gi.file_size) AS `total_gallery_size` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=last_insert_rowid();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Identifier", model.Identifier);
                cmd.Parameters.AddWithValue("Name", model.Name.ToLower());
                cmd.Parameters.AddWithValue("SecretKey", !string.IsNullOrWhiteSpace(model.SecretKey) ? model.SecretKey : DBNull.Value);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("Owner", model.Owner);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleries(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            if (result != null) 
            {
                await this.AddSetting(new SettingModel() 
                {
                    Id = Settings.Gallery.SecretKey,
                    Value = model.SecretKey
                }, result.Id);
            }

            return result;
        }

        public async Task<GalleryModel?> EditGallery(GalleryModel model)
        {
            GalleryModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `galleries` SET `name`=@Name, `secret_key`=@SecretKey, `owner`=@Owner WHERE `id`=@Id; SELECT g.*, COUNT(gi.`id`) AS `total`, SUM(CASE WHEN gi.`state`=@ApprovedState THEN 1 ELSE 0 END) AS `approved`, SUM(CASE WHEN gi.`state`=@PendingState THEN 1 ELSE 0 END) AS `pending`, SUM(gi.file_size) AS `total_gallery_size` FROM `galleries` AS g LEFT JOIN `gallery_items` AS gi ON g.`id` = gi.`gallery_id` WHERE g.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Name", model.Name?.ToLower());
                cmd.Parameters.AddWithValue("SecretKey", !string.IsNullOrWhiteSpace(model.SecretKey) ? model.SecretKey : DBNull.Value);
                cmd.Parameters.AddWithValue("ApprovedState", (int)GalleryItemState.Approved);
                cmd.Parameters.AddWithValue("PendingState", (int)GalleryItemState.Pending);
                cmd.Parameters.AddWithValue("Owner", model.Owner);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleries(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> WipeGallery(GalleryModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_settings` WHERE `gallery_id`=@Id; DELETE FROM `gallery_items` WHERE `gallery_id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> WipeAllGalleries()
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_settings`; DELETE FROM `gallery_items`; DELETE FROM `galleries` WHERE `id` > 1;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteGallery(GalleryModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_settings` WHERE `gallery_id`=@Id; DELETE FROM `gallery_items` WHERE `gallery_id`=@Id; DELETE FROM `galleries` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region Gallery Items
        public async Task<IDictionary<string, long>> GetGalleryItemCount(int? galleryId, GalleryItemState state = GalleryItemState.All, MediaType type = MediaType.All, ImageOrientation orientation = ImageOrientation.None)
        {
            var results = new Dictionary<string, long>();

            using (var conn = await GetConnection())
            {
                var query = $"SELECT `state`, COUNT(gi.`id`) AS `count` FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE {(galleryId != null && galleryId > 0 ? "gi.`gallery_id`=@Id" : "gi.`gallery_id` > 0")}{(type != MediaType.All ? " AND gi.`media_type`=@Type" : string.Empty)}{(orientation != ImageOrientation.None ? " AND gi.`orientation`=@Orientation" : string.Empty)}{(state != GalleryItemState.All ? " AND gi.`state`=@State" : string.Empty)} GROUP BY `state`;";

                var cmd = CreateCommand(query, conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", galleryId);
                cmd.Parameters.AddWithValue("Type", type);
                cmd.Parameters.AddWithValue("Orientation", orientation);
                cmd.Parameters.AddWithValue("State", state);

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var key = !await reader.IsDBNullAsync("state") ? (GalleryItemState)reader.GetInt32("state") : GalleryItemState.Pending;
                            var value = !await reader.IsDBNullAsync("count") ? reader.GetInt64("count") : 0;
                            results.Add(key.ToString(), value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to parse gallery item model from database - {ex?.Message}");
                        }
                    }
                }

                await conn.CloseAsync();
            }

            foreach (var s in Enum.GetNames(typeof(GalleryItemState)))
            {
                if (!results.ContainsKey(s))
                {
                    results.Add(s, s.Equals("All", StringComparison.OrdinalIgnoreCase) ? results.Sum(x => x.Value) : 0);
                }
            }

            return results;
        }

        public async Task<List<GalleryItemModel>> GetAllGalleryItems(int? galleryId, GalleryItemState state = GalleryItemState.All, MediaType type = MediaType.All, ImageOrientation orientation = ImageOrientation.None, GalleryGroup group = GalleryGroup.None, GalleryOrder order = GalleryOrder.Descending, int limit = int.MaxValue, int page = 1)
        {
            List<GalleryItemModel> result;

            using (var conn = await GetConnection())
            {
                var query = $"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE {(galleryId != null && galleryId > 0 ? "gi.`gallery_id`=@Id" : "gi.`gallery_id` > 0")}{(type != MediaType.All ? " AND gi.`media_type`=@Type" : string.Empty)}{(orientation != ImageOrientation.None ? " AND gi.`orientation`=@Orientation" : string.Empty)}{(state != GalleryItemState.All ? " AND gi.`state`=@State" : string.Empty)};";
                switch (group)
                {
                    case GalleryGroup.Date:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`uploaded_date` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                        break;
                    case GalleryGroup.Uploader:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`uploaded_by` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                        break;
                    case GalleryGroup.MediaType:
                        query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`media_type` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                        break;
                    case GalleryGroup.None:
                        switch (order)
                        {
                            case GalleryOrder.Random:
                                query = $"{query.TrimEnd(' ', ';')} ORDER BY RANDOM();";
                                break;
                            default:
                                query = $"{query.TrimEnd(' ', ';')} ORDER BY gi.`uploaded_date` {(order == GalleryOrder.Ascending ? "ASC" : "DESC")};";
                                break;
                        }
                        break;
                    default:
                        break;
                }

                if (limit > 0 && page > 0)
                { 
                    query = $"{query.TrimEnd(' ', ';')} LIMIT @Limit OFFSET @Offset;";
                }
                
                var cmd = CreateCommand(query, conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", galleryId);
                cmd.Parameters.AddWithValue("Type", type);
                cmd.Parameters.AddWithValue("Orientation", orientation);
                cmd.Parameters.AddWithValue("State", state);
                cmd.Parameters.AddWithValue("Limit", limit);
                cmd.Parameters.AddWithValue("Offset", ((page - 1) * limit));

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = await ReadGalleryItems(reader);
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<GalleryItemModel>> GetPendingGalleryItems(int? galleryId = null)
        {
            List<GalleryItemModel> result;
          
            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON g.`id` = gi.`gallery_id` WHERE gi.`state`=@State {(galleryId != null && galleryId > 0 ? "AND gi.`gallery_id`=@GalleryId" : string.Empty)};", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("GalleryId", galleryId);
                cmd.Parameters.AddWithValue("State", (int)GalleryItemState.Pending);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = await ReadPendingGalleryItems(reader);
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> GetPendingGalleryItem(int id)
        {
            GalleryItemModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON g.`id` = gi.`gallery_id` WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadPendingGalleryItems(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<int> GetPendingGalleryItemCount(int? galleryId = null)
        {
            int result = 0;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT COUNT(`id`) FROM `gallery_items` {(galleryId != null && galleryId > 0 ? "WHERE `gallery_id`=@GalleryId" : string.Empty)};", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("GalleryId", galleryId);


                await conn.OpenAsync();
                result = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> GetGalleryItem(int id)
        {
            GalleryItemModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id`  WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadGalleryItems(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> GetGalleryItemByChecksum(int galleryId, string checksum) 
        {
            GalleryItemModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id`  WHERE g.`id`=@Id AND gi.`checksum`=@Checksum;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", galleryId);
                cmd.Parameters.AddWithValue("Checksum", checksum);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadGalleryItems(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<GalleryItemModel?> AddGalleryItem(GalleryItemModel model)
        {
            GalleryItemModel? result = null;

            if (model.GalleryId > 0)
            { 
                using (var conn = await GetConnection())
                {
                    var cmd = CreateCommand($"INSERT INTO `gallery_items` (`gallery_id`, `title`, `state`, `uploaded_by`, `uploader_email`, `uploaded_date`, `checksum`, `media_type`, `orientation`, `file_size`) VALUES (@GalleryId, @Title, @State, @UploadedBy, @UploaderEmailAddress, @UploadedDate, @Checksum, @MediaType, @Orientation, @FileSize); SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE gi.`id`=last_insert_rowid();", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("GalleryId", model.GalleryId);
                    cmd.Parameters.AddWithValue("Title", model.Title);
                    cmd.Parameters.AddWithValue("State", (int)model.State);
                    cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                    cmd.Parameters.AddWithValue("UploaderEmailAddress", !string.IsNullOrWhiteSpace(model.UploaderEmailAddress) ? model.UploaderEmailAddress : DBNull.Value);
                    cmd.Parameters.AddWithValue("UploadedDate", ((model.UploadedDate ?? DateTime.UtcNow) - new DateTime(1970, 1, 1)).TotalSeconds);
                    cmd.Parameters.AddWithValue("Checksum", !string.IsNullOrWhiteSpace(model.Checksum) ? model.Checksum : DBNull.Value);
                    cmd.Parameters.AddWithValue("MediaType", (int)model.MediaType);
                    cmd.Parameters.AddWithValue("Orientation", (int)model.Orientation);
                    cmd.Parameters.AddWithValue("FileSize", (int)model.FileSize);

                    await conn.OpenAsync();
                    var tran = await CreateTransaction(conn);

                    try
                    {
                        cmd.Transaction = tran;
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            result = (await ReadGalleryItems(reader))?.FirstOrDefault();
                        }
                        await tran.CommitAsync();
                    }
                    catch
                    {
                        await tran.RollbackAsync();
                    }

                    await conn.CloseAsync();
                }
            }

            return result;
        }

        public async Task<GalleryItemModel?> EditGalleryItem(GalleryItemModel model)
        {
            GalleryItemModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `gallery_items` SET `title`=@Title, `state`=@State, `uploaded_by`=@UploadedBy, `uploader_email`=@UploaderEmailAddress, `uploaded_date`=@UploadedDate, `checksum`=@Checksum, `media_type`=@MediaType, `orientation`=@Orientation, `file_size`=@FileSize  WHERE `id`=@Id; SELECT g.`name` AS `gallery_name`, gi.* FROM `gallery_items` AS gi LEFT JOIN `galleries` AS g ON gi.`gallery_id` = g.`id` WHERE gi.`id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Title", model.Title);
                cmd.Parameters.AddWithValue("State", (int)model.State);
                cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrWhiteSpace(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                cmd.Parameters.AddWithValue("UploaderEmailAddress", !string.IsNullOrWhiteSpace(model.UploaderEmailAddress) ? model.UploaderEmailAddress : DBNull.Value);
                cmd.Parameters.AddWithValue("UploadedDate", model.UploadedDate != null ? ((DateTime)model.UploadedDate - new DateTime(1970, 1, 1)).TotalSeconds : (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
                cmd.Parameters.AddWithValue("Checksum", !string.IsNullOrWhiteSpace(model.Checksum) ? model.Checksum : DBNull.Value);
                cmd.Parameters.AddWithValue("MediaType", (int)model.MediaType);
                cmd.Parameters.AddWithValue("Orientation", (int)model.Orientation);
                cmd.Parameters.AddWithValue("FileSize", (int)model.FileSize);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadGalleryItems(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteGalleryItem(GalleryItemModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"DELETE FROM `gallery_items` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region Users
        public async Task<bool> InitOwnerAccount(UserModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `username`=@Username, `password`=@Password, `level`=@Level WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", 1);
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Password", model.Password);
                cmd.Parameters.AddWithValue("Level", (int)UserLevel.Owner);

                await conn.OpenAsync();
                result = await cmd.ExecuteNonQueryAsync() > 0;
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> ValidateCredentials(string username, string password)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT COUNT(`id`) FROM `users` WHERE `username`=@Username AND `password`=@Password;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", 1);
                cmd.Parameters.AddWithValue("Username", username.ToLower());
                cmd.Parameters.AddWithValue("Password", password);

                await conn.OpenAsync();
                result = (long)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<UserModel>?> GetAllUsers()
        {
            List<UserModel> result = new List<UserModel>();

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `users` ORDER BY UPPER(`username`) ASC;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadUsers(reader));
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> GetUser(int id)
        {
            UserModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadUsers(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> GetUser(string username)
        {
            UserModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `users` WHERE `username`=@Username;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Username", username.ToLower());

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadUsers(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> AddUser(UserModel model)
        {
            UserModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"INSERT INTO `users` (`username`, `email`, `password`, `state`, `level`) VALUES (@Username, @Email, @Password, @State, @Level); SELECT * FROM `users` WHERE `id`=last_insert_rowid();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Email", !string.IsNullOrEmpty(model.Email) ? model.Email : DBNull.Value);
                cmd.Parameters.AddWithValue("State", AccountState.Active);
                cmd.Parameters.AddWithValue("Password", model.Password);
                cmd.Parameters.AddWithValue("Level", model.Level);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadUsers(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<UserModel?> EditUser(UserModel model)
        {
            UserModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `username`=@Username, `email`=@Email, `state`=@State, `level`=@Level, `failed_logins`=@FailedLogins, `lockout_until`=@LockoutUntil WHERE `id`=@Id; SELECT * FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Username", model.Username.ToLower());
                cmd.Parameters.AddWithValue("Email", !string.IsNullOrEmpty(model.Email) ? model.Email : DBNull.Value);
                cmd.Parameters.AddWithValue("State", (int)model.State);
                cmd.Parameters.AddWithValue("Level", (int)model.Level);
                cmd.Parameters.AddWithValue("FailedLogins", model.FailedLogins);
                cmd.Parameters.AddWithValue("LockoutUntil", model.LockoutUntil != null ? ((DateTime)model.LockoutUntil - new DateTime(1970, 1, 1)).TotalSeconds : DBNull.Value);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadUsers(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteUser(UserModel model)
        {
            bool result = false;

            if (model.Id > 1)
            { 
                using (var conn = await GetConnection())
                {
                    var cmd = CreateCommand($"DELETE FROM `users` WHERE `id`=@Id;", conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("Id", model.Id);

                    await conn.OpenAsync();
                    var tran = await CreateTransaction(conn);

                    try
                    {
                        cmd.Transaction = tran;
                        result = (await cmd.ExecuteNonQueryAsync()) > 0;
                        await tran.CommitAsync();
                    }
                    catch
                    {
                        await tran.RollbackAsync();
                    }

                    await conn.CloseAsync();
                }
            }

            return result;
        }

        public async Task<bool> ChangePassword(UserModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `password`=@Password WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("Password", model.Password);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (int)(await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<int> IncrementLockoutCount(int id)
        {
            int result = 0;

            var user = await this.GetUser(id);
            if (user != null)
            {
                user.FailedLogins++;
                result = (await this.EditUser(user))?.FailedLogins ?? 0;
            }

            return result;
        }

        public async Task<bool> SetLockout(int id, DateTime? datetime) 
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                if (datetime != null)
                {
                    var lockout = (DateTime)datetime;
                    datetime = new DateTime(lockout.Year, lockout.Month, lockout.Day, lockout.Hour, lockout.Minute, 0, DateTimeKind.Utc);
                }

                var cmd = CreateCommand($"UPDATE `users` SET `lockout_until`=@LockoutUntil WHERE `id`=@Id; SELECT `lockout_until` FROM `users` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("LockoutUntil", datetime != null ? ((DateTime)datetime - new DateTime(1970, 1, 1)).TotalSeconds : DBNull.Value);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    { 
                        if (reader != null && reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    result = (!await reader.IsDBNullAsync("lockout_until") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("lockout_until")) : null) == datetime;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Failed to parse user lockout from database - {ex?.Message}");
                                }
                            }
                        }
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> ResetLockoutCount(int id)
        {
            bool result = false;

            var user = await this.GetUser(id);
            if (user != null)
            {
                user.FailedLogins = 0;
                result = ((await this.EditUser(user))?.FailedLogins ?? 0) == 0;
            }

            return result;
        }

        public async Task<bool> SetMultiFactorToken(int id, string token)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `2fa_token`=@Token WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("Token", token);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = await cmd.ExecuteNonQueryAsync() > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> ResetMultiFactorToDefault()
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `users` SET `2fa_token`='';", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = await cmd.ExecuteNonQueryAsync() > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region CustomResources
        public async Task<CustomResourceModel?> GetCustomResource(int id)
        {
            CustomResourceModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `custom_resources` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", id);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadCustomResources(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<CustomResourceModel>> GetAllCustomResources()
        {
            List<CustomResourceModel> result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `custom_resources`;", conn);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadCustomResources(reader));
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<CustomResourceModel?> AddCustomResource(CustomResourceModel model)
        {
            CustomResourceModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"INSERT INTO `custom_resources` (`file_name`, `uploaded_by`, `owner`) VALUES (@FileName, @UploadedBy, @Owner); SELECT * FROM `custom_resources` WHERE `id`=last_insert_rowid();", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("FileName", model.FileName);
                cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrEmpty(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                cmd.Parameters.AddWithValue("Owner", model.Owner);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadCustomResources(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<CustomResourceModel?> EditCustomResource(CustomResourceModel model)
        {
            CustomResourceModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `custom_resource` SET `file_name`=@FileName, `uploaded_by`=@UploadedBy, `owner`=@Owner WHERE `id`=@Id; SELECT * FROM `custom_resources` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("FileName", model.FileName);
                cmd.Parameters.AddWithValue("UploadedBy", !string.IsNullOrEmpty(model.UploadedBy) ? model.UploadedBy : DBNull.Value);
                cmd.Parameters.AddWithValue("Owner", model.Owner);

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadCustomResources(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteCustomResource(CustomResourceModel model)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"UPDATE `settings` SET `value`='' WHERE `value`=@FileName; DELETE FROM `gallery_settings` WHERE `id`=@BannerImageKey AND `value`=@FileName; DELETE FROM `custom_resources` WHERE `id`=@Id;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Id", model.Id);
                cmd.Parameters.AddWithValue("BannerImageKey", Settings.Gallery.BannerImage);
                cmd.Parameters.AddWithValue("FileName", $"/custom_resources/{model.FileName}");

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region Settings
        public async Task<IEnumerable<SettingModel>?> GetAllSettings(int? galleryId = null)
        {
            List<SettingModel> result = new List<SettingModel>();

            using (var conn = await GetConnection())
            {
                await conn.OpenAsync();

                // Get Default Settings
                var cmd = CreateCommand($"SELECT `id`, `value` FROM `settings`;", conn);
                cmd.CommandType = CommandType.Text;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = await ReadSettings(reader);
                }

                if (result != null && galleryId != null)
                { 
                    // Get Gallery Overrides
                    cmd = CreateCommand($"SELECT `id`, `value` FROM `gallery_settings` WHERE `gallery_id`=@GalleryId;", conn);
                    cmd.Parameters.AddWithValue("GalleryId", (int)galleryId);
                    cmd.CommandType = CommandType.Text;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var overrides = (await ReadSettings(reader))?.Where(x => !string.IsNullOrWhiteSpace(x.Value));
                        if (overrides != null && overrides.Any())
                        {
                            result = result.Where(x => !overrides.Any(o => o.Id.Equals(x.Id, StringComparison.OrdinalIgnoreCase))).ToList();
                            result.AddRange(overrides);
                        }
                    }
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<SettingModel?> GetSetting(string id)
        {
            return await GetSetting(id, 0);
        }

        public async Task<SettingModel?> GetSetting(string id, int galleryId)
        {
            SettingModel? result;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT `id`, `value` FROM (SELECT `id`, `value`, '2' AS `priority` FROM `settings` WHERE `id`=@Id UNION SELECT `id`, `value`, '1' AS `priority` FROM `gallery_settings` WHERE `id`=@Id AND `gallery_id`=@GalleryId) ORDER BY `priority` ASC LIMIT 1;", conn);

                cmd.Parameters.AddWithValue("Id", id.ToUpper());
                cmd.Parameters.AddWithValue("GalleryId", galleryId);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadSettings(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<SettingModel?> GetGallerySpecificSetting(string id, int galleryId)
        {
            SettingModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT `id`, `value` FROM `gallery_settings` WHERE `id`=@Id AND `gallery_id`=@GalleryId;", conn);

                cmd.Parameters.AddWithValue("Id", id.ToUpper());
                cmd.Parameters.AddWithValue("GalleryId", galleryId);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = (await ReadSettings(reader))?.FirstOrDefault();
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<SettingModel?> AddSetting(SettingModel model, int? galleryId = null)
        {
            SettingModel? result = null;

            using (var conn = await GetConnection())
            {
                SqliteCommand cmd;
                if (galleryId != null)
                {
                    cmd = CreateCommand($"INSERT INTO `gallery_settings` (`id`, `gallery_id`, `value`) VALUES (@Id, @GalleryId, @Value); SELECT * FROM `gallery_settings` WHERE `id`=last_insert_rowid() AND `gallery_id`=@GalleryId;", conn);
                    cmd.Parameters.AddWithValue("GalleryId", galleryId);
                }
                else
                {
                    cmd = CreateCommand($"INSERT INTO `settings` (`id`, `value`) VALUES (@Id, @Value); SELECT * FROM `settings` WHERE `id`=last_insert_rowid();", conn);
                }

                cmd.Parameters.AddWithValue("Id", model.Id.ToUpper());
                cmd.Parameters.AddWithValue("Value", !string.IsNullOrEmpty(model.Value) ? model.Value : DBNull.Value);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadSettings(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            if (result == null)
            {
                result = galleryId != null ? await this.GetSetting(model.Id, galleryId.Value) : await this.GetSetting(model.Id);
            }

            return result;
        }

        public async Task<SettingModel?> EditSetting(SettingModel model, int? galleryId = null)
        {
            SettingModel? result = null;

            using (var conn = await GetConnection())
            {
                SqliteCommand cmd;
                if (galleryId != null)
                {
                    cmd = CreateCommand($"UPDATE `gallery_settings` SET `value`=@Value WHERE `id`=@Id AND `gallery_id`=@GalleryId; SELECT * FROM `gallery_settings` WHERE `id`=@Id AND `gallery_id`=@GalleryId;", conn);
                    cmd.Parameters.AddWithValue("GalleryId", galleryId);
                }
                else
                {
                    cmd = CreateCommand($"UPDATE `settings` SET `value`=@Value WHERE `id`=@Id; SELECT * FROM `settings` WHERE `id`=@Id;", conn);
                }

                cmd.Parameters.AddWithValue("Id", model.Id.ToUpper());
                cmd.Parameters.AddWithValue("Value", !string.IsNullOrEmpty(model.Value) ? model.Value : DBNull.Value);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadSettings(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<SettingModel?> SetSetting(SettingModel model, int? galleryId = null)
        {
            if (!string.IsNullOrWhiteSpace(model.Id))
            {
                try
                {
                    if (galleryId != null)
                    {
                        // Gallery Override
                        var result = await GetGallerySpecificSetting(model.Id, galleryId.Value);
                        if (result == null && !string.IsNullOrEmpty(model.Value))
                        {
                            return await AddSetting(new SettingModel()
                            {
                                Id = model.Id.ToUpper(),
                                Value = model.Value
                            }, galleryId);
                        }
                        else if (result != null && !string.IsNullOrEmpty(model.Value))
                        {
                            return await EditSetting(new SettingModel()
                            {
                                Id = model.Id.ToUpper(),
                                Value = model.Value
                            }, galleryId);
                        }
                        else if (result != null && string.IsNullOrEmpty(model.Value))
                        {
                            await DeleteSetting(new SettingModel()
                            {
                                Id = model.Id.ToUpper(),
                                Value = model.Value
                            }, galleryId);
                        }
                    }
                    else
                    {
                        // Default Setting
                        var result = await GetSetting(model.Id);
                        if (result == null && !string.IsNullOrEmpty(model.Value))
                        {
                            return await AddSetting(new SettingModel()
                            {
                                Id = model.Id.ToUpper(),
                                Value = model.Value
                            });
                        }
                        else if (result != null && !string.IsNullOrEmpty(model.Value))
                        {
                            return await EditSetting(new SettingModel()
                            {
                                Id = model.Id.ToUpper(),
                                Value = model.Value
                            });
                        }
                        else if (result != null && string.IsNullOrEmpty(model.Value))
                        {
                            await DeleteSetting(new SettingModel()
                            {
                                Id = model.Id.ToUpper(),
                                Value = model.Value
                            });
                        }
                    }
                }
                catch { }
            }

            return new SettingModel()
            {
                Id = model.Id.ToUpper(),
                Value = null
            };
        }

        public async Task<bool> DeleteSetting(SettingModel model, int? galleryId = null)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                SqliteCommand cmd;
                if (galleryId != null)
                {
                    cmd = CreateCommand($"DELETE FROM `gallery_settings` WHERE `id`=@Id AND `gallery_id`=@GalleryId;", conn);
                    cmd.Parameters.AddWithValue("GalleryId", galleryId);
                }
                else
                {
                    cmd = CreateCommand($"DELETE FROM `gallery_settings` WHERE `id`=@Id; DELETE FROM `settings` WHERE `id`=@Id;", conn);
                }

                cmd.Parameters.AddWithValue("Id", model.Id.ToUpper());
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<bool> DeleteAllSettings(int? galleryId = null)
        {
            bool result = false;

            using (var conn = await GetConnection())
            {
                SqliteCommand cmd;
                if (galleryId != null)
                { 
                    cmd = CreateCommand($"DELETE FROM `gallery_settings` WHERE `gallery_id`=@GalleryId;", conn);
                    cmd.Parameters.AddWithValue("GalleryId", galleryId);
                }
                else
                {
                    cmd = CreateCommand($"DELETE FROM `gallery_settings`; DELETE FROM `settings`;", conn);
                }

                cmd.CommandType = CommandType.Text;
                
                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    result = (await cmd.ExecuteNonQueryAsync()) > 0;
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region Audit Logs
        public async Task<IEnumerable<AuditLogModel>?> GetAuditLogs(string term = "", int limit = 100)
        {
            List<AuditLogModel> result = new List<AuditLogModel>();

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"SELECT * FROM `audit_logs` WHERE `username` LIKE @Term OR `message` LIKE @Term ORDER BY `id` DESC LIMIT @Limit;", conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("Term", $"%{term?.Trim()}%");
                cmd.Parameters.AddWithValue("Limit", limit);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    result = await ReadAuditLogs(reader);
                }
                await conn.CloseAsync();
            }

            return result;
        }

        public async Task<AuditLogModel?> AddAuditLog(AuditLogModel model)
        {
            AuditLogModel? result = null;

            using (var conn = await GetConnection())
            {
                var cmd = CreateCommand($"INSERT INTO `audit_logs` (`message`, `username`, `timestamp`) VALUES (@Message, @Username, @Timestamp); SELECT * FROM `audit_logs` WHERE `id`=last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("Message", model.Message);
                cmd.Parameters.AddWithValue("Username", model.Username?.ToLower());
                cmd.Parameters.AddWithValue("Timestamp", ((DateTime)DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
                cmd.CommandType = CommandType.Text;

                await conn.OpenAsync();
                var tran = await CreateTransaction(conn);

                try
                {
                    cmd.Transaction = tran;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        result = (await ReadAuditLogs(reader))?.FirstOrDefault();
                    }
                    await tran.CommitAsync();
                }
                catch
                {
                    await tran.RollbackAsync();
                }

                await conn.CloseAsync();
            }

            return result;
        }
        #endregion

        #region Backups
        public async Task<bool> Import(string path)
        {
            bool result = false;

            try
            {
                using (var backup = await GetConnection(path))
                using (var conn = await GetConnection())
                {
                    await backup.OpenAsync();
                    await conn.OpenAsync();

                    backup.BackupDatabase(conn);

                    await conn.CloseAsync();
                    await backup.CloseAsync();

                    ClearPool(backup);
                }

                result = true;
            }
            catch { }

            return result;
        }

        public async Task<bool> Export(string path)
        {
            bool result = false;

            try
            {
                using (var conn = await GetConnection())
                using (var backup = await GetConnection(path))
                {
                    await conn.OpenAsync();
                    await backup.OpenAsync();

                    conn.BackupDatabase(backup);

                    await backup.CloseAsync();
                    await conn.CloseAsync();
                
                    ClearPool(backup);
                }

                result = true;
            }
            catch { }

            return result;
        }
        #endregion

        #region Data Parsers
        private async Task<List<GalleryModel>> ReadGalleries(SqliteDataReader? reader)
        {
            var items = new List<GalleryModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        items.Add(new GalleryModel()
                        {
                            Id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0,
                            Identifier = !await reader.IsDBNullAsync("identifier") ? reader.GetString("identifier") : GalleryHelper.GenerateGalleryIdentifier(),
                            Name = !await reader.IsDBNullAsync("name") ? reader.GetString("name") : "Unknown",
                            SecretKey = !await reader.IsDBNullAsync("secret_key") ? reader.GetString("secret_key") : null,
                            TotalItems = !await reader.IsDBNullAsync("total") ? reader.GetInt32("total") : 0,
                            ApprovedItems = !await reader.IsDBNullAsync("approved") ? reader.GetInt32("approved") : 0,
                            PendingItems = !await reader.IsDBNullAsync("pending") ? reader.GetInt32("pending") : 0,
                            TotalGallerySize = !await reader.IsDBNullAsync("total_gallery_size") ? reader.GetInt64("total_gallery_size") : 0,
                            Owner = !await reader.IsDBNullAsync("owner") ? reader.GetInt32("owner") : 0
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse gallery model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<GalleryItemModel>> ReadGalleryItems(SqliteDataReader? reader)
        {
            var items = new List<GalleryItemModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        { 
                            items.Add(new GalleryItemModel()
                            {
                                Id = id,
                                GalleryId = !await reader.IsDBNullAsync("gallery_id") ? reader.GetInt32("gallery_id") : 0,
                                Title = !await reader.IsDBNullAsync("title") ? reader.GetString("title") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                UploaderEmailAddress = !await reader.IsDBNullAsync("uploader_email") ? reader.GetString("uploader_email") : null,
                                UploadedDate = !await reader.IsDBNullAsync("uploaded_date") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("uploaded_date")) : null,
                                Checksum = !await reader.IsDBNullAsync("checksum") ? reader.GetString("checksum") : null,
                                MediaType = !await reader.IsDBNullAsync("media_type") ? (MediaType)reader.GetInt32("media_type") : MediaType.Unknown,
                                Orientation = !await reader.IsDBNullAsync("orientation") ? (ImageOrientation)reader.GetInt32("orientation") : ImageOrientation.None,
                                State = !await reader.IsDBNullAsync("state") ? (GalleryItemState)reader.GetInt32("state") : GalleryItemState.Pending,
                                FileSize = !await reader.IsDBNullAsync("file_size") ? reader.GetInt32("file_size") : 0,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse gallery item model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<GalleryItemModel>> ReadPendingGalleryItems(SqliteDataReader? reader)
        {
            var items = new List<GalleryItemModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {

                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        { 
                            items.Add(new GalleryItemModel()
                            {
                                Id = id,
                                GalleryId = !await reader.IsDBNullAsync("gallery_id") ? reader.GetInt32("gallery_id") : 0,
                                Title = !await reader.IsDBNullAsync("title") ? reader.GetString("title") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                UploaderEmailAddress = !await reader.IsDBNullAsync("uploader_email") ? reader.GetString("uploader_email") : null,
                                Checksum = !await reader.IsDBNullAsync("checksum") ? reader.GetString("checksum") : null,
                                MediaType = !await reader.IsDBNullAsync("media_type") ? (MediaType)reader.GetInt32("media_type") : MediaType.Unknown,
                                Orientation = !await reader.IsDBNullAsync("orientation") ? (ImageOrientation)reader.GetInt32("orientation") : ImageOrientation.None,
                                State = !await reader.IsDBNullAsync("state") ? (GalleryItemState)reader.GetInt32("state") : GalleryItemState.Pending,
                                FileSize = !await reader.IsDBNullAsync("file_size") ? reader.GetInt32("file_size") : 0,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse pending gallery item model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<UserModel>> ReadUsers(SqliteDataReader? reader)
        {
            var items = new List<UserModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        { 
                            items.Add(new UserModel()
                            {
                                Id = id,
                                Username = !await reader.IsDBNullAsync("failed_logins") ? reader.GetString("username").ToLower() : string.Empty,
                                Email = !await reader.IsDBNullAsync("email") ? reader.GetString("email") : null,
                                State = !await reader.IsDBNullAsync("state") ? (AccountState)reader.GetInt32("state") : AccountState.Active,
                                Level = !await reader.IsDBNullAsync("level") ? (UserLevel)reader.GetInt32("level") : UserLevel.Basic,
                                Password = null,
                                FailedLogins = !await reader.IsDBNullAsync("failed_logins") ? reader.GetInt32("failed_logins") : 0,
                                LockoutUntil = !await reader.IsDBNullAsync("lockout_until") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("lockout_until")) : null,
                                MultiFactorToken = !await reader.IsDBNullAsync("2fa_token") ? reader.GetString("2fa_token") : null
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse user model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<CustomResourceModel>> ReadCustomResources(DbDataReader? reader)
        {
            var items = new List<CustomResourceModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        {
                            items.Add(new CustomResourceModel()
                            {
                                Id = id,
                                FileName = !await reader.IsDBNullAsync("file_name") ? reader.GetString("file_name") : string.Empty,
                                UploadedBy = !await reader.IsDBNullAsync("uploaded_by") ? reader.GetString("uploaded_by") : null,
                                Owner = !await reader.IsDBNullAsync("owner") ? reader.GetInt32("owner") : 0
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse custom resource model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<SettingModel>> ReadSettings(SqliteDataReader? reader)
        {
            var items = new List<SettingModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetString("id").ToUpper() : string.Empty;
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            items.Add(new SettingModel()
                            {
                                Id = id,
                                Value = !await reader.IsDBNullAsync("value") ? reader.GetString("value") : string.Empty
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse setting model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }

        private async Task<List<AuditLogModel>> ReadAuditLogs(SqliteDataReader? reader)
        {
            var items = new List<AuditLogModel>();

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        var id = !await reader.IsDBNullAsync("id") ? reader.GetInt32("id") : 0;
                        if (id > 0)
                        {
                            items.Add(new AuditLogModel()
                            {
                                Id = id,
                                Username = !await reader.IsDBNullAsync("username") ? reader.GetString("username") : string.Empty,
                                Message = !await reader.IsDBNullAsync("message") ? reader.GetString("message") : string.Empty,
                                Timestamp = !await reader.IsDBNullAsync("timestamp") ? DateTime.UnixEpoch.AddSeconds(reader.GetInt32("timestamp")) : default(DateTime)
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to parse audit log model from database - {ex?.Message}");
                    }
                }
            }

            return items;
        }
        #endregion
    }
}