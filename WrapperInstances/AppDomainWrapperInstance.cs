using System;
using System.Reflection;

using PokeD.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class AppDomainWrapperInstance : IAppDomain
    {
        public Assembly GetCallingAssembly()
        {
            return Assembly.GetCallingAssembly();
        }

        public Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        public Assembly LoadAssembly(byte[] assemblyData)
        {
            return Assembly.Load(assemblyData);
        }
    }
}
