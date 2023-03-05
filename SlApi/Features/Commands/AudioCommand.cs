using CommandSystem;

using PluginAPI.Core;

using SlApi.Extensions;
using SlApi.Features.Audio;

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
                response = "Missing arguments! \naudio <function> (function args)\n\n" +
                    "Functions:\n" +
                    "   - create <speaker>\n" +
                    "   - destroy\n" +
                    "   - config <name> <value>\n" +
                    "   - play <url>\n" +
                    "   - pause\n" +
                    "   - resume\n" +
                    "   - skip\n" +
                    "   - stop\n" +
                    "   - clear\n" +
                    "   - loop\n";
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
                case "create":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "audio create <speaker>";
                            return false;
                        }

                        if (!HubExtensions.TryGetHub(arguments.At(1), out var speaker))
                        {
                            response = "You have to select the speaker!";
                            return false;
                        }

                        if (AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You already own an audio player.";
                            return false;
                        }

                        audioPlayer = AudioPlayer.Create(sender, speaker);

                        response = $"Audio player created with speaker {speaker.nicknameSync.Network_myNickSync}";
                        return true;
                    }

                case "destroy":
                    {
                        if (AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            GameObject.Destroy(audioPlayer);

                            response = $"Audio player destroyed.";
                            return true;
                        }

                        response = "You don't seem to own an audio player.";
                        return false;
                    }

                case "config":
                    {
                        if (arguments.Count < 3)
                        {
                            response = "Missing arguments! audio config <type> <value>";
                            return false;
                        }

                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player to configure.";
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

                                    audioPlayer.VoiceChannel = channel;

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

                                    audioPlayer.Volume = volume;

                                    response = $"Volume set to {volume}";
                                    return true;
                                }

                            default:
                                {
                                    response = "Unknown config key.";
                                    return false;
                                }
                        }
                    }

                case "play":
                    {
                        if (arguments.Count < 2)
                        {
                            response = "audio play <query>";
                            return false;
                        }

                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player.";
                            return false;
                        }

                        string query = string.Join(" ", arguments.Skip(1));

                        audioPlayer.TryPlay(query);

                        response = $"Searching for: {query}\nYou can monitor playback progress via your player console.";
                        return true;
                    }

                case "pause":
                    {
                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player.";
                            return false;
                        }

                        audioPlayer.ShouldPlay = false;

                        response = "Audio paused.";
                        return true;
                    }

                case "resume":
                    {
                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player.";
                            return false;
                        }

                        audioPlayer.ShouldPlay = true;
                        
                        response = "Audio resumed.";
                        return true;
                    }

                case "stop":
                    {
                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player.";
                            return false;
                        }

                        audioPlayer.Stop();

                        response = "Audio stopped.";
                        return true;
                    }

                case "clear":
                    {
                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player.";
                            return false;
                        }

                        audioPlayer.TrackQueue.Clear();

                        response = "Queue cleared.";
                        return true;
                    }

                case "skip":
                    {
                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player.";
                            return false;
                        }

                        audioPlayer.Skip();

                        response = "Track skipped.";
                        return true;
                    }

                case "loop":
                    {
                        if (!AudioPlayer.TryGet(sender, out var audioPlayer))
                        {
                            response = "You don't seem to own an audio player.";
                            return false;
                        }

                        if (audioPlayer.IsLooping)
                        {
                            audioPlayer.IsLooping = false;

                            response = "Loop disabled.";
                            return true;
                        }
                        else
                        {
                            audioPlayer.IsLooping = true;

                            response = "Loop enabled.";
                            return true;
                        }
                    }
            }

            response = "Invalid function.";
            return false;
        }
    }
}
