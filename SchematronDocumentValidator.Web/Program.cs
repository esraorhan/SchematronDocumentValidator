using SchematronDocumentValidator.Application.Abstractions;
using SchematronDocumentValidator.Application.Services;
using SchematronDocumentValidator.Core.Abstractions;
using SchematronDocumentValidator.Infrastructure.Saxon.Validation;
using SchematronDocumentValidator.Core.Abstractions;
using SchematronDocumentValidator.Infrastructure.Saxon.Persistence;
using SchematronDocumentValidator.Application.Validation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ISchematronValidationEngine, SaxonSchematronValidationEngine>();
builder.Services.AddScoped<IDocumentValidationService, DocumentValidationService>();
builder.Services.AddSingleton<ICustomSchematronRuleRepository>(_ =>
    new JsonCustomSchematronRuleRepository(
        Path.Combine(
            builder.Environment.ContentRootPath,
            "App_Data",
            "custom-schematron-rules.json")));

builder.Services.AddScoped<CustomSchematronRuleValidator>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();