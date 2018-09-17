using Meadow.CoverageReport.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.CoverageReport
{
    public class CoveragePageRenderer
    {
        public static CoveragePageRenderer Instance => Services.Provider.GetRequiredService<CoveragePageRenderer>();

        readonly IViewRender _viewRender;

        public CoveragePageRenderer(IViewRender viewRender)
        {
            _viewRender = viewRender;
        }

        public string Render<TViewModel>(string viewName, TViewModel model)
        {
            var result = _viewRender.Render(viewName, model);
            return result;
        }

        public string RenderCoverageReport(SourceFileMap sourceFileMap)
        {
            return _viewRender.Render("CoveragePage", sourceFileMap);
        }

        public string RenderIndexPage(IndexViewModel indexViewModel)
        {
            return _viewRender.Render("Index", indexViewModel);
        }
    }


    public class ViewRender : IViewRender
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public ViewRender(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderAsync(string name)
        {
            return await RenderAsync<object>(name, null);
        }

        public string Render<TModel>(string name, TModel model)
        {
            return RenderAsync(name, model).Result;
        }

        public async Task<string> RenderAsync<TModel>(string name, TModel model)
        {
            var actionContext = GetActionContext();

            var viewEngineResult = _viewEngine.FindView(actionContext, name, false);

            if (!viewEngineResult.Success)
            {
                throw new InvalidOperationException($"Couldn't find view '{name}'");
            }

            var view = viewEngineResult.View;

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    new ViewDataDictionary<TModel>(
                        metadataProvider: new EmptyModelMetadataProvider(),
                        modelState: new ModelStateDictionary())
                    {
                        Model = model
                    },
                    new TempDataDictionary(
                        actionContext.HttpContext,
                        _tempDataProvider),
                    output,
                    new HtmlHelperOptions());

                await view.RenderAsync(viewContext);
                return output.ToString();
            }
        }

        private ActionContext GetActionContext()
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }

    public interface IViewRender
    {
        Task<string> RenderAsync(string name);

        Task<string> RenderAsync<TModel>(string name, TModel model);

        string Render<TModel>(string name, TModel model);
    }
}
