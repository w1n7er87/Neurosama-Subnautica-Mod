using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SCHIZO.Commands.Attributes;
using SCHIZO.Helpers;
using SCHIZO.SwarmControl;
using SwarmControl.Models.Game.Messages;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using UnityEngine;

namespace SCHIZO.Twitch;

[CommandCategory("Twitch")]
partial class TwitchIntegration
{
    private const string _usernamePlayerPrefsKey = "SCHIZO_TwitchIntegration_Username";
    private const string _tokenPlayerPrefsKey = "SCHIZO_TwitchIntegration_OAuthToken";

    private TwitchClient _client;
    private readonly ConcurrentQueue<string> _msgQueue = new();
    private HashSet<string> _allowedUsersSet;

    private void Awake()
    {
        _allowedUsersSet = new HashSet<string>(whitelistedUsers, StringComparer.OrdinalIgnoreCase);
        ClientOptions clientOptions = new()
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        WebSocketClient customClient = new(clientOptions);
        _client = new TwitchClient(customClient);

        _client.OnError += (_, evt) => LOGGER.LogError(evt.Exception);
        _client.OnIncorrectLogin += (_, evt) => LOGGER.LogError($"Could not connect to Twitch: {evt.Exception.Message}");
        _client.OnConnected += (_, evt) => LOGGER.LogInfo($"Connected to Twitch as {evt.BotUsername}");
        _client.OnConnectionError += (_, evt) => LOGGER.LogError($"Could not connect to Twitch: {evt.Error.Message}");
        _client.OnJoinedChannel += (_, evt) => LOGGER.LogInfo($"Joined Twitch channel {evt.Channel}");
        _client.OnFailureToReceiveJoinConfirmation += (_, evt) => LOGGER.LogError($"Could not join Twitch channel: {evt.Exception.Details}");
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnUserTimedout += Client_OnTimeoutReceived;

        string username = PlayerPrefs.GetString(_usernamePlayerPrefsKey, "");
        string token = PlayerPrefs.GetString(_tokenPlayerPrefsKey, "");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
        {
            LOGGER.LogWarning("Twitch OAuth token is not set, Twitch Integration will be disabled.");
            LOGGER.LogMessage("Run 'settwitchlogin <username> <token>' in the developer console and restart Subnautica in order to enable it.");
            return;
        }
        ConnectionCredentials credentials = new(username, token);

        _client.Initialize(credentials, targetChannel);

        _client.Connect();
    }

    private void Client_OnTimeoutReceived(object sender, OnUserTimedoutArgs e)
    {
        if (!_timeoutCallbacks.TryRemove(e.UserTimeout.TargetUserId, out Action cb)) return;
        SwarmControlManager.Instance.QueueOnMainThread(cb);
    }

    private void Client_OnMessageReceived(object _, OnMessageReceivedArgs evt)
    {
        ChatMessage message = evt.ChatMessage;
        TwitchUser user = new(message.UserId, message.Username, message.DisplayName);
        InvokeCallbacksOnMessage(user, message.Message);
        if (!IsUserWhitelisted(message.Username)) return; // ensure I don't get isekaid
        if (!CheckPrefix(message.Message)) return;

        // OnMessageReceived runs in a worker thread, where we can't use Unity APIs
        _msgQueue.Enqueue(message.Message[commandPrefix.Length..]);
    }

    private bool IsUserWhitelisted(string username)
    {
        return _allowedUsersSet.Contains(username);
    }

    private bool CheckPrefix(string message)
    {
        return message.StartsWith(commandPrefix, prefixIsCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
    }

    private void FixedUpdate()
    {
        if (_msgQueue.TryDequeue(out string message)) HandleMessage(message);
    }

    private static void HandleMessage(string message)
    {
        MessageHelpers.SuppressOutput = true;
        DevConsole.SendConsoleCommand(message);
        MessageHelpers.SuppressOutput = false;
    }

    [Command(Name = "settwitchlogin",
        DisplayName = "Set Twitch Login",
        Description = "Set your Twitch username and oauth token.\nYou can get a token from https://twitchtokengenerator.com (only scope `chat:read` is required)",
        RegisterConsoleCommand = true)]
    public static string SetTwitchLogin(string username, string token)
    {
        PlayerPrefs.SetString(_usernamePlayerPrefsKey, username);
        PlayerPrefs.SetString(_tokenPlayerPrefsKey, token);
        return "Twitch login updated. Please restart Subnautica.";
    }

    private static ConcurrentDictionary<string, (Action<string> Callback, float ModerationDelay)> _nextMessageCallbacks = [];
    private static ConcurrentDictionary<string, Action> _timeoutCallbacks = [];
    public static void AddNextMessageCallback(TwitchUser user, Action<string> callback, float moderationDelay = 5f)
    {
        _nextMessageCallbacks.AddOrUpdate(user.Id, (callback, moderationDelay), (_, existing) =>
        {
            // i love delegates
            existing.Callback += callback;
            existing.ModerationDelay = Mathf.Max(existing.ModerationDelay, moderationDelay);
            return existing;
        });
    }

    private static void InvokeCallbacksOnMessage(TwitchUser user, string message)
    {
        if (!_nextMessageCallbacks.TryRemove(user.Id, out (Action<string> callback, float delay) existing))
            return;

        if (existing.delay <= 0)
        {
            SwarmControlManager.Instance.QueueOnMainThread(() => existing.callback(message));
            return;
        }
        bool timedOut = false;
        Action cb = () => timedOut = true;
        _timeoutCallbacks.AddOrUpdate(user.Id, cb, (_, existing) => existing + cb);
        Task.Delay(TimeSpan.FromSeconds(existing.delay)).ContinueWith(t =>
        {
            _timeoutCallbacks.TryRemove(user.Id, out _);
            if (timedOut)
                message = "(filtered)";
            SwarmControlManager.Instance.QueueOnMainThread(() => existing.callback(message));
        });
    }
}
