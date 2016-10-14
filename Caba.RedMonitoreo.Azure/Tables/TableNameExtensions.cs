using System;
using System.Linq;

namespace Caba.RedMonitoreo.Azure.Tables
{
	public static class TableNameExtensions
	{
		private static readonly string[] tableEntityTypePostfixSequence = {"DataRow", "Data", "TableEntity", "Entity"};
		public static string AsTableStorageName(this Type dataRowType)
		{
			var typeName = dataRowType.Name;
			return tableEntityTypePostfixSequence
				.Select(x => TrucateTableName(x, typeName))
				.FirstOrDefault(x => x != null) ?? typeName;
		}

		private static string TrucateTableName(string postfix, string typeName)
		{
			return typeName.EndsWith(postfix) ? typeName.Substring(0, typeName.Length - postfix.Length) : null;
		}
	}
}