using System;

namespace EasyKeeper.CustomAttributes
{
	public class ColumnAttribute : Attribute
	{
		public string Name { get; set; }

		public ColumnAttribute(string name)
		{
			Name = name;
		}
	}
}
