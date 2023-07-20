using System;
using System.Runtime.Serialization;
using System.Text;
using Core.Utils.Extensions;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour
{
    [Serializable, DataContract]
    public struct CharacterPositioning
    {
        private static readonly StringBuilder Builder = new();
        
        [SerializeField, DataMember]
        public int size;
        
        [SerializeField, DataMember]
        public int startPosition;

        public int Min => startPosition;

        public CharacterPositioning(int size, int startPosition)
        {
            this.size = size;
            this.startPosition = startPosition;
        }

        public override string ToString() => Builder.Override("Size: ", size.ToString(), ". Index: ", startPosition.ToString()).ToString();

        public static CharacterPositioning None => default;

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref struct Enumerator
        {
            private int _index;
            private readonly int _size;
            private readonly int _startIndex;
            
            public Enumerator(CharacterPositioning positions)
            {
                _index = 0;
                Current = positions.startPosition;
                _size = positions.size;
                _startIndex = positions.startPosition;
            }

            public bool MoveNext()
            {
                if (_index >= _size)
                    return false;

                Current = _index + _startIndex;
                _index++;
                return true;
            }

            public void Reset() => _index = 0;

            public int Current { get; private set; }
        }
    }
}