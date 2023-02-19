using System.Diagnostics;
using System.IO;

namespace SlApi.Features.Audio.Conversion.Ffmpeg
{
    public class FfmpegConverter : IConverter
    {
        private Process _process;

        private string _mp4Output;
        private string _rawOutput;

        public FfmpegConverter()
        {
            _process = new Process();
            _process.StartInfo = new ProcessStartInfo("ffmpeg");
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;

            _mp4Output = Path.ChangeExtension(Path.GetTempFileName(), "mp4");
            _rawOutput = Path.ChangeExtension(Path.GetTempFileName(), "raw");
        }

        public bool Convert(byte[] input, ConversionProperties properties, out byte[] output)
        {
            _process.StartInfo.Arguments = FormulateString(properties);

            File.WriteAllBytes(_mp4Output, input);

            _process.Start();
            _process.WaitForExit();

            while (!_process.HasExited)
                continue;

            _process.Dispose();
            _process = null;

            File.Delete(_mp4Output);

            output = File.ReadAllBytes(_rawOutput);

            File.Delete(_rawOutput);

            return true;
        }

        private string FormulateString(ConversionProperties properties)
        {
            return $"-i {_mp4Output} -ac {properties.Channels} " +
                   $"-preset veryslow -acodec pcm_s16le -f s16le -ar {properties.SampleRate} {_rawOutput}";
        }
    }
}
