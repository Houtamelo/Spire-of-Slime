using System;
using System.Runtime.CompilerServices;
using Core.Local_Map.Scripts.Enums;
using JetBrains.Annotations;

namespace Core.Local_Map.Scripts.Coordinates
{
    /// <summary>
	/// Represents the position of a hex within a hex grid using cube-space coordinates. This is a 
	/// bit weird, but it is comparable to representing 3D rotations with quaternions (in 4-space).
	/// This coordinate system is a bit less memory-efficient but enables simple algorithms for 
	/// most hex grid operations. Because of this, the CubicHexCoord type contains basically all of 
	/// the methods for operating on hexes. That being said, this data format is non-ideal for 
	/// storage or end-use, so it is advised that the user transform their data into another 
	/// coordinate system for use in most applications or storage in memory/to disk.
	/// </summary>
	/// <remarks>It is advisable to keep your hex position data structures as CubicHexCoord type 
	/// whenever possible. Since most of the computational work is done in cube-space, this type 
	/// is the most efficient to use, but is the least practical for end-user grid 
	/// implementations. Simply keep a CubicHexCoord to do the work and allow it to return results 
	/// that you can convert to other types.</remarks>
	public struct Cubic
	{
		#region Members

		public int q;
		public int r;
		public int s;

		#endregion


		#region Constants

		private static readonly Cubic[] DIAGONALS = {
			new(  q: 1, r: -2,  s: 1 ),
			new( q: -1, r: -1,  s: 2 ),
			new( q: -2,  r: 1,  s: 1 ),
			new( q: -1,  r: 2, s: -1 ),
			new(  q: 1,  r: 1, s: -2 ),
			new(  q: 2, r: -1, s: -1 )
		};

		private static readonly Cubic[] DIRECTIONS = {
			new(  q: 1, r: -1,  s: 0 ),
			new(  q: 0, r: -1,  s: 1 ),
			new( q: -1,  r: 0,  s: 1 ),
			new( q: -1,  r: 1,  s: 0 ),
			new(  q: 0,  r: 1, s: -1 ),
			new(  q: 1,  r: 0, s: -1 )
		};

		#endregion


		#region Constructors
		
		/// <summary>
		/// Create a new CubicHexCoord given the coordinates x, y and z.
		/// </summary>
		/// <param name="q">The position on the x-axis in cube-space.</param>
		/// <param name="r">The position on the y-axis in cube-space.</param>
		/// <param name="s">The position on the z-axis in cube-space.</param>
		public Cubic( int q, int r, int s ) 
		{
			this.q = q;
			this.r = r;
			this.s = s;
		}

		#endregion


		#region Type Conversions

		/// <summary>
		/// Return this hex as an Axial.
		/// </summary>
		/// <returns>An Axial representing the hex.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Axial ToAxial() => new(q1: q, r1: r);


		/// <summary>
		/// Return this hex as an Offset.
		/// </summary>
		/// <returns>An Offset representing the hex.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Offset ToOffset()
		{
			int q1 = q + ((r - (r & 1 )) / 2);
			int r1 = r;

			return new Offset( col: q1, row: r1 );
		}

		#endregion


		#region Operator Overloads
		
		/// <summary>
		/// Add 2 CubicHexCoords together and return the result.
		/// </summary>
		/// <param name="lhs">The CubicHexCoord on the left-hand side of the + sign.</param>
		/// <param name="rhs">The CubicHexCoord on the right-hand side of the + sign.</param>
		/// <returns>A new CubicHexCoord representing the sum of the inputs.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cubic operator +( Cubic lhs, Cubic rhs ) 
		{
			int x = lhs.q + rhs.q;
			int y = lhs.r + rhs.r;
			int z = lhs.s + rhs.s;
			return new Cubic( q: x, r: y, s: z );
		}

		
		/// <summary>
		/// Subtract 1 CubicHexCoord from another and return the result.
		/// </summary>
		/// <param name="lhs">The CubicHexCoord on the left-hand side of the - sign.</param>
		/// <param name="rhs">The CubicHexCoord on the right-hand side of the - sign.</param>
		/// <returns>A new CubicHexCoord representing the difference of the inputs.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Cubic operator -( Cubic lhs, Cubic rhs ) 
		{
			int x = lhs.q - rhs.q;
			int y = lhs.r - rhs.r;
			int z = lhs.s - rhs.s;

			return new Cubic( q: x, r: y, s: z );
		}

		
		/// <summary>
		/// Check if 2 CubicHexCoords represent the same hex on the grid.
		/// </summary>
		/// <param name="lhs">The CubicHexCoord on the left-hand side of the == sign.</param>
		/// <param name="rhs">The CubicHexCoord on the right-hand side of the == sign.</param>
		/// <returns>A bool representing whether or not the CubicHexCoords are equal.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==( Cubic lhs, Cubic rhs ) => ( lhs.q == rhs.q ) && ( lhs.r == rhs.r ) && ( lhs.s == rhs.s );

		/// <summary>
		/// Check if 2 CubicHexCoords represent the different hexes on the grid.
		/// </summary>
		/// <param name="lhs">The CubicHexCoord on the left-hand side of the != sign.</param>
		/// <param name="rhs">The CubicHexCoord on the right-hand side of the != sign.</param>
		/// <returns>A bool representing whether or not the CubicHexCoords are unequal.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=( Cubic lhs, Cubic rhs ) => ( lhs.q != rhs.q ) || ( lhs.r != rhs.r ) || ( lhs.s != rhs.s );

		/// <summary>
		/// Get a hash reflecting the contents of the CubicHexCoord.
		/// </summary>
		/// <returns>An integer hash code reflecting the contents of the CubicHexCoord.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + q.GetHashCode();
				hash = (hash * 23) + r.GetHashCode();
				hash = (hash * 23) + s.GetHashCode();
				return hash;
			}
		}

		
		/// <summary>
		/// Check if this CubicHexCoord is equal to an arbitrary object.
		/// </summary>
		/// <returns>Whether or not this CubicHexCoord and the given object are equal.</returns>
		public override
		bool 
		Equals( object obj )
		{
			if ( obj == null || GetType() != obj.GetType())
				return false;

			return (Cubic) obj == this;
		}

		#endregion


		#region Instance Methods
		
		/// <summary>
		/// Returns an array of CubicHexCoords within the given range from this hex (including 
		/// this hex) in no particular order.
		/// </summary>
		/// <param name="range">The maximum number of grid steps away from this hex that 
		/// CubicHexCoords will be returned for.</param>
		/// <returns>An array of CubicHexCoords within the given range from this hex (including 
		/// this hex) in no particular order.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining), NotNull]
		public Cubic[] AreaAround( int range ) => Area( center: this, range: range );

		/// <summary>
		/// Returns a CubicHexCoord representing the diagonal of this hex in the given diagonal 
		/// direction.
		/// </summary>
		/// <param name="direction">The diagonal direction of the requested neighbor.</param>
		/// <returns>A CubicHexCoord representing the diagonal of this hex in the given diagonal 
		/// direction.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Cubic Diagonal( DiagonalEnum direction ) => this + DIAGONALS[ (int)direction ];

		/// <summary>
		/// Returns an array of CubicHexCoords representing this hex's diagonals (in clockwise
		/// order).
		/// </summary>
		/// <returns>An array of CubicHexCoords representing this hex's diagonals (in clockwise
		/// order).</returns>
		[NotNull]
		public Cubic[] Diagonals() => new[] {
			                                    this + DIAGONALS[ (int)DiagonalEnum.ESE ], 
			                                    this + DIAGONALS[ (int)DiagonalEnum.S   ],
			                                    this + DIAGONALS[ (int)DiagonalEnum.WSW ], 
			                                    this + DIAGONALS[ (int)DiagonalEnum.WNW ], 
			                                    this + DIAGONALS[ (int)DiagonalEnum.N   ], 
			                                    this + DIAGONALS[ (int)DiagonalEnum.ENE ]
		                                    };


		/// <summary>
		/// Returns the minimum number of grid steps to get from this hex to the given hex.
		/// </summary>
		/// <param name="other">Any CubicHexCoord.</param>
		/// <returns>An integer number of grid steps from this hex to the given hex.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int DistanceTo( Cubic other ) => Distance( a: this, b: other );


		/// <summary>
		/// Returns an array of CubicHexCoords that form the straightest path from this hex to the 
		/// given end. The hexes in the line are determined by forming a straight line from start 
		/// to end and linearly interpolating and rounding each of the interpolated points to 
		/// the nearest hex position.
		/// </summary>
		/// <param name="other">The CubicHexCoord representing the last hex in the line.</param>
		/// <returns>An array of CubicHexCoords ordered as a line from start to end.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining), NotNull]
		public Cubic[] LineTo(Cubic other) => Line(start: this, end: other);


		/// <summary>
		/// Returns a CubicHexCoord representing the neighbor of this hex in the given direction.
		/// </summary>
		/// <param name="direction">The direction of the requested neighbor.</param>
		/// <returns>A CubicHexCoord representing the neighbor of this hex in the given direction.
		/// </returns>
		public Cubic Neighbor( Direction direction ) => this + DIRECTIONS[ (int)direction ];


		/// <summary>
		/// Returns an array of CubicHexCoords representing this hex's neighbors (in clockwise
		/// order).
		/// </summary>
		/// <returns>An array of CubicHexCoords representing this hex's neighbors (in clockwise
		/// order).</returns>
		[NotNull]
		public Cubic[] Neighbors()
		{
			return new[] {
				this + DIRECTIONS[ (int)Direction.East ], 
				this + DIRECTIONS[ (int)Direction.SouthEast ], 
				this + DIRECTIONS[ (int)Direction.SouthWest ], 
				this + DIRECTIONS[ (int)Direction.West  ], 
				this + DIRECTIONS[ (int)Direction.NorthWest ], 
				this + DIRECTIONS[ (int)Direction.NorthEast ]
			};
		}
		
		
		/// <summary>
		/// Returns an array of CubicHexCoords that appear at the given range around this hex. 
		/// The ring begins from the CubicHexCoord range grid steps away from the center, heading 
		/// in the given direction, and encircling the center in clockwise order.
		/// </summary>
		/// <param name="range">The number of grid steps distance away from the center that the 
		/// ring will be.</param>
		/// <param name="startDirection">The direction in which the first CubicHexCoord of the 
		/// ring will appear in.</param>
		/// <returns>An array of CubicHexCoords ordered as a ring.</returns>
		[NotNull]
		public Cubic[] RingAround( int range, Direction startDirection = Direction.East ) => Ring( center: this, range: range, startDirection: startDirection );

		/// <summary>
		/// Scale this CubicHexCoord by the given factor, such that the x, y and z values of 
		/// the CubicHexCoord change proprtionally to the factor provided.
		/// </summary>
		/// <param name="factor">A multiplicative factor to scale by.</param>
		/// <returns>A new scaled CubicHexCoord.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Cubic Scale( float factor ) => new FloatCubic( cubic: this ).Scale( factor: factor ).Round();


		/// <summary>
		/// Returns an array of CubicHexCoords of a area centering around ths hex and extending in 
		/// every direction up to the given range. The hexes are ordered starting from the center 
		/// and then spiraling outward clockwise beginning from the neighbor in the given 
		/// direction, until the outside ring is complete.
		/// </summary>
		/// <param name="range">The number of grid steps distance away from the center that the 
		/// edge of the spiral will be.</param>
		/// <param name="startDirection">The direction in which the first CubicHexCoord of the 
		/// spiral will appear in.</param>
		/// <returns>An array of CubicHexCoords ordered as a spiral, beginning from the center
		/// and proceeding clockwise until it reaches the outside of the spiral.</returns>
		[NotNull]
		public Cubic[] SpiralAroundInward( int range, Direction startDirection = Direction.East ) => SpiralInward( center: this, range: range, startDirection: startDirection );

		/// <summary>
		/// Returns an array of CubicHexCoords of a area centering around this hex and extending in 
		/// every direction up to the given range. The hexes are ordered starting from the maximum 
		/// range, in the given direction, spiraling inward in a clockwise direction until the 
		/// center is reached (and is the last element in the array).
		/// </summary>
		/// <param name="range">The number of grid steps distance away from the center that the 
		/// edge of the spiral will be.</param>
		/// <param name="startDirection">The direction in which the first CubicHexCoord of the 
		/// spiral will appear in.</param>
		/// <returns>An array of CubicHexCoords ordered as a spiral, beginning from the outside
		/// and proceeding clockwise until it reaches the center of the spiral.</returns>
		/// <returns></returns>
		[NotNull]
		public Cubic[] SpiralAroundOutward( int range, Direction startDirection = Direction.East ) => SpiralOutward( center: this, range: range, startDirection: startDirection );

		#endregion


		#region Static Methods
		
		/// <summary>
		/// Returns an array of CubicHexCoords within the given range from the given center 
		/// (including the center itself) in no particular order.
		/// </summary>
		/// <param name="center">The CubicHexCoord around which the area is formed.</param>
		/// <param name="range">The maximum number of grid steps away from the center that 
		/// CubicHexCoords will be returned for.</param>
		/// <returns>An array of CubicHexCoords within the given range from the given center 
		/// (including the center itself) in no particular order.</returns>
		[NotNull]
		public static Cubic[] Area( Cubic center, int range )
		{
			if ( range < 0 )
				throw new ArgumentOutOfRangeException( paramName: "range must be a non-negative integer value." );
			else if ( range == 0 )
				return new[] { center };

			int arraySize = 1;
			for ( int i = range; i > 0; i-- )
				arraySize += 6 * i;

			Cubic[] result = new Cubic[ arraySize ];

			for ( int i = 0, dx = -range; dx <= range; dx++ )
			{
				int dyMinBound = Math.Max( val1: -range, val2: -dx - range );
				int dyMaxBound = Math.Min(  val1: range, val2: -dx + range );

				for ( int dy = dyMinBound; dy <= dyMaxBound; dy++ )
				{
					int dz = -dx - dy;
					result[ i++ ] = center + new Cubic( q: dx, r: dy, s: dz );
				}
			}

			return result;
		}

		
		/// <summary>
		/// Returns a CubicHexCoord representing the diff between some hex and its diagonal in 
		/// the given diagonal direction.
		/// </summary>
		/// <param name="direction">The diagonal direction to return a diff for.</param>
		/// <returns>A CubicHexCoord representing the diff between some hex and its diagonal in 
		/// the given diagonal direction.</returns>
		public static Cubic DiagonalDiff( DiagonalEnum direction ) => DIAGONALS[ (int)direction ];

		/// <summary>
		/// Returns a CubicHexCoord representing the diff between some hex and its neighbor in 
		/// the given direction.
		/// </summary>
		/// <param name="direction">The direction to return a diff for.</param>
		/// <returns>A CubicHexCoord representing the diff between some hex and its neighbor in 
		/// the given direction.</returns>
		public static Cubic DirectionDiff( Direction direction ) => DIRECTIONS[ (int)direction ];

		/// <summary>
		/// Returns the minimum number of grid steps to get from a to b.
		/// </summary>
		/// <param name="a">Any CubicHexCoord.</param>
		/// <param name="b">Any CubicHexCoord.</param>
		/// <returns>An integer number of grid steps from a to b.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Distance( Cubic a, Cubic b )
		{
			int dx = Math.Abs( value: a.q - b.q );
			int dy = Math.Abs( value: a.r - b.r );
			int dz = Math.Abs( value: a.s - b.s );

			return Math.Max( val1: Math.Max( val1: dx, val2: dy ), val2: dz );
		}

		
		/// <summary>
		/// Returns an array of CubicHexCoords that form the straightest path from the given start
		/// to the given end. The hexes in the line are determined by forming a straight line from 
		/// start to end and linearly interpolating and rounding each of the interpolated points to 
		/// the nearest hex position.
		/// </summary>
		/// <param name="start">The CubicHexCoord representing the first hex in the line.</param>
		/// <param name="end">The CubicHexCoord representing the last hex in the line.</param>
		/// <returns>An array of CubicHexCoords ordered as a line from start to end.</returns>
		[NotNull]
		public static Cubic[] Line( Cubic start, Cubic end )
		{
			int distance = Distance( a: start, b: end );

			Cubic[] result = new Cubic[ distance + 1 ];

			for ( int i = 0; i <= distance; i++ )
			{
				float xLerp = start.q + (( end.q - start.q ) * 1f / distance * i);
				float yLerp = start.r + (( end.r - start.r ) * 1f / distance * i);
				float zLerp = start.s + (( end.s - start.s ) * 1f / distance * i);

				result[ i ] = new FloatCubic( x: xLerp, y: yLerp, z: zLerp ).Round();
			}

			return result;
		}

		
		/// <summary>
		/// Returns an array of CubicHexCoords that appear at the given range around the given 
		/// center hex. The ring begins from the CubicHexCoord range grid steps away from the 
		/// center, heading in the given direction, and encircling the center in clockwise order.
		/// </summary>
		/// <param name="center">The CubicHexCoord around which the ring is formed.</param>
		/// <param name="range">The number of grid steps distance away from the center that the 
		/// ring will be.</param>
		/// <param name="startDirection">The direction in which the first CubicHexCoord of the 
		/// ring will appear in.</param>
		/// <returns>An array of CubicHexCoords ordered as a ring.</returns>
		[NotNull]
		public static Cubic[] Ring( Cubic center, int range, Direction startDirection = Direction.East )
		{
			if ( range <= 0 )
				throw new ArgumentOutOfRangeException( paramName: "range must be a positive integer value." );

			Cubic[] result = new Cubic[ 6 * range ];

			Cubic cube = center + DIRECTIONS[ (int)startDirection ].Scale( factor: range );

			int[] directions = new int[ 6 ];
			for ( int i = 0; i < 6; i++ )
				directions[ i ] = ( (int)startDirection + i ) % 6;

			int index = 0;
			for ( int i = 0; i < 6; i++ )
			{
				int neighborDirection = ( directions[ i ] + 2 ) % 6;
				for ( int j = 0; j < range; j++ )
				{
					result[ index++ ] = cube;
					cube = cube.Neighbor( direction: (Direction)neighborDirection );
				}
			}

			return result;
		}

		/// <summary>
		/// Returns an array of CubicHexCoords of a area centering around the given center hex and 
		/// extending in every direction up to the given range. The hexes are ordered starting from 
		/// the center and then spiraling outward clockwise beginning from the neighbor in the 
		/// given direction, until the outside ring is complete.
		/// </summary>
		/// <param name="center">The CubicHexCoord around which the spiral is formed.</param>
		/// <param name="range">The number of grid steps distance away from the center that the 
		/// edge of the spiral will be.</param>
		/// <param name="startDirection">The direction in which the first CubicHexCoord of the 
		/// spiral will appear in.</param>
		/// <returns>An array of CubicHexCoords ordered as a spiral, beginning from the center
		/// and proceeding clockwise until it reaches the outside of the spiral.</returns>
		[NotNull]
		public static Cubic[] SpiralInward( Cubic center, int range, Direction startDirection = Direction.East )
		{
			if ( range < 0 )
				throw new ArgumentOutOfRangeException( paramName: "range must be a positive integer value." );
			else if ( range == 0 )
				return new[] { center };

			int arraySize = 1;
			for ( int i = range; i > 0; i-- )
				arraySize += 6 * i;

			Cubic[] result = new Cubic[ arraySize ];

			result[ result.Length - 1 ] = center;

			int arrayIndex = result.Length - 1;
			for ( int i = range; i >= 1; i-- ) {
				Cubic[] ring = Ring( center: center, range: i, startDirection: startDirection );
				arrayIndex -= ring.Length;
				ring.CopyTo( array: result, index: arrayIndex );
			}
				
			return result;
		}

		
		/// <summary>
		/// Returns an array of CubicHexCoords of a area centering around the given center hex and 
		/// extending in every direction up to the given range. The hexes are ordered starting from 
		/// the maximum range, in the given direction, spiraling inward in a clockwise direction 
		/// until the center is reached (and is the last element in the array).
		/// </summary>
		/// <param name="center">The CubicHexCoord around which the spiral is formed.</param>
		/// <param name="range">The number of grid steps distance away from the center that the 
		/// edge of the spiral will be.</param>
		/// <param name="startDirection">The direction in which the first CubicHexCoord of the 
		/// spiral will appear in.</param>
		/// <returns>An array of CubicHexCoords ordered as a spiral, beginning from the outside
		/// and proceeding clockwise until it reaches the center of the spiral.</returns>
		[NotNull]
		public static Cubic[] SpiralOutward( Cubic center, int range, Direction startDirection = Direction.East )
		{
			if ( range < 0 )
				throw new ArgumentOutOfRangeException( paramName: "range must be a positive integer value." );
			else if ( range == 0 )
				return new[] { center };

			int arraySize = 1;
			for ( int i = range; i > 0; i-- )
				arraySize += 6 * i;

			Cubic[] result = new Cubic[ arraySize ];

			result[ 0 ] = center;

			int arrayIndex = 1;
			for ( int i = 1; i <= range; i++ ) {
				Cubic[] ring = Ring( center: center, range: i, startDirection: startDirection );
				ring.CopyTo( array: result, index: arrayIndex );
				arrayIndex += ring.Length;
			}
				
			return result;
		}

		public static implicit operator Axial(Cubic cubic) => new(q1: cubic.q, r1: cubic.r);

		#endregion
	}
}