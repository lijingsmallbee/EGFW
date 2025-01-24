using UnityEngine;

public class UnityEntry : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Game.Instance.Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        Game.Instance.Update();
    }

    private void LateUpdate()
    {
        Game.Instance.LateUpdate();
    }
}
