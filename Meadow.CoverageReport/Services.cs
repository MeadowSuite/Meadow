using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Meadow.CoverageReport
{
    public static class Services
    {
        public static readonly ServiceProvider Provider;

        static readonly string ApplicationName = typeof(Services).Assembly.GetName().Name;
        static readonly IFileProvider FileProvider = new EmbeddedFileProvider(typeof(Services).Assembly);

        static Services()
        {
            var services = new ServiceCollection();

            services.AddSingleton<DiagnosticSource, SilentDiagnosticSource>();
            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment { ApplicationName = ApplicationName });
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddLogging();

            services.AddSingleton<IViewRender, ViewRender>();
            services.AddSingleton<CoveragePageRenderer>();

            services
                .AddMvcCore()
                .AddRazorViewEngine(options =>
                {
                    options.ViewLocationFormats.Add("/Views/{0}.cshtml");
                    options.FileProviders.Clear();
                    options.FileProviders.Add(FileProvider);
                });


            Provider = services.BuildServiceProvider();
        }

        internal class SilentDiagnosticSource : DiagnosticSource
        {
            public override void Write(string name, object value)
            {
                // Do nothing
            }

            public override bool IsEnabled(string name) => true;
        }

    }
}
