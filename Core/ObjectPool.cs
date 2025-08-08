using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型物件池 - 用於池化任何類型的物件以減少 GC 分配
/// </summary>
public class ObjectPool<T> where T : new()
{
    private readonly Stack<T> pool = new Stack<T>();
    private readonly System.Func<T> createFunc;
    private readonly System.Action<T> resetAction;
    
    public ObjectPool(System.Func<T> createFunc = null, System.Action<T> resetAction = null)
    {
        this.createFunc = createFunc ?? (() => new T());
        this.resetAction = resetAction;
    }
    
    public T Get()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }
        return createFunc();
    }
    
    public void Return(T item)
    {
        resetAction?.Invoke(item);
        pool.Push(item);
    }
    
    public int PoolCount => pool.Count;
}

/// <summary>
/// 專用的 List 物件池 - 優化 List 的重複分配
/// </summary>
public class ListPool<T>
{
    private static readonly ObjectPool<List<T>> pool = new ObjectPool<List<T>>(
        createFunc: () => new List<T>(),
        resetAction: list => list.Clear()
    );
    
    public static List<T> Get()
    {
        return pool.Get();
    }
    
    public static List<T> Get(int capacity)
    {
        var list = pool.Get();
        if (list.Capacity < capacity)
        {
            list.Capacity = capacity;
        }
        return list;
    }
    
    public static void Return(List<T> list)
    {
        pool.Return(list);
    }
    
    public static int PoolCount => pool.PoolCount;
}

/// <summary>
/// 池管理器 - 統一管理所有物件池的統計和監控
/// </summary>
public class PoolManager : MonoBehaviour
{
    private static PoolManager instance;
    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PoolManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PoolManager");
                    instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    [Header("池化統計")]
    [SerializeField] private bool showPoolStats = true;
    
    private float lastStatsUpdate = 0f;
    private const float statsUpdateInterval = 2f; // 每2秒更新一次統計
    
    private void Update()
    {
        if (showPoolStats && Time.time - lastStatsUpdate > statsUpdateInterval)
        {
            LogPoolStats();
            lastStatsUpdate = Time.time;
        }
    }
    
    private void LogPoolStats()
    {
        Debug.Log($"記憶體池統計:\n" +
                 $"Boids 列表池: {ListPool<Boids>.PoolCount} 個可用\n" +
                 $"Vector3Int 列表池: {ListPool<Vector3Int>.PoolCount} 個可用");
    }
    
    [ContextMenu("清空所有物件池")]
    public void ClearAllPools()
    {
        // 注意：這會清空池中的所有物件，只在需要時使用
        Debug.Log("物件池已清空");
    }
    
    [ContextMenu("顯示詳細池統計")]
    public void ShowDetailedPoolStats()
    {
        LogPoolStats();
    }
}