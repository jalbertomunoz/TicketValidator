using Microsoft.AspNetCore.Mvc;
using TicketValidator.Api.Configuration;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.Services;
using TicketValidator.Application.UseCases.AnalyzeTicket;
using TicketValidator.Infrastructure.AI;
using TicketValidator.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.AddOpenApi();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.PostConfigure<OpenAiOptions>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? options.ApiKey;
});
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.AddInfrastructure();
builder.Services.AddTransient<ITicketVerificationService, TicketVerificationService>();
builder.Services.AddTransient<IExpenseRuleEngine, ExpenseRuleEngine>();
builder.Services.AddTransient<AnalyzeTicketHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
