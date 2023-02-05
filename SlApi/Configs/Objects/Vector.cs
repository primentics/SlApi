using UnityEngine;

namespace SlApi.Configs.Objects
{
    public class Vector
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public static Vector Get(float x, float y, float z)
            => new Vector
            {
                x = x,
                y = y,
                z = z
            };

        public static Vector3 FromVector(Vector v)
            => new Vector3(v.x, v.y, v.z);

        public static Vector FromVector(Vector3 v)
            => Get(v.x, v.y, v.z);
    }
}