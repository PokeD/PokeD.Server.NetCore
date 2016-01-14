using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public string ResolveFileName(string filename, Table globalContext) => $"{filename}.lua";

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
        private string LuaName { get; }

        private Script LuaScript { get; }


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

        public object[] CallFunction(string functionName, params object[] args)
        {
            return LuaScript.Call(LuaScript.Globals[functionName], args).Tuple;
            //return LuaScript.Call(LuaScript.Globals[functionName], args).Tuple.Select(obj => (object) obj).ToArray();
        }
    }

    public class MoonLuaTable : ILuaTable
    {
        private Table TableScript { get; }

        private MoonLuaTable(Table tableScript) { TableScript = tableScript; }
        public MoonLuaTable(ILua lua, string tableName) { TableScript = lua[tableName] as Table; }

        public object this[object field]
        {
            get { return TableScript[field] is Table ? new MoonLuaTable((Table) TableScript[field]) : TableScript[field]; }
            set { TableScript[field] = value; }
        }
        public object this[string field]
        {
            get { return TableScript[field] is Table ? new MoonLuaTable((Table) TableScript[field]) : TableScript[field]; }
            set { TableScript[field] = value; }
        }

        public Dictionary<object, object> ToDictionary() => TableScript.Pairs.ToDictionary<TablePair, object, object>(pair => pair.Key, pair => RecursiveParse(pair.Value));
        private static object RecursiveParse(object value)
        {
            if (value is Table)
                return RecursiveParse(new MoonLuaTable((Table) value).ToDictionary());

            return value;
        }

        public List<object> ToList() => TableScript.Values.Cast<object>().ToList();
        public object[] ToArray() => TableScript.Values.Cast<object>().ToArray();
    }

    public class MoonLuaWrapperInstance : ILuaWrapper
    {
        public ILua CreateLua() { return new MoonLua(); }
        public ILua CreateLua(string scriptName) { return new MoonLua(scriptName); }
        public ILuaTable CreateTable(ILua lua, string tableName) { return new MoonLuaTable(lua, tableName); }
    }
}
