using System;
using System.Collections.Generic;
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

        public IList<IAssembly> GetAssemblies()
        {
            var result = new List<IAssembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                result.Add(new AssemblyWrapper(assembly));

            return result;
        }

        public Assembly LoadAssembly(byte[] assemblyData)
        {
            return Assembly.Load(assemblyData);
        }
    }

    public class AssemblyWrapper : IAssembly
    {
        private readonly Assembly _assembly;

        public AssemblyWrapper(Assembly assembly)
        {
            _assembly = assembly;
        }

        public string GetName()
        {
            return _assembly.GetName().ToString();
        }

        public IList<Type> GetTypes()
        {
            return _assembly.GetTypes();
        }
    }
}
