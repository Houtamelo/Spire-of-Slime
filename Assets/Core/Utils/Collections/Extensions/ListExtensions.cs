using System;
using System.Collections.Generic;
using System.Text;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

namespace Core.Utils.Collections.Extensions
{
	public static class ListExtensions
	{
		private static readonly StringBuilder Builder = new();
		private static readonly Random Random = new();
		
		public static T GetRandom<T>([NotNull] this List<T> source)
		{
			if (source == null || source.Count == 0)
				throw new ArgumentException(message: "Source list is null or empty", nameof(source));
            
			int chosenIndex = Random.Next(maxValue: source.Count);
			return source[chosenIndex];
		}
		
		public static T GetWeightedRandom<T>([NotNull] this List<(T element, float points)> source)
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

		public static void Add<T>([NotNull] this List<T> source, [NotNull] List<T> toAdd)
		{
			foreach (T element in toAdd)
				source.Add(element);
		}
		
		public static void Add<T>([NotNull] this List<T> source, [NotNull] T[] toAdd)
		{
			for (int index = 0; index < toAdd.Length; index++)
			{
				T element = toAdd[index];
				source.Add(element);
			}
		}
		
		public static bool AddIfDoesNotContain<T>([NotNull] this List<T> source, T element)
		{
			if (source.Contains(element) == false)
			{
				source.Add(element);
				return true;
			}

			return false;
		}
		
		public static void ReInsert<T>([NotNull] this List<T> source, T element, int targetIndex)
		{
			int currentIndex = source.IndexOf(element);
			targetIndex = Mathf.Clamp(targetIndex, 0, source.Count - 1);
			if (currentIndex == -1 || currentIndex == targetIndex)
				return;
            
			source.RemoveAt(currentIndex);
			source.Insert(index: targetIndex, item: element);
		}
        
		public static void ReInsert<T>([NotNull] this List<T> source, int currentIndex, int targetIndex)
		{
			targetIndex = Mathf.Clamp(targetIndex, 0, source.Count - 1);
			if (currentIndex == -1 || currentIndex == targetIndex)
				return;
            
			T element = source.TakeAt<T, List<T>>(currentIndex);
			source.Insert(targetIndex, element);
		}
		
		public static void DoForEach<T>([NotNull] this List<T> source, Action<T> action)
		{
			foreach (T element in source)
				action.Invoke(element);
		}
		
		public static bool None<T>([NotNull] this List<T> source, Predicate<T> predicate)
		{
			foreach (T element in source)
			{
				if (predicate.Invoke(element))
					return false;
			}

			return true;
		}
		
		[NotNull]
		public static string ElementsToString<T>([NotNull] this List<T> source)
		{
			Builder.Clear();
			foreach (T element in source)
				Builder.AppendLine(element.ToString());

			return Builder.ToString();
		}
		
		public static FixedEnumerator<T> FixedEnumerate<T>([NotNull] this List<T> source) => new(source);
		
		public static Option<TSearch> FindType<TSearch, TSource>([NotNull] this List<TSource> source) where TSearch : class, TSource where TSource : class
		{
			foreach (TSource variable in source)
			{
				if (variable is TSearch casted)
					return casted;
			}

			return Option.None;
		}
		
		public static T TakeFirst<T>([NotNull] this List<T> collection)
		{
			T value = collection[0];
			collection.RemoveAt(index: 0);
			return value;
		}
		
		public static T TakeAt<T>([NotNull] this List<T> source, int index)
		{
			T element = source[index];
			source.RemoveAt(index);
			return element;
		}
	}
}