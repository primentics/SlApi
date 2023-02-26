using AdminToys;

using Mirror;
using PluginAPI.Core;
using System.Linq;

using UnityEngine;

namespace SlApi.Features.Grab
{
    public class PlayerGrabbableObjectBase
    {
        public static readonly Color RedColor = new Color(1f, 0f, 0f);

        public bool Active { get; set; }

        public virtual bool ShouldSpawnLight { get; } = false;
        public virtual bool ReturnToPosition { get; } = true;

        public GameObject Target { get; set; }
        public ReferenceHub GrabbedBy { get; set; }
        public LightSourceToy Light { get; set; }

        public Vector3 InitialPosition { get; set; }

        public Vector3 TargetPosition { get; set; }
        public Quaternion TargetRotation { get; set; }

        public virtual void Update()
        {
            if (!Active)
                return;

            Log.Debug($"Update", "SL API::Grab");

            if (Light is null && ShouldSpawnLight)
            {
                Light = SpawnLight();
                Light.NetworkLightColor = RedColor;
            }

            TargetPosition = GrabbedBy.PlayerCameraReference.forward * 2f;
            TargetRotation = Quaternion.LookRotation(Target.transform.position - GrabbedBy.transform.position, Vector3.up);

            SetTargetPosition();
            SetTargetRotation();

            if (Light != null && ShouldSpawnLight)
                SetLightProperties();

            Log.Debug($"Update End", "SL API::Grab");
        }

        public virtual void Grab(GameObject target, ReferenceHub grabbedBy)
        {
            Active = true;
            Target = target;
            GrabbedBy = grabbedBy;
            InitialPosition = target.transform.position;

            Log.Debug($"Grabbed: {target.name}", "SL API::Grab");
        }

        public virtual void Stop()
        {
            Log.Debug($"Stopping", "SL API::Grab");

            Active = false;

            if (ReturnToPosition)
            {
                TargetPosition = InitialPosition;
                SetTargetPosition();

                Log.Debug($"Returned to position", "SL API::Grab");
            }

            Target = null;
            GrabbedBy = null;

            if (Light != null)
                NetworkServer.Destroy(Light.gameObject);

            Light = null;

            Log.Debug($"Stopped", "SL API::Grab");
        }

        public virtual void SetLightProperties()
        {
            Light.NetworkPosition = TargetPosition;

            Log.Debug($"Set light position", "SL API::Grab");
        }

        public virtual void SetTargetRotation()
        {

        }

        public virtual void SetTargetPosition()
        {
            NetworkServer.UnSpawn(Target);
            Target.transform.position = TargetPosition;
            NetworkServer.Spawn(Target);

            Log.Debug($"Set position", "SL API::Grab");
        }

        public static LightSourceToy SpawnLight()
        {
            Log.Debug($"Spawning light", "SL API::Grab");

            var prefab = NetworkClient.prefabs.FirstOrDefault(x => x.Value.TryGetComponent(out AdminToyBase adminToyBase) 
                && adminToyBase.CommandName is "LightSource").Value;

            if (prefab is null)
                return null;

            Log.Debug($"Prefab retrieved", "SL API::Grab");

            return LightSourceToy.Instantiate(prefab).GetComponentInParent<LightSourceToy>();
        }
    }
}
