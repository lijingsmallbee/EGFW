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

    public void Initialize()
    {

    }
}
