﻿namespace Core.Local_Map.Scripts.Enums
{
	/// <summary>
	/// Represents a diagonal hex (see http://www.redblobgames.com/grids/hexagons/), relative to
	/// a central pointy-topped hex. Note that cardinal directions are used here to make these 
	/// easier to distinguish.
	/// </summary>
	public enum DiagonalEnum
	{
		ESE = 0,
		S   = 1,
		WSW = 2, 
		WNW = 3, 
		N   = 4,
		ENE = 5
	}
}
