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

    public void Initialize()
    {

    }
}
