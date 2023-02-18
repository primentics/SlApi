using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;

using HarmonyLib;

using RemoteAdmin;

using SlApi.Extensions;
using SlApi.Features.Tags;

using System;

namespace SlApi.Patches.Feature.PersistentTags
{
    [HarmonyPatch(typeof(HideTagCommand), nameof(HideTagCommand.Execute))]
    public static class HideTagCommand_Patch
    {
        public static bool Prefix(ref bool __result, 
            ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            PlayerCommandSender playerCommandSender;

            if ((playerCommandSender = (sender as PlayerCommandSender)) == null)
            {
                response = "You must be in-game to use this command!";
                __result = false;
                return false;
            }

            ServerRoles serverRoles = playerCommandSender.ReferenceHub.serverRoles;

            if (!serverRoles.BypassStaff)
            {
                if (!string.IsNullOrEmpty(serverRoles.HiddenBadge))
                {
                    response = "Your badge is already hidden.";
                    __result = false;
                    return false;
                }
                if (string.IsNullOrEmpty(serverRoles.MyText))
                {
                    response = "Your don't have any badge.";
                    __result = false;
                    return false;
                }
            }

            serverRoles.GlobalHidden = serverRoles.GlobalSet;
            serverRoles.HiddenBadge = serverRoles.MyText;
            serverRoles.NetworkGlobalBadge = null;
            serverRoles.SetText(null);
            serverRoles.SetColor(null);
            serverRoles.RefreshHiddenTag();
            response = "Tag hidden!";

            if (PersistentTagController.PersistentTags.Remove(serverRoles._hub.characterClassManager.UserId))
                serverRoles._hub.ConsoleMessage("[Persistent Tags] Your tag won't be shown automatically.");

            __result = true;
            return false;
        }
    }
}