using System;
using System.Collections.Generic;
using System.Text;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Utils.Collections.Extensions
{
	public static class ArrayExtensions
	{
		private static readonly StringBuilder Builder = new();
		private static readonly Random Random = new();
		
		public static int GetWeightedRandomIndex([NotNull] this float[] source)
		{
			float total = 0f;
			for (int i = 0; i < source.Length; i++)
				total += source[i];
            
			double randomPoint = Random.NextDouble() * total;
            
			for (int i = 0; i < source.Length; i++)
			{
				if (randomPoint < source[i])
					return i;
                
				randomPoint -= source[i];
			}
            
			return source.Length - 1;
		}
		
		public static Patterns.Option<int> IndexOf<T>([NotNull] this T[] source, T value)
		{
			int result = Array.IndexOf(source, value);
			return result != -1 ? result : Patterns.Option<int>.None;
		}
		
		public static T GetRandom<T>([NotNull] this T[] source)
		{
            int length = source.Length;
			return source[Random.Next(maxValue: length)];
		}
		
		public static T GetWeightedRandom<T>([NotNull] this (T element, float points)[] source)
		{
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
		
		public static void DoForEach<T>([NotNull] this T[] source, Action<T> action)
		{
			foreach (T element in source)
				action.Invoke(element);
		}
		
		[NotNull]
		public static string ElementsToString<T>([NotNull] this T[] source)
		{
			Builder.Clear();

			for (int index = 0; index < source.Length; index++)
			{
				T element = source[index];
				Builder.AppendLine(element.ToString());
			}

			return Builder.ToString();
		}

		[NotNull]
		public static T[] ToArrayNonAlloc<T>([NotNull] this List<T> source)
		{
			T[] array = new T[source.Count];
			for (int i = 0; i < source.Count; i++)
				array[i] = source[i];

			return array;
		}
		
		[NotNull]
		public static T[] ToArrayNonAlloc<T>([NotNull] this T[] source)
		{
			T[] destinationArray = new T[source.Length];
			Array.Copy(source, destinationArray, source.Length);
			return destinationArray;
		}
	}
}