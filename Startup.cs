using MetaverseMax.Controllers;
using MetaverseMax.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MetaverseMax
{
    public class Startup
    {
        public static bool isDevelopment { get; set; }
        public static string serverIP { get; set; }
        public static bool logServiceInfo { get; set; }
        public static bool showPrediction { get; set; }
        public static string dbConnectionStringTron { get; set; }
        public static string dbConnectionStringBNB { get; set; }
        public static string dbConnectionStringETH { get; set; }

        public Startup(IConfiguration configuration)
        {
            // Hook into the appsettings.json file to pull database and app settings used by services - within published site pulling from web.config
            serverIP = configuration["ServerIP"];
            logServiceInfo = configuration["logServiceInfo"] == "1";
            showPrediction = configuration["showPrediction"] == "1";
            dbConnectionStringTron = configuration.GetConnectionString("DatabaseConnection");
            dbConnectionStringBNB = configuration.GetConnectionString("DatabaseConnectionBNB");
            dbConnectionStringETH = configuration.GetConnectionString("DatabaseConnectionETH");
        }

        // Persist the current environment settings to use within other app classes/code
        public IWebHostEnvironment CurrentEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // The third segment, {id?} is used for an optional id. The ? in {id?} makes it optional. id is used to map to a model entity.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            //services.AddDbContext<MetaverseMaxDbContext>(options => options.UseSqlServer(dbConnectionStringTron));
            services.AddDbContext<MetaverseMaxDbContext>();


            //services.AddDbContextPool
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {            
            isDevelopment = env.IsDevelopment();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "api/{controller}/{id?}"
                    //defaults: new { id = RouteParameter.Optional }
                    );
                //pattern: "api/{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });

        }
    }
}
