using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.NetCore.Storage.Folders
{
    internal sealed class TempFolder : BaseFolder
    {
        public TempFolder() : base(new ApplicationRootFolder().CreateFolder("Temp", CreationCollisionOption.OpenIfExists)) { }
    }
}