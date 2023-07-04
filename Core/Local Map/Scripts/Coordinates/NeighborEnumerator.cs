using Core.Local_Map.Scripts.Enums;

namespace Core.Local_Map.Scripts.Coordinates
{
    public ref struct NeighborEnumerator
    {
        private readonly Axial _east;
        private readonly Axial _west;
        private readonly Axial _northEast;
        private readonly Axial _northWest;
        private readonly Axial _southEast;
        private readonly Axial _southWest;
        private int _index;
        
        public NeighborEnumerator(Axial center)
        {
            _east = center + Direction.East.ToAxial();
            _west = center + Direction.West.ToAxial();
            _northEast = center + Direction.NorthEast.ToAxial();
            _northWest = center + Direction.NorthWest.ToAxial();
            _southEast = center + Direction.SouthEast.ToAxial();
            _southWest = center + Direction.SouthWest.ToAxial();
            _index = -1;
            Current = default;
        }

        public bool MoveNext()
        {
            _index++;
            switch (_index)
            {
                case 0:  Current = (Direction.East, _east); return true;
                case 1:  Current = (Direction.West, _west); return true;
                case 2:  Current = (Direction.NorthEast, _northEast); return true;
                case 3:  Current = (Direction.NorthWest, _northWest); return true;
                case 4:  Current = (Direction.SouthEast, _southEast); return true;
                case 5:  Current = (Direction.SouthWest, _southWest); return true;
                default: return false;
            }
        }

        public void Reset()
        {
            _index = -1;
            Current = default;
        }

        public (Direction direction, Axial axial) Current { get; private set; }
        
        public NeighborEnumerator GetEnumerator() => this;
    }
}