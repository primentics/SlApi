using MEC;

using NVorbis;

using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Networking;
using VoiceChat.Codec.Enums;

using SlApi.Features.AsyncHelpers;
using SlApi.Features.Audio.Conversion.Ffmpeg;
using SlApi.Features.Audio.Conversion.Ogg;
using SlApi.Events;
using SlApi.Events.CustomHandlers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using YoutubeSearch;
using YoutubeExplode;
using YoutubeExplode.Videos;

using UnityEngine;

using Utils.NonAllocLINQ;

using SlApi.Extensions;
using SlApi.Features.Audio.Conversion;

namespace SlApi.Features.Audio {
    public class AudioPlayer : MonoBehaviour {
        public const int HeadSamples = 1920;

        public static HashSet<AudioPlayer> AllPlayers { get; } = new HashSet<AudioPlayer>();
        public static HashSet<string> Mutes { get; } = new HashSet<string>();

        public static VideoSearch YouTubeSearchClient { get; private set; } = new VideoSearch();
        public static YoutubeClient YouTubeDownloadClient { get; private set; } = new YoutubeClient();

        public CoroutineHandle PlaybackCoroutineHandle { get; private set; }

        public AudioTrack CurrentTrack { get; private set; }
        public OpusEncoder Encoder { get; } = new OpusEncoder(OpusApplicationType.Voip);
        public PlaybackBuffer PlaybackBuffer { get; } = new PlaybackBuffer();
        public MemoryStream PlaybackStream { get; private set; }
        public VorbisReader Reader { get; private set; }

        public Queue<AudioTrack> TrackQueue { get; private set; } = new Queue<AudioTrack>();
        public Queue<float> StreamBuffer { get; } = new Queue<float>();

        public HashSet<string> Whitelist { get; } = new HashSet<string>();
        public HashSet<string> Blacklist { get; } = new HashSet<string>();

        public byte[] EncodedBuffer { get; } = new byte[512];

        public bool ShouldStop { get; private set; }
        public bool ShouldPlay { get; set; }

        public bool IsReady { get; private set; }
        public bool IsLooping { get; set; }

        public float Samples { get; private set; }
        public float Volume { get; set; } = 100f;

        public int SamplesPerSecond { get; private set; }


        public float[] SendBuffer { get; set; }
        public float[] ReadBuffer { get; set; }

        public ReferenceHub Owner { get; set; }
        public ReferenceHub Speaker { get; set; }

        public VoiceChatChannel VoiceChannel { get; set; } = VoiceChatChannel.Proximity;

        static AudioPlayer() {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
        }

        public void Setup(ReferenceHub owner, ReferenceHub speaker) {
            Owner = owner;
            Speaker = speaker;

            AllPlayers.Add(this);
        }

        public void OnDestroy() {
            Timing.KillCoroutines(PlaybackCoroutineHandle);

            ReadBuffer = null;
            SendBuffer = null;
            Owner = null;

            AllPlayers.Remove(this);
        }

        public bool TryPlay(string query) {
            if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
                return TryPlay(uri);

            TaskHelper.RunAsAsyncUnsafe(YouTubeSearchClient.SearchQueryTaskAsync(query, 1), matches => {
                if (matches is null || matches.Count <= 0) {
                    UpdateStatus($"No matches.");
                    return;
                }

                var match = matches.First();

                UpdateStatus($"Selected first match: {match.Title}");
                TryPlay(new Uri(match.Url));
            });

            return true;
        }

        private bool TryPlay(Uri uri) {
            TaskHelper.RunAsAsyncUnsafe(YouTubeDownloadClient.Videos.GetAsync(VideoId.Parse(uri.AbsoluteUri)), video => {
                if (video is null) {
                    UpdateStatus($"Failed to find your video.");
                    return;
                }

                TaskHelper.RunAsAsyncUnsafe(YouTubeDownloadClient.Videos.Streams.GetManifestAsync(video.Id), manifest => {
                    if (manifest is null) {
                        UpdateStatus($"Failed to find your video's manifest.");
                        return;
                    }

                    var available = manifest.Streams.Where(x => x.Bitrate.BitsPerSecond <= VoiceChatSettings.MaxBitrate);
                    var stream = available.FirstOrDefault();

                    if (stream is null) {
                        UpdateStatus($"That video does not have any available streams.");
                        return;
                    }

                    TaskHelper.RunAsAsyncUnsafe(YouTubeDownloadClient.Videos.Streams.GetAsync(stream), dStream => {
                        if (dStream is null) {
                            UpdateStatus($"Failed to fetch the video's audio stream.");
                            return;
                        }

                        byte[] bytes = null;

                        using (var memStream = new MemoryStream()) {
                            dStream.CopyTo(memStream);

                            bytes = memStream.ToArray();
                        }

                        dStream.Dispose();

                        UpdateStatus($"Downloaded {bytes.Length} bytes.");

                        TryPlay(new AudioTrack {
                            Data = bytes,
                            RequiresConvert = true,
                            RequiresDownload = false,
                            Url = uri.AbsoluteUri
                        });
                    });
                });
            });

            return true;
        }

        public bool TryConvert(AudioTrack track) {
            ThreadHelper.RunAsAsyncUnsafe(() => {
                var ffmpegConverter = new FfmpegConverter();
                var oggConverter = new OggConverter();
                var props = new ConversionProperties
                {
                    Channels = VoiceChatSettings.Channels,
                    SampleRate = VoiceChatSettings.SampleRate
                };

                if (ffmpegConverter.Convert(track.Data, props, out var ffmpegBytes)) {
                    UpdateStatus($"FFMPEG: Converted {ffmpegBytes.Length}/{track.Data.Length}");

                    if (oggConverter.Convert(ffmpegBytes, props, out var oggBytes)) {
                        UpdateStatus($"OGG: Converted {oggBytes.Length}/{ffmpegBytes.Length}/{track.Data.Length}");

                        track.Data = oggBytes;
                        track.RequiresConvert = false;

                        TryPlay(track);
                    }
                    else {
                        UpdateStatus("OGG conversion failed.");
                    }
                }
                else {
                    UpdateStatus("FFMPEG conversion failed.");
                }
            });

            return true;
        }

        public bool TryPlay(AudioTrack track) {
            if (CurrentTrack != null) {
                TrackQueue.Enqueue(track);
                UpdateStatus($"Queued: {track.Url}");
                return true;
            }

            if (track.RequiresConvert) {
                TryConvert(track);
                return false;
            }
            else {
                ForcePlay(track);
                return true;
            }
        }

        public void Stop() {
            CurrentTrack = null;

            Timing.KillCoroutines(PlaybackCoroutineHandle);

            UpdateStatus($"Stopped.");
        }

        public void Skip() {
            if (!TrackQueue.TryDequeue(out var next)) {
                Stop();
                UpdateStatus($"Stopped.");
            }
            else {
                Stop();
                TryPlay(next);

                UpdateStatus($"Skipped.");
            }
        }

        private void ForcePlay(AudioTrack track) {
            CurrentTrack = track;

            Timing.KillCoroutines(PlaybackCoroutineHandle);

            PlaybackCoroutineHandle = Timing.RunCoroutine(Playback());
        }

        public virtual void Update() {
            if (Speaker == null 
                || !IsReady 
                || StreamBuffer.Count == 0 
                || !ShouldPlay)
                return;

            Samples += Time.deltaTime * SamplesPerSecond;

            int toCopy = Mathf.Min(Mathf.FloorToInt(Samples), StreamBuffer.Count);
            if (toCopy > 0) {
                for (int i = 0; i < toCopy; i++) {
                    PlaybackBuffer.Write(StreamBuffer.Dequeue() * (Volume / 100f));
                }
            }

            Samples -= toCopy;

            while (PlaybackBuffer.Length >= 480) {
                PlaybackBuffer.ReadTo(SendBuffer, (long)480, 0L);
                int dataLen = Encoder.Encode(SendBuffer, EncodedBuffer, 480);

                foreach (var plr in ReferenceHub.AllHubs) {
                    if (plr.connectionToClient == null) 
                        continue;

                    if (Mutes.Contains(plr.UserId()))
                        continue;

                    if (Blacklist.Contains(plr.UserId()))
                        continue;

                    if (Whitelist.Count > 0 && !Whitelist.Contains(plr.UserId()))
                        continue;

                    plr.connectionToClient.Send(new VoiceMessage(Speaker, VoiceChannel, EncodedBuffer, dataLen, false));
                }
            }
        }

        public virtual IEnumerator<float> Playback() {

            ShouldStop = false;

            PlaybackStream = new MemoryStream(CurrentTrack.Data);
            PlaybackStream.Seek(0, SeekOrigin.Begin);

            Reader = new VorbisReader(PlaybackStream);
            SamplesPerSecond = VoiceChatSettings.SampleRate * VoiceChatSettings.Channels;
            SendBuffer = new float[SamplesPerSecond / 5 + HeadSamples];
            ReadBuffer = new float[SamplesPerSecond / 5 + HeadSamples];

            int cnt;
            while ((cnt = Reader.ReadSamples(ReadBuffer, 0, ReadBuffer.Length)) > 0) {
                if (ShouldStop) {
                    Reader.SeekTo(Reader.TotalSamples - 1);
                    ShouldStop = false;
                }
                while (!ShouldPlay) {
                    yield return Timing.WaitForOneFrame;
                }
                while (StreamBuffer.Count >= ReadBuffer.Length) {
                    IsReady = true;
                    yield return Timing.WaitForOneFrame;
                }
                for (int i = 0; i < ReadBuffer.Length; i++) {
                    StreamBuffer.Enqueue(ReadBuffer[i]);
                }
            }

            OnTrackFinishedHandler(CurrentTrack);
        }

        private void OnTrackFinishedHandler(AudioTrack track) {
            if (IsLooping) {
                ForcePlay(track);
                return;
            }

            if (TrackQueue.TryDequeue(out var next))
                TryPlay(next);
        }

        private void UpdateStatus(object message) {
            if (Owner is null)
                return;

            Owner.ConsoleMessage(message);
            Owner.queryProcessor.TargetReply(Owner.connectionToClient, message.ToString(), true, true, "");
        }

        public static AudioPlayer Create(ReferenceHub owner, ReferenceHub speaker) {
            var player = speaker.gameObject.AddComponent<AudioPlayer>();

            player.Setup(owner, speaker);

            return player;
        }

        public static bool TryGet(ReferenceHub owner, out AudioPlayer player) {
            player = AllPlayers.FirstOrDefault(x => x.Owner != null && x.Owner.netId == owner.netId) ?? owner.GetComponent<AudioPlayer>();
            return player != null;
        }

        private static void OnRoundRestart(object[] args) {
            foreach (var player in AllPlayers) {
                GameObject.Destroy(player);
            }

            AllPlayers.Clear();
        }
    }
}