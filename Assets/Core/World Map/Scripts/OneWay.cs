using System;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.World_Map.Scripts
{
    [Serializable]
    public struct OneWay // represents a path between two locations, origin and destination are RELEVANT
    {
        [SerializeField, ValidateInput(nameof(IsOriginAndDestinationDifferent))]
        public LocationEnum Origin;
        
        [SerializeField, ValidateInput(nameof(IsOriginAndDestinationDifferent))]
        public LocationEnum Destination;
        
        private bool IsOriginAndDestinationDifferent() => Origin != Destination;

        public OneWay(LocationEnum origin, LocationEnum destination)
        {
            Origin = origin;
            Destination = destination;
            
            if (origin == destination)
            {
                Debug.LogWarning($"Trying to create a BothWays with the same origin/destination: {Enum<LocationEnum>.ToString(origin)}");
                Debug.DebugBreak();
            }
        }

        public void Deconstruct(out LocationEnum origin, out LocationEnum destination)
        {
            origin = Origin;
            destination = Destination;
        }

        public override bool Equals(object obj) => obj is OneWay tuple && tuple.Equals(other: this);

        public bool Equals(OneWay other) => other.Origin == Origin && other.Destination == Destination;

        public override int GetHashCode()
        {
            unchecked
            {
                return Origin.GetHashCode() ^ (50 * Destination.GetHashCode()) ^ 5;
            }
        }

        public static bool operator ==(OneWay left, OneWay right) => left.Equals(other: right);
        public static bool operator !=(OneWay left, OneWay right) => !left.Equals(other: right);
        public static implicit operator BothWays(OneWay singleWay) => new(singleWay.Origin, singleWay.Destination);
        public static implicit operator (LocationEnum, LocationEnum)(OneWay singleWay) => (singleWay.Origin, singleWay.Destination);
        public static implicit operator OneWay((LocationEnum, LocationEnum) tuple) => new(tuple.Item1, tuple.Item2);

        public override string ToString() => $"{Enum<LocationEnum>.ToString(Origin)} -> {Enum<LocationEnum>.ToString(Destination)}";
    }
}