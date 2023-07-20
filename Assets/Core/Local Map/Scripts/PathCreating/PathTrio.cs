using System.Collections.Generic;

namespace Core.Local_Map.Scripts.PathCreating
{
    public class PathTrio
    {
        public readonly List<HexagonObject.Cell> One;
        public readonly List<HexagonObject.Cell> Two;
        public readonly List<HexagonObject.Cell> Three;
        public readonly int PathCount;
        public readonly int Score;

        public PathTrio(List<HexagonObject.Cell> one)
        {
            One = one;
            PathCount = 1;
            Score = 0;
        }
        
        public PathTrio(List<HexagonObject.Cell> one, List<HexagonObject.Cell> two)
        {
            One = one;
            Two = two;
            PathCount = 2;
            Score = 0;
            HashSet<HexagonObject.Cell> oneSet = new(One);
            HashSet<HexagonObject.Cell> twoSet = new(Two);
            
            foreach (HexagonObject.Cell cell in oneSet)
            {
                if (twoSet.Contains(cell))
                {
                    Score += 50;
                    continue;
                }

                foreach (HexagonObject.Cell neighbor in cell.Neighbors.Values)
                {
                    if (twoSet.Contains(neighbor))
                        Score += 1;
                }
            }
        }
        
        public PathTrio(List<HexagonObject.Cell> one, List<HexagonObject.Cell> two, List<HexagonObject.Cell> three)
        {
            One = one;
            Two = two;
            Three = three;
            PathCount = 3;
            Score = 0;
            
            HashSet<HexagonObject.Cell> oneSet = new(One);
            HashSet<HexagonObject.Cell> twoSet = new(Two);
            HashSet<HexagonObject.Cell> threeSet = new(Three);
            
            foreach (HexagonObject.Cell cell in oneSet)
            {
                if (twoSet.Contains(cell) || threeSet.Contains(cell))
                {
                    Score += 50;
                    continue;
                }

                foreach (HexagonObject.Cell neighbor in cell.Neighbors.Values)
                {
                    if (twoSet.Contains(neighbor) || threeSet.Contains(neighbor))
                        Score += 1;
                }
            }
            
            foreach (HexagonObject.Cell cell in twoSet)
            {
                if (threeSet.Contains(cell))
                {
                    Score += 50;
                    continue;
                }

                foreach (HexagonObject.Cell neighbor in cell.Neighbors.Values)
                {
                    if (threeSet.Contains(neighbor))
                        Score += 1;
                }
            }
        }
    }
}