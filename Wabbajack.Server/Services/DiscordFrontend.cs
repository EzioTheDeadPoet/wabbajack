﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OMODFramework;
using Wabbajack.BuildServer;
using Wabbajack.Common;
using Wabbajack.Server.DataLayer;
using Utils = Wabbajack.Common.Utils;

namespace Wabbajack.Server.Services
{
    public class DiscordFrontend : IStartable
    {
        private ILogger<DiscordFrontend> _logger;
        private AppSettings _settings;
        private QuickSync _quickSync;
        private DiscordSocketClient _client;
        private SqlService _sql;
        private MetricsKeyCache _keyCache;

        public DiscordFrontend(ILogger<DiscordFrontend> logger, AppSettings settings, QuickSync quickSync, SqlService sql, MetricsKeyCache keyCache)
        {
            _logger = logger;
            _settings = settings;
            _quickSync = quickSync;
            
            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;

            _sql = sql;
            _keyCache = keyCache;
        }

        private async Task MessageReceivedAsync(SocketMessage arg)
        {
            _logger.LogInformation(arg.Content);
            if (arg.Content.StartsWith("!dervenin"))
            {
                var parts = arg.Content.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (parts[0] != "!dervenin")
                    return;
                
                if (parts.Length == 1)
                {
                    await ReplyTo(arg, "Wat?");
                }

                if (parts[1] == "purge-nexus-cache")
                {
                    if (parts.Length != 3)
                    {
                        await ReplyTo(arg, "Welp you did that wrong, gotta give me a mod-id or url");
                        return;
                    }
                    await PurgeNexusCache(arg, parts[2]);
                }
                else if (parts[1] == "quick-sync")
                {
                    var options = await _quickSync.Report();
                    if (parts.Length != 3)
                    {
                        var optionsStr = string.Join(", ", options.Select(o => o.Key.Name));
                        await ReplyTo(arg, $"Can't expect me to quicksync the whole damn world! Try: {optionsStr}");
                    }
                    else
                    {
                        foreach (var pair in options.Where(o => o.Key.Name == parts[2]))
                        {
                            await _quickSync.Notify(pair.Key);
                            await ReplyTo(arg, $"Notified {pair.Key}");
                        }
                    }
                }
                else if (parts[1] == "purge-list")
                {
                    if (parts.Length != 3)
                    {
                        await ReplyTo(arg, $"Yeah, I'm not gonna purge the whole server...");
                    }
                    else
                    {
                        var deleted = await _sql.PurgeList(parts[2]);
                        await _quickSync.Notify<ModListDownloader>();
                        await ReplyTo(arg, $"Purged all traces of #{parts[2]} from the server, triggered list downloading. {deleted} records removed");
                    }
                }
                else if (parts[1] == "users")
                {
                    await ReplyTo(arg, $"Wabbajack has {await _keyCache.KeyCount()} known unique users");
                }
            }
        }

        private async Task PurgeNexusCache(SocketMessage arg, string mod)
        {
            if (Uri.TryCreate(mod, UriKind.Absolute, out var url))
            {
                mod = Enumerable.Last(url.AbsolutePath.Split("/", StringSplitOptions.RemoveEmptyEntries));
            }
            
            if (int.TryParse(mod, out var mod_id))
            {
                await _sql.PurgeNexusCache(mod_id);
                await _quickSync.Notify<ListValidator>();
                await ReplyTo(arg, $"It is done, {mod_id} has been purged, list validation has been triggered");
            }
        }

        private async Task ReplyTo(SocketMessage socketMessage, string message)
        {
            await socketMessage.Channel.SendMessageAsync(message);
        }

        private async Task ReadyAsync()
        {
        }

        private async Task LogAsync(LogMessage arg)
        {
            switch (arg.Severity)
            {
                case LogSeverity.Info:
                    _logger.LogInformation(arg.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(arg.Message);
                    break;
                case LogSeverity.Critical:
                    _logger.LogCritical(arg.Message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace(arg.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(arg.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Start()
        {
            _client.LoginAsync(TokenType.Bot, Utils.FromEncryptedJson<string>("discord-key").Result).Wait();
            _client.StartAsync().Wait();
        }
        
    }
}
