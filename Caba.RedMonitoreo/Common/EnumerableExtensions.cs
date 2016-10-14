using System;
using System.Collections.Generic;
using System.Linq;

namespace Caba.RedMonitoreo.Common
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Yield<T>(this T source)
		{
			yield return source;
		}

		public static IEnumerable<IEnumerable<TSource>> PagedBy<TSource>(this IEnumerable<TSource> source, int pageSize)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (pageSize <= 0)
			{
				yield break;
			}
			using (var enumerator = source.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var page = new List<TSource>(pageSize) {enumerator.Current};
					while (page.Count < pageSize && enumerator.MoveNext())
					{
						page.Add(enumerator.Current);
					}
					yield return page.AsEnumerable();
				}
			}
		}
	}
}