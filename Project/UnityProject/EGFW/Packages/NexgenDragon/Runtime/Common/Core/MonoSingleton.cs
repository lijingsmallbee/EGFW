﻿using UnityEngine;
using System.Collections;

namespace NexgenDragon
{
	public class MonoSingleton<T> : NexgenBehaviour where T : MonoBehaviour
	{
		private static T _instance = null;

		public static T Instance 
		{
			get
			{
				if( _instance == null) {
					GameObject go = new GameObject(typeof(T).ToString());

					_instance = go.AddComponent<T>();
				}

				return _instance;
			}
		}

		protected virtual void Awake()
		{
			DontDestroyOnLoad(gameObject);

			_instance = this as T;
		}

		protected virtual void OnDestory()
		{
			_instance = null;
		}

        public override void Release ()
        {
            
        }

		public static void DestroyInstance()
		{
			_instance = null;
		}
	}
}