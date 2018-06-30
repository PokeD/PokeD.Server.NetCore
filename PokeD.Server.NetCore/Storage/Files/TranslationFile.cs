using System.Globalization;

using NGettext;

using PCLExt.FileStorage;

using PokeD.Server.NetCore.Storage.Folders;

namespace PokeD.Server.NetCore.Storage.Files
{
    public class TranslationFile : BaseFile, ICatalog
    {
        private ICatalog Catalog { get; }

        private static IFile GetFile(string fileName, CultureInfo cultureInfo = null)
        {
            if(cultureInfo == null)
                cultureInfo = CultureInfo.CurrentUICulture;

            return new TranslationFolder()
                       .CreateFolder(cultureInfo.Name, CreationCollisionOption.OpenIfExists)
                       .CheckExists(fileName) == ExistenceCheckResult.FileExists
                ? new TranslationFolder()
                    .CreateFolder(cultureInfo.Name, CreationCollisionOption.OpenIfExists)
                    .GetFile(fileName)
                : new NonExistingFile();
        }
        public TranslationFile(string fileName, CultureInfo cultureInfo = null) : base(GetFile(fileName, cultureInfo))
        {
            Catalog = Exists ? new Catalog(Open(FileAccess.Read)) : new Catalog();
        }

        public string GetString(string text) => Catalog.GetString(text);
        public string GetString(string text, params object[] args) => Catalog.GetString(text, args);
        public string GetPluralString(string text, string pluralText, long n) => Catalog.GetPluralString(text, pluralText, n);
        public string GetPluralString(string text, string pluralText, long n, params object[] args) => Catalog.GetPluralString(text, pluralText, n, args);
        public string GetParticularString(string context, string text) => Catalog.GetParticularString(context, text);
        public string GetParticularString(string context, string text, params object[] args) => Catalog.GetParticularString(context, text, args);
        public string GetParticularPluralString(string context, string text, string pluralText, long n) => Catalog.GetParticularPluralString(context, text, pluralText, n);
        public string GetParticularPluralString(string context, string text, string pluralText, long n, params object[] args) => Catalog.GetParticularPluralString(context, text, pluralText, n, args);
    }
}