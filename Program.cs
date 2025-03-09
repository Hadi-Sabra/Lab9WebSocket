using ChatServer.Data;
using ChatServer.Hubs;
using ChatServer.Repositories;
using ChatServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddGrpc();

// Add database context
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=chat.db"));

// Register repositories
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// CORS for client connection
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure middleware
app.UseRouting();
app.UseCors("AllowAll");
app.UseGrpcWeb(); // Add gRPC-Web middleware

// Map endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ChatHub>("/chatHub");
    endpoints.MapGrpcService<ChatHistoryService>().EnableGrpcWeb().RequireCors("AllowAll");
});

app.Run();