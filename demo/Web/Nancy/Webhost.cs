using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Web.API;
using LuaToolkit;
using LuaToolkit.Decompiler;
using LuaToolkit.Beautifier;
using LuaToolkit.Core;
using Nancy.Extensions;

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

            // Decompiler API (only 5.1)
            Post("/api/decompile", async x =>
            {
                // TODO: add protection to prevent spam?

                // check attached files
                var file = Request.Files.FirstOrDefault();
                if (file != null)
                {
                    byte[] LuaC = new byte[file.Value.Length];
                    file.Value.Read(LuaC, 0, (int)file.Value.Length);
                    return Response.AsJson<APIResponse<ResponseDecompiler>>(APIHelper.Decompile(LuaC));
                }

                // check body as bytecode
                string content = Request.Body.AsString();
                if(content.Substring(1,3) == "Lua")
                {
                    return Response.AsJson<APIResponse<ResponseDecompiler>>(APIHelper.Decompile(Encoding.UTF8.GetBytes(content)));
                }

                return Response.AsJson<APIResponse<ResponseDecompiler>>(APIHelper.Decompile(null)); // its for error handling
            });

            // Lua Beautifier API
            Post("/api/beautifie", async x =>
            {
                return Response.AsJson<APIResponse<ResponseBeautifier>>(APIHelper.Beautifie());
            });

            // Highlight API
            Post("/api/{version}/highlight", async x =>
            {
                return Response.AsJson<APIResponse<ResponseHighlighter>>(APIHelper.Highlight());
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
