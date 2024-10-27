using MarketPulse.Api.Middleware;
using MarketPulse.Api.Models;
using MarketPulse.Api.ServiceWorker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register WebSocket connection manager and price update service
builder.Services.Configure<TiingoSettings>(builder.Configuration.GetSection("TiingoSettings"));
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddHostedService<MarketUpdateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();

app.Run();