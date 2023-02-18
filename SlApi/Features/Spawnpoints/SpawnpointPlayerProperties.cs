namespace SlApi.Features.Spawnpoints
{
    public class SpawnpointPlayerProperties
    {
        public bool ClearItems { get; set; } = true;

        public bool Freeze { get; set; } = true;

        public bool GhostToNonAdmin { get; set; } = true;
        public bool GhostToSpectators { get; set; } = true;

        public bool GodMode { get; set; } = true;
    }
}