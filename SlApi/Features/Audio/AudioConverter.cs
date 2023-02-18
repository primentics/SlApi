using MEC;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;

using VoiceChat;

using System.Threading;

using SlApi.Extensions;
using SlApi.Configs;

namespace SlApi.Audio
{
    public enum AudioPreset
    {
        fast,
        veryfast,
        slow,
        veryslow
    }

    public class AudioConverter
    {
        [Config("Ffmpeg.Preset", "The preset to use for audio conversion.")]
        public static AudioPreset Preset { get; set; } = AudioPreset.veryslow;

        public string Mp4FilePath { get; private set; }
        public string RawFilePath { get; private set; }

        public volatile TaskResult<byte[]> OggResult;

        public Process FfmpegProcess { get; private set; }

        public AudioConverter()
        {
            OggResult = new TaskResult<byte[]>();

            Mp4FilePath = Path.ChangeExtension(Path.GetTempFileName(), "mp4");
            RawFilePath = Path.ChangeExtension(Path.GetTempFileName(), "raw");

            FfmpegProcess = new Process();
            FfmpegProcess.StartInfo = new ProcessStartInfo()
            {
                Arguments = $"-i {Mp4FilePath} -ac {VoiceChatSettings.Channels} " +
                            $"-preset {Preset} -acodec pcm_s16le -f s16le -ar {VoiceChatSettings.SampleRate} {RawFilePath}",
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = "ffmpeg",
            };
        }

        public void Convert(byte[] input)
        {
            Timing.RunCoroutine(ConvertCoroutine(input));
        }

        private IEnumerator<float> ConvertCoroutine(byte[] input)
        {
            File.WriteAllBytes(Mp4FilePath, input);

            try
            {
                FfmpegProcess.Start();
                FfmpegProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                Logger.Debug(ex);
                yield break;
            }

            yield return Timing.WaitUntilTrue(() => FfmpegProcess.HasExited);

            FfmpegProcess.Dispose();

            File.Delete(Mp4FilePath);

            new Thread(() =>
            {
                try
                {
                    OggEncoder.EncodeRawPcm(
                        VoiceChatSettings.SampleRate,
                        VoiceChatSettings.Channels,

                        input,

                        PcmSample.SixteenBit,

                        VoiceChatSettings.SampleRate,
                        VoiceChatSettings.Channels,

                        OggResult);
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex);

                    OggResult.IsSuccesfull = false;
                    OggResult.Error = ex;
                    OggResult.IsFinished = true;
                }
            }).Start();
        }
    }
}