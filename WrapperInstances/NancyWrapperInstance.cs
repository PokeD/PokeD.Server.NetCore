using System;
using System.Collections.Generic;
using System.Linq;

using Aragas.Core.Wrappers;

using Nancy;
using Nancy.Bootstrapper;
using Nancy.ErrorHandling;
using Nancy.Hosting.Self;
using Nancy.ViewEngines;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class CustomStatusCode : DefaultViewRenderer, IStatusCodeHandler
    {
        public CustomStatusCode(IViewFactory factory) : base(factory) { }


        public void Handle(HttpStatusCode statusCode, NancyContext context) { context.Response.StatusCode = HttpStatusCode.Forbidden; }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context) =>
                statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.InternalServerError ||
                statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.Unauthorized;
    }
    public class CustomRootPathProvider : IRootPathProvider
    {
        public string GetRootPath() => FileSystemWrapper.ContentFolder.Path;
    }


    public class ApiNancyWrapperInstance : NancyModule
    {
        public ApiNancyWrapperInstance() : base("/api")
        {
            foreach (var pageAction in NancyCreatorWrapperInstance.DataApi.List)
                Get[$"/{pageAction.Page}"] = pageAction.Action;
        }
    }

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override IRootPathProvider RootPathProvider => new CustomRootPathProvider();

        protected override IEnumerable<ModuleRegistration> Modules => GetType().Assembly.GetTypes().Where(type => type.BaseType == typeof(NancyModule)).Select(type => new ModuleRegistration(type));
    }
    public class NancyCreatorWrapperInstance : INancyCreatorWrapper
    {
        public static NancyData DataApi { get; private set; }

        private static NancyHost Server { get; set; }


        public void SetDataApi(NancyData data) { DataApi = data; }

        public void Start(string url, ushort port)
        {
            var config = new HostConfiguration { RewriteLocalhost = false };

            Server?.Stop();
            Server = new NancyHost(new Bootstrapper(), config, new Uri($"http://{url}:{port}/"));
            //Server = new NancyHost(config, new Uri($"http://{url}:{port}/api/"));
            Server.Start();
        }
        public void Stop() { Server?.Dispose(); }
    }
}
