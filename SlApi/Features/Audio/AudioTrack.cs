namespace SlApi.Features.Audio
{
    public class AudioTrack
    {
        public string Url { get; set; }

        public byte[] Data { get; set; }

        public bool RequiresDownload { get; set; }
        public bool RequiresConvert { get; set; }
    }
}