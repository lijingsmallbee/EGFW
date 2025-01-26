using NexgenDragon;
using UnityEngine;
using Puerts;
using Puerts.TSLoader;
using NUnit;

namespace EGFW
{
    //管理js脚本的类，跟js相关的功能
    public class JsManager : IManager
    {
        private JsEnv _env;

        public JsEnv JsEnv { get { return _env; } }
        public void Initialize()
        {
            _env = Puerts.WebGL.MainEnv.Get(new TSLoader());
        }

        public void Tick(float delta)
        {
            _env.Tick();
        }

        public void Release()
        {
            _env.Dispose();
        }

        public void Start()
        {

        }
    }
}

