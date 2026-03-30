using LithoTwinAPI.Data;
using LithoTwinAPI.Services;
using LithoTwinAPI.Simulation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Persistence: InMemory by default, SQLite when configured
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");
if (useSqlite)
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=lithotwin.db"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseInMemoryDatabase("LithoTwinDB"));
}

// Services — each one owns a single behavioral domain
builder.Services.AddScoped<MachineLifecycleService>();
builder.Services.AddScoped<FaultService>();
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddScoped<ExposureService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<ReticleService>();

// Simulation — background thermal drift engine
builder.Services.AddHostedService<ThermalSimulationService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LithoTwin API",
        Version = "v1",
        Description = "Industrial digital twin — state-driven lifecycle management, " +
                      "fault propagation, and telemetry simulation for EUV lithography tools",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "LithoTwin Team",
            Email = "support@lithotwin.local"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Initialize database schema and apply initial seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();