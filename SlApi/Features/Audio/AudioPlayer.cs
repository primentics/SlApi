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

namespace SlApi.Audio
{
    // Most of this code belongs to ced777rick's SCPSLAudioApi (https://github.com/CedModV2/SCPSLAudioApi), I've only implemented YouTube playback.

    public class AudioPlayer : MonoBehaviour
    {
        public const int HeadSamples = 1920;
        public const int EncodeBufferSize = 512;
        public const int MaxPlaybackSize = 480;

        public static readonly HashSet<int> BlacklistedSelf = new HashSet<int>();

        private static readonly YoutubeSearch.VideoSearch _search = new YoutubeSearch.VideoSearch();
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

        public AudioSettings AudioSettings = new AudioSettings
        {
            Channel = VoiceChatChannel.Proximity,
            Loop = false,
            Play = true,
            Volume = 100f
        };

        public readonly HashSet<int> Whitelisted = new HashSet<int>();
        public readonly HashSet<int> Blacklisted = new HashSet<int>();

        public void Start()
        {
            _allPlayers.Add(this);
        }

        public void TryPlay(string url, AudioCommandChannel cmd)
        {
            if (Track != null)
            {
                _requestQueue.Enqueue(url);

                cmd.Write($"Request queued: {url}");

                return;
            }

            if (_playbackCoroutine.IsValid)
                Timing.KillCoroutines(_playbackCoroutine);

            _playbackCoroutine = Timing.RunCoroutine(Playback(url, cmd), Segment.FixedUpdate);

            cmd.Write($"Attempting to play your request ..");
        }

        public void Search(string query, AudioCommandChannel cmd)
        {
            Timing.RunCoroutine(SearchDelay(query, cmd));
        }

        public void TryPlay(Video track, AudioCommandChannel cmd)
        {
            if (_playbackCoroutine.IsValid)
                Timing.KillCoroutines(_playbackCoroutine);

            Track = track;

            _playbackCoroutine = Timing.RunCoroutine(Playback(null, cmd), Segment.FixedUpdate);
        }

        public void OnDestroy()
        {
            Stop(true);

            _allPlayers.Remove(this);

            Timing.KillCoroutines(_playbackCoroutine);
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

        public void Pause(AudioCommandChannel cmd)
        {
            AudioSettings.Play = false;

            cmd.Write($"Paused.");
        }

        public void Resume(AudioCommandChannel cmd)
        {
            AudioSettings.Play = true;

            cmd.Write("Resumed.");
        }

        public void Skip(AudioCommandChannel cmd)
        {
            Stop();

            if (_trackQueue.TryDequeue(out var track))
                TryPlay(track, cmd);
            else if (_requestQueue.TryDequeue(out var url))
                TryPlay(url, cmd);

            cmd.Write("Skipped.");
        }

        public void ClearQueue()
        {
            while (_trackQueue.TryDequeue(out _))
                continue;
        }

        private IEnumerator<float> SearchDelay(string query, AudioCommandChannel cmd)
        {
            cmd.Write($"Searching for: {query} ..");

            var searchTask = new TaskResult<List<VideoInformation>>();

            TaskUtils.RunTask(_search.SearchQueryTaskAsync(query, 1, 0, true), searchTask);

            yield return searchTask.Wait();

            if (!searchTask.IsSuccesfull)
            {
                cmd.Write($"Search failed.");

                if (searchTask.Error != null)
                    cmd.Write($"{searchTask.Error.Message}");

                yield break;
            }

            var vid = searchTask.Result.FirstOrDefault();

            if (vid == null)
            {
                cmd.Write("Search failed: No results.");

                yield break;
            }

            TryPlay(vid.Url, cmd);
        }

        private IEnumerator<float> Playback(string url, AudioCommandChannel cmd)
        {
            _stop = false;
            AudioSettings.Play = true;

            if (Track is null)
            {
                cmd.Write($"Searching: {url}");

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    cmd.Write($"Failed to create a valid URL!");
                    yield break;
                }

                var searchTask = new TaskResult<Video>();

                TaskUtils.RunTask(_yt.Videos.GetAsync(uri.AbsoluteUri), searchTask);

                yield return searchTask.Wait();

                if (!searchTask.IsSuccesfull || searchTask.Result == null)
                {
                    cmd.Write("Search failed!");
                    yield break;
                }

                Track = searchTask.Result;
            }

            if (AudioSettings.Loop)
            {
                ClearQueue();

                _trackQueue.Enqueue(Track);
            }

            cmd.Write($"Retrived video: {Track.Title} (by {Track.Author.ChannelTitle})");

            var manifestTask = new TaskResult<StreamManifest>();

            TaskUtils.RunTask(_yt.Videos.Streams.GetManifestAsync(Track.Id), manifestTask);

            yield return manifestTask.Wait();

            if (!manifestTask.IsSuccesfull)
            {
                cmd.Write($"Failed to retrieve the video's manifest!");
                yield break;
            }

            var audioStream = manifestTask.Result.GetAudioStreams().GetWithHighestBitrate();

            if (audioStream == null)
            {
                cmd.Write("Failed to retrieve an audio stream!");
                yield break;
            }

            var streamTask = new TaskResult<Stream>();

            TaskUtils.RunTask(_yt.Videos.Streams.GetAsync(audioStream), streamTask);

            yield return streamTask.Wait();

            if (!streamTask.IsSuccesfull)
            {
                cmd.Write($"Failed to retrieve audio stream data!");
                yield break;
            }

            cmd.Write("Downloading your audio ..");

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

            cmd.Write("Audio downloaded, converting. This might take a minute.");

            var ffmpeg = new AudioConverter();

            try
            {
                ffmpeg.Convert(data);
            }
            catch (Exception ex)
            {
                cmd.Write(ex);

                yield break;
            }

            yield return ffmpeg.OggResult.Wait();

            if (!ffmpeg.OggResult.IsSuccesfull)
            {
                cmd.Write("Failed to convert the audio file!");

                if (ffmpeg.OggResult.Error != null)
                    cmd.Write(ffmpeg.OggResult.Error);

                yield break;
            }

            cmd.Write("Audio file converted.");

            _playbackStream = new MemoryStream(ffmpeg.OggResult.Result);
            _playbackStream.Seek(0, SeekOrigin.Begin);
            _reader = new VorbisReader(_playbackStream);

            if (_reader.Channels > VoiceChatSettings.Channels)
            {
                cmd.Write("Failed: audio must be mono!");

                _reader.Dispose();
                _playbackStream.Dispose();

                yield break;
            }

            if (_reader.SampleRate != VoiceChatSettings.SampleRate)
            {
                cmd.Write("Failed: sample rate must be 48 000");

                _reader.Dispose();
                _playbackStream.Dispose();

                yield break;
            }

            cmd.Write($"Playing: {Track.Title} (by {Track.Author.ChannelTitle})");

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
                TryPlay(track, cmd);
            else if (_requestQueue.TryDequeue(out var reqUrl))
                TryPlay(reqUrl, cmd);
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

                foreach (var plr in ReferenceHub.AllHubs)
                {
                    if (plr.Mode != ClientInstanceMode.ReadyClient)
                        continue;

                    if (plr.connectionToClient is null)
                        continue;

                    if (Blacklisted.Contains(plr.GetInstanceID()))
                        continue;

                    if (Whitelisted.Count > 0 && !Whitelisted.Contains(plr.GetInstanceID()))
                        continue;

                    if (plr.GetInstanceID() == Owner.GetInstanceID())
                        continue;

                    if (BlacklistedSelf.Contains(plr.GetInstanceID()) && plr.GetInstanceID() != Owner.GetInstanceID())
                        continue;

                    plr.connectionToClient.Send(new VoiceMessage(
                        Owner,
                        AudioSettings.Channel,
                        _encodeBuffer,
                        dataLen,
                        false));
                }
            }
        }

        public static void DestroyAll()
        {
            foreach (var player in _allPlayers)
                UnityEngine.Object.Destroy(player);
        }

        public static AudioPlayer GetPlayer(ReferenceHub hub)
            => hub.gameObject.GetComponent<AudioPlayer>();  

        public static AudioPlayer GetOrCreatePlayer(ReferenceHub owner)
        {
            if (!owner.gameObject.TryGetComponent(out AudioPlayer player))
            {
                player = owner.gameObject.AddComponent<AudioPlayer>();
                player.Owner = owner;
            }

            return player;
        }
    }
}