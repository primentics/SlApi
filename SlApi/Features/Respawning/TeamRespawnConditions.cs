namespace SlApi.Features.Respawning {
    public class TeamRespawnConditions {
        public int MaxSpawns { get; set; } = -1;
        public int MinWaveSize { get; set; } = -1;
        public int MaxWaveSize { get; set; } = -1;

        public bool TargetedRoleAlive { get; set; } = true;
    }
}
