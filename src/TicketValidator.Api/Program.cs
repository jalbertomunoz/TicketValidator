using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TicketValidator.Api.Configuration;
using TicketValidator.Api.OpenApi;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.Services;
using TicketValidator.Application.UseCases.AnalyzeTicket;
using TicketValidator.Infrastructure.AI;
using TicketValidator.Infrastructure.DependencyInjection;
using TicketValidator.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<CamelCaseSchemaFilter>();
    options.OperationFilter<MultipartFormSchemaOperationFilter>();
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.PostConfigure<OpenAiOptions>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? options.ApiKey;
});
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.Configure<AuditLogOptions>(builder.Configuration.GetSection("AuditLog"));
builder.Services.AddInfrastructure();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddTransient<ITicketVerificationService, TicketVerificationService>();
builder.Services.AddTransient<IExpenseRuleEngine, ExpenseRuleEngine>();
builder.Services.AddTransient<AnalyzeTicketHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();

public partial class Program;
