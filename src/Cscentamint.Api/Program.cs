using Cscentamint.Api.Infrastructure;
using Cscentamint.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITextClassifier, InMemoryNaiveBayesClassifier>();
builder.Services.AddSingleton<ITextTokenizer, DefaultTextTokenizer>();
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
app.MapControllers();
app.Run();

/// <summary>
/// Entry point marker used by integration test host bootstrapping.
/// </summary>
public partial class Program;
