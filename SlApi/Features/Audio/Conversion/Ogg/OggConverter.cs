using OggVorbisEncoder;

using System;
using System.IO;

namespace SlApi.Features.Audio.Conversion.Ogg
{
    public class OggConverter : IConverter
    {
        public const int WriteBufferSize = 512;

        public bool Convert(byte[] input, ConversionProperties properties, out byte[] output)
        {
            var numPcmSamples = input.Length / 2 / properties.Channels;
            var pcmDuration = numPcmSamples / (float)properties.SampleRate;

            var numOutputSamples = (int)(pcmDuration * properties.SampleRate);

            numOutputSamples = (numOutputSamples / WriteBufferSize) * WriteBufferSize;

            float[][] outSamples = new float[properties.Channels][];

            for (int ch = 0; ch < properties.Channels; ch++)
            {
                outSamples[ch] = new float[numOutputSamples];
            }

            for (int sampleNumber = 0; sampleNumber < numOutputSamples; sampleNumber++)
            {
                for (int ch = 0; ch < properties.Channels; ch++)
                {
                    int sampleIndex = (sampleNumber * properties.Channels) * 2;

                    if (ch < properties.Channels) 
                        sampleIndex += (ch * 2);

                    outSamples[ch][sampleNumber] = ShortToSample((short)(input[sampleIndex + 1] << 8 | input[sampleIndex]));
                }
            }

            output = GetBytes(outSamples, properties);
            return true;
        }

        private byte[] GetBytes(float[][] floatSamples, ConversionProperties properties)
        {
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

            FlushPages(oggStream, outputData, true);

            var processingState = ProcessingState.Create(info);

            for (int readIndex = 0; readIndex <= floatSamples[0].Length; readIndex += WriteBufferSize)
            {
                if (readIndex == floatSamples[0].Length)
                {
                    processingState.WriteEndOfStream();
                }
                else
                {
                    processingState.WriteData(floatSamples, WriteBufferSize, readIndex);
                }

                while (!oggStream.Finished && processingState.PacketOut(out OggPacket packet))
                {
                    oggStream.PacketIn(packet);

                    FlushPages(oggStream, outputData, false);
                }
            }

            FlushPages(oggStream, outputData, true);

            var data = outputData.ToArray();

            outputData.Dispose();

            return data;
        }

        private void FlushPages(OggStream oggStream, Stream output, bool force)
        {
            while (oggStream.PageOut(out OggPage page, force))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
            }
        }

        private float ShortToSample(short pcmValue)
            => pcmValue / 32768f;
    }
}
