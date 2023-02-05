using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;

using Mirror;

namespace SlApi.Extensions
{
    public static class DoorExtensions
    {
        public static void ToggleState(this DoorVariant door)
            => door.NetworkTargetState = !door.NetworkTargetState;

        public static void Destroy(this DoorVariant door, bool deleteIfNotBreakable = false)
        {
            if (door is BreakableDoor breakableDoor)
                breakableDoor.Network_destroyed = true;
            else if (deleteIfNotBreakable)
                NetworkServer.Destroy(door.gameObject);
        }
    }
}