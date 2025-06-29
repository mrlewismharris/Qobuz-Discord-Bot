using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.Commands;
using QobuzDiscordBot;

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

var host = builder.Build();

host.AddModules(typeof(Program).Assembly);

host.UseGatewayHandlers();

await host.RunAsync();

/*client.MessageCreate += async message =>
{
    if (message.Author.IsBot || !message.Content.StartsWith(_discordPrefix))
        return;

    var result = await commands.ExecuteAsync(
        prefixLength: 1,
        new CommandContext(message, client)
    );

    _status = "Finished initialising";

    if (result is IFailResult fail)
        await message.ReplyAsync(fail.Message);
};*/

// client.VoiceStateUpdate += async (voiceState) =>
// {
//     //todo: implement way to leave when channel becomes empty.    
// };