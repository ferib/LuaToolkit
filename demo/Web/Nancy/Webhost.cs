using System;
using System.Collections.Generic;
using System.Text;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace Web.Nancy
{
    public class Webhost : NancyModule
    {
        public Webhost()
        {
            Get("/", async x =>
            {
                return View["public/index.html"];
            });

            Post("/api/test", async x =>
            {
                return "TODO";
            });
        }
    }

#if !DEBUG
    public class MyStatusHandler : IStatusCodeHandler
    {
        public bool HandlesStatusCode(global::Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            return true;
        }

        public void Handle(global::Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            return;
        }
    }
#endif

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            // CORS Enabled
            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                    .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                    .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type")
                    .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");
            });
        }
    }
}
