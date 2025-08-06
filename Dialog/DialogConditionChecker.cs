using UnityEngine;

/// <summary>
/// 對話條件檢查器 - 統一處理所有對話相關的條件檢查邏輯
/// </summary>
public static class DialogConditionChecker
{
    /// <summary>
    /// 檢查條件是否滿足
    /// </summary>
    /// <param name="condition">要檢查的條件</param>
    /// <returns>條件是否滿足</returns>
    public static bool CheckCondition(BaseDialogManager.DialogCondition condition)
    {
        if (condition == null || condition.type == null || condition.value == null)
        {
            return true; // 沒有條件視為滿足
        }
        
        try
        {
            switch (condition.type.ToUpper())
            {
                case "TAG_CHECK":
                    return CheckTagCondition(condition);
                    
                case "ITEM_OWNED":
                    return CheckItemCondition(condition);
                    
                default:
                    Debug.LogWarning($"未知的條件類型: {condition.type}");
                    return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"檢查條件時發生錯誤: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 檢查標籤條件
    /// </summary>
    /// <param name="condition">條件</param>
    /// <returns>是否滿足</returns>
    private static bool CheckTagCondition(BaseDialogManager.DialogCondition condition)
    {
        if (TagSystem.Instance == null)
        {
            Debug.LogWarning("TagSystem.Instance 為 null，無法檢查標籤條件");
            return false;
        }
        
        string tagId = condition.param;
        if (string.IsNullOrEmpty(tagId))
        {
            Debug.LogWarning("標籤條件缺少 tagId 參數");
            return false;
        }
        
        // 嘗試解析目標值
        if (!int.TryParse(condition.value, out int targetValue))
        {
            Debug.LogWarning($"無法解析標籤條件的目標值: {condition.value}");
            return false;
        }
        
        // 獲取標籤值
        int tagValue = TagSystem.Instance.GetTagValue(tagId);
        
        // 根據運算子比較
        return CompareValues(tagValue, targetValue, condition.@operator);
    }
    
    /// <summary>
    /// 檢查物品條件
    /// </summary>
    /// <param name="condition">條件</param>
    /// <returns>是否滿足</returns>
    private static bool CheckItemCondition(BaseDialogManager.DialogCondition condition)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance 為 null，無法檢查物品條件");
            return false;
        }
        
        string itemId = condition.param;
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("物品條件缺少 itemId 參數");
            return false;
        }
        
        // 嘗試解析目標數量
        if (!int.TryParse(condition.value, out int targetCount))
        {
            Debug.LogWarning($"無法解析物品條件的目標數量: {condition.value}");
            return false;
        }
        
        // 這裡需要根據itemId找到對應的Item物件
        // 暫時使用簡化的邏輯，實際實現可能需要物品數據庫
        // TODO: 實現根據itemId查找Item的邏輯
        Debug.LogWarning($"物品條件檢查尚未完全實現，itemId: {itemId}");
        return false;
    }
    
    /// <summary>
    /// 比較兩個值
    /// </summary>
    /// <param name="actualValue">實際值</param>
    /// <param name="targetValue">目標值</param>
    /// <param name="operator">運算子</param>
    /// <returns>比較結果</returns>
    private static bool CompareValues(int actualValue, int targetValue, string @operator)
    {
        switch (@operator?.ToUpper())
        {
            case "EQUAL":
            case "==":
                return actualValue == targetValue;
                
            case "GREATER_EQUAL":
            case ">=":
                return actualValue >= targetValue;
                
            case "LESS_THAN":
            case "<":
                return actualValue < targetValue;
                
            case "LESS_EQUAL":
            case "<=":
                return actualValue <= targetValue;
                
            case "GREATER_THAN":
            case ">":
                return actualValue > targetValue;
                
            case "NOT_EQUAL":
            case "!=":
                return actualValue != targetValue;
                
            default:
                Debug.LogWarning($"未知的比較運算子: {@operator}，默認使用等於比較");
                return actualValue == targetValue;
        }
    }
}