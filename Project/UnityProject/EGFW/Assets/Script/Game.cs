using EGFW;
using UnityEngine;
//���������Ϸ��Ψһȫ�ֱ�������ܽ�ֹʹ�õ���
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
