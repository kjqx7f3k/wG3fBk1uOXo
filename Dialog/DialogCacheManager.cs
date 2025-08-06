using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 對話緩存管理器 - 統一管理對話數據和DialogLine的緩存
/// 合併原本的dialogCache和dialogDataCache功能
/// </summary>
public static class DialogCacheManager
{
    private static Dictionary<string, CachedDialogData> dialogCache = new Dictionary<string, CachedDialogData>();
    
    /// <summary>
    /// 緩存的對話數據結構
    /// 注意：只緩存原始DialogData，不緩存處理後的DialogLine，因為DialogLine包含條件檢查結果會過時
    /// </summary>
    private class CachedDialogData
    {
        public DialogManager.DialogData dialogData;
        
        public CachedDialogData(DialogManager.DialogData dialogData)
        {
            this.dialogData = dialogData;
        }
    }
    
    /// <summary>
    /// 檢查對話文件是否已緩存
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>是否已緩存</returns>
    public static bool IsDialogCached(string fileName)
    {
        return dialogCache.ContainsKey(fileName);
    }
    
    /// <summary>
    /// 獲取緩存的對話數據
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>緩存的對話數據，如果不存在則返回null</returns>
    public static DialogManager.DialogData GetCachedDialogData(string fileName)
    {
        if (dialogCache.TryGetValue(fileName, out CachedDialogData cachedData))
        {
            return cachedData.dialogData;
        }
        return null;
    }
    
    
    /// <summary>
    /// 緩存對話數據
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="dialogData">對話數據</param>
    public static void CacheDialog(string fileName, DialogManager.DialogData dialogData)
    {
        dialogCache[fileName] = new CachedDialogData(dialogData);
        Debug.Log($"已緩存對話數據: {fileName}");
    }
    
    /// <summary>
    /// 清除特定文件的緩存
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>是否成功清除</returns>
    public static bool ClearDialogCache(string fileName)
    {
        bool removed = dialogCache.Remove(fileName);
        if (removed)
        {
            Debug.Log($"已清除對話緩存: {fileName}");
        }
        return removed;
    }
    
    /// <summary>
    /// 清除所有對話緩存
    /// </summary>
    public static void ClearAllDialogCache()
    {
        int cacheCount = dialogCache.Count;
        dialogCache.Clear();
        Debug.Log($"清除所有對話緩存，共清除 {cacheCount} 個文件");
    }
    
    /// <summary>
    /// 獲取緩存統計信息
    /// </summary>
    /// <returns>緩存統計信息字符串</returns>
    public static string GetCacheInfo()
    {
        int totalFiles = dialogCache.Count;
        int totalDialogs = 0;
        
        foreach (var cachedData in dialogCache.Values)
        {
            if (cachedData.dialogData?.dialogs != null)
            {
                totalDialogs += cachedData.dialogData.dialogs.Length;
            }
        }
        
        return $"對話緩存統計: {totalFiles} 個文件, 共 {totalDialogs} 個對話條目";
    }
    
    /// <summary>
    /// 獲取所有緩存的文件名
    /// </summary>
    /// <returns>緩存文件名列表</returns>
    public static List<string> GetCachedFileNames()
    {
        return new List<string>(dialogCache.Keys);
    }
}