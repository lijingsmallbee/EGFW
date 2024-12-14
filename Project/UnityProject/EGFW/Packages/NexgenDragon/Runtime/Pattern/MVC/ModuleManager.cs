using System.Collections.Generic;
using System;

namespace NexgenDragon
{
    public class ModuleManager : Singleton<ModuleManager>, IManager
    {
        private readonly Dictionary<Type, IModule> _moduleDict = new Dictionary<Type, IModule>();
        private readonly Dictionary<string, Type> _nameDict = new Dictionary<string, Type>();

        
        private readonly Dictionary<string,IModule> _luaModuleDic = new Dictionary<string, IModule>();
        private readonly Dictionary<string,bool> _luaModuleIsRegister = new Dictionary<string, bool>();
        readonly HashSet<string> _onCallInitCallbackLuaModules = new HashSet<string>();
        readonly Dictionary<string,string> _onSceneLoadedLuaModules = new Dictionary<string,string>();
        readonly Dictionary<string,string>  _onLoggedInLuaModules = new Dictionary<string,string> ();
        
        public void Initialize(NexgenObject configParam)
        {

        }

        public void Reset()
        {
            Release();
        }

        public override void Release()
        {
            var allModuleTypes = new List<Type>(_moduleDict.Keys);
            foreach (var type in allModuleTypes)
            {
                try
                {
                    RemoveModule(type);
                }
                catch (Exception e)
                {
                    NLogger.Error(type + "Removed with Error: " + e.Message);
                }
                
            }

            var allLuaModuleTypes = new List<string>(_luaModuleDic.Keys);
            foreach (var type in allLuaModuleTypes)
            {
                try
                {
                    RemoveModule(type);
                }
                catch (Exception e)
                {
                    NLogger.Error(type + " Removed with Error: " + e);
                }
            }
            _luaModuleDic.Clear();
            _onLoggedInLuaModules.Clear();
            _onSceneLoadedLuaModules.Clear();
            _luaModuleIsRegister.Clear();
            _moduleDict.Clear();
            _nameDict.Clear();
        }

        public T RegisterModule<T>() where T : IModule, new()
        {
            var type = typeof(T);
            return (T)RegisterModule(type);
        }

        public IModule RegisterModule(Type type)
        {
            IModule module;
            if (!_moduleDict.TryGetValue(type, out module))
            {
                module = Activator.CreateInstance(type) as IModule;
                if (module != null)
                {
                    _nameDict[type.ToString()] = type;
                    _moduleDict[type] = module;
                    module.OnRegister();
                }
            }

            return module;
        }

        public IModule RemoveModule(Type type)
        {
            IModule module;
            if (_moduleDict.TryGetValue(type, out module))
            {
                module.OnRemove();
                _moduleDict.Remove(type);
                _nameDict.Remove(type.ToString());
            }

            return module;
        }

        public IModule RemoveModule(string name)
        {
            IModule mod = null;
            if (_luaModuleDic.TryGetValue(name,out mod))
            {
                // 如果一个LuaModule没有调过OnRegister，就不应该调OnRemove
                if (_luaModuleIsRegister.TryGetValue(name, out bool flag) && flag)
                {
                    mod.OnRemove();
                }
                _luaModuleDic.Remove(name);
                return mod;
            }

            if (_onLoggedInLuaModules.TryGetValue(name,out string loggedInMod))
            {
                _onLoggedInLuaModules.Remove(loggedInMod);
            }
            
            if (_onSceneLoadedLuaModules.TryGetValue(name,out string sceneLoadedMod))
            {
                _onSceneLoadedLuaModules.Remove(sceneLoadedMod);
            }

            Type type;
            if (_nameDict.TryGetValue(name, out type))
            {
                return RemoveModule(type);
            }

            return null;
        }

        public T RetrieveModule<T>() where T : IModule, new()
        {
            var type = typeof(T);

            IModule module;
            if (_moduleDict.TryGetValue(type, out module))
            {
                return (T)module;
            }

            return (T)RegisterModule(type);
        }

        public void RegisterModule(IModule module)
        {
            if(module == null)
            {
                return;
            }

            var type = module.GetType();
            if(!_moduleDict.ContainsKey(type))
            {
                _nameDict[type.ToString()] = type;
                _moduleDict[type] = module;
                module.OnRegister();
            }
        }
        
        public void RegisterLuaModule(IModule module)
        {
            if(module == null)
            {
                return;
            }

            var type = module.GetName();
            if(!_luaModuleDic.ContainsKey(type))
            {                
                _luaModuleDic[type] = module;
                _luaModuleIsRegister[type] = false;
                //module.OnRegister();
            }
        }

        public IModule RetrieveModule(Type type)
        {
            IModule module;
            if(_moduleDict.TryGetValue(type, out module))
            {
                return module;
            }

            return RegisterModule(type);
        }

        public IModule RetrieveModule(string name)
        {
            Perf.BeginSample("VinayGao:ModuleManager.RetrieveModule");
            IModule mod;
            if (_luaModuleDic.TryGetValue(name, out mod))
            {
                if (!_luaModuleIsRegister[name])
                {
                    _luaModuleIsRegister[name] = true;
                    mod.OnRegister();
                }

                Perf.EndSample();
                return mod;
            }
            Type type;
            if(_nameDict.TryGetValue(name, out type))
            {
                var module = RetrieveModule(type);
                Perf.EndSample();
                return module;
            }
            Perf.EndSample();
            return null;
        }

        public void AddOnCallInitCallbackLuaModule(string name)
        {
            _onCallInitCallbackLuaModules.Add(name);
        }

        public void AddOnSceneLoadedLuaModule(string name)
        {
            if (!_onSceneLoadedLuaModules.ContainsKey(name))
            {
                _onSceneLoadedLuaModules.Add(name,name);
            }
        }
        
        public void AddOnLoggedInLuaModule(string name)
        {
            if (!_onLoggedInLuaModules.ContainsKey(name))
            {
                _onLoggedInLuaModules.Add(name,name);
            }
        }

        public void RetrieveLuaModuleOnSceneLoaded()
        {
            foreach (var mod in _onSceneLoadedLuaModules)
            {
                RetrieveModule(mod.Key);
            }
        }
        public void RetrieveLuaModuleOnLoggedIn()
        {
            foreach (var mod in _onLoggedInLuaModules)
            {
                RetrieveModule(mod.Key);
            }
        }
    }
}