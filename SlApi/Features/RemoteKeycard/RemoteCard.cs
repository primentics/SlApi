using SlApi.Configs;

namespace SlApi.Features.RemoteKeycard
{
    public static class RemoteCard
    {
        [Config("RemoteCard.Doors", "Whether or not to affect doors.")]
        public static bool Doors { get; set; } = true;
    }
}
