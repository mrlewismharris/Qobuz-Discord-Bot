using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.Commands;
using QobuzApiSharp.Service;
using QobuzDiscordBot;
using QobuzDiscordBot.Services;
using System.Net.Http;

Env.Load();
Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<DataContext>(opts => opts.UseSqlite($"Data Source=data.db"), ServiceLifetime.Singleton);

builder.Services.AddHttpClient();

builder.Services.AddDiscordGateway(opts =>
{
    opts.Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
    opts.Intents = GatewayIntents.All;
}).AddCommands(opts => { opts.Prefix = Environment.GetEnvironmentVariable("DISCORD_PREFIX"); });

QobuzApiService apiService = new QobuzApiService();
apiService.LoginWithEmail(
    Environment.GetEnvironmentVariable("QOBUZ_EMAIL"),
    Environment.GetEnvironmentVariable("QOBUZ_PASS_MD5")
);
builder.Services.AddSingleton(apiService);

builder.Services.AddSingleton<TextCommandModule>();
builder.Services.AddSingleton<SearchCacheService>();
builder.Services.AddSingleton<IOService>();
builder.Services.AddSingleton<DownloadService>();
builder.Services.AddSingleton<PlaybackService>();
builder.Services.AddSingleton<VoiceClientService>();

var host = builder.Build();

host.AddModules(typeof(Program).Assembly);

host.UseGatewayHandlers();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.Migrate();
}

await host.RunAsync();