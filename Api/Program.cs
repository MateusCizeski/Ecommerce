using Api.Middleware.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
