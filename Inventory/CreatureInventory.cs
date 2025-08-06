using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 生物背包系統 - 為每個生物管理獨立的物品欄
/// </summary>
[System.Serializable]
public class CreatureInventory
{
    [SerializeField] private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    [SerializeField] private int inventorySize;
    [SerializeField] private string ownerName;
    
    // 事件
    public System.Action OnInventoryChanged;
    
    public List<InventorySlot> InventorySlots => inventorySlots;
    public int InventorySize => inventorySize;
    public string OwnerName => ownerName;
    
    /// <summary>
    /// 初始化背包
    /// </summary>
    /// <param name="size">背包大小</param>
    /// <param name="owner">擁有者名稱</param>
    public void Initialize(int size, string owner)
    {
        inventorySize = size;
        ownerName = owner;
        inventorySlots.Clear();
        
        // 創建物品欄格子
        for (int i = 0; i < inventorySize; i++)
        {
            InventorySlot slot = new InventorySlot();
            inventorySlots.Add(slot);
        }
        
        Debug.Log($"初始化 {ownerName} 的背包，共 {inventorySlots.Count} 個格子");
    }
    
    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="item">要添加的物品</param>
    /// <param name="count">數量</param>
    /// <returns>實際添加的數量</returns>
    public int AddItem(Item item, int count)
    {
        if (item == null || count <= 0) return 0;
        
        int remainingCount = count;
        
        // 首先嘗試堆疊到現有的相同物品
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.CurrentItem == item)
            {
                int added = slot.AddItem(remainingCount);
                remainingCount -= added;
                
                if (remainingCount <= 0) break;
            }
        }
        
        // 如果還有剩餘，放到空格子中
        if (remainingCount > 0)
        {
            foreach (InventorySlot slot in inventorySlots)
            {
                if (slot.IsEmpty)
                {
                    int toAdd = Mathf.Min(remainingCount, item.MaxStackSize);
                    slot.SetItem(item, toAdd);
                    remainingCount -= toAdd;
                    
                    if (remainingCount <= 0) break;
                }
            }
        }
        
        int actualAdded = count - remainingCount;
        if (actualAdded > 0)
        {
            Debug.Log($"{ownerName} 添加物品: {item.Name} x{actualAdded}");
            OnInventoryChanged?.Invoke();
        }
        
        if (remainingCount > 0)
        {
            Debug.Log($"{ownerName} 的背包已滿，無法添加 {item.Name} x{remainingCount}");
        }
        
        return actualAdded;
    }
    
    /// <summary>
    /// 移除物品從背包
    /// </summary>
    /// <param name="item">要移除的物品</param>
    /// <param name="count">數量</param>
    /// <returns>實際移除的數量</returns>
    public int RemoveItem(Item item, int count)
    {
        if (item == null || count <= 0) return 0;
        
        int remainingCount = count;
        
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.CurrentItem == item)
            {
                int removed = slot.RemoveItem(remainingCount);
                remainingCount -= removed;
                
                if (remainingCount <= 0) break;
            }
        }
        
        int actualRemoved = count - remainingCount;
        if (actualRemoved > 0)
        {
            Debug.Log($"{ownerName} 移除物品: {item.Name} x{actualRemoved}");
            OnInventoryChanged?.Invoke();
        }
        
        return actualRemoved;
    }
    
    /// <summary>
    /// 檢查是否有足夠的物品
    /// </summary>
    /// <param name="item">要檢查的物品</param>
    /// <param name="count">需要的數量</param>
    /// <returns>是否有足夠的物品</returns>
    public bool HasItem(Item item, int count)
    {
        if (item == null) return false;
        
        int totalCount = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.CurrentItem == item)
            {
                totalCount += slot.ItemCount;
            }
        }
        
        return totalCount >= count;
    }
    
    /// <summary>
    /// 獲取物品的總數量
    /// </summary>
    /// <param name="item">要檢查的物品</param>
    /// <returns>物品總數量</returns>
    public int GetItemCount(Item item)
    {
        if (item == null) return 0;
        
        int totalCount = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty && slot.CurrentItem == item)
            {
                totalCount += slot.ItemCount;
            }
        }
        
        return totalCount;
    }
    
    /// <summary>
    /// 使用物品
    /// </summary>
    /// <param name="item">要使用的物品</param>
    /// <param name="user">使用者</param>
    /// <returns>是否成功使用</returns>
    public bool UseItem(Item item, GameObject user)
    {
        if (item == null || !item.IsUsable) return false;
        
        // 檢查是否有該物品
        if (!HasItem(item, 1)) return false;
        
        // 使用物品
        bool success = item.UseItem(user);
        
        // 如果是消耗品且使用成功，移除一個
        if (success && item.IsConsumable)
        {
            RemoveItem(item, 1);
        }
        
        return success;
    }
    
    /// <summary>
    /// 清空背包
    /// </summary>
    public void ClearInventory()
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            slot.ClearSlot();
        }
        
        Debug.Log($"{ownerName} 的背包已清空");
        OnInventoryChanged?.Invoke();
    }
    
    /// <summary>
    /// 獲取空格子數量
    /// </summary>
    /// <returns>空格子數量</returns>
    public int GetEmptySlotCount()
    {
        return inventorySlots.Count(slot => slot.IsEmpty);
    }
    
    /// <summary>
    /// 檢查背包是否已滿
    /// </summary>
    /// <returns>是否已滿</returns>
    public bool IsInventoryFull()
    {
        return GetEmptySlotCount() == 0;
    }
    
    /// <summary>
    /// 獲取所有物品及其數量
    /// </summary>
    /// <returns>物品和數量的列表</returns>
    public List<(Item item, int count)> GetAllItemsWithCounts()
    {
        var itemCounts = new Dictionary<Item, int>();
        
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                if (itemCounts.ContainsKey(slot.CurrentItem))
                {
                    itemCounts[slot.CurrentItem] += slot.ItemCount;
                }
                else
                {
                    itemCounts[slot.CurrentItem] = slot.ItemCount;
                }
            }
        }
        
        return itemCounts.Select(kvp => (kvp.Key, kvp.Value)).ToList();
    }
    
    /// <summary>
    /// 獲取背包狀態信息
    /// </summary>
    /// <returns>狀態信息字符串</returns>
    public string GetInventoryInfo()
    {
        int usedSlots = inventorySlots.Count - GetEmptySlotCount();
        return $"{ownerName} 的背包: {usedSlots}/{inventorySlots.Count}";
    }
    
    /// <summary>
    /// 添加起始物品
    /// </summary>
    /// <param name="startingItems">起始物品列表</param>
    public void AddStartingItems(List<Item> startingItems)
    {
        if (startingItems == null) return;
        
        foreach (Item item in startingItems)
        {
            if (item != null)
            {
                AddItem(item, 1);
            }
        }
    }
}
