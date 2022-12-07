using XSP.Engine;
using System.Web;
using Microsoft.AspNetCore.Connections;
using XSP.Web;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<XspHandler>(new XspEngine());

DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("default.xsp");

app.UseDefaultFiles(defaultFilesOptions);

app.Run();

