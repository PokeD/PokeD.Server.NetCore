using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using Aragas.Core.Wrappers;

using FileDbNs;

using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;

namespace PokeD.Server.Desktop.WrapperInstances
{
    /// <summary>
    /// SQL Database, only Primitive Types.
    /// </summary>
    public class SQLiteDatabase : IDatabase
    {
        public string FileExtension => ".sqlite3";
        public bool PrimitivesOnly => true;

        private SQLiteConnection Connection { get; set; }


        public IDatabase CreateDB(string databaseName)
        {
			Connection = new SQLiteConnection(new SQLitePlatformGeneric(), CombinePath(databaseName));

            return this;
        }


        public void CreateTable<T>() where T : DatabaseTable, new()
        {
			Connection.CreateTable<T>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);
        }

        public void Insert<T>(T obj) where T : DatabaseTable, new()
        {
            Connection.Insert(obj);
        }

        public void Update<T>(T obj) where T : DatabaseTable, new()
        {
            Connection.Update(obj);
        }

        public T Find<T>(Expression<Func<T, bool>> predicate) where T : DatabaseTable, new()
        {
            return Connection.Find(predicate);
        }


        private string CombinePath(string fileName) { return Path.Combine(FileSystemWrapper.DatabaseFolder.Path, fileName + FileExtension); }
    }

    /// <summary>
    /// NoSQL Database, only Primitive Types.
    /// </summary>
    public class FileDBDatabase : IDatabase
    {
        public string FileExtension => ".fdb";
        public bool PrimitivesOnly => true;

        private FileDb Database { get; set; }


        public IDatabase CreateDB(string databaseName)
        {
            Database = new FileDb();

            return this;
        }


        public void CreateTable<T>() where T : DatabaseTable, new()
        {
            Database.Create(CombinePath(typeof(T).Name), CreateFields(new T()));
            Database.Close();
        }

        public void Insert<T>(T obj) where T : DatabaseTable, new()
        {
            Database.Open(CombinePath(typeof(T).Name), false);
            Database.AddRecord(CreateFieldValues(obj));
            Database.Close();
        }

        public void Update<T>(T obj) where T : DatabaseTable, new()
        {
            Database.Open(CombinePath(typeof(T).Name), false);

            var idProp = obj.GetType().GetProperties().FirstOrDefault(property => property.Name == "Id");
            if (idProp != null)
                Database.UpdateRecords(new FilterExpression("Id", idProp.GetValue(obj), ComparisonOperatorEnum.Equal), CreateFieldValues(obj));

            Database.Close();
        }

        public T Find<T>(Expression<Func<T, bool>> predicate) where T : DatabaseTable, new()
        {
            Database.Open(CombinePath(typeof(T).Name), true);

            var table = Database.SelectAllRecords();
            if (table.Count > 0)
            {
                var function = predicate.Compile();
                var result = table.Select(CreateT<T>).FirstOrDefault(function);
                Database.Close();
                return result;
            }
            else
            {
                Database.Close();
                return null;
            }
        }


        private string CombinePath(string fileName) { return Path.Combine(FileSystemWrapper.DatabaseFolder.Path, fileName + FileExtension); }


        private static Fields CreateFields(object obj)
        {
            var fields = new Fields();

			foreach (var info in obj.GetType().GetProperties())
            {
                DataTypeEnum dataType;

                if (info.PropertyType.IsEnum)
                    dataType = DataTypeEnum.Int32;
				else if (info.PropertyType == typeof(Boolean))
                    dataType = DataTypeEnum.Bool;
                else
					if (!Enum.TryParse(info.PropertyType.Name, out dataType))
                        throw new Exception("Only Primitive Types supported!");


                var field = new Field(info.Name, dataType);
                if (info.Name == "Id")
                {
                    field.IsPrimaryKey = true;
                    field.AutoIncStart = 1;
                }

                fields.Add(field);
            }
            return fields;
        }

        private static FieldValues CreateFieldValues(object obj)
        {
            var fieldValues = new FieldValues();

			foreach (var propertyInfo in obj.GetType().GetProperties())
                fieldValues.Add(propertyInfo.Name, propertyInfo.GetValue(obj));

            return fieldValues;
        }

        private static T CreateT<T>(Record record) where T : class, new()
        {
            var instance = new T();

			foreach (var info in instance.GetType().GetProperties())
                info.SetValue(instance, record[info.Name]);

            return instance;
        }
    }
}
