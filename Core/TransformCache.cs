using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Transform 數據快取系統 - 批量緩存 Transform 數據以減少組件訪問開銷
/// </summary>
public class TransformCache
{
    public struct CachedTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 forward;
        public bool isDirty;
        
        public CachedTransform(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
            forward = transform.forward;
            isDirty = false;
        }
        
        public void UpdateFrom(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
            forward = transform.forward;
            isDirty = false;
        }
    }
    
    private Dictionary<Transform, CachedTransform> cache = new Dictionary<Transform, CachedTransform>();
    private List<Transform> dirtyTransforms = new List<Transform>();
    
    /// <summary>
    /// 註冊需要緩存的 Transform
    /// </summary>
    public void Register(Transform transform)
    {
        if (!cache.ContainsKey(transform))
        {
            cache[transform] = new CachedTransform(transform);
        }
    }
    
    /// <summary>
    /// 取消註冊 Transform
    /// </summary>
    public void Unregister(Transform transform)
    {
        cache.Remove(transform);
    }
    
    /// <summary>
    /// 獲取緩存的位置
    /// </summary>
    public Vector3 GetPosition(Transform transform)
    {
        if (cache.TryGetValue(transform, out CachedTransform cached))
        {
            return cached.position;
        }
        
        // Fallback - 直接獲取並緩存
        Register(transform);
        return transform.position;
    }
    
    /// <summary>
    /// 獲取緩存的旋轉
    /// </summary>
    public Quaternion GetRotation(Transform transform)
    {
        if (cache.TryGetValue(transform, out CachedTransform cached))
        {
            return cached.rotation;
        }
        
        Register(transform);
        return transform.rotation;
    }
    
    /// <summary>
    /// 獲取緩存的前方向
    /// </summary>
    public Vector3 GetForward(Transform transform)
    {
        if (cache.TryGetValue(transform, out CachedTransform cached))
        {
            return cached.forward;
        }
        
        Register(transform);
        return transform.forward;
    }
    
    /// <summary>
    /// 標記 Transform 為髒數據（需要更新）
    /// </summary>
    public void MarkDirty(Transform transform)
    {
        if (transform != null && cache.ContainsKey(transform) && !dirtyTransforms.Contains(transform))
        {
            dirtyTransforms.Add(transform);
        }
    }
    
    /// <summary>
    /// 批量更新所有髒數據的緩存
    /// </summary>
    public void UpdateDirtyCache()
    {
        // 使用反向遍歷以安全移除無效的 Transform
        for (int i = dirtyTransforms.Count - 1; i >= 0; i--)
        {
            Transform transform = dirtyTransforms[i];
            
            if (transform == null)
            {
                // 移除已銷毀的 Transform
                dirtyTransforms.RemoveAt(i);
                continue;
            }
            
            if (cache.ContainsKey(transform))
            {
                CachedTransform cached = cache[transform];
                cached.UpdateFrom(transform);
                cache[transform] = cached;
            }
        }
        dirtyTransforms.Clear();
    }
    
    /// <summary>
    /// 強制更新所有緩存
    /// </summary>
    public void UpdateAllCache()
    {
        var keys = new List<Transform>(cache.Keys);
        foreach (Transform transform in keys)
        {
            if (transform != null)
            {
                CachedTransform cached = cache[transform];
                cached.UpdateFrom(transform);
                cache[transform] = cached;
            }
        }
    }
    
    /// <summary>
    /// 清理無效的 Transform 引用
    /// </summary>
    public void CleanupNullReferences()
    {
        var keysToRemove = new List<Transform>();
        foreach (var kvp in cache)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (Transform key in keysToRemove)
        {
            cache.Remove(key);
        }
    }
    
    /// <summary>
    /// 獲取緩存統計資訊
    /// </summary>
    public string GetStats()
    {
        return $"Transform緩存: {cache.Count} 個註冊, {dirtyTransforms.Count} 個待更新";
    }
    
    /// <summary>
    /// 清空所有緩存
    /// </summary>
    public void Clear()
    {
        cache.Clear();
        dirtyTransforms.Clear();
    }
}