using LudoGame.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Register our custom services
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<RequestQueueService>();

// Add CORS policy to allow cross-origin requests (for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<Hubs.GameHub>("/gamehub");

// Health check endpoint
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Server = "LUDO-T Game Server"
})
.WithName("HealthCheck")
.WithOpenApi();

// Start the request queue service
var requestQueueService = app.Services.GetRequiredService<RequestQueueService>();

// Graceful shutdown handling
app.Lifetime.ApplicationStopping.Register(async () =>
{
    Console.WriteLine("Application is shutting down...");
    await requestQueueService.ShutdownAsync();
});

Console.WriteLine("LUDO-T Game Server starting...");
Console.WriteLine("Server will be available at: https://localhost:5001");
Console.WriteLine("Swagger UI available at: https://localhost:5001/swagger");

app.Run();