using System;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using Utils.Extensions;

namespace Core.Combat.Scripts.Behaviour
{
    [Serializable, DataContract]
    public struct CharacterPositioning
    {
        private static readonly StringBuilder Builder = new();
        
        [SerializeField, DataMember]
        public byte size;
        
        [SerializeField, DataMember]
        public byte startPosition;

        public int Min => startPosition;

        public CharacterPositioning(byte size, byte startPosition)
        {
            this.size = size;
            this.startPosition = startPosition;
        }

        public override string ToString() => Builder.Override("Size: ", size.ToString(), ". Index: ", startPosition.ToString()).ToString();

        public static CharacterPositioning None => default;

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref struct Enumerator
        {
            private byte _index;
            private readonly byte _size;
            private readonly byte _startIndex;
            
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