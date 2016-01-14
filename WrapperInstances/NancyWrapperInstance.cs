using System;

using Aragas.Core.Wrappers;

using Nancy;
using Nancy.ErrorHandling;
using Nancy.Hosting.Self;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class CustomStatusCode : IStatusCodeHandler
    {
        public void Handle(HttpStatusCode statusCode, NancyContext context) { context.Response.StatusCode = HttpStatusCode.Forbidden; }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context) =>
                statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.InternalServerError ||
                statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.Unauthorized;
    }
    public class CustomRootPathProvider : IRootPathProvider
    {
        public string GetRootPath() => FileSystemWrapper.ContentFolder.Path;
    }
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override IRootPathProvider RootPathProvider => new CustomRootPathProvider();
    }

    public class ApiNancyModule : NancyModule
    {
        public ApiNancyModule() : base("/api")
        {
            foreach (var pageAction in NancyWrapperInstance.DataApi.List)
                Get[$"/{pageAction.Page}"] = pageAction.Action;
        }
    }


    public class NancyWrapperInstance : INancyWrapper
    {
        public static NancyData DataApi { get; private set; }

        private static NancyHost Server { get; set; }


        public void SetDataApi(NancyData data) { DataApi = data; }

        public void Start(string url, ushort port)
        {
            var config = new HostConfiguration { RewriteLocalhost = false };

            Server?.Stop();
            Server = new NancyHost(new CustomBootstrapper(), config, new Uri($"http://{url}:{port}/"));
            Server.Start();
        }
        public void Stop() { Server?.Dispose(); }
    }
}
