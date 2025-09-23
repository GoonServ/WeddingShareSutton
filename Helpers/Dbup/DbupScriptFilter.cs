using DbUp.Engine;
using DbUp.Support;
using WeddingShare.Enums;

namespace WeddingShare.Helpers.Dbup
{
    public class DbupScriptFilter : IScriptFilter
    {
        private readonly DatabaseType _dbType;

        public DbupScriptFilter(DatabaseType dbType)
        {
            _dbType = dbType;
        }

        public IEnumerable<SqlScript> Filter(IEnumerable<SqlScript> sorted, HashSet<string> executedScriptNames, ScriptNameComparer comparer)
        {
            var scripts = sorted.Where(s => s.SqlScriptOptions.ScriptType == ScriptType.RunAlways || !executedScriptNames.Contains(s.Name, comparer));
            switch (_dbType)
            {
                case DatabaseType.SQLite:
                    return scripts.Where(s => s.Name.ToLower().Contains(".sqlscripts.sqlite."));
                case DatabaseType.MySQL:
                    return scripts.Where(s => s.Name.ToLower().Contains(".sqlscripts.mysql."));
                default:
                    return new List<SqlScript>();
            }
        }
    }
}