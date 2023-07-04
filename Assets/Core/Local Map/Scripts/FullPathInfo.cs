namespace Core.Local_Map.Scripts
{
    public readonly struct FullPathInfo
    {
        public readonly PathInfo PathInfo;
        public readonly float PolarEndAngle;
        public readonly TileInfo StartCellInfo;

        public FullPathInfo(PathInfo pathInfo, float polarEndAngle, TileInfo startCellInfo)
        {
            PathInfo = pathInfo;
            PolarEndAngle = polarEndAngle;
            StartCellInfo = startCellInfo;
        }
    }
}