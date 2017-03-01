using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Danubers.QldElectricity.Controllers;
using Danubers.QldElectricity.Factories;
using Danubers.QldElectricity.Jobs;
using FluentScheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;

namespace Danubers.QldElectricity
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Startup
    {
        public IContainer ApplicationContainer { get; private set; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add(new TypeFilterAttribute(typeof(ErrorFilter)));
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IActionContextAccessor, ActionContextAccessor>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Info()
                    {
                        Title = "QLD Energy API v1",
                        Version = "v1",
                        Contact = new Contact() { Url = "http://github.com/danubers" },
                        Description = "API for accessing power information for QLD, Australia"
                    });
                var path = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "swaggerDoc.xml");
                c.IncludeXmlComments(path);
            });

            var builder = new ContainerBuilder();
            builder.RegisterModule<Core>();
            builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>();

            builder.Populate(services);
            ApplicationContainer = builder.Build();

            InitialiseDatastore();
            StartServices();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        private void InitialiseDatastore()
        {
            ApplicationContainer.Resolve<IDataProvider>().Initialise().Wait();
        }

        private void StartServices()
        {
            //Start fluent scheduler
            var jobFactory = ApplicationContainer.Resolve<IJobFactory>();
            var jobRegistry = ApplicationContainer.Resolve<Registry>();
            JobManager.JobFactory = jobFactory;
            JobManager.Initialize(jobRegistry);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            //Default Files MUST be called before static files https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api-docs/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api-docs/v1/swagger.json", "API v1");
            });
        }
    }

    public class ErrorFilter : ExceptionFilterAttribute
    {
        private readonly IHostingEnvironment _environment;

        public ErrorFilter(IHostingEnvironment environment)
        {
            _environment = environment;
        }
        public override async Task OnExceptionAsync(ExceptionContext context)
        {
            var exception = context.Exception;

            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errorModel = new ErrorResponseModel()
            {
                Id = -1,
                Message = "Unhandled error",
                RequestId = context.HttpContext.TraceIdentifier
            };

            if (_environment.IsDevelopment())
                errorModel.Payload = new { Exception = exception.Message };

            context.Result = new JsonResult(errorModel);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}