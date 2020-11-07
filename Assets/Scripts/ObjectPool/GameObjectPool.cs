using System;
using System.Collections.Generic;
using UnityEngine;


public class GameObjectPool : MonoSingleton<GameObjectPool>
{
    private Dictionary<int, PrefabPool> mPoolDic;
    private Dictionary<GameObject, int> mGOTagDic = null;

    public void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        mPoolDic = new Dictionary<int, PrefabPool>();
        mGOTagDic = new Dictionary<GameObject, int>();
    }

    /// <summary>
    /// 初始化PrefabPool
    /// </summary>
    private PrefabPool GetPool(int tag)
    {
        if (mPoolDic.TryGetValue(tag, out PrefabPool pool))
        {
            return pool;
        }

        return null;
    }

    /// <summary>
    /// 初始化PrefabPool
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="preloadAmount">预加载个数</param>
    /// <param name="cullDespawned">自动释放</param>
    /// <param name="cullAbove">释放的上限</param>
    /// <param name="cullDelay">释放的延迟</param>
    /// <param name="cullMaxPerPass">单次释放最大个数</param>
    public PrefabPool CreatePool(GameObject prefab, int preloadAmount = 0, bool cullDespawned = false,
        int cullAbove = 10, int cullDelay = 30, int cullMaxPerPass = 5)
    {
        int tag = prefab.GetInstanceID();
        if (!this.mPoolDic.ContainsKey(tag))
        {
            PrefabPool pool =
                new PrefabPool(prefab, preloadAmount, cullDespawned, cullAbove, cullDelay, cullMaxPerPass);
            mPoolDic.Add(tag, pool);
        }

        return this.mPoolDic[tag];
    }

    /// <summary>
    /// 预加载对象
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="preloadAmount">预加载个数</param>
    public void PreloadGameObject(GameObject prefab, int preloadAmount = 1)
    {
        CreatePool(prefab, preloadAmount);
    }

    /// <summary>
    /// 从缓存池获取实例对象
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="parent">关联的父节点</param>
    /// <returns>实例</returns>
    public GameObject GetGameObject(GameObject prefab, Transform parent = null)
    {
        int tag = prefab.GetInstanceID();
        PrefabPool pool = GetPool(tag);

        if (pool == null)
        {
            pool = this.CreatePool(prefab);
        }

        GameObject go = pool.GetGameObject(parent);
        MarkAsOut(go, tag);
        return go;
    }

    /// <summary>
    /// 延迟回收
    /// </summary>
    /// <param name="go"></param>
    /// <param name="delay"></param>
    public async void RecycleGameObject(GameObject go, float delay)
    {
        await new WaitForSeconds(delay);
        
        RecycleGameObject(go);
    }


    /// <summary>
    /// 回收缓存池里的对象
    /// </summary>
    /// <param name="go">回收实例</param>
    public void RecycleGameObject(GameObject go)
    {
        if (go == null) return;

        int tag;
        if (!mGOTagDic.TryGetValue(go, out tag))
        {
            Debug.LogWarning("游戏对象不存在对象池：" + go.name);
            return;
        }

        RemoveOutMark(go);
        mPoolDic[tag].RecycleGameObject(go);
    }

    //标记gameObject
    private void MarkAsOut(GameObject go, int tag)
    {
        mGOTagDic.Add(go, tag);
    }

    //移除标记gameObject
    private void RemoveOutMark(GameObject go)
    {
        if (mGOTagDic.ContainsKey(go))
        {
            mGOTagDic.Remove(go);
        }
        else
        {
            Debug.LogError("Remove out mark erro, gameObject has not been marked");
        }
    }

    /// <summary>
    /// 释放单个预制体的对象池
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="stayNum">保留个数</param>
    public void Release(GameObject prefab)
    {
        int tag = prefab.GetInstanceID();

        if (!mPoolDic.ContainsKey(tag))
        {
            Debug.LogError("不存在对象池，无法释放：" + prefab.name);
            return;
        }

        var pool = mPoolDic[tag];
        pool.CleanUp();
        GC();
    }

    /// <summary>
    /// 释放所有对象池
    /// </summary>
    public void CleanUp()
    {
        foreach (var pair in mPoolDic)
        {
            var pool = pair.Value;
            pool.CleanUp();
        }

        mPoolDic.Clear();
        mGOTagDic.Clear();
        GC();
    }

    /// <summary>
    /// Garbage Collection
    /// </summary>
    public void GC()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

}
