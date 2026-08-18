using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Data;

var builder = WebApplication.CreateBuilder(args);

// الـconnection string بيتقرا بالترتيب ده: appsettings -> environment variable -> default محلي
// الترتيب ده هو نفسه اللي هيسهّل حقن القيمة من Kubernetes Secret بعدين
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=taskmanager;Username=postgres;Password=postgres";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// health endpoint عام (liveness) وendpoint تاني بيتحقق من الداتابيز فعليًا (readiness)
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapGet("/", () => Results.Ok(new { status = "Task Manager API is running" }));

app.Run();
