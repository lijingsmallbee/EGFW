using EGFW;
using System;
using System.Collections.Generic;
using UnityEngine;
//这个类是游戏的唯一全局变量，框架禁止使用单例
public class Game
{
    static Game _instance;
    public static Game Instance
    {
        get 
        {
            if (_instance == null)
            {
                _instance = new Game();                
            }
            return _instance; 
        }
    }
    private JsManager _jsManager;
    public JsManager JsManager=> _jsManager;
    private List<IManager> _managers = new List<IManager>();

    private void AddManager(Type type)
    {
        //if(_managers.FindAll())
    }

    public void Initialize()
    {
        _jsManager= new JsManager();
        _jsManager.Initialize();
    }

    public void Start()
    {
        _jsManager.Start();
    }

    public void Update()
    {

    }

    public void Destroy()
    {

    }

    public void LateUpdate()
    {

    }
}
