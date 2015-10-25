using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Aragas.Core.Wrappers;

using FileDbNs;

using Sqo;

using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;

namespace PokeD.Server.Desktop.WrapperInstances
{
    /// <summary>
    /// SQL Database, only Primitive types.
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
    /// NoSQL Database, only Primitive types.
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

            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                DataTypeEnum dataType;

                if (propertyInfo.PropertyType.IsEnum)
                    dataType = DataTypeEnum.Int32;
                else if (propertyInfo.PropertyType == typeof(Boolean))
                    dataType = DataTypeEnum.Bool;
                else
                    if (!Enum.TryParse(propertyInfo.PropertyType.Name, out dataType))
                    throw new Exception("Only Primitive types supported!");


                var field = new Field(propertyInfo.Name, dataType);
                if (propertyInfo.Name == "Id")
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

            foreach (var propertyInfo in instance.GetType().GetProperties())
                propertyInfo.SetValue(instance, record[propertyInfo.Name]);

            return instance;
        }
    }

    /// <summary>
    /// NoSQL Database. Array and List supported.
    /// Never again.
    /// </summary>
    public class SiaqodbDatabase : IDatabase
    {
        private int ChildClassMaxCount { get; set; }

        public string FileExtension => ".mdb";
        public bool PrimitivesOnly => false;

        Siaqodb Database { get; set; }


        public SiaqodbDatabase(int childClassMaxCount = 20)
        {
            ChildClassMaxCount = childClassMaxCount;
        }
        
        public IDatabase CreateDB(string databaseName)
        {
            Database = new Siaqodb(CombinePath(databaseName), 10485760);

            return this;
        }
        
        public void CreateTable<T>() where T : DatabaseTable, new()
        {
            var obj = new T();
            LoadAllCustomTypes(obj);
        }

        public T Find<T>(Expression<Func<T, bool>> predicate) where T : DatabaseTable, new()
        {
            var function = predicate.Compile();

            var fullList = new List<T>();

            // Original class
            //fullList.AddRange(Database.LoadAll<T>());

            // Custom classes
            var customTypeList = DatabaseTable.GetCustomTableType(typeof(T)).CustomTypes;
            for(var i = 0; i < customTypeList.Count; i++)
            {
                var customTypeInfo = customTypeList[i];

                var instance = Activator.CreateInstance(customTypeInfo.CustomType);
                var obj = TypeLoadAll(instance).Cast<T>().Where(function).FirstOrDefault();
                if (obj != null)
                    return (T)CreateBaseTypeAndFill(typeof(T), obj, i * 100);
            }

            return null;
        }

        public void Update<T>(T obj) where T : DatabaseTable, new()
        {
            // Original class
            //Database.StoreObject(obj);
            //
            // Custom classes
            //var customTypeList = TableInfo.GetCustomTableTypes(typeof(T)).CustomTypes;
            //customTypeList.ForEach(customTypeInfo => TypeStoreObject(obj));


            // Original class
            //Database.UpdateObjectBy("Id", obj);

            // Custom classes
            var customTypeList = DatabaseTable.GetCustomTableType(typeof(T)).CustomTypes;
            customTypeList.ForEach(customTypeInfo => TypeUpdateObjectBy("Id", CreateCustomTypeAndFill(customTypeInfo.CustomType, obj)));
        }

        public void Insert<T>(T obj) where T : DatabaseTable, new()
        {
            // Original class
            //var count = Database.LoadAll<T>().Count;
            //if (count < 100)
            //        Database.StoreObject(obj);
            // Custom classes
            //else
            {
                var customTypeInfo = DatabaseTable.GetCustomTableType(obj.GetType()).CustomTypes.First(cType => cType.Count < 100);
                var customType = customTypeInfo.CustomType;
                customTypeInfo.Count++;

                var newObj = CreateCustomTypeAndFill(customType, obj);
                Database.StoreObject(newObj);
            }
        }

        /// <summary>
        /// Do not delete! Called via reflection
        /// </summary>
        private void StoreObject( object obj)
        {
            Database.StoreObject(obj);
        }
        private void TypeStoreObject( object obj)
        {
            var method = typeof(SiaqodbDatabase).GetMethod("StoreObject", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(this, new[] { obj });
        }

        /// <summary>
        /// Do not delete! Called via reflection
        /// </summary>
        private bool UpdateObjectBy(string filedName, object obj)
        {
            return Database.UpdateObjectBy(filedName, obj);
        }
        private bool TypeUpdateObjectBy(string filedName, object obj)
        {
            var method = typeof(SiaqodbDatabase).GetMethod("UpdateObjectBy", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool) method.Invoke(this, new[] { filedName, obj });
        }

        /// <summary>
        /// Do not delete! Called via reflection
        /// </summary>
        private IEnumerable<T> LoadAll<T>()
        {
            return Database.LoadAll<T>();
        }
        private IEnumerable<T> TypeLoadAll<T>(T obj) where T : class, new()
        {
            var method = typeof(SiaqodbDatabase).GetMethod("LoadAll", BindingFlags.NonPublic | BindingFlags.Instance);
            method = method.MakeGenericMethod(obj.GetType());
            return (IEnumerable<T>) method.Invoke(this, null);
        }

        /// <summary>
        /// Do not delete! Called via reflection
        /// </summary>
        private int GetCount<T>()
        {
            int count = Database.Count<T>();
            return count;
        }
        private int GetTypeCount(Type customType)
        {
            var method = typeof(SiaqodbDatabase).GetMethod("GetCount", BindingFlags.NonPublic | BindingFlags.Instance);
            method = method.MakeGenericMethod(customType);
            return (int) method.Invoke(this, null);
        }

        private void LoadAllCustomTypes<T>(T obj) where T : class, new()
        {
            var type = obj.GetType();

            DatabaseTable.AddCustomTableType(type);
            for (var i = 0; i < ChildClassMaxCount; i++)
            {
                var name = type.Name;
                var customType = DeclareChildType(obj, name + i);

                var count = GetTypeCount(customType);
                DatabaseTable.AddCustomType(type, customType, count);
            }
        }

        private static Type DeclareChildType<TBaseType>(TBaseType obj, string name)
        {
            var type = obj.GetType();

            var assemblyName = new AssemblyName(type.Namespace);

            var thisDomain = Thread.GetDomain();
            var asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule(asmBuilder.GetName().Name, false);
            var typeBuilder = modBuilder.DefineType(
                type.Namespace + "." + name, 
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                type);


            var fieldInfo = type.GetField("Id");

            var propBuilder = typeBuilder.DefineProperty("OID", PropertyAttributes.HasDefault, fieldInfo.FieldType, Type.EmptyTypes);

            var getPropertyBuiler = CreatePropertyGetter(typeBuilder, fieldInfo);
            var setPropertyBuiler = CreatePropertySetter(typeBuilder, fieldInfo);

            propBuilder.SetGetMethod(getPropertyBuiler);
            propBuilder.SetSetMethod(setPropertyBuiler);


            return typeBuilder.CreateType();
        }
        private static MethodBuilder CreatePropertyGetter(TypeBuilder typeBuilder, FieldInfo fieldInfo)
        {
            MethodBuilder getMethodBuilder =
                typeBuilder.DefineMethod("get_" + fieldInfo.Name,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    fieldInfo.FieldType, Type.EmptyTypes);

            ILGenerator getIL = getMethodBuilder.GetILGenerator();

            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldInfo);
            getIL.Emit(OpCodes.Ret);

            return getMethodBuilder;
        }
        private static MethodBuilder CreatePropertySetter(TypeBuilder typeBuilder, FieldInfo fieldInfo)
        {
            MethodBuilder setMethodBuilder =
                typeBuilder.DefineMethod("set_" + fieldInfo.Name,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new Type[] { fieldInfo.FieldType });

            ILGenerator setIL = setMethodBuilder.GetILGenerator();

            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldInfo);
            setIL.Emit(OpCodes.Ret);

            return setMethodBuilder;
        }
        
        private static object CreateCustomTypeAndFill<TCustomType, TBaseType>(TCustomType type, TBaseType obj) where  TCustomType : Type
        {
            var instance = Activator.CreateInstance(type);

            foreach (var info in obj.GetType().GetProperties())
                info.SetValue(instance, info.GetValue(obj));

            foreach (var info in obj.GetType().GetFields())
                info.SetValue(instance, info.GetValue(obj));

            return instance;
        }
        private static object CreateBaseTypeAndFill<TBaseType, TCustomType>(TBaseType type, TCustomType obj, int idOffset) where TBaseType : Type
        {
            var instance = Activator.CreateInstance(type);

            foreach (var info in instance.GetType().GetProperties())
                info.SetValue(instance, info.GetValue(obj));

            foreach (var info in instance.GetType().GetFields())
                info.SetValue(instance, info.GetValue(obj));

            var prop = obj.GetType().GetProperties().First(propertyInfo => propertyInfo.Name.Contains("OID"));
            var field = obj.GetType().GetFields().First(fieldInfo => fieldInfo.Name.Contains("Id"));
            field.SetValue(instance, (int)prop.GetValue(obj) + idOffset);

            return instance;
        }


        private string CombinePath(string fileName) { return Path.Combine(FileSystemWrapper.DatabaseFolder.Path, fileName); }
    }

    /*
    /// <summary>
    /// NoSQL Database
    /// </summary>
    public class CouchbaseDatabase : IDatabase
    {
        public string FileExtension => ".cblite";
        public bool PrimitivesOnly => false;

        Manager Manager { get; set; }
        Database Database { get; set; }


        public IDatabase CreateDB(string databaseName)
        {
            Manager = new Manager(new DirectoryInfo(FileSystemWrapper.DatabaseFolder.Path), ManagerOptions.Default);
            Database = Manager.GetDatabase(databaseName);

            return this;
        }


        public void CreateTable<T>() where T : TableInfo, new()
        {
            var t = Database.GetDocument(typeof(T).Name);
        }

        public T Find<T>(Expression<Func<T, bool>> predicate) where T : TableInfo, new()
        {
            var document = Database.GetDocument(typeof(T).Name);
            //document.

            return null;
        }

        public void Update<T>(T obj) where T : TableInfo, new()
        {
            var document = Database.GetDocument(typeof(T).Name);
            document.Update(newRevision =>
            {
                var properties = newRevision.Properties;

                foreach (var propertyInfo in obj.GetType().GetProperties())
                    properties[propertyInfo.Name] = propertyInfo.GetValue(obj);

                return true;
            });
        }

        public void Insert<T>(T obj) where T : TableInfo, new()
        {
            var t = CreateDictionary(obj);
            Database.PutLocalDocument(typeof(T).Name, CreateDictionary(obj));
        }


        private static IDictionary<string, object> CreateDictionary(object obj)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var propertyInfo in obj.GetType().GetProperties())
                dictionary.Add(propertyInfo.Name, propertyInfo.GetValue(obj));

            return dictionary;
        }

        private static T CreateT<T>(Document document) where T : class, new()
        {
            var instance = new T();

            foreach (var propertyInfo in instance.GetType().GetProperties())
                propertyInfo.SetValue(instance, document.GetProperty(propertyInfo.Name));

            return instance;
        }
    }
    */
}
