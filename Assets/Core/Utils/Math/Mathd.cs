using UnityEngine;

namespace Core.Utils.Math
{
    public static class Mathd
    {
        public static int CeilToInt(double value) => Mathf.CeilToInt((float)value);
        public static int FloorToInt(double value) => Mathf.FloorToInt((float)value);
    }
}