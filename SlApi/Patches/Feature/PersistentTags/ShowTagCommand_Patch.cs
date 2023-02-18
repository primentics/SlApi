using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;

using HarmonyLib;

using RemoteAdmin;

using SlApi.Extensions;
using SlApi.Features.Tags;

using System;

namespace SlApi.Patches.Feature.PersistentTags
{
    [HarmonyPatch(typeof(ShowTagCommand), nameof(ShowTagCommand.Execute))]
    public static class ShowTagCommand_Patch
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
            serverRoles.HiddenBadge = null;
            serverRoles.GlobalHidden = false;
            serverRoles.RpcResetFixed();
            serverRoles.RefreshPermissions(true);
            response = "Local tag refreshed!";
            __result = true;

            if (PersistentTagController.PersistentTags.Add(serverRoles._hub.characterClassManager.UserId))
                serverRoles._hub.ConsoleMessage("[Persistent Tags] Your tag will now be shown automatically each round. Use hidetag to disable.");

            return false;
        }
    }
}