using System.Collections.Generic;
using JetBrains.Annotations;

namespace Core.Utils.Collections.Extensions
{
	public static class QueueExtensions
	{
		public static bool EnqueueIfDoesNotContain<T>([NotNull] this Queue<T> source, T element)
		{
			if (source.Contains(element) == false)
			{
				source.Enqueue(element);
				return true;
			}

			return false;
		}
	}
}