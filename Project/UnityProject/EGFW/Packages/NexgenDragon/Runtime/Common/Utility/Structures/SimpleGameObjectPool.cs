using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
	public class SimpleGameObjectPool
	{
		GameObject m_Prototype;
		GameObject m_Root;
		Stack<GameObject> m_Pool = new Stack<GameObject>();

		public GameObject Prototype
		{
			get { return m_Prototype; }
		}

		// initialize
		public void Initialize(GameObject prototype, GameObject root)
		{
			m_Prototype = prototype;
			m_Root = root;	
		}

		private int m_nLimit = -1;
		public void InitLimitSize(int limit)
		{
			if (limit > 0)
			{
				m_nLimit = limit;
			}
		}
		
		// allocate an object
		public GameObject Allocate()
		{
			if (m_Pool.Count > 0)
			{
				GameObject go = m_Pool.Pop();
				go.SetActive(true);
				go.transform.parent = null;
				return go;
			}

			if(m_Prototype != null)
			{
				return GameObject.Instantiate(m_Prototype) as GameObject;
			}

			NLogger.Error("Prototype is null.");

			return null;
		}

		// release an object
		public void Release(GameObject go)
		{
			if (go != null)
			{
				if (m_nLimit > 0 && PoolSize > m_nLimit)
				{
					if (Application.isPlaying)
					{
						GameObject.Destroy(go);
					}
					else
					{
						GameObject.DestroyImmediate(go);
					}
					return;
				}
				
				go.transform.parent = m_Root.transform;
				go.SetActive(false);
				m_Pool.Push(go);
			}
		}

		public void Clear()
		{
			while (m_Pool.Count > 0)
			{
				GameObject go = m_Pool.Pop ();
				go.transform.parent = null;
				if (Application.isPlaying)
				{
					GameObject.Destroy(go);
				}
				else
				{
					GameObject.DestroyImmediate(go);
				}
			}
		}


		public int PoolSize => m_Pool.Count;
		
		
		// add child
		public GameObject AddChild(GameObject parent, bool resetlayer = true)
		{
			GameObject go = Allocate();

			if (go != null && parent != null)
			{
				Transform t = go.transform;
				t.parent = parent.transform;
				if (resetlayer)
				{
					t.localPosition = Vector3.zero;
					t.localRotation = Quaternion.identity;
					t.localScale = Vector3.one;
					go.layer = parent.layer;
				}
			}
			return go;
		}
	}
}