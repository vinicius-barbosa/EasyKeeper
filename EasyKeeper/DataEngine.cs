using EasyKeeper.CustomAttributes;
using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace EasyKeeper
{
	public class DataEngine
	{
		private string _dataProjectNamespace;
		private string _connectionStringName;

		public DataEngine(string dataProjectNamespace, string connectionStringName)
		{
			_dataProjectNamespace = dataProjectNamespace;
			_connectionStringName = connectionStringName;
		}

		public T[] Retrieve<T>(string relationships = "", string filters = "(1 = 1)")
		{
			var dataObject = GetDataObject<T>();
			var sqlCommand = GetRetrieveCommand(dataObject, relationships, filters);
			var properties = GetClassProperties(dataObject.GetType());
			var values = new object[properties.Length];
			var result = new T[0];

			using (var connection = new DataAccess(_connectionStringName).Connection)
			{
				connection.Open();
				using (DbCommand command = connection.CreateCommand())
				{
					command.CommandText = sqlCommand;
					using (DbDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							for (int index = 0; index < properties.Length; index++)
								values[index] = reader.GetValue(index);

							var filledObject = FillPropertiesWithValues<T>(properties, values);
							Include<T>(ref result, filledObject);
						}
					}
				}
			}

			return result;
		}

		public void Insert<T>(T objectWithValues)
		{
			var dataObject = GetDataObject<T>();
			var sqlCommand = GetCreateCommand(dataObject, objectWithValues);
			
			using (var connection = new DataAccess(_connectionStringName).Connection)
			{
				connection.Open();
				using (DbCommand command = connection.CreateCommand())
				{
					command.CommandText = sqlCommand;
					command.ExecuteNonQuery();
				}
			}
		}

		public void Update<T>(T objectWithData)
		{
			var dataObject = GetDataObject<T>();
			var sqlCommand = GetUpdateCommand(dataObject, objectWithData);

			using (var connection = new DataAccess(_connectionStringName).Connection)
			{
				connection.Open();
				using (DbCommand Command = connection.CreateCommand())
				{
					Command.CommandText = sqlCommand;
					Command.ExecuteNonQuery();
				}
			}
		}

		public void Delete<T>(T objetoComDados)
		{
			var dataObject = GetDataObject<T>();
			var sqlCommand = GetDeleteCommand(dataObject, objetoComDados);
			
			using (var connection = new DataAccess(_connectionStringName).Connection)
			{
				connection.Open();
				using (DbCommand Command = connection.CreateCommand())
				{
					Command.CommandText = sqlCommand;
					Command.ExecuteNonQuery();
				}
			}
		}

		private string GetRetrieveCommand(object dataObject, string relationships, string filters)
		{
			var tableName = GetTableName(dataObject);
			var objectType = dataObject.GetType().BaseType;
			var propertyInformation = objectType.GetProperties().Where(property => property.Name.Equals("RetrieveCommand")).First();
			var retrieveCommand = propertyInformation.GetValue(dataObject).ToString();
			
			return string.Format(retrieveCommand, tableName, relationships, filters);
		}

		private string GetCreateCommand(object dataObject, object objectWithData)
		{
			var tablaName = GetTableName(dataObject);
			var objectBaseType = dataObject.GetType().BaseType;
			var propertyInfo = objectBaseType.GetProperties().Where(property => property.Name.Equals("CreateCommand")).First();

			var objectType = dataObject.GetType();
			var propertiesInfo = objectType.GetProperties().Where(propriedade => !propriedade.Name.Contains("Command") && !propriedade.Name.Contains("Id")).ToArray();

			var totalOfProperties = propertiesInfo.Count();
			var columns = new string[totalOfProperties];
			var values = new string[totalOfProperties];

			for (int index = 0; index < totalOfProperties; index++)
			{
				columns[index] = GetColumnName(dataObject, propertiesInfo[index].Name);
				values[index] = GetScriptValueForSqlExecution(propertiesInfo[index], objectWithData);
			}

			var CommandDeInclusao = propertyInfo.GetValue(dataObject).ToString();
			return string.Format(CommandDeInclusao, tablaName, string.Join(", ", columns), string.Join(", ", values));
		}

		private string GetUpdateCommand(object dataObject, object objectWithData)
		{
			var tableName = GetTableName(dataObject);
			var dataObjectBaseType = dataObject.GetType().BaseType;
			var propertyInformation = dataObjectBaseType.GetProperties().Where(property => property.Name.Equals("UpdateCommand")).First();

			var objectType = dataObject.GetType();
			var propertiesInfo = objectType.GetProperties().Where(property => (!property.Name.Contains("Command") && !property.Name.Contains("Codigo")) || property.Name.Equals("Codigo")).ToArray();

			var totalOfProperities = propertiesInfo.Count();
			var updates = new string[totalOfProperities - 1];

			for (int index = 1; index < totalOfProperities; index++)
			{
				var column = GetColumnName(dataObject, propertiesInfo[index].Name);
				var value = GetScriptValueForSqlExecution(propertiesInfo[index], objectWithData);
				updates[index - 1] = String.Concat(column, " = ", value);
			}

			var updatesBlock = String.Join(", ", updates);
			var filter = String.Concat("codigo", " = ", GetScriptValueForSqlExecution(propertiesInfo.First(), objectWithData));
			var updateCommand = propertyInformation.GetValue(dataObject).ToString();
			return string.Format(updateCommand, tableName, updatesBlock, filter);
		}

		private string GetDeleteCommand(object dataObject, object objectWithData)
		{
			var tableName = GetTableName(dataObject);
			var objectBaseType = dataObject.GetType().BaseType;
			var propertyInformation = objectBaseType.GetProperties().Where(property => property.Name.Equals("DeleteCommand")).First();

			var objectType = dataObject.GetType();
			var propertiesInformation = objectType.GetProperties().Where(property => (!property.Name.Contains("Command") && !property.Name.Contains("Codigo")) || property.Name.Equals("Codigo")).ToArray();

			var filter = String.Concat("codigo", " = ", GetScriptValueForSqlExecution(propertiesInformation.First(), objectWithData));
			var deleteCommand = propertyInformation.GetValue(dataObject).ToString();
			return string.Format(deleteCommand, tableName, filter);
		}

		private string GetScriptValueForSqlExecution(PropertyInfo propertyInformation, object objectWithData)
		{
			var returnValue = string.Empty;
			var typeOfObjectWithData = objectWithData.GetType();
			var correctPropertySearch = typeOfObjectWithData.GetProperties().Where(property => property.Name.Equals(propertyInformation.Name));

			if (correctPropertySearch.Any())
			{
				var informationOfPropertyWithData = correctPropertySearch.First();
				var valueOfProperty = informationOfPropertyWithData.GetValue(objectWithData);
				
				if (valueOfProperty == null)
					returnValue = "null";
				else
					switch (propertyInformation.PropertyType.FullName)
					{
						case "System.String":
							returnValue = String.Concat("'", valueOfProperty.ToString(), "'");
							break;
						case "System.Int32":
							returnValue = valueOfProperty.ToString();
							break;
						case "System.Boolean":
							returnValue = Convert.ToBoolean(valueOfProperty) ? "1" : "0";
							break;
						default:
							break;
					}
			}

			return returnValue;
		}

		public T FillPropertiesWithValues<T>(string[] properties, object[] values)
		{
			var objectType = typeof(T);
			var propertiesInfo = objectType.GetProperties();
			var classInstance = Activator.CreateInstance(objectType);

			for (int index = 0; index < properties.Length; index++)
			{
				var informationsOfPropertiesSearch = objectType.GetProperties().Where(info => info.Name.Equals(properties[index]));

				if (informationsOfPropertiesSearch.Any())
				{
					var propertyInformation = informationsOfPropertiesSearch.First();
					var propertyType = propertiesInfo[index].PropertyType;
					object convertedValue = null;

					if (!string.IsNullOrEmpty(values[index].ToString()))
					{
						if (Nullable.GetUnderlyingType(propertyType) != null)
							propertyType = Nullable.GetUnderlyingType(propertyType);

						if (propertyType.IsEnum)
							convertedValue = Enum.Parse(propertyType, values[index].ToString());
						else
							convertedValue = Convert.ChangeType(values[index], Type.GetType(propertyType.FullName));
					}

					propertyInformation.SetValue(classInstance, convertedValue);
				}
			}

			return (T)classInstance;
		}

		private string[] GetClassProperties(Type tipo)
		{
			var informacoesDePropriedades = tipo.GetProperties().Where(propriedade => !propriedade.Name.Contains("Command")).ToArray();
			var numeroDePropriedades = informacoesDePropriedades.Count();
			var propriedades = new string[numeroDePropriedades];
			for (int indice = 0; indice < numeroDePropriedades; indice++)
			{
				propriedades[indice] = informacoesDePropriedades[indice].Name;
			}

			return propriedades;
		}

		private string GetTableName(object dataObject)
		{
			foreach (var attribute in Attribute.GetCustomAttributes(dataObject.GetType()))
			{
				if (attribute.GetType() == typeof(TableAttribute))
				{
					foreach (var property in attribute.GetType().GetProperties())
					{
						if (property.Name.Equals("Name"))
						{
							return property.GetValue(attribute).ToString();
						}
					}
				}
			}

			return null;
		}

		private string GetColumnName(object dataObject, string propertyName)
		{
			var correctPropertySearch = dataObject.GetType().GetMembers().Where(property => property.Name.Equals(propertyName));

			if (correctPropertySearch.Any())
			{
				var correctProperty = correctPropertySearch.First();

				foreach (var attribute in correctProperty.GetCustomAttributes())
					if (attribute.GetType() == typeof(ColumnAttribute))
						foreach (var property in attribute.GetType().GetProperties())
							if (property.Name.Equals("Name"))
								return property.GetValue(attribute).ToString();
			}

			return null;
		}

		private object GetDataObject<T>()
		{
			var nameOfTheClass = typeof(T).Name;
			var baseInstancePath = GetBaseInstancePath();
			var instancePath = string.Format(baseInstancePath, nameOfTheClass);
			var objectType = Type.GetType(instancePath);
			return Activator.CreateInstance(objectType);
		}

		private string GetBaseInstancePath()
		{
			var namespaceItems = _dataProjectNamespace.Split('.');
			var capacityOfAuxiliarArray = namespaceItems.Length - 1;
			var auxiliarArrayOfNamespaceItems = new string[capacityOfAuxiliarArray];

			for (int index = 0; index < capacityOfAuxiliarArray; index++)
				auxiliarArrayOfNamespaceItems[index] = namespaceItems[index];

			return string.Concat(_dataProjectNamespace, ".{0}, ", string.Join(".", auxiliarArrayOfNamespaceItems));
		}

		public void Include<T>(ref T[] collection, T objectToInsert)
		{
			var newLength = collection.Length + 1;
			var auxiliarCollection = new T[newLength];

			for (int indice = 0; indice < collection.Length; indice++)
			{
				auxiliarCollection[indice] = collection[indice];
			}

			auxiliarCollection[newLength - 1] = objectToInsert;
			collection = auxiliarCollection;
		}
	}
}
