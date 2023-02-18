using HarmonyLib;

using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Keycards;

using Respawning;

using UnityEngine;

namespace SlApi.Patches.Feature.RemoteCard
{
    [HarmonyPatch(typeof(PlayerInteract), nameof(PlayerInteract.UserCode_CmdSwitchAWButton))]
    public static class PlayerInteract_UserCode_CmdSwitchAWButton_Patch
    {
        internal static AlphaWarheadOutsitePanel _outsitePanelScript;

        public static AlphaWarheadOutsitePanel OutsitePanelScript
        {
            get
            {
                if (_outsitePanelScript is null)
                    _outsitePanelScript = GameObject.Find("OutsitePanelScript").GetComponentInParent<AlphaWarheadOutsitePanel>();

                return _outsitePanelScript;
            }
        }

        public static bool Prefix(PlayerInteract __instance)
        {
            if (!__instance.CanInteract)
                return false;

            if (OutsitePanelScript is null)
            {
                Logger.Warn("PlayerInteract::UserCode_CmdSwitchAWButton: OutsitePanelScript is missing!");
                return false;
            }

            if (!__instance.ChckDis(OutsitePanelScript.transform.position))
                return false;

            if (__instance._sr.BypassMode
                || (__instance._hub.inventory.CurInstance != null
                   && __instance._hub.inventory.CurInstance is KeycardItem keycardItem
                   && keycardItem.Permissions.HasFlagFast(KeycardPermissions.AlphaWarhead))
                || Features.RemoteKeycard.RemoteCard.CanOpenControls(__instance._hub))
            {
                if (OutsitePanelScript.keycardEntered)
                    return false;

                __instance.OnInteract();

                OutsitePanelScript.NetworkkeycardEntered = true;

                if (__instance._hub.TryGetAssignedSpawnableTeam(out var team))
                    RespawnTokensManager.GrantTokens(team, 1f);
            }

            return false;
        }
    }
}
