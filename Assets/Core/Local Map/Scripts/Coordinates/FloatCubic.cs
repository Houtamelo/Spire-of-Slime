using System;
using System.Runtime.CompilerServices;

namespace Core.Local_Map.Scripts.Coordinates
{
	/// <summary>
	/// FloatCubic represents a pseudo-position on the hex grid. It does not directly represent
	/// the position of a hex, but instead is used as a means to compute a hex position by rounding
	/// a FloatCubic using Cubic.Round(), which returns a Cubic.
	/// </summary>
	public struct FloatCubic
	{
		#region Members

		public float x;
		public float y;
		public float z;

		#endregion


		#region Constructors

		/// <summary>
		/// Create a new FloatCubic given a CubicHexIndex.
		/// </summary>
		/// <param name="cubic">Any Cubic representing a hex.</param>
		public
		FloatCubic(Cubic cubic )
		{
			x = cubic.q;
			y = cubic.r;
			z = cubic.s;
		}


		/// <summary>
		/// Create a new FloatCubic given the coordinates x, y and z.
		/// </summary>
		/// <param name="x">The position on this point on the x-axis.</param>
		/// <param name="y">The position on this point on the y-axis.</param>
		/// <param name="z">The position on this point on the z-axis.</param>
		public
		FloatCubic( float x, float y, float z )
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		#endregion


		#region Type Conversions

		/// <summary>
		/// Return this FloatCubic as a FloatAxial.
		/// </summary>
		/// <returns>A FloatAxial representing this FloatCubic.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FloatAxial ToFloatAxial()
		{
			float q = x;
			float r = z;

			return new FloatAxial( q: q, r: r );
		}

		#endregion


		#region Instance Methods

		/// <summary>
		/// Returns a new Cubic representing the nearest hex to this FloatCubic.
		/// </summary>
		/// <returns>A new Cubic representing the nearest hex to this FloatCubic.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Cubic Round()
		{
			int rx = (int)Math.Round( a: x );
			int ry = (int)Math.Round( a: y );
			int rz = (int)Math.Round( a: z );

			float xDiff = Math.Abs(value: rx - x);
			float yDiff = Math.Abs(value: ry - y);
			float zDiff = Math.Abs(value: rz - z);

			if ( xDiff > yDiff && xDiff > zDiff )
				rx = -ry - rz;
			else if ( yDiff > zDiff )
				ry = -rx - rz;
			else
				rz = -rx - ry;

			return new Cubic( q: rx, r: ry, s: rz );
		}


		/// <summary>
		/// Scale the world space by the given factor, causing q and r to change proportionally
		/// to factor.
		/// </summary>
		/// <param name="factor">The multiplicative factor by which the world space is being
		/// scaled.</param>
		/// <returns>A new FloatCubic representing the new floating hex position.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FloatCubic Scale( float factor ) =>
			new
				(
				x: x * factor,
				y: y * factor,
				z: z * factor
				);

#endregion
	}
}
