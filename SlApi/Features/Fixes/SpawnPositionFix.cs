using MEC;

using PlayerRoles.FirstPersonControl;

using PluginAPI.Core;

using SlApi.Configs;
using SlApi.Dummies;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;

using UnityEngine;

namespace SlApi.Features.Fixes
{
    public static class SpawnPositionFix
    {
        [Config("SpawnPositionFix.Enabled", "Whether or not to enable the spawn fix.")]
        public static bool Enabled = true;

        static SpawnPositionFix()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerSpawn, OnPlayerSpawned));
        }

        private static void OnPlayerSpawned(object[] args)
        {
            if (Enabled)
            {
                Timing.CallDelayed(0.4f, () =>
                {
                    var hub = (args[0] as Player).ReferenceHub;

                    if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
                        return;

                    var pos = hub.GetRealPosition();
                    if (pos.y < -1000f)
                    {
                        if (fpcRole.SpawnpointHandler != null 
                            && fpcRole.SpawnpointHandler.TryGetSpawnpoint(out pos, out var rot))
                        {
                            RespawnPlayer(fpcRole, pos, rot);
                            return;
                        }
                    }

                    if (fpcRole.SpawnpointHandler is null)
                        return;

                    if (fpcRole.SpawnpointHandler.TryGetSpawnpoint(out var spawnPos, out var spawnRot))
                    {
                        if (Vector3.Distance(spawnPos, pos) > 2f)
                        {
                            RespawnPlayer(fpcRole, spawnPos, spawnRot);
                            return;
                        }
                    }
                });
            }
        }

        private static void RespawnPlayer(IFpcRole role, Vector3 pos, float rot)
        {
            if (role.FpcModule is null)
                return;

            role.FpcModule.ServerOverridePosition(pos, Vector3.one * rot);
        }
    }
}
