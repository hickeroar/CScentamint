using Cscentamint.Api.Infrastructure;
using Cscentamint.Core;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var lang = builder.Configuration["Tokenization:Language"] ?? "english";
var removeStopWords = string.Equals(builder.Configuration["Tokenization:RemoveStopWords"], "true", StringComparison.OrdinalIgnoreCase);
builder.Services.AddSingleton<ITextTokenizer>(_ => new DefaultTextTokenizer(lang, removeStopWords));
builder.Services.AddSingleton<ITextClassifier>(sp =>
    new InMemoryNaiveBayesClassifier(sp.GetRequiredService<ITextTokenizer>()));
builder.Services.AddSingleton<ReadinessState>();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.PostConfigure<AuthOptions>(options =>
{
    if (string.IsNullOrWhiteSpace(options.Token))
    {
        options.Token = builder.Configuration["auth-token"];
    }
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CScentamint API",
        Version = "v1",
        Description = "Trainable naive Bayes text classification API."
    });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "Cscentamint.Api.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
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

/// <summary>
/// Entry point marker used by integration test host bootstrapping.
/// </summary>
public partial class Program;
