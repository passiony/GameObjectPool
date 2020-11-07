using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabPool
{
    private GameObjectPool _spawnPool;

    private GameObjectPool spawnPool
    {
        get
        {
            if (this._spawnPool == null)
            {
                this._spawnPool = GameObjectPool.Instance;
            }

            return this._spawnPool;
        }
    }

    private GameObject prefab;

    public int preloadAmount = 1;

    public bool cullDespawned = false;

    public int cullAbove = 10;
    public int cullDelay = 30;
    public int cullMaxPerPass = 5;

    public bool _logMessages = true;


    private Queue<GameObject> _spawned = new Queue<GameObject>();

    private bool cullingActive;

    public int totalCount
    {
        get
        {
            return _spawned.Count;
            ;
        }
    }

    public PrefabPool(GameObject prefab)
    {
        this.prefab = prefab;
    }

    public PrefabPool(GameObject prefab, int preloadAmount = 0, bool cullDespawned = false, int cullAbove = 10,
        int cullDelay = 30, int cullMaxPerPass = 5)
    {
        this.prefab = prefab;
        this.preloadAmount = preloadAmount;
        this.cullDespawned = cullDespawned;
        this.cullAbove = cullAbove;
        this.cullDelay = cullDelay;
        this.cullMaxPerPass = cullMaxPerPass;

        for (int i = 0; i < this.preloadAmount; i++)
        {
            GameObject go = UnityEngine.Object.Instantiate(prefab);
            go.name = prefab.name;
            RecycleGameObject(go);
        }
    }

    private GameObject PopGo()
    {
        if (_spawned.Count > 0)
        {
            GameObject obj = _spawned.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return null;
    }

    public GameObject GetGameObject(Transform parent = null)
    {
        GameObject go = PopGo();
        if (go == null)
        {
            go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;
        }
        else
        {
            go.transform.SetParent(parent, false);
        }

        go.SetActive(true);
        return go;
    }

    /// <summary>
    /// 回收缓存池里的对象
    /// </summary>
    /// <param name="go"></param>
    public void RecycleGameObject(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(spawnPool.transform, false);
        _spawned.Enqueue(go);

        if (!this.cullingActive && // Cheap & Singleton. Only trigger once!
            this.cullDespawned && // Is the feature even on? Cheap too.
            this.totalCount > this.cullAbove) // Criteria met?
        {
            this.cullingActive = true;
            CullDespawned();
        }
    }

    private async void CullDespawned()
    {
        // First time always pause, then check to see if the condition is
        //   still true before attempting to cull.
        await new WaitForSeconds(cullDelay);
        while (this.totalCount > this.cullAbove)
        {
            int tempCount = this.totalCount;

            // Attempt to delete an amount == this.cullMaxPerPass
            for (int i = 0; i < this.cullMaxPerPass; i++)
            {
                // Break if this.cullMaxPerPass would go past this.cullAbove
                if (this.totalCount <= this.cullAbove)
                    break; // The while loop will stop as well independently

                // Destroy the last item in the list
                if (this._spawned.Count > 0)
                {
                    GameObject inst = this._spawned.Dequeue();
                    UnityEngine.Object.Destroy(inst.gameObject);
                }
                else if (this._logMessages)
                {
                    Debug.Log(string.Format("SpawnPool {0} :CULLING waiting for despawn. Checking again in {1} sec",
                        this.prefab.name, this.cullDelay));
                    break;
                }
            }

            if (this._logMessages)
                Debug.Log(string.Format("SpawnPool {0} : CULLING to {1} instances. Now from {2} to {3}.",
                    this.prefab.name, this.cullAbove,
                    tempCount, totalCount));

            // Check again later
            await new WaitForSeconds(cullDelay);
        }

        if (this._logMessages)
            Debug.Log(string.Format("SpawnPool {0} : CULLING FINISHED! Stopping", this.prefab.name));

        // Reset the singleton so the feature can be used again if needed.
        this.cullingActive = false;
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void CleanUp()
    {
        while (this._spawned.Count > 0)
        {
            var inst = _spawned.Dequeue();
            UnityEngine.Object.Destroy(inst);
        }
    }
}
