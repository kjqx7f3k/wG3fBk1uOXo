using UnityEngine;

/// <summary>
/// 物品欄格子 - 代表背包中的一個格子，可以存放物品
/// </summary>
[System.Serializable]
public class InventorySlot
{
    [SerializeField] private Item currentItem;
    [SerializeField] private int itemCount;
    
    /// <summary>
    /// 當前物品
    /// </summary>
    public Item CurrentItem => currentItem;
    
    /// <summary>
    /// 物品數量
    /// </summary>
    public int ItemCount => itemCount;
    
    /// <summary>
    /// 格子是否為空
    /// </summary>
    public bool IsEmpty => currentItem == null || itemCount <= 0;
    
    /// <summary>
    /// 格子是否已滿
    /// </summary>
    public bool IsFull => currentItem != null && itemCount >= currentItem.MaxStackSize;
    
    /// <summary>
    /// 可以添加的物品數量
    /// </summary>
    public int AvailableSpace => currentItem == null ? 0 : currentItem.MaxStackSize - itemCount;
    
    /// <summary>
    /// 預設構造函數 - 創建空格子
    /// </summary>
    public InventorySlot()
    {
        currentItem = null;
        itemCount = 0;
    }
    
    /// <summary>
    /// 構造函數 - 創建帶物品的格子
    /// </summary>
    /// <param name="item">物品</param>
    /// <param name="count">數量</param>
    public InventorySlot(Item item, int count)
    {
        SetItem(item, count);
    }
    
    /// <summary>
    /// 設置格子中的物品
    /// </summary>
    /// <param name="item">物品</param>
    /// <param name="count">數量</param>
    public void SetItem(Item item, int count)
    {
        if (item == null)
        {
            ClearSlot();
            return;
        }
        
        currentItem = item;
        itemCount = Mathf.Clamp(count, 0, item.MaxStackSize);
        
        // 如果數量為0，清空格子
        if (itemCount <= 0)
        {
            ClearSlot();
        }
    }
    
    /// <summary>
    /// 添加物品到格子
    /// </summary>
    /// <param name="count">要添加的數量</param>
    /// <returns>實際添加的數量</returns>
    public int AddItem(int count)
    {
        if (currentItem == null || count <= 0) return 0;
        
        int canAdd = Mathf.Min(count, AvailableSpace);
        itemCount += canAdd;
        
        return canAdd;
    }
    
    /// <summary>
    /// 從格子移除物品
    /// </summary>
    /// <param name="count">要移除的數量</param>
    /// <returns>實際移除的數量</returns>
    public int RemoveItem(int count)
    {
        if (IsEmpty || count <= 0) return 0;
        
        int canRemove = Mathf.Min(count, itemCount);
        itemCount -= canRemove;
        
        // 如果數量變為0，清空格子
        if (itemCount <= 0)
        {
            ClearSlot();
        }
        
        return canRemove;
    }
    
    /// <summary>
    /// 清空格子
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        itemCount = 0;
    }
    
    /// <summary>
    /// 檢查是否可以添加指定物品
    /// </summary>
    /// <param name="item">要檢查的物品</param>
    /// <param name="count">要添加的數量</param>
    /// <returns>是否可以添加</returns>
    public bool CanAddItem(Item item, int count)
    {
        if (item == null || count <= 0) return false;
        
        // 如果格子為空，可以添加
        if (IsEmpty) return count <= item.MaxStackSize;
        
        // 如果是相同物品且有空間，可以添加
        if (currentItem == item && !IsFull)
        {
            return count <= AvailableSpace;
        }
        
        return false;
    }
    
    /// <summary>
    /// 檢查是否可以移除指定數量的物品
    /// </summary>
    /// <param name="count">要移除的數量</param>
    /// <returns>是否可以移除</returns>
    public bool CanRemoveItem(int count)
    {
        return !IsEmpty && count <= itemCount;
    }
    
    /// <summary>
    /// 初始化格子（用於UI系統）
    /// </summary>
    /// <param name="slotIndex">格子索引</param>
    public void Initialize(int slotIndex)
    {
        // 這個方法主要用於UI系統的初始化
        // 在當前架構中，格子的邏輯狀態由這個類管理
        // UI顯示由InventoryManager處理
        Debug.Log($"初始化物品欄格子 {slotIndex}");
    }
    
    /// <summary>
    /// 獲取格子的詳細信息
    /// </summary>
    /// <returns>格子信息字符串</returns>
    public string GetSlotInfo()
    {
        if (IsEmpty)
        {
            return "空格子";
        }
        
        return $"{currentItem.Name} x{itemCount}/{currentItem.MaxStackSize}";
    }
    
    /// <summary>
    /// 複製格子內容到另一個格子
    /// </summary>
    /// <param name="targetSlot">目標格子</param>
    public void CopyTo(InventorySlot targetSlot)
    {
        if (targetSlot == null) return;
        
        if (IsEmpty)
        {
            targetSlot.ClearSlot();
        }
        else
        {
            targetSlot.SetItem(currentItem, itemCount);
        }
    }
    
    /// <summary>
    /// 與另一個格子交換內容
    /// </summary>
    /// <param name="otherSlot">另一個格子</param>
    public void SwapWith(InventorySlot otherSlot)
    {
        if (otherSlot == null) return;
        
        Item tempItem = currentItem;
        int tempCount = itemCount;
        
        SetItem(otherSlot.currentItem, otherSlot.itemCount);
        otherSlot.SetItem(tempItem, tempCount);
    }
    
    /// <summary>
    /// 檢查兩個格子是否相等
    /// </summary>
    /// <param name="other">另一個格子</param>
    /// <returns>是否相等</returns>
    public bool Equals(InventorySlot other)
    {
        if (other == null) return false;
        
        return currentItem == other.currentItem && itemCount == other.itemCount;
    }
    
    /// <summary>
    /// 重寫ToString方法
    /// </summary>
    /// <returns>格子的字符串表示</returns>
    public override string ToString()
    {
        return GetSlotInfo();
    }
}
