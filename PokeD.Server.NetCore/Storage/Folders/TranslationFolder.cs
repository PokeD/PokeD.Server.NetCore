using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.NetCore.Storage.Folders
{
    public class TranslationFolder : BaseFolder
    {
        public TranslationFolder() : base(new ApplicationRootFolder().CreateFolder("Translation", CreationCollisionOption.OpenIfExists))
        {

        }
    }
}