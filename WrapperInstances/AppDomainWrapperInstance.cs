using System;
using System.Reflection;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class AppDomainWrapperInstance : IAppDomain
    {
        public Assembly GetAssembly(Type type) => Assembly.GetAssembly(type);

        public Assembly[] GetAssemblies() => AppDomain.CurrentDomain.GetAssemblies();

        public Assembly LoadAssembly(byte[] assemblyData) => Assembly.Load(assemblyData);
    }
}
