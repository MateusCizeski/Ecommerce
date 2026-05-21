using Api.Middleware.Extensions;
using Application;
using Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseGlobalExceptionHandling();
app.UseTenantResolution();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
