using UnityEngine;

/// <summary>
/// 統一的遊戲條件檢查器 - 處理所有遊戲相關的條件檢查邏輯
/// 用於對話系統、事件觸發器等各種需要條件檢查的系統
/// </summary>
public static class ConditionChecker
{
    /// <summary>
    /// 檢查條件是否滿足
    /// </summary>
    /// <param name="condition">要檢查的條件</param>
    /// <returns>條件是否滿足</returns>
    public static bool CheckCondition(GameCondition condition)
    {
        // 如果條件為 null，視為無條件限制
        if (condition == null)
        {
            Debug.Log("[ConditionChecker] 條件為 null，返回 true（無條件限制）");
            return true; 
        }
        
        // 如果條件類型為空或無效，視為無條件限制
        if (string.IsNullOrEmpty(condition.type))
        {
            Debug.Log("[ConditionChecker] 條件類型為空，返回 true（無條件限制）");
            return true;
        }
        
        // 如果條件值為空，視為無條件限制
        if (string.IsNullOrEmpty(condition.value))
        {
            Debug.Log("[ConditionChecker] 條件值為空，返回 true（無條件限制）");
            return true;
        }
        
        Debug.Log($"[ConditionChecker] 開始檢查條件 - 類型: {condition.type}, 參數: {condition.param}, 值: {condition.value}, 運算子: {condition.@operator}");
        
        try
        {
            switch (condition.type.ToUpper())
            {
                case "TAG_CHECK":
                    return CheckTagCondition(condition);
                    
                case "ITEM_OWNED":
                    return CheckItemCondition(condition);
                    
                default:
                    Debug.LogWarning($"[ConditionChecker] 未知的條件類型: {condition.type}，返回 false");
                    return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ConditionChecker] 檢查條件時發生錯誤: {e.Message}，返回 false");
            return false;
        }
    }
    
    /// <summary>
    /// 檢查標籤條件
    /// </summary>
    /// <param name="condition">條件</param>
    /// <returns>是否滿足</returns>
    private static bool CheckTagCondition(GameCondition condition)
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
    private static bool CheckItemCondition(GameCondition condition)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance 為 null，無法檢查物品條件");
            return false;
        }

        if (ItemDatabase.Instance == null)
        {
            Debug.LogWarning("ItemDatabase.Instance 為 null，無法檢查物品條件");
            return false;
        }

        string itemIdStr = condition.param;
        if (string.IsNullOrEmpty(itemIdStr))
        {
            Debug.LogWarning("物品條件缺少 itemId 參數");
            return false;
        }

        // 嘗試解析物品ID
        if (!int.TryParse(itemIdStr, out int itemId))
        {
            Debug.LogWarning($"無法解析物品ID: {itemIdStr}");
            return false;
        }

        // 嘗試解析目標數量
        if (!int.TryParse(condition.value, out int targetCount))
        {
            Debug.LogWarning($"無法解析物品條件的目標數量: {condition.value}");
            return false;
        }

        // 通過 ItemDatabase 查找物品
        Item item = ItemDatabase.Instance.GetItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"找不到ID為 {itemId} 的物品");
            return false;
        }

        // 獲取玩家擁有的物品數量
        int currentCount = InventoryManager.Instance.GetItemCount(item);

        Debug.Log($"[ConditionChecker] 物品條件檢查: {item.Name} - 目前有 {currentCount}，需要 {targetCount}，運算子: {condition.@operator}");

        // 根據運算子比較
        return CompareValues(currentCount, targetCount, condition.@operator);
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