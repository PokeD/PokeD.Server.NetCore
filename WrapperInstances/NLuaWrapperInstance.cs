﻿using System.Collections.Generic;
using System.IO;

using Aragas.Core.Wrappers;

using NLua;

using PCLStorage;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class NLua : ILua
    {
        string LuaName { get; }

        internal Lua InternalLua => LuaScript;
        Lua LuaScript { get; }

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

        public object this[string fullPath]
        {
            get { return LuaScript[fullPath]; }
            set { LuaScript[fullPath] = value; }
        }

        public object[] CallFunction(string functionName, params object[] args) { return (LuaScript[functionName] as LuaFunction)?.Call(args); }
    }

    public class NLuaTable : ILuaTable
    {
        LuaTable TableScript { get; }

        private NLuaTable(LuaTable tableScript) { TableScript = tableScript; }
        public NLuaTable(ILua lua, string tableName) { TableScript = ((NLua) lua).InternalLua.GetTable(tableName); }

        public object this[object field]
        {
            get
            {
                if (TableScript[field] is LuaTable)
                    return new NLuaTable((LuaTable) TableScript[field]);
                return TableScript[field];
            }
            set { TableScript[field] = value; }
        }
        public object this[string field]
        {
            get
            {
                if (TableScript[field] is LuaTable)
                    return new NLuaTable((LuaTable) TableScript[field]);
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
                return RecursiveParse(new NLuaTable((LuaTable) value).ToDictionary());

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
        public ILua CreateLua() { return new NLua(); }
        public ILua CreateLua(string scriptName) { return new NLua(scriptName); }
        public ILuaTable CreateTable(ILua lua, string tableName) { return new NLuaTable(lua, tableName); }
    }
}
