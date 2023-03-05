using OggVorbisEncoder;

using System;
using System.IO;

namespace SlApi.Features.Audio.Conversion.Ogg
{
    public class OggConverter : IConverter
    {
        public const int WriteBufferSize = 512;

        public bool Convert(byte[] input, ConversionProperties properties, out byte[] output) {
            var numPcmSamples = input.Length / 2 / properties.Channels;
            var pcmDuration = numPcmSamples / (float)properties.SampleRate;
            var numOutputSamples = (int)(pcmDuration * properties.SampleRate);
            numOutputSamples = (numOutputSamples / WriteBufferSize) * WriteBufferSize;
            var outSamples = new float[properties.Channels][];

            for (int ch = 0; ch < properties.Channels; ch++) {
                outSamples[ch] = new float[numOutputSamples];
            }

            for (int sampleNumber = 0; sampleNumber < numOutputSamples; sampleNumber++) {
                for (int ch = 0; ch < properties.Channels; ch++) {
                    var sampleIndex = (sampleNumber * properties.Channels) * 2;
                    if (ch < properties.Channels)
                        sampleIndex += (ch * 2);
                    outSamples[ch][sampleNumber] = ((short)(input[sampleIndex + 1] << 8 | input[sampleIndex])) / 32768f;
                }
            }

            var outputData = new MemoryStream();
            var info = VorbisInfo.InitVariableBitRate(properties.Channels, properties.SampleRate, 0.5f);
            var serial = new Random().Next();
            var oggStream = new OggStream(serial);
            var comments = new Comments();
            comments.AddTag("ARTIST", "TEST");
            var infoPacket = HeaderPacketBuilder.BuildInfoPacket(info);
            var commentsPacket = HeaderPacketBuilder.BuildCommentsPacket(comments);
            var booksPacket = HeaderPacketBuilder.BuildBooksPacket(info);

            oggStream.PacketIn(infoPacket);
            oggStream.PacketIn(commentsPacket);
            oggStream.PacketIn(booksPacket);

            void FlushPages(Stream outp, bool force) {
                while (oggStream.PageOut(out OggPage page, force)) {
                    outp.Write(page.Header, 0, page.Header.Length);
                    outp.Write(page.Body, 0, page.Body.Length);
                }
            }

            FlushPages(outputData, true);

            var processingState = ProcessingState.Create(info);

            for (int readIndex = 0; readIndex <= outSamples[0].Length; readIndex += WriteBufferSize) {
                if (readIndex == outSamples[0].Length) {
                    processingState.WriteEndOfStream();
                }
                else {
                    processingState.WriteData(outSamples, WriteBufferSize, readIndex);
                }

                while (!oggStream.Finished && processingState.PacketOut(out OggPacket packet)) {
                    oggStream.PacketIn(packet);

                    FlushPages(outputData, false);
                }
            }

            FlushPages(outputData, true);

            output = outputData.ToArray();
            outputData.Dispose();

            return true;
        }
    }
}
