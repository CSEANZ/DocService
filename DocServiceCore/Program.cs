using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using System.Web.Http;
using Swashbuckle.Application;

namespace DocServiceCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            new HttpConfiguration()
     .EnableSwagger(c => c.SingleApiVersion("v1", "Transcript Document Generator"))
     .EnableSwaggerUi();

            host.Run();
        }
    }
}
