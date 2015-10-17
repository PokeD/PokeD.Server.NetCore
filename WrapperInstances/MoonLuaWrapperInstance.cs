using System.Collections.Generic;
using System.IO;
using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

using PCLStorage;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class FileSystemScriptLoader : IScriptLoader
    {
        public object LoadFile(string file, Table globalContext)
        {
            if (FileSystemWrapper.LuaFolder.CheckExistsAsync(file).Result == ExistenceCheckResult.FileExists)
                using (var stream = FileSystemWrapper.LuaFolder.GetFileAsync(file).Result.OpenAsync(PCLStorage.FileAccess.Read).Result)
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();

            return null;
        }

        public string ResolveFileName(string filename, Table globalContext)
        {
            return $"{filename}.lua";
        }

        public string ResolveModuleName(string modname, Table globalContext)
        {
            //var moduleName = $"{modname}.lua";
            //if (FileSystemWrapper.LuaFolder.CheckExistsAsync(moduleName).Result == ExistenceCheckResult.FileExists)
            //    return moduleName;

            return modname;
        }
    }


    public class MoonLuaClass : ILua
    {
        static MoonLuaClass()
        {
            UserData.RegisterType<Vector3>();
        }

        string LuaName { get; }

        Script Script { get; }

        public MoonLuaClass() { Script = new Script(); }
        public MoonLuaClass(string luaName, bool instantInit = false)
        {
            LuaName = luaName;

            Script = new Script();
            Script.Options.ScriptLoader = new FileSystemScriptLoader();

            if (instantInit)
                ReloadFile();
        }

        public bool ReloadFile()
        {

            if (FileSystemWrapper.LuaFolder.CheckExistsAsync(LuaName).Result == ExistenceCheckResult.FileExists)
                using (var stream = FileSystemWrapper.LuaFolder.GetFileAsync(LuaName).Result.OpenAsync(PCLStorage.FileAccess.Read).Result)
                using (var reader = new StreamReader(stream))
                {
                    Script.DoString(reader.ReadToEnd());
                    return true;
                }

            return false;
        }

        public object this[string fullPath]
        {
            get { return Script.Globals[fullPath]; }
            set
            {
                var type = value.GetType();
                if (!UserData.IsTypeRegistered(type))
                    UserData.RegisterType(type);
                
                Script.Globals[fullPath] = value;
            }
        }

        public object[] CallFunction(string functionName, params object[] args)
        {
            return Script.Call(Script.Globals[functionName], args).Tuple;
        }
    }

    public class MoonLuaTableClass : ILuaTable
    {
        Table Table { get; }

        private MoonLuaTableClass(Table table) { Table = table; }
        public MoonLuaTableClass(ILua lua, string tableName) { Table = lua[tableName] as Table; }

        public object this[object field]
        {
            get
            {
                if (Table[field] is Table)
                    return new MoonLuaTableClass((Table) Table[field]);
                return Table[field];
            }
            set { Table[field] = value; }
        }
        public object this[string field]
        {
            get
            {
                if (Table[field] is Table)
                    return new MoonLuaTableClass((Table) Table[field]);
                return Table[field];
            }
            set { Table[field] = value; }
        }

        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>();

            foreach (var pair in Table.Pairs)
                dictionary.Add(pair.Key, RecursiveParse(pair.Value));
            
            return dictionary;
        }
        private static object RecursiveParse(object value)
        {
            if (value is Table)
                return RecursiveParse(new MoonLuaTableClass((Table) value).ToDictionary());

            return value;
        }

        public List<object> ToList()
        {
            var list = new List<object>();
            foreach (var value in Table.Values)
                list.Add(value);

            return list;
        }
        public object[] ToArray()
        {
            var list = new List<object>();
            foreach (var value in Table.Values)
                list.Add(value);

            return list.ToArray();
        }
    }

    public class MoonLuaWrapperInstance : ILuaWrapper
    {
        public ILua Create() { return new MoonLuaClass(); }
        public ILua Create(string scriptName) { return new MoonLuaClass(scriptName); }
        public ILuaTable Create(ILua lua, string tableName) { return new MoonLuaTableClass(lua, tableName); }
    }
}
