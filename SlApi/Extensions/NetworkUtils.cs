using InventorySystem.Items.Firearms.Modules;

using PlayerRoles;
using PlayerRoles.FirstPersonControl.NetworkMessages;

using UnityEngine;

namespace SlApi.Extensions
{
    public static class NetworkUtils
    {
        public static void SendRoleMessage(this ReferenceHub hub, RoleSyncInfo msg)
        {
            foreach (var ply in ReferenceHub.AllHubs)
            {
                if (ply.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (ply.GetInstanceID() == hub.GetInstanceID())
                    continue;

                ply.connectionToClient.Send(msg);
            }
        }

        public static void SendPositionMessage(this ReferenceHub hub, FpcOverrideMessage msg)
        {
            foreach (var ply in ReferenceHub.AllHubs)
            {
                if (ply.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (ply.GetInstanceID() == hub.GetInstanceID())
                    continue;

                ply.connectionToClient.Send(msg);
            }
        }

        public static void SendDisruptorHitMessage(Vector3 pos, Quaternion rot)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.connectionToClient == null)
                    continue;

                hub.connectionToClient.Send(new DisruptorHitreg.DisruptorHitMessage
                {
                    Position = pos,
                    Rotation = new LowPrecisionQuaternion(rot)
                });
            }
        }
    }
}