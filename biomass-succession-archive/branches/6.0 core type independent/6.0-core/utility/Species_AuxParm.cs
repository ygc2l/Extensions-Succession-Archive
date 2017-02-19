//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.Core;

namespace Landis.Species
{
	/// <summary>
	/// An auxiliary parameter for species.
	/// </summary>
	public class AuxParm<T>
	{
		private T[] values;

		//---------------------------------------------------------------------

		public T this[ISpecies species]
		{
			get {
				return values[species.Index];
			}

			set {
				values[species.Index] = value;
			}
		}

		//---------------------------------------------------------------------

		public AuxParm(IDataset species)
		{
			values = new T[species.Count];
		}
	}
}