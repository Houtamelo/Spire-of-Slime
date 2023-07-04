using System;
using UnityEngine;

namespace Core.Character_Panel.Scripts.Skills
{
    [Serializable]
    public struct SerializableBounds
    {
        [SerializeField]
        private Vector3 lowerLeft;

        [SerializeField]
        private Vector3 lowerRight;

        [SerializeField]
        private Vector3 upperLeft;

        [SerializeField]
        private Vector3 upperRight;
        
        public SerializableBounds(Vector3 lowerLeft, Vector3 lowerRight, Vector3 upperLeft, Vector3 upperRight)
        {
            this.lowerLeft = lowerLeft;
            this.lowerRight = lowerRight;
            this.upperLeft = upperLeft;
            this.upperRight = upperRight;
        }

        public SerializableBounds(Vector3[] bounds)
        {
            lowerLeft = bounds[0];
            lowerRight = bounds[1];
            upperLeft = bounds[2];
            upperRight = bounds[3];
        }

        public Vector3 this[int index]
        {
            get => index switch
            {
                0 => lowerLeft,
                1 => lowerRight,
                2 => upperLeft,
                3 => upperRight,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
            
            set
            {
                switch (index)
                {
                    case 0: lowerLeft = value; break;
                    case 1: lowerRight = value; break;
                    case 2: upperLeft = value; break;
                    case 3: upperRight = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index), index, null);
                }
            }
        }
    }
}