using UnityEngine;

/// <summary>
/// Author: Josh H.
/// Support: assetstore.joshh@gmail.com
/// </summary>

namespace UIGradient.Scripts
{
    public static class Vector2Extension
    {
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(f: degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(f: degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }
    }
}