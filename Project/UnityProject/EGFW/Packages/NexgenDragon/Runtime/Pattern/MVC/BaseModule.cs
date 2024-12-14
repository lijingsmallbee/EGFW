namespace NexgenDragon
{
    public abstract class BaseModule : NexgenObject, IModule
    {
        private object _luaModule = null;

        //注意，这个字段，在c#实现的module里，一定要设置为true
        protected bool _luaInited = false;
        // 当模块注册到DatabaseManager的时候触发
        public virtual void OnRegister ()
        {
            
        }

		// 当模块从DatabaseManager移除的时候触发
        public virtual void OnRemove ()
        {
            
        }

        public override void Release ()
        {

        }

        public virtual string GetName()
        {
            return this.GetType().ToString();
        }

        public void SetLuaModule(object lua)
        {
            _luaModule = lua;
            _luaInited = true;
        }

        public object Lua
        {
            get { return _luaModule; }
        }

        public bool LuaInit => _luaInited;
    }
}   