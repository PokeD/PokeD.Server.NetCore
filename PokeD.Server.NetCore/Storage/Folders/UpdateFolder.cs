using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.NetCore.Storage.Folders
{
    internal sealed class UpdateFolder : BaseFolder
    {
        public UpdateFolder() : base(new ApplicationRootFolder().CreateFolder("Update", CreationCollisionOption.OpenIfExists)) { }
    }
}