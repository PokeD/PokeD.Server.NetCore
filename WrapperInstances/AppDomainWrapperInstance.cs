using System;
using System.Reflection;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class AppDomainWrapperInstance : IAppDomain
    {
        public Assembly GetAssembly(Type type) { return Assembly.GetAssembly(type); }

        public Assembly[] GetAssemblies() { return AppDomain.CurrentDomain.GetAssemblies(); }

        public Assembly LoadAssembly(byte[] assemblyData) { return Assembly.Load(assemblyData); }
    }
}
