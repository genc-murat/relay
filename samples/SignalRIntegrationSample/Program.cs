using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Relay;
using Relay.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddRelayConfiguration();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<NotificationHandler>();

var app = builder.Build();

app.MapHub<NotificationHub>("/notificationHub");
app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Relay SignalR Demo</title>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.0/signalr.min.js'></script>
</head>
<body>
    <h1>🚀 Relay SignalR Integration</h1>
    <div id='messages'></div>
    <script>
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/notificationHub')
            .build();

        connection.on('ReceiveNotification', (message) => {
            const div = document.getElementById('messages');
            div.innerHTML += '<p>' + message + '</p>';
        });

        connection.start().then(() => {
            console.log('Connected to SignalR hub');
        });
    </script>
</body>
</html>
", "text/html"));

Console.WriteLine("🚀 Relay SignalR Integration Sample");
Console.WriteLine("Server running on: http://localhost:5002");
Console.WriteLine("Open browser to see real-time notifications");
Console.WriteLine();

// Simulate sending notifications
_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    using var scope = app.Services.CreateScope();
    var relay = scope.ServiceProvider.GetRequiredService<IRelay>();
    var hub = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

    for (int i = 1; i <= 10; i++)
    {
        var message = $"System notification #{i} at {DateTime.Now:HH:mm:ss}";
        await hub.Clients.All.SendAsync("ReceiveNotification", message);
        Console.WriteLine($"📡 Broadcast: {message}");
        await Task.Delay(3000);
    }
});

app.Run("http://localhost:5002");

// Notifications
public record SystemNotification(string Message, string Level) : INotification;

// SignalR Hub
public class NotificationHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
}

// Relay notification handler
public class NotificationHandler
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHandler(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [Notification]
    public async ValueTask HandleAsync(SystemNotification notification, CancellationToken ct)
    {
        var message = $"[{notification.Level}] {notification.Message}";
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, ct);
    }
}

public class NotificationService
{
    // Service placeholder
}
