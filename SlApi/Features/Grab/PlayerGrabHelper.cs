using UnityEngine;

namespace SlApi.Features.Grab
{
    public static class PlayerGrabHelper
    {
        public static readonly LayerMask GrabMask = LayerMask.GetMask("Hitbox", "Door", "Locker", "Pickup", "Default");

        public static bool TryGetObject(ReferenceHub hub, out GameObject target)
        {
            if (!Physics.Raycast(hub.PlayerCameraReference.position, hub.PlayerCameraReference.forward, 
                out var hit, 20f, GrabMask))
            {
                target = null;
                return false;
            }

            if (hit.transform.parent != null)
            {
                target = hit.transform.parent.gameObject;
                return true;
            }
            else
            {
                target = hit.transform.gameObject;
                return true;
            }
        }
    }
}