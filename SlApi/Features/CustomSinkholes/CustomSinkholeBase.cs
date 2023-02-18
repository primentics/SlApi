using System.Collections.Generic;

using UnityEngine;

namespace SlApi.Features.CustomSinkholes
{
    public class CustomSinkholeBase
    {
        public int CurDuration { get; set; }
        public int MaxDuration { get; set; } = -1;
        public int MaxAmount { get; set; } = 1;
        public int SpawnedInstances { get; set; } = 0;
        public int SpawnedThisRound { get; set; } = 0;
        public int Chance { get; set; } = 0;

        public float Bounds { get; set; } = 0f;

        public bool IsCopy { get; set; }

        public HashSet<ReferenceHub> SteppedPlayers { get; set; }

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public virtual CustomSinkholeBase Prefab { get; set; }

        public virtual CustomSinkholeBase DoCopy() { return null; }

        public virtual GameObject GetPrefab() { return null; }

        public virtual bool CheckSpawnInterval() { return false; }
        public virtual bool CheckRoundState() { return false; }

        public virtual void Spawn() { }
        public virtual void Despawn() { }

        public virtual void IncrementSpawnDuration() { }

        public virtual void OnPlayer(ReferenceHub hub) { }
    }
}