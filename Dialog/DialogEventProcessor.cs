using UnityEngine;

public static class DialogEventProcessor
{
    public static void ProcessDialogEvents(DialogManager.DialogEvent[] events)
    {
        if (events == null || events.Length == 0)
        {
            return;
        }

        Debug.Log($"處理 {events.Length} 個對話事件");

        foreach (DialogManager.DialogEvent dialogEvent in events)
        {
            if (ShouldExecuteEvent(dialogEvent))
            {
                ExecuteEvent(dialogEvent);
            }
        }
    }

    private static bool ShouldExecuteEvent(DialogManager.DialogEvent dialogEvent)
    {
        if (dialogEvent.condition == null)
        {
            return true;
        }

        return DialogConditionChecker.CheckCondition(dialogEvent.condition);
    }

    private static void ExecuteEvent(DialogManager.DialogEvent dialogEvent)
    {
        if (string.IsNullOrEmpty(dialogEvent.event_type))
        {
            Debug.LogWarning("事件類型為空，跳過執行");
            return;
        }

        Debug.Log($"執行事件: {dialogEvent.event_type}");

        switch (dialogEvent.event_type.ToLower())
        {
            case "update_tag":
                ExecuteUpdateTagEvent(dialogEvent);
                break;
                
            case "give_item":
                ExecuteGiveItemEvent(dialogEvent);
                break;
                
            case "take_item":
                ExecuteTakeItemEvent(dialogEvent);
                break;
                
            default:
                Debug.LogWarning($"未知的事件類型: {dialogEvent.event_type}");
                break;
        }
    }

    private static void ExecuteUpdateTagEvent(DialogManager.DialogEvent dialogEvent)
    {
        if (TagSystem.Instance == null)
        {
            Debug.LogWarning("TagSystem.Instance 為 null，無法執行更新標籤事件");
            return;
        }

        string tagId = dialogEvent.param1;
        if (string.IsNullOrEmpty(tagId))
        {
            Debug.LogWarning("更新標籤事件缺少 tagId 參數");
            return;
        }

        if (!int.TryParse(dialogEvent.param2, out int tagValue))
        {
            Debug.LogWarning($"無法解析標籤值: {dialogEvent.param2}");
            return;
        }

        TagSystem.Instance.SetTag(tagId, tagValue);
        Debug.Log($"更新標籤: {tagId} = {tagValue}");
    }

    private static void ExecuteGiveItemEvent(DialogManager.DialogEvent dialogEvent)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance 為 null，無法執行給予物品事件");
            return;
        }

        string itemId = dialogEvent.param1;
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("給予物品事件缺少 itemId 參數");
            return;
        }

        if (!int.TryParse(dialogEvent.param2, out int itemCount))
        {
            Debug.LogWarning($"無法解析物品數量: {dialogEvent.param2}");
            return;
        }

        // TODO: 需要實現根據itemId查找Item物件的邏輯
        Debug.Log($"給予物品: {itemId} x{itemCount} (尚未完全實現)");
    }

    private static void ExecuteTakeItemEvent(DialogManager.DialogEvent dialogEvent)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance 為 null，無法執行拿取物品事件");
            return;
        }

        string itemId = dialogEvent.param1;
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("拿取物品事件缺少 itemId 參數");
            return;
        }

        if (!int.TryParse(dialogEvent.param2, out int itemCount))
        {
            Debug.LogWarning($"無法解析物品數量: {dialogEvent.param2}");
            return;
        }

        // TODO: 需要實現根據itemId查找Item物件的邏輯
        Debug.Log($"拿取物品: {itemId} x{itemCount} (尚未完全實現)");
    }
}