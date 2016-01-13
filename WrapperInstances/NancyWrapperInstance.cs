using System;
using System.Collections.Generic;
using System.Linq;

using Aragas.Core.Wrappers;

using Nancy;
using Nancy.Bootstrapper;
using Nancy.ErrorHandling;
using Nancy.Hosting.Self;

namespace PokeD.Server.Desktop.WrapperInstances
{
    // Not sure if this is well done.

    public class CustomStatusCode : IStatusCodeHandler
    {
        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
            =>
                statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.InternalServerError ||
                statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.Unauthorized;

        public void Handle(HttpStatusCode statusCode, NancyContext context) { context.Response.StatusCode = HttpStatusCode.Forbidden; }
    }

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        /// <summary>
        /// Register only NancyModules found in this assembly
        /// </summary>
        protected override IEnumerable<ModuleRegistration> Modules => GetType().Assembly.GetTypes().Where(type => type.BaseType == typeof (NancyModule)).Select(type => new ModuleRegistration(type));

    }
    public class NancyWrapperInstance : NancyModule, INancyWrapper
    {
        private static NancyHost Server { get; set; }
        public static NancyData Data { private get; set; }


        public NancyWrapperInstance()
        {
            foreach (var pageAction in Data.List)
                Get[$"/{pageAction.Page}"] = pageAction.Action;
        }


        public void Start(string url, ushort port)
        {
            var config = new HostConfiguration { RewriteLocalhost = false };

            Server?.Stop();
            Server = new NancyHost(new Bootstrapper(), config, new Uri($"http://{url}:{port}/api/"));
            Server.Start();
        }
    }

    public class NancyCreatorWrapperInstance : INancyCreatorWrapper
    {
        public INancyWrapper CreateNancyWrapper(NancyData data) { NancyWrapperInstance.Data = data; return new NancyWrapperInstance(); }
    }
}
