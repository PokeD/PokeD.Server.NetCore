using System.Collections.Generic;
using System.IO;

using Aragas.Core.Wrappers;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

using PCLStorage;

namespace PokeD.Server.Desktop.WrapperInstances
{
    internal class FileSystemScriptLoader : IScriptLoader
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


    public class MoonLua : ILua
    {
        string LuaName { get; }

        Script LuaScript { get; }

        public MoonLua() { LuaScript = new Script(); }
        public MoonLua(string luaName, bool instantInit = false)
        {
            LuaName = luaName;

            LuaScript = new Script();
            LuaScript.Options.ScriptLoader = new FileSystemScriptLoader();

            if (instantInit)
                ReloadFile();
        }

        public bool ReloadFile()
        {

            if (FileSystemWrapper.LuaFolder.CheckExistsAsync(LuaName).Result == ExistenceCheckResult.FileExists)
                using (var stream = FileSystemWrapper.LuaFolder.GetFileAsync(LuaName).Result.OpenAsync(PCLStorage.FileAccess.Read).Result)
                using (var reader = new StreamReader(stream))
                {
                    LuaScript.DoString(reader.ReadToEnd());
                    return true;
                }

            return false;
        }

        public object this[string fullPath]
        {
            get { return LuaScript.Globals[fullPath]; }
            set
            {
                var type = value.GetType();
                if (!UserData.IsTypeRegistered(type))
                    UserData.RegisterType(type);
                
                LuaScript.Globals[fullPath] = value;
            }
        }

        public object[] CallFunction(string functionName, params object[] args)
        {
            return LuaScript.Call(LuaScript.Globals[functionName], args).Tuple;
        }
    }

    public class MoonLuaTable : ILuaTable
    {
        Table TableScript { get; }

        private MoonLuaTable(Table tableScript) { TableScript = tableScript; }
        public MoonLuaTable(ILua lua, string tableName) { TableScript = lua[tableName] as Table; }

        public object this[object field]
        {
            get
            {
                if (TableScript[field] is Table)
                    return new MoonLuaTable((Table) TableScript[field]);
                return TableScript[field];
            }
            set { TableScript[field] = value; }
        }
        public object this[string field]
        {
            get
            {
                if (TableScript[field] is Table)
                    return new MoonLuaTable((Table) TableScript[field]);
                return TableScript[field];
            }
            set { TableScript[field] = value; }
        }

        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>();

            foreach (var pair in TableScript.Pairs)
                dictionary.Add(pair.Key, RecursiveParse(pair.Value));
            
            return dictionary;
        }
        private static object RecursiveParse(object value)
        {
            if (value is Table)
                return RecursiveParse(new MoonLuaTable((Table) value).ToDictionary());

            return value;
        }

        public List<object> ToList()
        {
            var list = new List<object>();
            foreach (var value in TableScript.Values)
                list.Add(value);

            return list;
        }
        public object[] ToArray()
        {
            var list = new List<object>();
            foreach (var value in TableScript.Values)
                list.Add(value);

            return list.ToArray();
        }
    }

    public class MoonLuaWrapperInstance : ILuaWrapper
    {
        public ILua CreateLua() { return new MoonLua(); }
        public ILua CreateLua(string scriptName) { return new MoonLua(scriptName); }
        public ILuaTable CreateTable(ILua lua, string tableName) { return new MoonLuaTable(lua, tableName); }
    }
}
