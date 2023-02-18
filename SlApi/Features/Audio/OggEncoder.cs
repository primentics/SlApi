using OggVorbisEncoder;

using System;
using System.IO;

using SlApi.Extensions;

namespace SlApi.Audio
{
    public enum PcmSample : int
    {
        EightBit = 1,
        SixteenBit = 2
    }

    public static class OggEncoder
    {
        public const int WriteBufferSize = 512;

        public static void EncodeRawPcm(
            int outputSampleRate, 
            int outputChannels, 

            byte[] pcmSamples, 

            PcmSample pcmSampleSize, 

            int pcmSampleRate, 
            int pcmChannels,

            TaskResult<byte[]> result)
        {
            try
            {
                int numPcmSamples = (pcmSamples.Length / (int)pcmSampleSize / pcmChannels);
                float pcmDuraton = numPcmSamples / (float)pcmSampleRate;

                int numOutputSamples = (int)(pcmDuraton * outputSampleRate);
                numOutputSamples = (numOutputSamples / WriteBufferSize) * WriteBufferSize;

                float[][] outSamples = new float[outputChannels][];

                for (int ch = 0; ch < outputChannels; ch++)
                {
                    outSamples[ch] = new float[numOutputSamples];
                }

                for (int sampleNumber = 0; sampleNumber < numOutputSamples; sampleNumber++)
                {
                    float rawSample = 0.0f;

                    for (int ch = 0; ch < outputChannels; ch++)
                    {
                        int sampleIndex = (sampleNumber * pcmChannels) * (int)pcmSampleSize;

                        if (ch < pcmChannels) sampleIndex += (ch * (int)pcmSampleSize);

                        switch (pcmSampleSize)
                        {
                            case PcmSample.EightBit:
                                rawSample = ByteToSample(pcmSamples[sampleIndex]);
                                break;
                            case PcmSample.SixteenBit:
                                rawSample = ShortToSample((short)(pcmSamples[sampleIndex + 1] << 8 | pcmSamples[sampleIndex]));
                                break;
                        }

                        outSamples[ch][sampleNumber] = rawSample;
                    }
                }

                result.Result = BytesToFileFormat(outSamples, outputSampleRate, outputChannels);
                result.Error = null;
                result.IsSuccesfull = true;
                result.IsFinished = true;
            }
            catch (Exception ex)
            {
                result.Result = null;
                result.Error = ex;
                result.IsSuccesfull = false;
                result.IsFinished = true;
            }
        }

        private static byte[] BytesToFileFormat(float[][] floatSamples, int sampleRate, int channels)
        {
            var outputData = new MemoryStream();
            var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, 0.5f);
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

        private static void FlushPages(OggStream oggStream, Stream output, bool force)
        {
            while (oggStream.PageOut(out OggPage page, force))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
            }
        }

        private static float ByteToSample(short pcmValue)
            => pcmValue / 128f;

        private static float ShortToSample(short pcmValue)
            => pcmValue / 32768f;
    }
}