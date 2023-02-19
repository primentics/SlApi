namespace SlApi.Features.Audio.Conversion
{
    public interface IConverter
    {
        bool Convert(byte[] input, ConversionProperties properties, out byte[] output);
    }
}