using EGFW;
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
                _instance.Initialize();
            }
            return _instance; 
        }
    }
    private JsManager _jsManager;
    public JsManager JsManager=> _jsManager;

    public void Initialize()
    {
        _jsManager= new JsManager();
        _jsManager.Initialize();
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
