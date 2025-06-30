using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.Commands;
using QobuzApiSharp.Service;
using QobuzDiscordBot;
using QobuzDiscordBot.Services;

Env.Load();
Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<DataContext>(opts => opts.UseSqlite("Data Source=data.db"), ServiceLifetime.Singleton);

builder.Services.AddDiscordGateway(opts =>
{
    opts.Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
    opts.Intents = GatewayIntents.All;
}).AddCommands(opts => { opts.Prefix = Environment.GetEnvironmentVariable("DISCORD_PREFIX"); });

if (!Environment.GetCommandLineArgs()[0].Contains("ef.dll"))
{
    QobuzApiService apiService = new QobuzApiService();
    apiService.LoginWithEmail(
        Environment.GetEnvironmentVariable("QOBUZ_EMAIL"),
        Environment.GetEnvironmentVariable("QOBUZ_PASS_MD5")
    );
    builder.Services.AddSingleton(apiService);
}

builder.Services.AddSingleton<TextCommandModule>();
builder.Services.AddSingleton<SearchCacheService>();

var host = builder.Build();

host.AddModules(typeof(Program).Assembly);

host.UseGatewayHandlers();

await host.RunAsync();