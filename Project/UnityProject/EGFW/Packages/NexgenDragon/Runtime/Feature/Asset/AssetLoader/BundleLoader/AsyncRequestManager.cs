using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NexgenDragon;
using UnityEngine.Profiling;

public abstract class AsyncRequestBase
{
    protected bool _needRemove;
    public abstract bool CheckComplete ();
    public bool NeedRemove
    {
        get{ return _needRemove; }
    }
}
internal class AsyncRequest<T>:AsyncRequestBase where T: AsyncOperation
{
    private T request;
    private Action<T,object> completeHandler;
    private object userData;
    public static AsyncRequest<T> Create(T Req,Action<T,object> handler,object userData)
    {
        AsyncRequest<T> inst = new AsyncRequest<T>();
        inst.request = Req;
        inst.completeHandler = handler;
        inst.userData = userData;
        return inst;
    }
    //外部无法构造这个类，需要用create
    private AsyncRequest()
    {
    }

    public override bool CheckComplete()
    {
        if (request.isDone)
        {
            _needRemove = true;
            if (completeHandler != null)
            {
                completeHandler(request,userData);
            }
            return true;
        }
        return false;
    }
}
internal class AsyncRequestManager:ITicker
{
    
    static private AsyncRequestManager _Instance;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReLoadInit()
    {
        _Instance = null;
    }
#endif
    static public AsyncRequestManager Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new AsyncRequestManager();
                _Instance.Init();
            }
            return _Instance;
        }
    }
    private List<AsyncRequestBase> _allTask = new List<AsyncRequestBase>(32);
    private List<AsyncRequestBase> _addTask = new List<AsyncRequestBase>(32);
    private bool _inTicking = false;
    private string vinayGaoClassType = null;
    public string VinayGaoGetClassType
    {
        get
        {
            if (vinayGaoClassType == null)
            {
                vinayGaoClassType ="VinayGao:" + GetType().Name;
            }

            return vinayGaoClassType;
        }
    }
    void Init()
    {
        GameFacade.Instance.AddTicker(this);
    }

    public void AddTask(AsyncRequestBase req)
    {
        //遍历中加入的任务，下一帧再处理
        if (_inTicking == true)
        {
            _addTask.Add(req);
        }
        else
        {
            _allTask.Add(req);
        }

    }

    public void Tick(float delta)
    {
        Perf.BeginSample("VinayGao:AsyncRequestManager.Tick");
        var taskIt = _allTask.GetEnumerator();
        _inTicking = true;
        while (taskIt.MoveNext())
        {
            taskIt.Current.CheckComplete();
        }
        _allTask.RemoveAll(x=>x.NeedRemove);
        _inTicking = false;
        _allTask.AddRange(_addTask);
        _addTask.Clear();
        Perf.EndSample();
    }

    public void Clear()
    {
        _allTask.Clear();
        _addTask.Clear();
    }
}
