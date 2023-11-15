using MetaverseMax.Database;
using MetaverseMax.ServiceClass;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MetaverseMaxDbContext>();         // inject dbContext into service controller - allow context to initiate when first used

//builder.Services.AddDbContext<MyDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("ConnStr")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
}
ServiceCommon.AssignSetting(app.Environment.IsDevelopment());

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "api/{controller}/{id?}");
    //pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
