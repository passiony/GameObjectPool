using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example01 : MonoBehaviour
{
    public GameObject prefab;

    private void OnGUI()
    {
        if(GUI.Button(new Rect(100, 100, 200, 50), "创建+延迟回收"))
        {
            var go = GameObjectPool.Instance.GetGameObject(prefab);
            
            GameObjectPool.Instance.RecycleGameObject(go,2);
        }
    }
}
