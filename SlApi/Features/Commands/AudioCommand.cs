using CommandSystem;

using PluginAPI.Core;

using SlApi.Audio;
using SlApi.Dummies;
using SlApi.Extensions;

using System;
using System.Linq;

using UnityEngine;

using VoiceChat;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AudioCommand : ICommand
    {
        public string Command { get; } = "audio";
        public string[] Aliases { get; } = new string[] { "adi" };
        public string Description { get; } = "Performs an audio action.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender cmdSender, out string response)
        {
            if (arguments.Count < 1) 
            {
                response = "Missing arguments! audio <function> (function args)\n" +
                    "Functions:\n" +
                    "   - spawn (true/false) (Spawns an audio player, the second argument indicates whether to use the sender's voice chat or spawn a dummy instead.).\n" +
                    "   - despawn (Despawns your audio player).\n" +
                    "   - config <name> <value> (Configures your audio player - channel, volume, follow, nick).\n" +
                    "   - search <query> (Starts playing the first search result on YouTube)." +
                    "   - play <url> (Starts playing a YouTube URL).\n" +
                    "   - pause (Pauses the playback of your audio player).\n" +
                    "   - resume (Resumes the playback of your audio player).\n" +
                    "   - skip (Skips the current track).\n" +
                    "   - stop (Stops playback).\n" +
                    "   - clear (Clears the queue).\n" +
                    "   - whitelist/wh <player> (Whitelists the broadcast to a player).\n" +
                    "   - loop (Loops the current track).\n" +
                    "   - scale (Scales your dummy).\n" +
                    "   - blacklist/bh (Blacklists the broadcast from a player).";
                return false;
            }

            var sender = Player.Get(cmdSender)?.ReferenceHub;

            if (sender is null)
            {
                response = "Failed to fetch your Reference Hub.";
                return false;
            }

            switch (arguments.At(0).ToLower())
            {
                case "spawn":
                    {
                        if (TryGetPlayer(sender, out var player))
                        {
                            response = "You already own a player!";
                            return false;
                        }

                        if (arguments.Count >= 2 && bool.TryParse(arguments.At(1), out var useSelf) && useSelf)
                            player = AudioPlayer.GetOrCreatePlayer(sender);
                        else
                        {
                            var dummy = DummyPlayer.GetOrCreateDummy(sender, true);

                            dummy.Owner = sender;

                            player = AudioPlayer.GetOrCreatePlayer(dummy.Hub);
                        }

                        response = "Player spawned.";
                        return true;
                    }

                case "despawn":
                    {
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own any active players!";
                            return false;
                        }

                        UnityEngine.Object.Destroy(player);

                        if (DummyPlayer.TryGetDummyByOwner(sender, out var dummy))
                            dummy.Destroy();

                        response = "Player despawned.";
                        return true;
                    }

                case "config":
                    {
                        if (arguments.Count < 3)
                        {
                            response = "Missing arguments! audio config <type> <value>";
                            return false;
                        }
                        
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        switch (arguments.At(1).ToLower())
                        {
                            case "channel":
                                {
                                    if (!Enum.TryParse<VoiceChatChannel>(arguments.At(2), out var channel))
                                    {
                                        response = "Failed to parse channel type! (Spectator, RoundSummary, Proximity, ScpChat, Scp1576, Intercom).";
                                        return false;
                                    }

                                    player.AudioSettings.Channel = channel;

                                    response = $"Channel set to {channel}";
                                    return true;
                                }

                            case "volume":
                                {
                                    if (!float.TryParse(arguments.At(2), out var volume))
                                    {
                                        response = "Failed to parse volume!";
                                        return false;
                                    }

                                    player.AudioSettings.Volume = volume;

                                    response = $"Volume set to {volume}";
                                    return true;
                                }

                            case "follow":
                                {
                                    if (arguments.Count != 3)
                                    {
                                        response = "Missing arguments! audio config follow <target>";
                                        return false;
                                    }

                                    if (!DummyPlayer.TryGetDummyByOwner(sender, out var dummy))
                                    {
                                        response = "You do not have an active dummy.";
                                        return false;
                                    }

                                    if (!HubExtensions.TryGetHub(arguments.At(2), out var hub))
                                    {
                                        response = "Stopped following.";
                                        dummy.Follow = null;
                                        return false;
                                    }

                                    dummy.Owner = sender;
                                    dummy.Follow = hub;

                                    response = $"Following {hub.nicknameSync.MyNick}";
                                    return true;
                                }

                            case "nick":
                                {
                                    if (arguments.Count < 3)
                                    {
                                        response = "Missing arguments! audio config nick <nick>";
                                        return false;
                                    }

                                    if (!DummyPlayer.TryGetDummyByOwner(sender, out var dummy))
                                    {
                                        response = "You do not have an active dummy.";
                                        return false;
                                    }

                                    string nick = string.Join(" ", arguments.Skip(2));

                                    dummy.Nick = nick;

                                    response = $"Nick set to {nick}";
                                    return true;
                                }

                            default:
                                {
                                    response = "Unknown config key.";
                                    return false;
                                }
                        }
                    }

                case "search":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! audio play <url>";
                            return false;
                        }

                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own an audio player.";
                            return false;
                        }

                        string query = string.Join(" ", arguments.Skip(1));

                        player.Search(query, new AudioCommandChannel(sender));

                        response = "";
                        return true;
                    }

                case "play":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! audio play <url>";
                            return false;
                        }

                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own an audio player.";
                            return false;
                        }

                        string url = arguments.At(1);

                        player.TryPlay(url, new AudioCommandChannel(sender));

                        response = "";
                        return true;
                    }

                case "pause":
                    {
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        player.Pause(new AudioCommandChannel(sender));

                        response = "";
                        return true;
                    }

                case "resume":
                    {
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        player.Resume(new AudioCommandChannel(sender));
                        
                        response = "";
                        return true;
                    }

                case "stop":
                    {
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        player.Stop(true);

                        response = "Stopped.";
                        return true;
                    }

                case "clear":
                    {
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        player.ClearQueue();

                        response = "Queue cleared.";
                        return true;
                    }

                case "skip":
                    {
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        player.Skip(new AudioCommandChannel(sender));

                        response = "";
                        return true;
                    }

                case "loop":
                    {
                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        if (player.AudioSettings.Loop)
                        {
                            player.AudioSettings.Loop = false;

                            response = "Loop disabled.";
                            return true;
                        }
                        else
                        {
                            player.AudioSettings.Loop = true;

                            response = "Loop enabled.";
                            return true;
                        }
                    }

                case "wh":
                case "whitelist":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! audio whitelist <player>";
                            return false;
                        }

                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        if (!HubExtensions.TryGetHub(arguments.At(1), out var hub))
                        {
                            response = "Failed to find your target.";
                            return false;
                        }

                        if (player.Whitelisted.Contains(hub.GetInstanceID()))
                        {
                            player.Whitelisted.Remove(hub.GetInstanceID());

                            response = $"{hub.nicknameSync.Network_myNickSync} removed from whitelist.";
                            return true;
                        }
                        else
                        {
                            player.Whitelisted.Add(hub.GetInstanceID());

                            response = $"{hub.nicknameSync.Network_myNickSync} added to whitelist.";
                            return true;
                        }
                    }

                case "bh":
                case "blacklist":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "Missing arguments! audio blacklist <player>";
                            return false;
                        }

                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't own a player!";
                            return false;
                        }

                        if (!HubExtensions.TryGetHub(arguments.At(1), out var hub))
                        {
                            response = "Failed to find your target.";
                            return false;
                        }

                        if (player.Blacklisted.Contains(hub.GetInstanceID()))
                        {
                            player.Blacklisted.Remove(hub.GetInstanceID());

                            response = $"{hub.nicknameSync.Network_myNickSync} removed from blacklist.";
                            return true;
                        }
                        else
                        {
                            player.Blacklisted.Add(hub.GetInstanceID());

                            response = $"{hub.nicknameSync.Network_myNickSync} added to blacklist.";
                            return true;
                        }
                    }

                case "scale":
                    {
                        if (arguments.Count != 4)
                        {
                            response = "Missing arguments! audio scale <x> <y> <z>";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(1), out var x))
                        {
                            response = "Failed to parse the X axis!";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(2), out var y))
                        {
                            response = "Failed to parse the Y axis!";
                            return false;
                        }

                        if (!float.TryParse(arguments.At(3), out var z))
                        {
                            response = "Failed to parse the Z axis!";
                            return false;
                        }

                        if (!DummyPlayer.TryGetDummyByOwner(sender, out var dummy))
                        {
                            response = "You don't have an active dummy.";
                            return false;
                        }

                        dummy.Scale = new Vector3(x, y, z);

                        response = $"Dummy scaled to {dummy.Scale.ToPreciseString()}";
                        return true;
                    }
            }

            response = "Invalid function.";
            return false;
        }

        public static bool TryGetPlayer(ReferenceHub hub, out AudioPlayer player)
        {
            if (DummyPlayer.TryGetDummyByOwner(hub, out var dummy))
            {
                player = AudioPlayer.GetPlayer(dummy.Hub);
            }
            else
            {
                player = AudioPlayer.GetPlayer(hub);
            }

            return player != null;
        }
    }
}
