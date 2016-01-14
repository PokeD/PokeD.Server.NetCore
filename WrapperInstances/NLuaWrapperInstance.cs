using System.Collections.Generic;
using System.IO;
using System.Linq;

using Aragas.Core.Wrappers;

using NLua;

using PCLStorage;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class NLua : ILua
    {
        private string LuaName { get; }

        internal Lua InternalLua => LuaScript;
        private Lua LuaScript { get; }


        public object this[string fullPath]
        {
            get { return LuaScript[fullPath]; }
            set { LuaScript[fullPath] = value; }
        }

        public NLua() { LuaScript = new Lua(); }
        public NLua(string luaName, bool instantInit = false)
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

        public object[] CallFunction(string functionName, params object[] args) => (LuaScript[functionName] as LuaFunction)?.Call(args);
    }

    public class NLuaTable : ILuaTable
    {
        private LuaTable TableScript { get; }


        public object this[object field]
        {
            get
            {
                if (TableScript[field] is LuaTable)
                    return new NLuaTable((LuaTable)TableScript[field]);
                return TableScript[field];
            }
            set { TableScript[field] = value; }
        }
        public object this[string field]
        {
            get
            {
                if (TableScript[field] is LuaTable)
                    return new NLuaTable((LuaTable)TableScript[field]);
                return TableScript[field];
            }
            set { TableScript[field] = value; }
        }

        private NLuaTable(LuaTable tableScript) { TableScript = tableScript; }
        public NLuaTable(ILua lua, string tableName) { TableScript = ((NLua) lua).InternalLua.GetTable(tableName); }

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
                return RecursiveParse(new NLuaTable((LuaTable) value).ToDictionary());

            return value;
        }

        public List<object> ToList() => TableScript.Values.Cast<object>().ToList();
        public object[] ToArray() => TableScript.Values.Cast<object>().ToArray();
    }

    public class NLuaWrapperInstance : ILuaWrapper
    {
        public ILua CreateLua() { return new NLua(); }
        public ILua CreateLua(string scriptName) { return new NLua(scriptName); }
        public ILuaTable CreateTable(ILua lua, string tableName) { return new NLuaTable(lua, tableName); }
    }
}
