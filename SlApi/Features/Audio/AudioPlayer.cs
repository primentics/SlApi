using MEC;

using NVorbis;

using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Networking;
using VoiceChat.Codec.Enums;

using SlApi.Features.ThreadingHelpers;
using SlApi.Features.Audio.Conversion.Ffmpeg;
using SlApi.Features.Audio.Conversion.Ogg;
using SlApi.Events;
using SlApi.Events.CustomHandlers;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using YoutubeSearch;
using YoutubeExplode;
using YoutubeExplode.Videos;

using PluginAPI.Core;

using UnityEngine;

using Utils.NonAllocLINQ;

using SlApi.Extensions;
using YoutubeExplode.Videos.Streams;

namespace SlApi.Features.Audio
{
    public class AudioPlayer
    {
        public const int HeadSamples = 1920;
        public const int EncodeBufferSize = 512;
        public const int MaxPlaybackSize = 480;

        public static HashSet<AudioPlayer> AllPlayers { get; } = new HashSet<AudioPlayer>();
        public static HashSet<string> Mutes { get; } = new HashSet<string>();

        public static VideoSearch YouTubeSearchClient { get; private set; } = new VideoSearch();
        public static YoutubeClient YouTubeDownloadClient { get; private set; } = new YoutubeClient();

        private CoroutineHandle _playbackCoroutine;

        public bool IsEnabled { get; set; }
        public bool SendToSpeaker { get; set; } 

        public ReferenceHub Speaker { get; set; }
        public ReferenceHub Owner { get; set; }

        public AudioTrack CurrentTrack { get; private set; }

        public HashSet<VoiceChatChannel> Channels { get; private set; } = new HashSet<VoiceChatChannel>()
        { 
            VoiceChatChannel.Proximity
        };

        public HashSet<string> Blacklisted { get; private set; } = new HashSet<string>();
        public HashSet<string> Whitelisted { get; private set; } = new HashSet<string>();

        public Queue<AudioTrack> TrackQueue { get; private set; } = new Queue<AudioTrack>();

        public float Volume { get; set; } = 100f;

        public bool IsReady { get; set; }
        public bool IsLooping { get; set; }

        public bool ShouldStop { get; set; }
        public bool ShouldPlay { get; set; }

        internal Queue<float> StreamBuffer { get; private set; } = new Queue<float>();

        internal PlaybackBuffer PlaybackBuffer { get; private set; }
        internal MemoryStream PlaybackStream { get; private set; }
        internal VorbisReader Reader { get; private set; }
        internal OpusEncoder Encoder { get; private set; }

        internal int SamplesPerSecond { get; private set; }

        internal float Samples { get; private set; }

        internal float[] SendBuffer { get; private set; }
        internal float[] ReadBuffer { get; private set; }

        internal byte[] EncodeBuffer { get; private set; }

        public event Action<AudioTrack> OnTrackFinished;
        public event Action<AudioTrack> OnTrackStarted;
        public event Action<AudioTrack> OnTrackQueued;
        public event Action<AudioTrack> OnTrackStopped;

        static AudioPlayer()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
        }

        internal AudioPlayer(ReferenceHub owner, ReferenceHub speaker) 
        {
            Owner = owner;
            Speaker = speaker;

            StaticUnityMethods.OnUpdate += OnUpdate;

            PlaybackBuffer = new PlaybackBuffer();
            Encoder = new OpusEncoder(OpusApplicationType.Audio);
            EncodeBuffer = new byte[EncodeBufferSize];

            OnTrackFinished += OnTrackFinishedHandler;

            AllPlayers.Add(this);
        }

        public void Dispose()
        {
            Timing.KillCoroutines(_playbackCoroutine);

            OnTrackFinished -= OnTrackFinishedHandler;
            StaticUnityMethods.OnUpdate -= OnUpdate;

            PlaybackStream.Dispose();
            Reader.Dispose();
            PlaybackBuffer.Dispose();
            Encoder.Dispose();
            StreamBuffer.Clear();
            TrackQueue.Clear();
            Channels.Clear();
            Blacklisted.Clear();
            Whitelisted.Clear();

            EncodeBuffer = null;
            ReadBuffer = null;
            SendBuffer = null;
            StreamBuffer = null;
            TrackQueue = null;
            Channels = null;
            Speaker = null;
            Owner = null;
            CurrentTrack = null;
            YouTubeDownloadClient = null;
            YouTubeSearchClient = null;
            Whitelisted = null;
            Blacklisted = null;

            AllPlayers.Remove(this);
        }

        public bool TrySearch(AudioSearch search, out ThreadResult<AudioTrack> track, Action<ThreadResult<AudioTrack>> continueWith = null)
        {
            UpdateStatus($"TrySearch: {search.Query}");

            if (!Uri.TryCreate(search.Query, UriKind.Absolute, out var uri))
                return TrySearchQuery(search, out track, continueWith);

            UpdateStatus($"Created an URI: {uri.AbsoluteUri}");

            track = ThreadHelper.RunThread(() =>
            {
                UpdateStatus($"Downloading ..");

                var video = YouTubeDownloadClient.Videos.GetAsync(uri.AbsoluteUri).GetAwaiter().GetResult();

                if (video is null)
                {
                    UpdateStatus($"Failed to fetch your video.");
                    return null;
                }

                var manifest = YouTubeDownloadClient.Videos.Streams.GetManifestAsync(video.Id).GetAwaiter().GetResult();

                if (manifest is null)
                {
                    UpdateStatus("Failed to fetch the video's manifest.");
                    return null;
                }

                var available = manifest.Streams.Where(x => x.Bitrate.BitsPerSecond <= VoiceChatSettings.MaxBitrate);
                var stream = available.FirstOrDefault();

                if (stream is null)
                {
                    UpdateStatus($"That video does not have any available streams.");
                    return null;
                }

                var dStream = YouTubeDownloadClient.Videos.Streams.GetAsync(stream).GetAwaiter().GetResult();

                if (dStream is null)
                {
                    UpdateStatus($"Failed to download the audio stream.");
                    return null;
                }

                var memStream = new MemoryStream();

                dStream.CopyToAsync(memStream).GetAwaiter().GetResult();

                var bytes = memStream.ToArray();

                dStream.Dispose();
                memStream.Dispose();

                UpdateStatus($"Downloaded {bytes.Length} bytes.");

                return new AudioTrack
                {
                    Data = bytes,
                    RequiresConvert = true,
                    RequiresDownload = false,
                    Url = uri.AbsoluteUri
                };
            }, continueWith);

            return true;
        }

        public bool TrySearchQuery(AudioSearch search, out ThreadResult<AudioTrack> track, Action<ThreadResult<AudioTrack>> continueWith = null)
        {
            UpdateStatus($"TrySearchQuery: {search.Query}");

            track = ThreadHelper.RunThread(() =>
            {
                try
                {
                    UpdateStatus($"Loading query results ..");

                    var queryRes = YouTubeSearchClient.SearchQuery(search.Query, 1);

                    UpdateStatus($"Loaded {queryRes.Count} query results.");

                    if (queryRes is null || queryRes.Count < 0)
                    {
                        UpdateStatus($"Failed to find a video matching your query.");
                        return null;
                    }

                    queryRes = queryRes.Where(x => VideoId.TryParse(x.Url).HasValue).ToList();

                    if (queryRes is null || queryRes.Count < 0)
                    {
                        UpdateStatus($"Failed to find a video matching your query.");
                        return null;
                    }

                    UpdateStatus($"Downloading {queryRes.First().Url} ..");

                    var result = new AudioTrack();
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var video = await YouTubeDownloadClient.Videos.GetAsync(queryRes.First().Url);

                            if (video is null)
                            {
                                UpdateStatus($"Failed to fetch your video.");
                                return;
                            }

                            var manifest = YouTubeDownloadClient.Videos.Streams.GetManifestAsync(video.Id).GetAwaiter().GetResult();

                            if (manifest is null)
                            {
                                UpdateStatus("Failed to fetch the video's manifest.");
                                return;
                            }

                            var available = manifest.Streams.Where(x => x.Bitrate.BitsPerSecond <= VoiceChatSettings.MaxBitrate);
                            var stream = available.FirstOrDefault();

                            if (stream is null)
                            {
                                UpdateStatus($"That video does not have any available streams.");
                                return;
                            }

                            var dStream = YouTubeDownloadClient.Videos.Streams.GetAsync(stream).GetAwaiter().GetResult();

                            if (dStream is null)
                            {
                                UpdateStatus($"Failed to download the audio stream.");
                                return;
                            }

                            var memStream = new MemoryStream();

                            dStream.CopyToAsync(memStream).GetAwaiter().GetResult();

                            var bytes = memStream.ToArray();

                            dStream.Dispose();
                            memStream.Dispose();

                            UpdateStatus($"Downloaded {bytes.Length} bytes.");

                            result.Data = bytes;
                            result.Url = video.Url;
                            result.RequiresDownload = false;
                            result.RequiresConvert = true;
                        }
                        catch (Exception ex)
                        {
                            result = null;

                            Log.Error(ex.ToString());
                        }
                    });

                    task.Wait();

                    return result;
                }
                catch (Exception ex)
                {
                    UpdateStatus(ex);
                    return null;
                }
            }, continueWith);

            return true;
        }

        public bool TryConvert(AudioTrack track, out ThreadResult<AudioTrack> converted, Action<ThreadResult<AudioTrack>> continueWith = null)
        {
            UpdateStatus($"Converting ..");

            converted = ThreadHelper.RunThread(() =>
            {
                var ffmpegConverter = new FfmpegConverter();
                var oggConverter = new OggConverter();

                if (track.RequiresDownload)
                {
                    UpdateStatus($"Downloading ..");

                    var video = YouTubeDownloadClient.Videos.GetAsync(track.Url).GetAwaiter().GetResult();

                    if (video is null)
                    {
                        UpdateStatus($"Failed to fetch your video.");
                        return null;
                    }

                    var manifest = YouTubeDownloadClient.Videos.Streams.GetManifestAsync(video.Id).GetAwaiter().GetResult();

                    if (manifest is null)
                    {
                        UpdateStatus("Failed to fetch the video's manifest.");
                        return null;
                    }

                    var available = manifest.Streams.Where(x => x.Bitrate.BitsPerSecond <= VoiceChatSettings.MaxBitrate);
                    var stream = available.FirstOrDefault();

                    if (stream is null)
                    {
                        UpdateStatus($"That video does not have any available streams.");
                        return null;
                    }

                    var dStream = YouTubeDownloadClient.Videos.Streams.GetAsync(stream).GetAwaiter().GetResult();

                    if (dStream is null)
                    {
                        UpdateStatus($"Failed to download the audio stream.");
                        return null;
                    }

                    var memStream = new MemoryStream();

                    dStream.CopyToAsync(memStream).GetAwaiter().GetResult();

                    var bytes = memStream.ToArray();

                    dStream.Dispose();
                    memStream.Dispose();

                    UpdateStatus($"Downloaded {bytes.Length} bytes.");

                    track.Data = bytes;
                }

                UpdateStatus($"Converting to MP3 using FFMPEG ..");

                if (ffmpegConverter.Convert(track.Data, new Conversion.ConversionProperties
                {
                    Channels = VoiceChatSettings.Channels,
                    SampleRate = VoiceChatSettings.SampleRate
                }, out var ffmpegConverted))
                {
                    UpdateStatus($"FFMPEG: Converted - {ffmpegConverted.Length} bytes.");
                    UpdateStatus($"Converting to OGG ..");

                    if (oggConverter.Convert(ffmpegConverted, new Conversion.ConversionProperties
                    {
                        Channels = VoiceChatSettings.Channels,
                        SampleRate = VoiceChatSettings.SampleRate
                    }, out var convertedBytes))
                    {
                        UpdateStatus($"OGG: Converted - {convertedBytes.Length} bytes.");

                        return new AudioTrack
                        {
                            Data = convertedBytes,
                            RequiresConvert = false,
                            RequiresDownload = false,
                            Url = track.Url
                        };
                    }
                    else
                    {
                        UpdateStatus($"OGG conversion failed!");
                        return null;
                    }
                }

                UpdateStatus($"FFMPEG conversion failed'");

                return null;
            }, continueWith);

            return true;
        }

        public bool TryPlay(AudioTrack track)
        {
            UpdateStatus($"TryPlay: {track.Url}");

            if (CurrentTrack != null)
            {
                TrackQueue.Enqueue(track);
                OnTrackQueued?.Invoke(track);
                UpdateStatus($"Queued: {track.Url}");
                return true;
            }

            if (track.RequiresConvert || track.RequiresDownload)
            {
                UpdateStatus($"Requested track requires conversion.");

                TryConvert(track, out var res, x =>
                {
                    if (!x.IsError)
                    {
                        TryPlay(x.Result);
                    }
                    else
                    {
                        Log.Error($"Conversion failed: {x.Exception?.ToString() ?? "unknown error"}", "SL API::AudioPlayer");
                    }
                });
                return false;
            }
            else
            {
                ForcePlay(track);
                return true;
            }
        }

        public void Stop()
        {
            CurrentTrack = null;

            Timing.KillCoroutines(_playbackCoroutine);

            PlaybackStream.Dispose();
            Reader.Dispose();
            PlaybackBuffer.Dispose();
            Encoder.Dispose();
            StreamBuffer.Clear();
            TrackQueue.Clear();

            UpdateStatus($"Stopped.");
        }

        public void Skip()
        {
            if (!TrackQueue.TryDequeue(out var next))
            {
                Stop();
                UpdateStatus($"Stopped.");
            }
            else
            {
                Stop();
                TryPlay(next);

                UpdateStatus($"Skipped.");
            }
        }

        private void ForcePlay(AudioTrack track)
        {
            CurrentTrack = track;

            Timing.KillCoroutines(_playbackCoroutine);

            _playbackCoroutine = Timing.RunCoroutine(PlaybackCoroutine());

            UpdateStatus($"ForcePlay: {track.Url}");
        }

        private void OnUpdate()
        {
            if (!IsEnabled
                || !IsReady
                || !ShouldPlay
                || StreamBuffer.Count <= 0
                || Speaker is null)
                return;

            Samples += Time.deltaTime * SamplesPerSecond;

            var copy = Mathf.Min(Mathf.FloorToInt(Samples), StreamBuffer.Count);
            if (copy > 0)
            {
                for (int i = 0; i < copy; i++)
                {
                    PlaybackBuffer.Write(StreamBuffer.Dequeue() * (Volume / 100f));
                }
            }

            Samples -= copy;
            while (PlaybackBuffer.Length >= MaxPlaybackSize)
            {
                PlaybackBuffer.ReadTo(SendBuffer, MaxPlaybackSize);

                var dataLen = Encoder.Encode(SendBuffer, EncodeBuffer, MaxPlaybackSize);

                foreach (var channel in Channels)
                {
                    var msg = new VoiceMessage(Speaker, channel, EncodeBuffer, dataLen, false);

                    ReferenceHub.AllHubs.ForEach(x =>
                    {
                        if (x.Mode != ClientInstanceMode.ReadyClient)
                            return;

                        if (x.netId == Speaker.netId && !SendToSpeaker)
                            return;

                        if (Blacklisted.Contains(x.characterClassManager.UserId))
                            return;

                        if (Whitelisted.Any() && !Whitelisted.Contains(x.characterClassManager.UserId))
                            return;

                        if (Mutes.Contains(x.characterClassManager.UserId))
                            return;

                        x.connectionToClient.Send(msg);
                    });
                }
            }
        }

        private IEnumerator<float> PlaybackCoroutine()
        {
            ShouldStop = false;
            ShouldPlay = true;

            if (CurrentTrack is null)
                yield break;

            PlaybackStream = new MemoryStream(CurrentTrack.Data);
            PlaybackStream.Seek(0, SeekOrigin.Begin);

            Reader = new VorbisReader(PlaybackStream);

            if (Reader.Channels != VoiceChatSettings.Channels)
            {
                Log.Error("Only mono audio is supported.", "SL API::AudioPlayer");

                Reader.Dispose();
                PlaybackStream.Dispose();

                OnTrackStopped?.Invoke(CurrentTrack);

                yield break;
            }

            if (Reader.SampleRate != VoiceChatSettings.SampleRate)
            {
                Log.Error("Sample rate mismatch.", "SL API::AudioPlayer");

                Reader.Dispose();
                PlaybackStream.Dispose();

                OnTrackStopped?.Invoke(CurrentTrack);

                CurrentTrack = null;

                yield break;
            }

            SamplesPerSecond = VoiceChatSettings.SampleRate * VoiceChatSettings.Channels;

            SendBuffer = new float[(SamplesPerSecond / 5) + HeadSamples];
            ReadBuffer = new float[(SamplesPerSecond / 5) + HeadSamples];

            UpdateStatus($"Channels: {Reader.Channels}");
            UpdateStatus($"Sample rate: {Reader.SampleRate}");
            UpdateStatus($"Samples per sec: {SamplesPerSecond}");
            UpdateStatus($"Buffers: {SendBuffer.Length} / {ReadBuffer.Length}");

            OnTrackStarted?.Invoke(CurrentTrack);

            UpdateStatus($"Starting playback ..");

            int cnt;
            while ((cnt = Reader.ReadSamples(ReadBuffer, 0, ReadBuffer.Length)) > 0)
            {
                if (ShouldStop)
                {
                    Reader.SeekTo(Reader.TotalSamples - 1);
                    ShouldStop = false;
                }

                while (!ShouldPlay)
                    yield return Timing.WaitForOneFrame;

                while (StreamBuffer.Count >= ReadBuffer.Length)
                {
                    IsReady = true;
                    yield return Timing.WaitForOneFrame;
                }

                for (int i = 0; i < ReadBuffer.Length; i++)
                    StreamBuffer.Enqueue(ReadBuffer[i]);
            }

            OnTrackStopped?.Invoke(CurrentTrack);
            OnTrackFinished?.Invoke(CurrentTrack);
            CurrentTrack = null;

            UpdateStatus($"Playback finished.");
        }

        private void OnTrackFinishedHandler(AudioTrack track)
        {
            if (TrackQueue.TryDequeue(out var next))
                TryPlay(next);
        }

        private void UpdateStatus(object message)
        {
            if (Owner is null)
                return;

            Owner.ConsoleMessage(message);
        }

        public static AudioPlayer Create(ReferenceHub owner, ReferenceHub speaker)
            => new AudioPlayer(owner, speaker);

        public static bool TryGet(ReferenceHub owner, out AudioPlayer player)
        {
            player = AllPlayers.FirstOrDefault(x => x.Owner != null && x.Owner.netId == owner.netId);
            return player != null;
        }

        private static void OnRoundRestart(object[] args)
        {
            foreach (var player in AllPlayers)
            {
                player.Dispose();
            }

            AllPlayers.Clear();
        }
    }
}