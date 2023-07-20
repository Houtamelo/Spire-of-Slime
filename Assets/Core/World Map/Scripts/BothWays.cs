using System;
using System.Collections.Generic;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World_Map.Scripts
{
    [Serializable]
    public struct BothWays : IEquatable<BothWays>
    {
        [SerializeField, ValidateInput(nameof(IsOriginAndDestinationDifferent), defaultMessage: "Origin and destination must be different")] 
        public LocationEnum One;
        
        [SerializeField, ValidateInput(nameof(IsOriginAndDestinationDifferent), defaultMessage: "Origin and destination must be different")]
        public LocationEnum Two;

        private bool IsOriginAndDestinationDifferent() => One != Two;

        public BothWays(LocationEnum one, LocationEnum two)
        {
            One = one;
            Two = two;
            if (one == two)
            {
                Debug.LogWarning($"Trying to create a BothWays with the same origin/destination: {Enum<LocationEnum>.ToString(one)}");
                Debug.Break();
            }
        }

        public void Deconstruct(out LocationEnum from, out LocationEnum to)
        {
            from = One;
            to = Two;
        }


        public override bool Equals(object obj) => obj is BothWays y && ((y.One == One && y.Two == Two) || (y.Two == One && y.One == Two));

        public bool Equals(BothWays y) => (y.One == One && y.Two == Two) || (y.Two == One && y.One == Two);

        public override int GetHashCode()
        {
            unchecked
            {
                LocationEnum one = One;
                LocationEnum two = Two;
                int oneId = (int)one;
                int twoId = (int)two;
                if (twoId > oneId)
                    return oneId ^ (6 + twoId) ^ 12;

                return twoId ^ (6 + oneId) ^ 12;
            }
        }


        public static bool operator ==(BothWays left, BothWays right) => left.Equals(y: right);
        public static bool operator !=(BothWays left, BothWays right) => !left.Equals(y: right);
        public static implicit operator (LocationEnum, LocationEnum)(BothWays bothWays) => (bothWays.One, bothWays.Two);
        public static implicit operator BothWays((LocationEnum, LocationEnum) bothWays) => new(bothWays.Item1, bothWays.Item2);

        public override string ToString() => $"{One.FormattedName()} - {Two.FormattedName()}";

        private sealed class BothWaysComparer : IEqualityComparer<BothWays>
        {
            public bool Equals(BothWays x, BothWays y)
            {
                bool equals = (x.One == y.One && x.Two == y.Two) || (x.Two == y.One && x.One == y.Two);
                return equals;
            }

            public int GetHashCode(BothWays obj)
            {
                LocationEnum one = obj.One;
                LocationEnum two = obj.Two;
                int oneId = (int)one;
                int twoId = (int)two;
                if (twoId > oneId)
                    return oneId ^ (6 + twoId) ^ 12;

                return twoId ^ (6 + oneId) ^ 12;
            }
        }

        public static readonly IEqualityComparer<BothWays> OneTwoComparer = new BothWaysComparer();
    }
}