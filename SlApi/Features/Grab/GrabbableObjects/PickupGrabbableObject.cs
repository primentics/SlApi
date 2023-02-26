using InventorySystem.Items.Pickups;

using UnityEngine;

namespace SlApi.Features.Grab.GrabbableObjects
{
    public class PickupGrabbableObject : PlayerGrabbableObjectBase
    {
        public ItemPickupBase GrabbedItem { get; set; }

        public override bool ReturnToPosition { get; } = false;

        public override void Grab(GameObject target, ReferenceHub grabbedBy)
        {
            base.Grab(target, grabbedBy);

            GrabbedItem = target.GetComponent<ItemPickupBase>();
        }

        public override void Stop()
        {
            base.Stop();

            GrabbedItem = null;
        }

        public override void SetTargetPosition()
        {
            GrabbedItem.transform.position = TargetPosition;
            GrabbedItem.transform.rotation = TargetRotation;
            GrabbedItem.RefreshPositionAndRotation();
        }
    }
}
