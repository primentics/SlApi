using HarmonyLib;

using Mirror;

using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.Visibility;

using SlApi.Dummies;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.InvisibleStates;

namespace SlApi.Patches.Feature
{
    [HarmonyPatch(typeof(FpcServerPositionDistributor), nameof(FpcServerPositionDistributor.WriteAll))]
    public static class FpcServerPositionDistributor_WriteAll_Patch
    {
        public static bool Prefix(ReferenceHub receiver, NetworkWriter writer)
        {
            ushort index = 0;

            VisibilityController controller = null;

            if (receiver.roleManager.CurrentRole is ICustomVisibilityRole visRole)
                controller = visRole.VisibilityController;

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (DummyPlayer.TryGetDummy(hub, out var dummy) && (dummy.IsInvisible || dummy.IsInvisibleTo(receiver.netId)))
                    continue;
                else if (hub.netId != receiver.netId && hub.roleManager.CurrentRole is IFpcRole fpcRole)
                {
                    bool isInvisible = controller != null && !controller.ValidateVisibility(hub);

                    if (hub.TryGetState<InvisibilityState>(out var invisState))
                        isInvisible = !invisState.IsVisibleTo(receiver);

                    var syncData = FpcServerPositionDistributor.GetNewSyncData(receiver, hub, fpcRole.FpcModule, isInvisible);

                    if (!isInvisible)
                    {
                        FpcServerPositionDistributor._bufferPlayerIDs[index] = hub.PlayerId;
                        FpcServerPositionDistributor._bufferSyncData[index] = syncData;

                        index++;
                    }
                }
            }

            writer.WriteUInt16(index);

            for (int i = 0; i < index; i++)
            {
                writer.WriteRecyclablePlayerId(new RecyclablePlayerId(FpcServerPositionDistributor._bufferPlayerIDs[i]));

                FpcServerPositionDistributor._bufferSyncData[i].Write(writer);
            }

            return false;
        }
    }
}