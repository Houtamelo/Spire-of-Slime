using System;
using JetBrains.Annotations;

namespace Core.Utils.Collections.Extensions
{
	public static class SpanExtensions
	{
		public static T GetWeightedRandom<T>(this ref ReadOnlySpan<(T element, float points)> source)
		{
			if (source.Length == 0)
				return default;
            
			float pointsSum = 0f;
			foreach ((_, float weight) in source)
				pointsSum += weight;

			if (pointsSum <= 0)
				return source[0].element;
            
			float numericValue = UnityEngine.Random.value * pointsSum;
			foreach ((T element, float weight) in source)
			{
				numericValue -= weight;
				if (numericValue > 0)
					continue;
                
				return element;
			}

			return source[0].element;
		}
		
		public static T GetWeightedRandom<T>(this ref Span<(T element, float points)> source)
		{
			if (source.Length == 0)
				return default;
            
			float pointsSum = 0f;
			foreach ((_, float weight) in source)
				pointsSum += weight;

			if (pointsSum <= 0)
				return source[0].element;
            
			float numericValue = UnityEngine.Random.value * pointsSum;
			foreach ((T element, float weight) in source)
			{
				numericValue -= weight;
				if (numericValue > 0)
					continue;
                
				return element;
			}

			return source[0].element;
		}

		public static int Max<T>(this ref ReadOnlySpan<T> source, [NotNull] Func<T, int> selector)
		{
            int max = selector(source[0]);

			for (int index = 1; index < source.Length; index++)
			{
				T element = source[index];
				int value = selector(element);
				if (value > max)
					max = value;
			}
			
			return max;
		}
	}
}