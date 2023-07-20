using System;
using System.Collections.Generic;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Interfaces
{
    public interface ITick : IEquatable<ITick>
    {
        void Tick(TSpan timeStep);
        bool IEquatable<ITick>.Equals(ITick other) => EqualityComparer<ITick>.Default.Equals(this, other);
    }
}