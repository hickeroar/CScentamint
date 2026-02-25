using System.Diagnostics.CodeAnalysis;
using Cscentamint.Api.Infrastructure;
using Cscentamint.Core;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

if (ShouldShowHelpAndExit(args))
{
    return;
}

var builder = WebApplication.CreateBuilder(args);

var serverOptions = ServerOptionsBuilder.Build(builder.Configuration);
builder.Services.AddSingleton(serverOptions);

var lang = GetStringEnvOrConfig("CSCENTAMINT_LANGUAGE", "Tokenization:Language") ?? "english";
var removeStopWordsRaw = GetStringEnvOrConfig("CSCENTAMINT_REMOVE_STOP_WORDS", "Tokenization:RemoveStopWords");
var removeStopWords = ParseRemoveStopWords(removeStopWordsRaw);

string? GetStringEnvOrConfig(string envKey, string configKey)
{
    var env = Environment.GetEnvironmentVariable(envKey);
    if (!string.IsNullOrWhiteSpace(env))
    {
        return env.Trim();
    }
    return builder.Configuration[configKey];
}
builder.Services.AddSingleton<ITextTokenizer>(_ => new DefaultTextTokenizer(lang, removeStopWords));
builder.Services.AddSingleton<ITextClassifier>(sp =>
    new InMemoryNaiveBayesClassifier(sp.GetRequiredService<ITextTokenizer>()));
builder.Services.AddSingleton<ReadinessState>();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.PostConfigure<AuthOptions>(options =>
{
    if (string.IsNullOrWhiteSpace(options.Token))
    {
        options.Token = GetStringEnvOrConfig("CSCENTAMINT_AUTH_TOKEN", "auth-token");
    }
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ConfigureSwagger);

builder.WebHost.UseUrls($"http://{serverOptions.Host}:{serverOptions.Port}");

var app = builder.Build();

UseDevelopmentMiddleware(app);

app.UseExceptionHandler();
if (serverOptions.Verbose)
{
    app.UseMiddleware<VerboseLoggingMiddleware>();
}
app.UseMiddleware<RootEndpointRequestSizeMiddleware>();
app.UseMiddleware<BearerTokenMiddleware>();

var readinessState = app.Services.GetRequiredService<ReadinessState>();
app.Lifetime.ApplicationStopping.Register(readinessState.MarkNotReady);

app.MapGet("/healthz", () => Results.Json(new { status = "ok" }));
app.MapGet("/readyz", (ReadinessState readiness) =>
    readiness.IsReady
        ? Results.Json(new { status = "ready" })
        : Results.Json(new { status = "not ready" }, statusCode: StatusCodes.Status503ServiceUnavailable));
app.MapControllers();
app.Run();

[ExcludeFromCodeCoverage]
static bool ShouldShowHelpAndExit(string[] args)
{
    return CommandLineHelp.TryShowHelp(args, Console.Out);
}

[ExcludeFromCodeCoverage]
static void UseDevelopmentMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
}

[ExcludeFromCodeCoverage]
static bool ParseRemoveStopWords(string? raw)
{
    return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
        (raw != null && raw.Trim() is { } r && (string.Equals(r, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "yes", StringComparison.OrdinalIgnoreCase)));
}

static void ConfigureSwagger(SwaggerGenOptions options)
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CScentamint API",
        Version = "v1",
        Description = "Trainable naive Bayesian text classification API."
    });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "Cscentamint.Api.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
}

/// <summary>
/// Entry point marker used by integration test host bootstrapping.
/// </summary>
public partial class Program;
