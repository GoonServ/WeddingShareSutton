using WeddingShare.Constants;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Helpers.Dbup;

namespace WeddingShare.Configurations
{
    internal static class DatabaseConfiguration
    {
        public static IDatabaseHelper AddDatabaseConfiguration(this IServiceCollection services, IConfigHelper config, ILoggerFactory loggerFactory)
        {
            IDatabaseHelper helper;

            var databaseType = config.GetOrDefault(Settings.Database.Type, "sqlite");
            switch (databaseType?.ToLower())
            {
                case "mysql":
                    services.AddSingleton<IDatabaseHelper, MySqlDatabaseHelper>();
                    helper = new MySqlDatabaseHelper(config, loggerFactory.CreateLogger<MySqlDatabaseHelper>());
                    break;
                default:
                    services.AddSingleton<IDatabaseHelper, SQLiteDatabaseHelper>();
                    helper = new SQLiteDatabaseHelper(config, loggerFactory.CreateLogger<SQLiteDatabaseHelper>());
                    break;
            }

            services.AddHostedService<DbupMigrator>();

            return helper;
        }
    }
}