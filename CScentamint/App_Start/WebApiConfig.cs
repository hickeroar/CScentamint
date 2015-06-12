using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace CScentamint
{
    public static class WebApiConfig
    {
        private class TextMediaTypeFormatter : MediaTypeFormatter
        {
            public TextMediaTypeFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
            }

            public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
            {
                var taskCompletionSource = new TaskCompletionSource<object>();
                try
                {
                    var memoryStream = new MemoryStream();
                    readStream.CopyTo(memoryStream);
                    var s = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                    taskCompletionSource.SetResult(s);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
                return taskCompletionSource.Task;
            }

            public override bool CanReadType(Type type)
            {
                return type == typeof(string);
            }

            public override bool CanWriteType(Type type)
            {
                return false;
            }
        }

        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{category}",
                defaults: new { controller = "Home", category = RouteParameter.Optional }
            );

            config.Formatters.Clear();
            config.Formatters.Add(new TextMediaTypeFormatter());
            config.Formatters.Add(new JsonMediaTypeFormatter());
            
            // Uncomment the following line of code to enable query support for actions with an IQueryable or IQueryable<T> return type.
            // To avoid processing unexpected or malicious queries, use the validation settings on QueryableAttribute to validate incoming queries.
            // For more information, visit http://go.microsoft.com/fwlink/?LinkId=279712.
            //config.EnableQuerySupport();

            // To disable tracing in your application, please comment out or remove the following line of code
            // For more information, refer to: http://www.asp.net/web-api
            config.EnableSystemDiagnosticsTracing();
        }
    }
}
