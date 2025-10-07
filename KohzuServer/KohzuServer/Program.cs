using System.Net;
using KohzuServer.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Parse("192.168.1.2"), 5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddSingleton<KohzuServer.SerialPort.SerialPortCommunication>();

var app = builder.Build();

app.MapGrpcService<RotorStatusService>();
app.MapGrpcService<RotorDriveService>();

app.Run();