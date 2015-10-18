using System.Collections.Generic;
using System.IO;

using Aragas.Core.Wrappers;

using NLua;

using PCLStorage;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class NLuaClass : ILua
    {
        string LuaName { get; }

        internal Lua InternalLua => LuaScript;
        Lua LuaScript { get; }

        public NLuaClass() { LuaScript = new Lua(); }
        public NLuaClass(string luaName, bool instantInit = false)
        {
            LuaName = luaName;

            LuaScript = new Lua();
            LuaScript.LoadCLRPackage();

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
            get { return LuaScript[fullPath]; }
            set { LuaScript[fullPath] = value; }
        }

        public object[] CallFunction(string functionName, params object[] args) { return (LuaScript[functionName] as LuaFunction)?.Call(args); }
    }

    public class NLuaTableClass : ILuaTable
    {
        LuaTable TableScript { get; }

        private NLuaTableClass(LuaTable tableScript) { TableScript = tableScript; }
        public NLuaTableClass(ILua lua, string tableName) { TableScript = ((NLuaClass) lua).InternalLua.GetTable(tableName); }

        public object this[object field]
        {
            get
            {
                if (TableScript[field] is LuaTable)
                    return new NLuaTableClass((LuaTable) TableScript[field]);
                return TableScript[field];
            }
            set { TableScript[field] = value; }
        }
        public object this[string field]
        {
            get
            {
                if (TableScript[field] is LuaTable)
                    return new NLuaTableClass((LuaTable) TableScript[field]);
                return TableScript[field];
            }
            set { TableScript[field] = value; }
        }

        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>();

            var enumerator = TableScript.GetEnumerator();
            while (enumerator.MoveNext())
                dictionary.Add(enumerator.Key, RecursiveParse(enumerator.Value));

            return dictionary;
        }
        private static object RecursiveParse(object value)
        {
            if (value is LuaTable)
                return RecursiveParse(new NLuaTableClass((LuaTable) value).ToDictionary());

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

    public class NLuaWrapperInstance : ILuaWrapper
    {
        public ILua Create() { return new NLuaClass(); }
        public ILua Create(string scriptName) { return new NLuaClass(scriptName); }
        public ILuaTable Create(ILua lua, string tableName) { return new NLuaTableClass(lua, tableName); }
    }
}
