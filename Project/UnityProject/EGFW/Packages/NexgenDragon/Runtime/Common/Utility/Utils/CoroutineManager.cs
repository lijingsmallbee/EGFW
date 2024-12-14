using UnityEngine;
using System.Collections;

namespace NexgenDragon
{
	public class CoroutineManager : Singleton<CoroutineManager>, IManager
    {
        public class Proxy : MonoBehaviour
        {
        }
    
        private GameObject _root;
        private Proxy _proxy;
        
		public void Initialize (NexgenObject configParam)
		{
            _root = new GameObject("CoroutineManager");
            Object.DontDestroyOnLoad(_root);
            _proxy = _root.AddComponent<Proxy>();
        }

		public void Reset ()
		{
			StopAll();
		}

		public override void Release ()
		{
			StopAll();

            if (_proxy)
            {
                _proxy.StopAllCoroutines();
                _proxy = null;
            }
            
            if (_root)
            {
                Object.Destroy(_root);
                _root = null;    
            }
        }

		public static Coroutine Start(IEnumerator coroutine)
		{
			return Instance.StartCoroutine(coroutine);
		}

		public static void Stop(Coroutine coroutine)
		{
            Instance.StopCoroutine(coroutine);
		}

        public static void StopAll()
		{
            Instance.StopAllCoroutines();
		}

        public void StopAllCoroutines()
        {
            if (_proxy)
            {
                _proxy.StopAllCoroutines();
            }
        }

        public void StopCoroutine(Coroutine coroutine)
        {
            if (_proxy)
            {
                _proxy.StopCoroutine(coroutine);
            }
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            if (_proxy)
            {
                return _proxy.StartCoroutine(routine);
            }

            return null;
        }

        public bool isActiveAndEnabled
        {
            get
            {
                if (_proxy)
                {
                    return _proxy.isActiveAndEnabled;
                }

                return false;
            }
        }
	}
}

