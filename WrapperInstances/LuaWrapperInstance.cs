using System.Collections.Generic;
using System.IO;

using Aragas.Core.Wrappers;

using NLua;

using PCLStorage;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class LuaClass : ILua
    {
        string LuaName { get; }

        internal Lua InternalLua => Script;
        Lua Script { get; }
        string ScriptText { get; }

        public LuaClass() { Script = new Lua(); }
        public LuaClass(string luaName, bool instantInit = false)
        {
            LuaName = luaName;

            Script = new Lua();
            Script.LoadCLRPackage();

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
            get { return Script[fullPath]; }
            set { Script[fullPath] = value; }
        }

        public object[] CallFunction(string functionName, params object[] args) { return (Script[functionName] as LuaFunction)?.Call(args); }
    }

    public class LuaTableClass : ILuaTable
    {
        LuaTable Table { get; }

        private LuaTableClass(LuaTable table) { Table = table; }
        public LuaTableClass(ILua lua, string tableName) { Table = ((LuaClass) lua).InternalLua.GetTable(tableName); }

        public object this[object field]
        {
            get
            {
                if (Table[field] is LuaTable)
                    return new LuaTableClass((LuaTable) Table[field]);
                return Table[field];
            }
            set { Table[field] = value; }
        }
        public object this[string field]
        {
            get
            {
                if (Table[field] is LuaTable)
                    return new LuaTableClass((LuaTable) Table[field]);
                return Table[field];
            }
            set { Table[field] = value; }
        }

        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>();

            var enumerator = Table.GetEnumerator();
            while (enumerator.MoveNext())
                dictionary.Add(enumerator.Key, RecursiveParse(enumerator.Value));

            return dictionary;
        }
        private static object RecursiveParse(object value)
        {
            if (value is LuaTable)
                return RecursiveParse(new LuaTableClass((LuaTable) value).ToDictionary());

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

    public class LuaWrapperInstance : ILuaWrapper
    {
        public ILua Create() { return new LuaClass(); }
        public ILua Create(string scriptName) { return new LuaClass(scriptName); }
        public ILuaTable Create(ILua lua, string tableName) { return new LuaTableClass(lua, tableName); }
    }
}
