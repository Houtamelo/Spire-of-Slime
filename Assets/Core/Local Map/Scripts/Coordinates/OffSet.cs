using System.Runtime.CompilerServices;
using Core.Local_Map.Scripts.Enums;

namespace Core.Local_Map.Scripts.Coordinates
{
	/// <summary>
	/// Represents the position of a hex within a hex grid using the Odd-Row offset grid layout
	/// scheme. This means that all hexes are pointy-topped and each odd row is offset in the 
	/// positive r direction by half of a hex width. Offset coordinates are a natural fit for 
	/// storage in a rectangular array of memory, and can be an ideal storage format for hexes.
	/// </summary>
	/// <remarks>This type is the least computationally efficient type to use for hex grid 
	/// computations, as all of the work has to be done by the Cubic type and converting 
	/// between OffsetHexCoord and Cubic is the most computationally expensive of the 
	/// type conversions provided by this library.</remarks>
	public struct Offset
	{
		#region Members

		public int col;
		public int row;

		#endregion
		
		
		#region Properties
		
		/// <summary>
		/// Return whether or not this hex belongs to an odd-numbered row.
		/// </summary>
		public bool IsOddRow => ( RowParity == ParityEnum.Odd );

		/// <summary>
		/// Return the row parity of the hex (whether its row number is even or odd).
		/// </summary>
		public ParityEnum RowParity => (ParityEnum)( row & 1 );

#endregion
		
		
		#region Constructors
		
		/// <summary>
		/// Create a new OffsetHexCoord given the coordinates q and r.
		/// </summary>
		/// <param name="col">The column position of the hex within the grid.</param>
		/// <param name="row">The row position of the hex within the grid.</param>
		public Offset( int col, int row )
		{
			this.col = col;
			this.row = row;
		}

		#endregion
		
		
		#region Type Conversions
		
		/// <summary>
		/// Return this hex as a Cubic.
		/// </summary>
		/// <returns>A Cubic representing the hex.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Cubic ToCubic()
		{
			int q = col - ((row - (row & 1)) / 2);
			int r = row;
			return new Cubic(q: q, r: r, s: -q - r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Axial ToAxial()
		{
			int q = col - ((row - (row & 1)) / 2);
			int r = row;
			return new Axial(q1: q, r1: r);
		}
		
		#endregion
		
		
		#region Operator Overloads
		
		/// <summary>
		/// Add 2 OffsetHexCoords together and return the result.
		/// </summary>
		/// <param name="lhs">The OffsetHexCoord on the left-hand side of the + sign.</param>
		/// <param name="rhs">The OffsetHexCoord on the right-hand side of the + sign.</param>
		/// <returns>A new OffsetHexCoord representing the sum of the inputs.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Offset operator +( Offset lhs, Offset rhs ) 
		{
			int q = lhs.col + rhs.col;
			int r = lhs.row + rhs.row;

			return new Offset( col: q, row: r );
		}
		

		/// <summary>
		/// Subtract 1 OffsetHexCoord from another and return the result.
		/// </summary>
		/// <param name="lhs">The OffsetHexCoord on the left-hand side of the - sign.</param>
		/// <param name="rhs">The OffsetHexCoord on the right-hand side of the - sign.</param>
		/// <returns>A new OffsetHexCoord representing the difference of the inputs.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Offset operator -( Offset lhs, Offset rhs ) 
		{
			int q = lhs.col - rhs.col;
			int r = lhs.row - rhs.row;

			return new Offset( col: q, row: r );
		}

		
		/// <summary>
		/// Check if 2 OffsetHexCoords represent the same hex on the grid.
		/// </summary>
		/// <param name="lhs">The OffsetHexCoord on the left-hand side of the == sign.</param>
		/// <param name="rhs">The OffsetHexCoord on the right-hand side of the == sign.</param>
		/// <returns>A bool representing whether or not the OffsetHexCoords are equal.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==( Offset lhs, Offset rhs ) => ( lhs.col == rhs.col ) && ( lhs.row == rhs.row );

		/// <summary>
		/// Check if 2 OffsetHexCoords represent the different hexes on the grid.
		/// </summary>
		/// <param name="lhs">The OffsetHexCoord on the left-hand side of the != sign.</param>
		/// <param name="rhs">The OffsetHexCoord on the right-hand side of the != sign.</param>
		/// <returns>A bool representing whether or not the OffsetHexCoords are unequal.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=( Offset lhs, Offset rhs ) => ( lhs.col != rhs.col ) || ( lhs.row != rhs.row );

		/// <summary>
		/// Get a hash reflecting the contents of the OffsetHexCoord.
		/// </summary>
		/// <returns>An integer hash code reflecting the contents of the OffsetHexCoord.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + col.GetHashCode();
				hash = (hash * 23) + row.GetHashCode();
				return hash;
			}
		}

		
		/// <summary>
		/// Check if this OffsetHexCoord is equal to an arbitrary object.
		/// </summary>
		/// <returns>Whether or not this OffsetHexCoord and the given object are equal.</returns>
		public override bool Equals( object obj )
		{
			if ( obj == null  || GetType() != obj.GetType())
				return false;

			Offset other = (Offset) obj;

			return ( col == other.col ) && ( row == other.row );
		}

		#endregion
	}
}