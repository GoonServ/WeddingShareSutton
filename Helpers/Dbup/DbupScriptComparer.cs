using System.Diagnostics.CodeAnalysis;

namespace WeddingShare.Helpers.Dbup
{
    public class DbupScriptComparer : IComparer<string>
    {
        public DbupScriptComparer()
        {
        }

        public int Compare([AllowNull] string x, [AllowNull] string y)
        {
            return GetFileName(x).ToLower().CompareTo(GetFileName(y).ToLower());
        }

        private string GetFileName(string name)
        {
            try
            {
                var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts != null && parts.Length >= 2)
                {
                    return string.Join(".", parts.Skip(parts.Length - 2).Take(2));
                }
            }
            catch { }

            return name;
        }
    }
}