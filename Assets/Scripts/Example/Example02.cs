using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example02 : MonoBehaviour
{
    public GameObject prefab;

    void Start()
    {
        GameObjectPool.Instance.CreatePool(prefab, 1, true, 10, 10, 5);
    }
    private void OnGUI()
    {
        if(GUI.Button(new Rect(100, 100, 400, 50), "创建+延迟回收+10s自动释放"))
        {
            var go = GameObjectPool.Instance.GetGameObject(prefab);
            
            GameObjectPool.Instance.RecycleGameObject(go,5);
        }
    }
}
