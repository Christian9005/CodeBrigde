// ═══════════════════════════════════════════════════════════════
//  CodeBridge sample: Blazor dashboard
//  ───────────────────────────────────
//  A live web dashboard for your board in about 15 lines of setup.
//
//    dotnet run                     -> uses the first board on USB, or the built-in simulator when none is plugged in
//    CodeBridge__Port=simulator     -> always the simulator
//    CodeBridge__Port=COM3          -> a specific USB port
//    CodeBridge__Port=192.168.1.50  -> a Wi-Fi board (set CodeBridge__AccessToken to its pairing token)
//
//  Open http://localhost:5080
// ═══════════════════════════════════════════════════════════════

using CodeBridge.Hosting;
using CodeBridge.Samples.IntegrationBlazor.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// One shared board for the whole app. Options come from appsettings.json ("CodeBridge" section) and environment variables.
builder.Services.AddCodeBridge(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
