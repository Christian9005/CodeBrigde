// CodeBridge as a Windows Service: log a sensor reading to a CSV file every minute, even when nobody is logged in.
//   dotnet publish -c Release -o publish
//   sc create "CodeBridge Sensor Logger" binPath= "C:\path\to\publish\CodeBridge.Samples.IntegrationWindowsService.exe" start= auto
//   sc start "CodeBridge Sensor Logger"
using CodeBridge.Samples.IntegrationWindowsService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "CodeBridge Sensor Logger");
builder.Services.AddHostedService<SensorLoggerWorker>();

builder.Build().Run();
