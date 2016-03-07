using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

using PCLStorage;
using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;

namespace PokeD.Server.Desktop.WrapperInstances
{
    internal class FileSystemScriptLoader : IScriptLoader
    {
        private static IFolder Modules => FileSystemWrapper.LuaFolder.CreateFolderAsync("modules", CreationCollisionOption.OpenIfExists).Result;

        public object LoadFile(string file, Table globalContext)
        {
            if (file.StartsWith("m_"))
            {
                if (FileSystemWrapper.LuaFolder.CheckExistsAsync(file).Result == ExistenceCheckResult.FileExists)
                    using (var stream = FileSystemWrapper.LuaFolder.GetFileAsync(file).Result.OpenAsync(PCLStorage.FileAccess.Read).Result)
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
            }
            else
            {
                if (Modules.CheckExistsAsync(file).Result == ExistenceCheckResult.FileExists)
                    using (var stream = Modules.GetFileAsync(file).Result.OpenAsync(PCLStorage.FileAccess.Read).Result)
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
            }
            
            return null;
        }

        public string ResolveFileName(string filename, Table globalContext) => $"{filename}.lua";

        public string ResolveModuleName(string modname, Table globalContext) => $"m_{modname}";
    }


    public class MoonLua : LuaScript
    {
        static MoonLua()
        {
            UserData.RegisterType<Vector3>();
            UserData.RegisterType<Vector2>();

            UserData.RegisterType<Client>();
            UserData.RegisterType<NPCPlayer>();
            UserData.RegisterType<CultureInfo>();
        }

        private string LuaName { get; }

        private Script LuaScript { get; }


        public override object this[string fullPath]
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

        public MoonLua(string luaName = "", bool instantInit = false)
        {
            LuaName = luaName;

            LuaScript = new Script();
            LuaScript.Options.ScriptLoader = new FileSystemScriptLoader();


            // Register custom modules that we allow to use.
            RegisterModule("hook");
            RegisterModule("translator");
            //LuaScript.Globals["Shit"] = new Table(LuaScript);
            //var t = LuaScript.LoadFile("translator", LuaScript.Globals["Shit"] as Table);
            //LuaScript.DoFile("hook");
            //LuaScript.DoFile("translator");

            RegisterCustom(LuaScript.Globals);


            if (instantInit)
                ReloadFile();
        }
        private Table CompileFile(string path)
        {
            CoreModules modules = CoreModules.Preset_SoftSandbox;
            IFolder folder = FileSystemWrapper.LuaFolder;

            var dirs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists).Result;
            
            var file = Path.GetFileName(path);
            var text = folder.GetFileAsync(file).Result.ReadAllTextAsync().Result;

            var table = new Table(LuaScript);
            table.RegisterCoreModules(modules);
            RegisterCustom(table);
            LuaScript.DoString(text, table);

            return table;
        }
        private string[] GetFiles(string path)
        {
            IFolder folder = FileSystemWrapper.LuaFolder;

            var dirs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists).Result;

            var files = folder.GetFilesAsync().Result;

            return files.Select(file => file.Name).ToArray();
        }
        private void RegisterModule(string moduleName, string tableName = "", CoreModules modules = CoreModules.Preset_SoftSandbox)
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = moduleName;

            var table = new Table(LuaScript);
            table.RegisterCoreModules(modules);
            RegisterCustom(table);
            LuaScript.DoFile(moduleName, table);

            LuaScript.Globals[tableName] = table;
        }
        private void RegisterCustom(Table table)
        {
            // Register custom types that we allow to use.
            table["Vector3"] = (Func<float, float, float, Vector3>)((x, y, z) => new Vector3(x, y, z));
            table["Vector2"] = (Func<float, float, Vector2>)((x, y) => new Vector2(x, y));
            table["CompileFile"] = (Func<string, Table>) CompileFile;
            table["GetFiles"] = (Func<string, string[]>) GetFiles;
        }


        public override bool ReloadFile()
        {
            if (FileSystemWrapper.LuaFolder.CheckExistsAsync(LuaName).Result == ExistenceCheckResult.FileExists)
                using (var stream = FileSystemWrapper.LuaFolder.GetFileAsync(LuaName).Result.OpenAsync(PCLStorage.FileAccess.Read).Result)
                using (var reader = new StreamReader(stream))
                {
                    var code = reader.ReadToEnd();
                     LuaScript.DoString(code);
                    return true;
                }

            return false;
        }

        public override object[] CallFunction(string functionName, params object[] args)
        {
            return LuaScript.Call(LuaScript.Globals[functionName], args).Tuple;
            //var t = LuaScript.Call(LuaScript.Globals[functionName], args);
            //return t.Tuple;
            //return LuaScript.Call(LuaScript.Globals[functionName], args).Tuple;
            //return LuaScript.Call(LuaScript.Globals[functionName], args).Tuple.Select(obj => (object) obj).ToArray();
        }
    }

    public class MoonLuaTable : LuaTable
    {
        private Table TableScript { get; }

        public MoonLuaTable(Table tableScript) { TableScript = tableScript; }
        public MoonLuaTable(LuaScript luaScript, string tableName) { TableScript = luaScript[tableName] as Table; }

        public override object this[object field]
        {
            get { return TableScript[field] is Table ? new MoonLuaTable((Table) TableScript[field]) : TableScript[field]; }
            set { TableScript[field] = value; }
        }
        public override object this[string field]
        {
            get { return TableScript[field] is Table ? new MoonLuaTable((Table) TableScript[field]) : TableScript[field]; }
            set { TableScript[field] = value; }
        }

        public override object[] CallFunction(string functionName, params object[] args) => TableScript.OwnerScript.Call(TableScript[functionName], args).Tuple;

        public override Dictionary<object, object> ToDictionary() => TableScript.Pairs.ToDictionary<TablePair, object, object>(pair => pair.Key, pair => RecursiveParse(pair.Value));
        private static object RecursiveParse(object value)
        {
            if (value is Table)
                return RecursiveParse(new MoonLuaTable((Table) value).ToDictionary());

            return value;
        }

        public override List<object> ToList() => TableScript.Values.Cast<object>().ToList();
        public override object[] ToArray() => TableScript.Values.Cast<object>().ToArray();
    }

    public class MoonLuaWrapperInstance : ILuaWrapper
    {
        public LuaScript CreateLuaScript(string luaScriptName = "") { return new MoonLua(luaScriptName); }

        public LuaTable CreateTable(LuaScript luaScript, string tableName) { return new MoonLuaTable(luaScript, tableName); }

        public LuaTable ToLuaTable(object obj)
        {
            var table = obj as Table;
            return table != null ? new MoonLuaTable(table) : null;
        }
    }
}
