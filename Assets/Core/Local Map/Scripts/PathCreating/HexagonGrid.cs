using System.Collections.Generic;
using Core.Local_Map.Scripts.Coordinates;
using Core.Local_Map.Scripts.Enums;
using UnityEngine;

namespace Core.Local_Map.Scripts.PathCreating
{
	public readonly struct HexagonGrid 
	{
		//Map settings
		public readonly MapShape MapShape;
		public readonly int MapWidth;
		public readonly int MapHeight;

		//Hex Settings
		public readonly HexOrientation HexOrientation;
		public readonly Axial Offset;

		//Internal variables
		public readonly HashSet<Axial> Tiles;
		private static readonly IReadOnlyList<Axial> Directions = new Axial[]
		{
			new(q1: 1, r1: -1),
			new(q1: 1, r1: 0),
			new(q1: 0, r1: 1),
			new(q1: -1, r1: 1),
			new(q1: -1, r1: 0),
			new(q1: 0, r1: -1)
		};

		#region Public Methods

		public HexagonGrid(MapShape mapShape, int mapWidth, int mapHeight, HexOrientation hexOrientation, Axial center)
		{
			MapShape = mapShape;
			MapWidth = mapWidth;
			MapHeight = mapHeight;
			HexOrientation = hexOrientation;
			Axial naturalOffset = (new Offset(col: -1 * mapWidth / 2, row: -1 * mapHeight / 2)).ToAxial();
			Offset = center + naturalOffset;
			Tiles = new HashSet<Axial>();
			GenerateGrid();
		}

		public void GenerateGrid()
		{
			ClearGrid();

			switch (MapShape)
			{
				case MapShape.Hexagon:
					GenHexShape();
					break;

				case MapShape.Rectangle:
					GenRectShape();
					break;

				case MapShape.Parrallelogram:
					GenParallelogramShape();
					break;

				case MapShape.Triangle:
					GenTriShape();
					break;
			}
		}

		public void ClearGrid() 
		{
			Tiles.Clear();
		}

		public List<Axial> Neighbours(Axial tile) 
		{
			List<Axial> ret = new();

			for (int i = 0; i < 6; i++)
			{
				Axial neighbor = tile + Directions[index: i];
				if (Tiles.Contains(neighbor))
					ret.Add(neighbor);
			}

			return ret;
		}

		public List<Axial> TilesInRange(Axial center, int range)
		{
			List<Axial> ret = new();

			for (int dx = -range; dx <= range; dx++)
			{
				for (int dy = Mathf.Max(-range, -dx - range); dy <= Mathf.Min(a: range, b: -dx + range); dy++)
				{
					Axial axial = new Axial(q1: dx, r1: dy) + center;
					if (Tiles.Contains(axial))
						ret.Add(axial);
				}
			}
			return ret;
		}
	
		#endregion

		#region Private Methods

		private void GenHexShape() 
		{
			int mapSize = Mathf.Max(MapWidth, MapHeight);

			for (int q = -mapSize; q <= mapSize; q++)
			{
				int r1 = Mathf.Max(-mapSize, -q - mapSize);
				int r2 = Mathf.Min(a: mapSize, b: -q + mapSize);
				for (int r = r1; r <= r2; r++) 
					Tiles.Add(new Axial(q1: q, r1: r) + Offset);
			}
		}

		private void GenRectShape() 
		{
			switch (HexOrientation)
			{
				case HexOrientation.Flat:
				{
					for (int q = 0; q < MapWidth; q++)
					{
						int qOff = q >> 1;
						for (int r = -qOff; r < MapHeight - qOff; r++)
						{
							Axial tile = new(q1: q, r1: r);
							Tiles.Add(tile + Offset);
						}
					}

					break;
				}
				case HexOrientation.Pointy:
				{
					for (int r = 0; r < MapHeight; r++)
					{
						int rOff = r >> 1;
						for (int q = -rOff; q < MapWidth - rOff; q++)
						{
							Axial tile = new(q1: q, r1: r);
							Tiles.Add(tile + Offset);
						}
					}

					break;
				}
			}
		}

		private void GenParallelogramShape() 
		{
			for (int q = 0; q <= MapWidth; q++)
				for (int r = 0; r <= MapHeight; r++)
					Tiles.Add(new Axial(q1: q, r1: r) + Offset);
		}

		public void GenTriShape()
		{
			int mapSize = Mathf.Max(MapWidth, MapHeight);

			for (int q = 0; q <= mapSize; q++)
				for (int r = 0; r <= mapSize - q; r++)
					Tiles.Add(new Axial(q1: q, r1: r) + Offset);
		}
		#endregion
	}
}