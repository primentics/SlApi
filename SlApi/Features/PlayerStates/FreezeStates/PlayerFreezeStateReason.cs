namespace SlApi.Features.PlayerStates.FreezeStates
{
    public enum PlayerFreezeStateReason : byte
    {
        ByRemoteAdmin = 0x25,
        BySpawnpointManager = 0x50
    }
}