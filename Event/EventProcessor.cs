using UnityEngine;

/// <summary>
/// 統一的遊戲事件處理器
/// 用於處理所有遊戲系統的事件（對話、觸發器等）
/// </summary>
public static class EventProcessor
{
    /// <summary>
    /// 處理遊戲事件數組
    /// </summary>
    /// <param name="events">要處理的事件數組</param>
    public static void ProcessEvents(GameEvent[] events)
    {
        if (events == null || events.Length == 0)
        {
            return;
        }

        Debug.Log($"處理 {events.Length} 個遊戲事件");

        foreach (GameEvent gameEvent in events)
        {
            if (ShouldExecuteEvent(gameEvent))
            {
                ExecuteEvent(gameEvent);
            }
        }
    }

    /// <summary>
    /// 檢查事件是否應該執行
    /// </summary>
    /// <param name="gameEvent">要檢查的事件</param>
    /// <returns>是否應該執行</returns>
    private static bool ShouldExecuteEvent(GameEvent gameEvent)
    {
        // 如果事件設定不使用條件檢查，直接返回 true
        if (!gameEvent.useCondition)
        {
            Debug.Log($"[EventProcessor] 事件 {gameEvent.event_type} 不使用條件檢查，直接執行");
            return true;
        }
        
        // 如果設定使用條件檢查，但條件為 null，則返回 true（向後兼容）
        if (gameEvent.condition == null)
        {
            Debug.Log($"[EventProcessor] 事件 {gameEvent.event_type} 啟用了條件檢查但 condition 為 null，視為無條件執行");
            return true;
        }

        Debug.Log($"[EventProcessor] 事件 {gameEvent.event_type} 使用條件檢查，條件類型: {gameEvent.condition.type}");
        bool result = ConditionChecker.CheckCondition(gameEvent.condition);
        Debug.Log($"[EventProcessor] 條件檢查結果: {result}");
        
        return result;
    }

    /// <summary>
    /// 執行單個事件
    /// </summary>
    /// <param name="gameEvent">要執行的事件</param>
    private static void ExecuteEvent(GameEvent gameEvent)
    {
        if (string.IsNullOrEmpty(gameEvent.event_type))
        {
            Debug.LogWarning("事件類型為空，跳過執行");
            return;
        }

        Debug.Log($"執行事件: {gameEvent.event_type}");

        switch (gameEvent.event_type.ToLower())
        {
            case "update_tag":
                ExecuteUpdateTagEvent(gameEvent);
                break;
                
            case "give_item":
                ExecuteGiveItemEvent(gameEvent);
                break;
                
            case "take_item":
                ExecuteTakeItemEvent(gameEvent);
                break;
                
            case "play_narration":
                ExecutePlayNarrationEvent(gameEvent);
                break;
                
            case "play_audio":
                ExecutePlayAudioEvent(gameEvent);
                break;
                
            case "load_scene":
                ExecuteLoadSceneEvent(gameEvent);
                break;
                
            default:
                Debug.LogWarning($"未知的事件類型: {gameEvent.event_type}");
                break;
        }
    }

    /// <summary>
    /// 執行更新標籤事件
    /// </summary>
    /// <param name="gameEvent">事件數據</param>
    private static void ExecuteUpdateTagEvent(GameEvent gameEvent)
    {
        if (TagSystem.Instance == null)
        {
            Debug.LogWarning("TagSystem.Instance 為 null，無法執行更新標籤事件");
            return;
        }

        string tagId = gameEvent.param1;
        if (string.IsNullOrEmpty(tagId))
        {
            Debug.LogWarning("更新標籤事件缺少 tagId 參數");
            return;
        }

        if (!int.TryParse(gameEvent.param2, out int tagValue))
        {
            Debug.LogWarning($"無法解析標籤值: {gameEvent.param2}");
            return;
        }

        TagSystem.Instance.SetTag(tagId, tagValue);
        Debug.Log($"更新標籤: {tagId} = {tagValue}");
    }

    /// <summary>
    /// 執行給予物品事件
    /// </summary>
    /// <param name="gameEvent">事件數據</param>
    private static void ExecuteGiveItemEvent(GameEvent gameEvent)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance 為 null，無法執行給予物品事件");
            return;
        }

        if (ItemDatabase.Instance == null)
        {
            Debug.LogWarning("ItemDatabase.Instance 為 null，無法執行給予物品事件");
            return;
        }

        string itemIdStr = gameEvent.param1;
        if (string.IsNullOrEmpty(itemIdStr))
        {
            Debug.LogWarning("給予物品事件缺少 itemId 參數");
            return;
        }

        if (!int.TryParse(itemIdStr, out int itemId))
        {
            Debug.LogWarning($"無法解析物品ID: {itemIdStr}");
            return;
        }

        if (!int.TryParse(gameEvent.param2, out int itemCount))
        {
            Debug.LogWarning($"無法解析物品數量: {gameEvent.param2}");
            return;
        }

        if (itemCount <= 0)
        {
            Debug.LogWarning($"物品數量必須大於0: {itemCount}");
            return;
        }

        // 通過 ItemDatabase 查找物品
        Item item = ItemDatabase.Instance.GetItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"找不到ID為 {itemId} 的物品");
            return;
        }

        // 給予物品到玩家背包
        int actualAdded = InventoryManager.Instance.AddItem(item, itemCount);
        
        if (actualAdded == itemCount)
        {
            Debug.Log($"成功給予物品: {item.Name} x{itemCount}");
        }
        else if (actualAdded > 0)
        {
            Debug.LogWarning($"部分給予物品: {item.Name} - 請求 {itemCount}，實際給予 {actualAdded} (背包可能已滿)");
        }
        else
        {
            Debug.LogWarning($"無法給予物品: {item.Name} x{itemCount} (背包可能已滿)");
        }
    }

    /// <summary>
    /// 執行拿取物品事件
    /// </summary>
    /// <param name="gameEvent">事件數據</param>
    private static void ExecuteTakeItemEvent(GameEvent gameEvent)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance 為 null，無法執行拿取物品事件");
            return;
        }

        if (ItemDatabase.Instance == null)
        {
            Debug.LogWarning("ItemDatabase.Instance 為 null，無法執行拿取物品事件");
            return;
        }

        string itemIdStr = gameEvent.param1;
        if (string.IsNullOrEmpty(itemIdStr))
        {
            Debug.LogWarning("拿取物品事件缺少 itemId 參數");
            return;
        }

        if (!int.TryParse(itemIdStr, out int itemId))
        {
            Debug.LogWarning($"無法解析物品ID: {itemIdStr}");
            return;
        }

        if (!int.TryParse(gameEvent.param2, out int itemCount))
        {
            Debug.LogWarning($"無法解析物品數量: {gameEvent.param2}");
            return;
        }

        if (itemCount <= 0)
        {
            Debug.LogWarning($"物品數量必須大於0: {itemCount}");
            return;
        }

        // 通過 ItemDatabase 查找物品
        Item item = ItemDatabase.Instance.GetItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"找不到ID為 {itemId} 的物品");
            return;
        }

        // 檢查玩家是否有足夠的物品
        int currentCount = InventoryManager.Instance.GetItemCount(item);
        if (currentCount < itemCount)
        {
            Debug.LogWarning($"物品數量不足: {item.Name} - 需要 {itemCount}，目前有 {currentCount}");
            return;
        }

        // 從玩家背包移除物品
        int actualRemoved = InventoryManager.Instance.RemoveItem(item, itemCount);
        
        if (actualRemoved == itemCount)
        {
            Debug.Log($"成功拿取物品: {item.Name} x{itemCount}");
        }
        else if (actualRemoved > 0)
        {
            Debug.LogWarning($"部分拿取物品: {item.Name} - 請求 {itemCount}，實際拿取 {actualRemoved}");
        }
        else
        {
            Debug.LogWarning($"無法拿取物品: {item.Name} x{itemCount}");
        }
    }
    
    /// <summary>
    /// 執行播放旁白事件
    /// </summary>
    /// <param name="gameEvent">事件數據</param>
    private static void ExecutePlayNarrationEvent(GameEvent gameEvent)
    {
        if (NarrationDialogManager.Instance == null)
        {
            Debug.LogWarning("NarrationDialogManager.Instance 為 null，無法執行播放旁白事件");
            return;
        }
        
        string dialogId = gameEvent.param1;
        if (string.IsNullOrEmpty(dialogId))
        {
            Debug.LogWarning("播放旁白事件缺少 dialogId 參數");
            return;
        }
        
        NarrationDialogManager.Instance.LoadDialog(dialogId);
        Debug.Log($"播放旁白: {dialogId}");
    }
    
    /// <summary>
    /// 執行播放音效事件
    /// </summary>
    /// <param name="gameEvent">事件數據</param>
    private static void ExecutePlayAudioEvent(GameEvent gameEvent)
    {
        string audioClipName = gameEvent.param1;
        if (string.IsNullOrEmpty(audioClipName))
        {
            Debug.LogWarning("播放音效事件缺少 audioClipName 參數");
            return;
        }
        
        // 嘗試解析音量參數
        float volume = 1.0f;
        if (!string.IsNullOrEmpty(gameEvent.param2))
        {
            if (!float.TryParse(gameEvent.param2, out volume))
            {
                Debug.LogWarning($"無法解析音量參數: {gameEvent.param2}，使用預設音量 1.0");
                volume = 1.0f;
            }
        }
        
        // TODO: 實現音效播放邏輯
        // 可以通過 AudioSource 或音效管理器播放
        Debug.Log($"播放音效: {audioClipName} (音量: {volume}) (尚未完全實現)");
    }
    
    /// <summary>
    /// 執行載入場景事件
    /// </summary>
    /// <param name="gameEvent">事件數據</param>
    private static void ExecuteLoadSceneEvent(GameEvent gameEvent)
    {
        string sceneName = gameEvent.param1;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("載入場景事件缺少 sceneName 參數");
            return;
        }
        
        // 嘗試解析是否顯示載入畫面參數
        bool showLoading = true;
        if (!string.IsNullOrEmpty(gameEvent.param2))
        {
            if (!bool.TryParse(gameEvent.param2, out showLoading))
            {
                Debug.LogWarning($"無法解析載入畫面參數: {gameEvent.param2}，使用預設值 true");
                showLoading = true;
            }
        }
        
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadScene(sceneName, showLoading);
            Debug.Log($"載入場景: {sceneName} (顯示載入畫面: {showLoading})");
        }
        else
        {
            Debug.LogWarning("GameSceneManager.Instance 為 null，使用Unity內建場景載入");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            Debug.Log($"載入場景: {sceneName} (使用Unity內建載入)");
        }
    }
}