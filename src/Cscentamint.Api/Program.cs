using Cscentamint.Api.Infrastructure;
using Cscentamint.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITextClassifier, InMemoryNaiveBayesClassifier>();
builder.Services.AddSingleton<ITextTokenizer, DefaultTextTokenizer>();
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
builder.Services.AddSwaggerGen();

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
