using System;
using System.Configuration;
using System.Data.Common;

namespace EasyKeeper
{
	public class DataAccess
	{
		private const string _SQLiteProvider = "System.Data.SQLite.SQLiteConnection, System.Data.SQLite";
		public DbConnection Connection { get; set; }
		public DbProviderFactory Factory
		{
			get { return DbProviderFactories.GetFactory(_SQLiteProvider); }
		}

		public DataAccess(string connectionStringName)
		{
			var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ToString();
			Connection = (DbConnection)Activator.CreateInstance(Type.GetType(_SQLiteProvider), connectionString);
		}
	}
}
