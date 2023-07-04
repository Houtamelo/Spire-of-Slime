using System;
using System.Collections.Generic;

namespace Core.Combat.Scripts.Interfaces
{
    public interface ITick : IEquatable<ITick>
    {
        void Tick(float timeStep);
        bool IEquatable<ITick>.Equals(ITick other) => EqualityComparer<ITick>.Default.Equals(this, other);
    }
}