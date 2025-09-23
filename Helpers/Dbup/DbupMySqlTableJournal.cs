using System.Data;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.MySql;

namespace WeddingShare.Helpers.Dbup
{
    public class DbupMySqlTableJournal : MySqlTableJournal
    {
        public DbupMySqlTableJournal(Func<IConnectionManager> connectionManager, Func<IUpgradeLog> logger, string scheme, string table)
            : base(connectionManager, logger, scheme, table)
        {
        }

        public override void StoreExecutedScript(SqlScript script, Func<IDbCommand> dbCommandFactory)
        {
            var scriptName = script.Name;

            try
            {
                var parts = script.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts != null && parts.Length >= 2)
                {
                    scriptName = string.Join(".", parts.Skip(parts.Length - 2).Take(2));
                }
            }
            catch { }

            base.StoreExecutedScript(new SqlScript(scriptName, script.Contents), dbCommandFactory);
        }
    }
}