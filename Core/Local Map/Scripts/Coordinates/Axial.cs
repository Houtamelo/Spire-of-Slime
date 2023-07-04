// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace Core.Local_Map.Scripts.Coordinates
{
    [Serializable]
    public struct Axial : IEquatable<Axial>
    {
        [ShowInInspector] public int q;
        [ShowInInspector] public int r;
        public int s => -q - r;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Axial(int q1, int r1)
        {
            q = q1;
            r = r1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Abs() => Mathf.Abs(value: q) + Mathf.Abs(value: r) + Mathf.Abs(value: s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNeighborOrEqual(ref Axial other) => PathUtils.ManhattanDistance(this, other) <= 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axial operator +(Axial left, Axial right) => new(q1: left.q + right.q, r1: left.r + right.r);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axial operator -(Axial left, Axial right) => new(q1: left.q - right.q, r1: left.r - right.r);

        public bool Equals(Axial other) => q == other.q && r == other.r;

        public override bool Equals( object obj )
        {
            if ( obj == null  || GetType() != obj.GetType())
                return false;
            
            return this == (Axial) obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + q.GetHashCode();
                hash = hash * 23 + r.GetHashCode();
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Axial left, Axial right) => left.q == right.q && left.r == right.r;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Axial left, Axial right) => left.q != right.q || left.r != right.r;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axial operator *(Axial left, int right) => new(q1: left.q * right, r1: left.r * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axial operator *(int left, Axial right) => new(q1: right.q * left, r1: right.r * left);
        
        public static readonly Axial Zero = new(q1: 0, r1: 0);
        private static readonly float Sqrt3 = Mathf.Sqrt(3);
        private static readonly float Sqrt3Half = Sqrt3 / 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"({q.ToString()},{r.ToString()})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToWorldCoordinates(float size)
        {
            float x = size * (Sqrt3 * q + Sqrt3Half * r);
            float y = size * (-1.5f * r);
            return new Vector3(x, y, 0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Offset ToOffset()
        {
            int col = q + (r - (r & 1)) / 2;
            int row = r;
            return new Offset(col: col, row: row);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float PolarAngleDeg()
        {
            Vector3 pos = ToWorldCoordinates(size: 1);
            return Mathf.Atan2(y: pos.y, x: pos.x) * Mathf.Rad2Deg;
        }
        
        public static Axial FromPolarAngleDeg(float polarAngleDeg, int length)
        {
            using PooledObject<HashSet<Axial>> pool = PathUtils.GetRing(center: Zero, radius: length, results: out HashSet<Axial> ring);
            float closestDistance = float.MaxValue;
            Axial bestMatch = ring.First();
            foreach (Axial cube in ring)
            {
                float candidate = cube.PolarAngleDeg();
                float distance = Mathf.Abs(Mathf.DeltaAngle(current: candidate, target: polarAngleDeg));
                if (distance < closestDistance)
                {
                    bestMatch = cube;
                    closestDistance = distance;
                }
            }
            
            return bestMatch;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetData() => $"{q.ToString()},{r.ToString()}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axial FromData(string data)
        {
            string[] split = data.Split(separator: ',');
            return new Axial(q1: int.Parse(s: split[0]), r1: int.Parse(s: split[1]));
        }

        public static bool TryParseData(string data, out Axial axial)
        {
            if (string.IsNullOrEmpty(data))
            {
                axial = Zero;
                return false;
            }
            
            string[] split = data.Split(separator: ',');
            if (split.Length != 2)
            {
                axial = Zero;
                return false;
            }
            
            if (!int.TryParse(s: split[0], result: out int q) || !int.TryParse(s: split[1], result: out int r))
            {
                axial = Zero;
                return false;
            }
            
            axial = new Axial(q1: q, r1: r);
            return true;
        }
    }
}