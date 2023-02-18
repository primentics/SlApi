using CommandSystem;

using PluginAPI.Core;

using SlApi.Audio;
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

                        player = AudioPlayer.GetOrCreatePlayer(sender);

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

                                    player.Player.VoiceChannel = channel;

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

                                    if (!HubExtensions.TryGetHub(arguments.At(2), out var hub))
                                    {
                                        response = "Stopped following.";
                                        player.Player.Follow(null);
                                        return false;
                                    }

                                    player.Player.Follow(hub);

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

                                    string nick = string.Join(" ", arguments.Skip(2));

                                    player.Player.NickName = nick;

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

                        player.CurrentCommand = sender;
                        player.Search(query);

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

                        player.CurrentCommand = sender;
                        player.TryPlay(url);

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

                        player.CurrentCommand = sender;
                        player.Pause();

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

                        player.CurrentCommand = sender;
                        player.Resume();
                        
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

                        player.CurrentCommand = sender;
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

                        player.CurrentCommand = sender;
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

                        player.CurrentCommand = sender;
                        player.Skip();

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

                        if (!TryGetPlayer(sender, out var player))
                        {
                            response = "You don't have an active dummy.";
                            return false;
                        }

                        player.Player.Scale = new Vector3(x, y, z);

                        response = $"Dummy scaled to {player.Player.Scale.ToPreciseString()}";
                        return true;
                    }
            }

            response = "Invalid function.";
            return false;
        }

        public static bool TryGetPlayer(ReferenceHub hub, out AudioPlayer player)
        {
            player = AudioPlayer.GetPlayer(hub);

            return player != null;
        }
    }
}
