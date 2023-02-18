using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System;

using MEC;

using NVorbis;

using UnityEngine;

using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;
using VoiceChat.Networking;

using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeSearch;

using System.Linq;

using SlApi.Extensions;
using SlApi.Dummies;

namespace SlApi.Audio
{
    // Most of this code belongs to ced777rick's SCPSLAudioApi (https://github.com/CedModV2/SCPSLAudioApi), I've only implemented YouTube playback.

    public class AudioPlayer : MonoBehaviour
    {
        public const int HeadSamples = 1920;
        public const int EncodeBufferSize = 512;
        public const int MaxPlaybackSize = 480;

        private static readonly VideoSearch _search = new VideoSearch();
        private static readonly YoutubeClient _yt = new YoutubeClient();
        private static readonly HashSet<AudioPlayer> _allPlayers = new HashSet<AudioPlayer>();

        private CoroutineHandle _playbackCoroutine;

        private readonly ConcurrentQueue<string> _requestQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<Video> _trackQueue = new ConcurrentQueue<Video>();
        private readonly Queue<float> _streamBuffer = new Queue<float>();

        private MemoryStream _playbackStream;
        private PlaybackBuffer _playbackBuffer = new PlaybackBuffer();
        private VorbisReader _reader;

        private readonly OpusEncoder _encoder = new OpusEncoder(OpusApplicationType.Voip);

        private int _sampsPerSec;
        private float[] _sendBuffer;
        private float[] _readBuffer;
        private byte[] _encodeBuffer = new byte[EncodeBufferSize];

        private float _samples;
        private bool _ready;
        private bool _stop;

        public Video Track;
        public ReferenceHub Owner;
        public DummyPlayer Player;
        public ReferenceHub CurrentCommand;

        public AudioSettings AudioSettings = new AudioSettings
        {
            Loop = false,
            Play = true,
            Volume = 100f
        };

        public void Start()
        {
            _allPlayers.Add(this);
        }

        public void TryPlay(string url)
        {
            if (Track != null)
            {
                _requestQueue.Enqueue(url);

                WriteCommand($"Request queued: {url}");

                return;
            }

            if (_playbackCoroutine.IsValid)
                Timing.KillCoroutines(_playbackCoroutine);

            _playbackCoroutine = Timing.RunCoroutine(Playback(url), Segment.FixedUpdate);

            WriteCommand($"Attempting to play your request ..");
        }

        public void Search(string query)
        {
            Timing.RunCoroutine(SearchDelay(query));
        }

        public void TryPlay(Video track)
        {
            if (_playbackCoroutine.IsValid)
                Timing.KillCoroutines(_playbackCoroutine);

            Track = track;

            _playbackCoroutine = Timing.RunCoroutine(Playback(null), Segment.FixedUpdate);
        }

        public void OnDestroy()
        {
            Stop(true);

            _allPlayers.Remove(this);

            Timing.KillCoroutines(_playbackCoroutine);

            Player.Destroy();
            Player = null;
            Owner = null;
            Track = null;
            CurrentCommand = null;
        }

        public void Stop(bool clear = false)
        {
            Track = null;

            _stop = true;
            AudioSettings.Play = false;

            if (_playbackCoroutine.IsValid)
                Timing.KillCoroutines(_playbackCoroutine);

            if (clear)
                ClearQueue();
        }

        public void Pause()
        {
            AudioSettings.Play = false;

            WriteCommand($"Paused.");
        }

        public void Resume()
        {
            AudioSettings.Play = true;

            WriteCommand("Resumed.");
        }

        public void Skip()
        {
            Stop();

            if (_trackQueue.TryDequeue(out var track))
                TryPlay(track);
            else if (_requestQueue.TryDequeue(out var url))
                TryPlay(url);

            WriteCommand("Skipped.");
        }

        public void ClearQueue()
        {
            while (_trackQueue.TryDequeue(out _))
                continue;
        }

        private IEnumerator<float> SearchDelay(string query)
        {
            WriteCommand($"Searching for: {query} ..");

            var searchTask = new TaskResult<List<VideoInformation>>();

            TaskUtils.RunTask(_search.SearchQueryTaskAsync(query, 1, 0, true), searchTask);

            yield return searchTask.Wait();

            if (!searchTask.IsSuccesfull)
            {
                WriteCommand($"Search failed.");

                if (searchTask.Error != null)
                    WriteCommand($"{searchTask.Error.Message}");

                yield break;
            }

            var vid = searchTask.Result.FirstOrDefault();

            if (vid == null)
            {
                WriteCommand("Search failed: No results.");

                yield break;
            }

            TryPlay(vid.Url);
        }

        private IEnumerator<float> Playback(string url)
        {
            _stop = false;
            AudioSettings.Play = true;

            if (Track is null)
            {
                WriteCommand($"Searching: {url}");

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    WriteCommand($"Failed to create a valid URL!");
                    yield break;
                }

                var searchTask = new TaskResult<Video>();

                TaskUtils.RunTask(_yt.Videos.GetAsync(uri.AbsoluteUri), searchTask);

                yield return searchTask.Wait();

                if (!searchTask.IsSuccesfull || searchTask.Result == null)
                {
                    WriteCommand("Search failed!");
                    yield break;
                }

                Track = searchTask.Result;
            }

            if (AudioSettings.Loop)
            {
                ClearQueue();

                _trackQueue.Enqueue(Track);
            }

            WriteCommand($"Retrived video: {Track.Title} (by {Track.Author.ChannelTitle})");

            var manifestTask = new TaskResult<StreamManifest>();

            TaskUtils.RunTask(_yt.Videos.Streams.GetManifestAsync(Track.Id), manifestTask);

            yield return manifestTask.Wait();

            if (!manifestTask.IsSuccesfull)
            {
                WriteCommand($"Failed to retrieve the video's manifest!");
                yield break;
            }

            var audioStream = manifestTask.Result.GetAudioStreams().GetWithHighestBitrate();

            if (audioStream == null)
            {
                WriteCommand("Failed to retrieve an audio stream!");
                yield break;
            }

            var streamTask = new TaskResult<Stream>();

            TaskUtils.RunTask(_yt.Videos.Streams.GetAsync(audioStream), streamTask);

            yield return streamTask.Wait();

            if (!streamTask.IsSuccesfull)
            {
                WriteCommand($"Failed to retrieve audio stream data!");
                yield break;
            }

            WriteCommand("Downloading your audio ..");

            var sourceStream = streamTask.Result;
            var destinationStream = new MemoryStream();

            bool copyFinished = false;

            try
            {
                sourceStream.CopyToAsync(destinationStream).ContinueWith(x =>
                {
                    copyFinished = true;
                });
            }
            catch (Exception ex)
            {
                Logger.Debug($"{ex}");
            }

            yield return Timing.WaitUntilTrue(() => copyFinished);

            var data = destinationStream.ToArray();

            destinationStream.Flush();
            destinationStream.Dispose();
            sourceStream.Dispose();

            WriteCommand("Audio downloaded, converting. This might take a minute.");

            var ffmpeg = new AudioConverter();

            try
            {
                ffmpeg.Convert(data);
            }
            catch (Exception ex)
            {
                WriteCommand(ex);

                yield break;
            }

            yield return ffmpeg.OggResult.Wait();

            if (!ffmpeg.OggResult.IsSuccesfull)
            {
                WriteCommand("Failed to convert the audio file!");

                if (ffmpeg.OggResult.Error != null)
                    WriteCommand(ffmpeg.OggResult.Error);

                yield break;
            }

            WriteCommand("Audio file converted.");

            _playbackStream = new MemoryStream(ffmpeg.OggResult.Result);
            _playbackStream.Seek(0, SeekOrigin.Begin);
            _reader = new VorbisReader(_playbackStream);

            if (_reader.Channels > VoiceChatSettings.Channels)
            {
                WriteCommand("Failed: audio must be mono!");

                _reader.Dispose();
                _playbackStream.Dispose();

                yield break;
            }

            if (_reader.SampleRate != VoiceChatSettings.SampleRate)
            {
                WriteCommand("Failed: sample rate must be 48 000");

                _reader.Dispose();
                _playbackStream.Dispose();

                yield break;
            }

            WriteCommand($"Playing: {Track.Title} (by {Track.Author.ChannelTitle})");

            _sampsPerSec = VoiceChatSettings.SampleRate * VoiceChatSettings.Channels;
            _sendBuffer = new float[_sampsPerSec / 5 + HeadSamples];
            _readBuffer = new float[_sampsPerSec / 5 + HeadSamples];

            int cnt;
            while ((cnt = _reader.ReadSamples(_readBuffer, 0, _readBuffer.Length)) > 0)
            {
                if (_stop)
                {
                    _reader.SeekTo(_reader.TotalSamples - 1);
                    _stop = false;
                }

                while (!AudioSettings.Play)
                    yield return Timing.WaitForOneFrame;

                while (_streamBuffer.Count >= _readBuffer.Length)
                {
                    _ready = true;
                    yield return Timing.WaitForOneFrame;
                }

                for (int i = 0; i < _readBuffer.Length; i++)
                    _streamBuffer.Enqueue(_readBuffer[i]);
            }

            Track = null;

            if (_trackQueue.TryDequeue(out var track))
                TryPlay(track);
            else if (_requestQueue.TryDequeue(out var reqUrl))
                TryPlay(reqUrl);
        }

        public void Update()
        {
            if (Owner == null
                || !_ready
                || _streamBuffer.Count == 0
                || !AudioSettings.Play)
                return;

            _samples += Time.deltaTime * _sampsPerSec;

            int toCopy = Mathf.Min(Mathf.FloorToInt(_samples), _streamBuffer.Count);

            if (toCopy > 0)
            {
                for (int i = 0; i < toCopy; i++)
                {
                    _playbackBuffer.Write(_streamBuffer.Dequeue() * (AudioSettings.Volume / 100f));
                }
            }

            _samples -= toCopy;

            while (_playbackBuffer.Length >= MaxPlaybackSize)
            {
                _playbackBuffer.ReadTo(_sendBuffer, MaxPlaybackSize);

                int dataLen = _encoder.Encode(_sendBuffer, _encodeBuffer, MaxPlaybackSize);

                Player?.Speak(_encodeBuffer, dataLen);
            }
        }

        public static void DestroyAll()
        {
            foreach (var player in _allPlayers)
                UnityEngine.Object.Destroy(player);

            _allPlayers.Clear();
        }

        public static AudioPlayer GetPlayer(ReferenceHub hub)
            => hub.gameObject.GetComponent<AudioPlayer>();  

        public static AudioPlayer GetOrCreatePlayer(ReferenceHub owner)
        {
            if (!owner.gameObject.TryGetComponent(out AudioPlayer player))
            {
                player = owner.gameObject.AddComponent<AudioPlayer>();
                player.Owner = owner;
                player.Player = new DummyPlayer(owner);
                player.Player.VoiceChannel = VoiceChatChannel.Proximity;

                Timing.CallDelayed(0.3f, () =>
                {
                    player.Player.RoleId = owner.roleManager.CurrentRole.RoleTypeId;
                    player.Player.Position = owner.GetRealPosition();
                    player.Player.Rotation = owner.GetRealRotation();
                });
            }

            return player;
        }

        private void WriteCommand(object message)
        {
            if (CurrentCommand != null)
            {
                CurrentCommand.ConsoleMessage($"[Audio Player] {message}", "red");
                CurrentCommand.queryProcessor.TargetReply(
                    CurrentCommand.connectionToClient,
                    $"[Audio Player] {message}",
                    true,
                    false,
                    "");
            }
        }
    }
}