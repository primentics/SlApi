using SlApi.Features.PlayerStates;
using SlApi.Features.Grab.GrabbableObjects;

using UnityEngine;

using InventorySystem.Items.Pickups;

using PluginAPI.Core;

namespace SlApi.Features.Grab
{
    public class PlayerGrabState : PlayerStateBase
    {
        public PlayerGrabState(ReferenceHub target) : base(target) { }

        public PlayerGrabbableObjectBase GrabbedObject { get; set; }

        public void GrabObject(PlayerGrabbableObjectBase grabbedObject, GameObject target)
        {
            Log.Debug($"Grabbing {grabbedObject} {target.name}", "SL API::Grab");

            DisposeState();

            GrabbedObject = grabbedObject;
            GrabbedObject.Grab(target, Target);
            GrabbedObject.Active = true;
        }

        public void UnGrab()
            => DisposeState();

        public void GrabPlayer(ReferenceHub target)
            => GrabObject(new PlayerGrabbableObject(), target.gameObject);

        public void GrabPickup(ItemPickupBase pickup)
            => GrabObject(new PickupGrabbableObject(), pickup.gameObject);

        public void GrabOther(GameObject gameObject)
            => GrabObject(new PlayerGrabbableObjectBase(), gameObject);

        public override bool CanUpdateState() 
            => GrabbedObject != null && GrabbedObject.Active;

        public override void DisposeState()
        {
            Log.Debug($"DisposeState", "SL API::Grab");

            if (GrabbedObject is null)
                return;

            GrabbedObject.Stop();
            GrabbedObject = null;
        }

        public override void OnDied()
        {
            if (GrabbedObject is null)
                return;

            GrabbedObject.Stop();
            GrabbedObject = null;
        }

        public override void OnRoleChanged()
        {
            if (GrabbedObject is null)
                return;

            GrabbedObject.Stop();
            GrabbedObject = null;
        }

        public override void UpdateState()
        {
            Log.Debug($"UpdateState", "SL API::Grab");

            if (GrabbedObject is null)
                return;

            GrabbedObject.Update();
        }
    }
}