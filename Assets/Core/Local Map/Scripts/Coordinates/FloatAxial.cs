﻿using System.Runtime.CompilerServices;

namespace Core.Local_Map.Scripts.Coordinates
{
	/// <summary>
	/// FloatAxial represents a pseudo-position on the hex grid. It does not directly represent 
	/// the position of a hex, but instead is used as a means to compute a hex position. It is  
	/// needed in order to transform the world-space point into hex coordinates in 
	/// HexGrid.PointToCubic() which eventually yields a CubicHexCoord from CubicHexCoord.Round().
	/// </summary>
	public struct FloatAxial
	{
		#region Members

		public float q;
		public float r;

		#endregion
		
		
		#region Constructors
		
		/// <summary>
		/// Create a new FloatAxial given a AxialHexCoord.
		/// </summary>
		/// <param name="axial">Any AxialHexCoord representing a hex.</param>
		public FloatAxial(Axial axial)
		{
			q = axial.q;
			r = axial.r;
		}
		
		
		/// <summary>
		/// Create a new FloatCubic given the coordinates q and r.
		/// </summary>
		/// <param name="q">The position of this hex on the column axis.</param>
		/// <param name="r">The position of this hex on the row axis.</param>
		public FloatAxial( float q, float r ) 
		{
			this.q = q;
			this.r = r;
		}

		#endregion
		
		
		#region Type Conversions
		
		/// <summary>
		/// Return this FloatAxial as a FloatCubic.
		/// </summary>
		/// <returns>A FloatCubic representing this FloatAxial.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FloatCubic ToFloatCubic()
		{
			float x = q;
			float z = r;
			float y = -x - z;

			return new FloatCubic( x: x, y: y, z: z );
		}

		#endregion
	}
}
