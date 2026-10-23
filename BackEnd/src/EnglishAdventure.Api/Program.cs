using EnglishAdventure.Api;
using EnglishAdventure.Api.CommandLine;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEnglishAdventureServices(builder.Configuration);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = builder.Environment.IsDevelopment();
    options.ValidateOnBuild = true;
});

var app = builder.Build();

// Administration tasks such as `bootstrap-superadmin` run instead of the web server.
if (CommandLineTasks.IsTask(args))
{
    return await CommandLineTasks.RunAsync(app, args);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("English Adventure API");
    });
}

app.UseExceptionHandler();
// Gives body-less error responses (e.g. the JWT bearer 401 challenge) a ProblemDetails body.
app.UseStatusCodePages();
// Not in Development: the front-end dev proxy calls the http port, and a redirect to the https port would send the
// browser to another origin (CORS error).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
return 0;

public partial class Program;
