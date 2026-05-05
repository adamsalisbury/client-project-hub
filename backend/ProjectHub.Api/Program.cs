using System.Text.Json.Serialization;
using ProjectHub.Api;
using ProjectHub.Persistence;
using ProjectHub.Services;
using ProjectHub.Services.Runner;
using ProjectHub.Services.Storage;
using ProjectHub.Services.Workers;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "project-hub.antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddOpenApi();

builder.Services
    .AddOptions<ClaudeRunnerOptions>()
    .Bind(builder.Configuration.GetSection(ClaudeRunnerOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services
    .AddOptions<JsonDataProviderOptions>()
    .Bind(builder.Configuration.GetSection(JsonDataProviderOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services
    .AddOptions<FilesystemOptions>()
    .Bind(builder.Configuration.GetSection(FilesystemOptions.SectionName));

// Persistence + low-level infrastructure.
builder.Services.AddSingleton<IClaudeRunner, ClaudeRunner>();
builder.Services.AddSingleton<IClaudeDataProvider, JsonClaudeDataProvider>();
builder.Services.AddSingleton<IClaudeJobQueue, ChannelClaudeJobQueue>();
builder.Services.AddHostedService<ClaudeJobWorker>();

// Service layer (web-agnostic).
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<IAgentService, AgentService>();
builder.Services.AddSingleton<ITicketService, TicketService>();
builder.Services.AddSingleton<IClientService, ClientService>();
builder.Services.AddSingleton<IKnowledgeService, KnowledgeService>();
builder.Services.AddSingleton<IProjectFileService, ProjectFileService>();
builder.Services.AddSingleton<IProjectGitService, ProjectGitService>();
builder.Services.AddSingleton<IFilesystemService, FilesystemService>();
builder.Services.AddSingleton<IClaudeJobService, ClaudeJobService>();
builder.Services.AddSingleton<IRepoAnalysisService, RepoAnalysisService>();

builder.WebHost.ConfigureKestrel(options =>
{
    var port = builder.Configuration.GetValue("Server:Port", 5090);
    options.ListenAnyIP(port);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ServiceExceptionMiddleware>();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (HttpMethods.IsGet(context.Request.Method) &&
        (path == "/" || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase) ||
         (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) && !Path.HasExtension(path))))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append(
            "XSRF-TOKEN",
            tokens.RequestToken!,
            new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Strict, IsEssential = true });
    }
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
