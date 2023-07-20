using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Core.Utils.Collections.Extensions
{
	public static class HashSetExtensions
	{
		private static readonly StringBuilder Builder = new();
		
		public static int Remove<T>(this HashSet<T> source, Predicate<T> predicate)
		{
			int removed = 0;
			foreach (T variable in source.FixedEnumerate())
			{
				if (predicate.Invoke(variable) && source.Remove(variable))
					removed++;
			}

			return removed;
		}
		
		public static void Remove<TKey, TValue>(this HashSet<TKey> source, [NotNull] Dictionary<TKey, TValue>.KeyCollection keyCollection)
		{
			foreach (TKey variable in keyCollection)
				source.Remove(variable);
		}

		public static FixedEnumerator<T> FixedEnumerate<T>([NotNull] this HashSet<T> source) => new(source);
		
		[NotNull]
		public static string ElementsToString<T>([NotNull] this HashSet<T> source)
		{
			Builder.Clear();
			foreach (T element in source)
				Builder.AppendLine(element.ToString());

			return Builder.ToString();
		}

		public static void Add<T>(this HashSet<T> source, [NotNull] HashSet<T> elementsToAdd)
		{
			foreach (T element in elementsToAdd)
				source.Add(element);
		}
		
		public static void Add<TKey, TValue>(this HashSet<TKey> source, [NotNull] Dictionary<TKey, TValue>.KeyCollection elementsToAdd)
		{
			foreach (TKey element in elementsToAdd)
				source.Add(element);
		}
		
		public static void Add<T>(this HashSet<T> source, [NotNull] List<T> elementsToAdd)
		{
			foreach (T element in elementsToAdd)
				source.Add(element);
		}
		
		public static void DoForEach<T>([NotNull] this HashSet<T> source, Action<T> action)
		{
			foreach (T element in source)
				action.Invoke(element);
		}
		
		public static bool None<T>([NotNull] this HashSet<T> source, Predicate<T> action)
		{
			foreach (T element in source)
			{
				if (action.Invoke(element))
					return false;
			}

			return true;
		}
	}
}