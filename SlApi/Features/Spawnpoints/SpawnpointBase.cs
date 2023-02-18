using PlayerRoles;

using SlApi.Configs.Objects;

namespace SlApi.Features.Spawnpoints
{
    public class SpawnpointBase
    {
        public string Name { get; set; } = "example";

        public RoleTypeId[] AcceptedRoles { get; set; } = new RoleTypeId[]
        {
            RoleTypeId.Tutorial
        };

        public SpawnpointPlayerProperties AdminProperties { get; set; } = new SpawnpointPlayerProperties();
        public SpawnpointPlayerProperties PlayerProperties { get; set; } = new SpawnpointPlayerProperties();

        public Vector Position { get; set; } = Vector.Get(0f, 0f, 0f);

        public float Bounds { get; set; } = 5f;

        public int MaxPlayers { get; set; } = -1;
        public int MaxNonAdminPlayers { get; set; } = -1;

        public byte VoiceChannelId { get; set; } = 0;
    }
}