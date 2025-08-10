using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 物品數據庫管理器 - 負責在遊戲啟動時加載所有物品並提供快速查找
/// 整合 3D 預覽系統，統一管理物品資料和 3D 模型預覽
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }
    
    [Header("物品數據庫設定")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool debugMode = false;
    
    [Header("物品加載設定")]
    [SerializeField] private bool useManualItemList = false;
    [SerializeField] private List<Item> manualItemList = new List<Item>();
    
    
    // 物品查找字典
    private Dictionary<int, Item> itemsById = new Dictionary<int, Item>();
    private Dictionary<string, Item> itemsByName = new Dictionary<string, Item>();
    
    // 所有物品列表
    private List<Item> allItems = new List<Item>();
    
    // 資料庫加載狀態
    private bool isLoaded = false;
    
    // === 3D 預覽系統變數 ===
    // 全域模型池
    private Dictionary<int, GameObject> globalModelPool = new Dictionary<int, GameObject>();
    private GameObject currentActiveModel;
    private Transform poolContainer;
    
    
    // 旋轉控制
    private bool isRotationEnabled = true;
    private Vector3 currentRotationSpeed = Vector3.zero;
    
    public bool IsLoaded => isLoaded;
    public int ItemCount => allItems.Count;
    public IReadOnlyList<Item> AllItems => allItems.AsReadOnly();
    
    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (loadOnAwake)
            {
                LoadAllItems();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 如果在 Awake 中沒有載入物品，在這裡載入
        if (!isLoaded && !loadOnAwake)
        {
            LoadAllItems();
        }
        
        // 初始化 3D 預覽系統
        Initialize3DPreviewSystem();
        
        // 載入 3D 模型
        Load3DModelsForItems();
    }
    
    private void Update()
    {
        // 處理自動旋轉
        if (isRotationEnabled && currentActiveModel != null && currentRotationSpeed != Vector3.zero)
        {
            currentActiveModel.transform.Rotate(currentRotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// 加載所有物品到數據庫
    /// </summary>
    public void LoadAllItems()
    {
        if (isLoaded)
        {
            Debug.LogWarning("[ItemDatabase] 物品數據庫已經加載過了");
            return;
        }
        
        Debug.Log("[ItemDatabase] 開始加載物品數據庫...");
        
        // 清空現有數據
        ClearDatabase();
        
        // 根據設定選擇加載方式
        if (useManualItemList)
        {
            LoadFromManualList();
        }
        else
        {
            LoadFromResources();
        }
        
        isLoaded = true;
        
        Debug.Log($"[ItemDatabase] 物品數據庫加載完成，共 {allItems.Count} 個物品");
        
        if (debugMode)
        {
            LogDatabaseContents();
        }
    }
    
    /// <summary>
    /// 從手動配置的物品列表加載
    /// </summary>
    private void LoadFromManualList()
    {
        Debug.Log($"[ItemDatabase] 使用手動物品列表加載模式，列表中有 {manualItemList.Count} 個物品");
        
        if (manualItemList == null || manualItemList.Count == 0)
        {
            Debug.LogWarning("[ItemDatabase] 手動物品列表為空！請在Inspector中添加物品，或切換到自動加載模式");
            return;
        }
        
        int addedCount = 0;
        int nullCount = 0;
        
        // 將手動列表中的物品添加到數據庫
        foreach (Item item in manualItemList)
        {
            if (item != null)
            {
                AddItemToDatabase(item);
                addedCount++;
            }
            else
            {
                nullCount++;
            }
        }
        
        Debug.Log($"[ItemDatabase] 手動加載完成：成功添加 {addedCount} 個物品，跳過 {nullCount} 個空引用");
        
        if (nullCount > 0)
        {
            Debug.LogWarning($"[ItemDatabase] 手動物品列表中有 {nullCount} 個空引用，請檢查Inspector中的配置");
        }
    }
    
    /// <summary>
    /// 從Resources文件夾自動掃描加載
    /// </summary>
    private void LoadFromResources()
    {
        Debug.Log("[ItemDatabase] 使用自動掃描加載模式，從Resources文件夾加載所有Item");
        
        // 從Resources文件夾加載所有Item
        Item[] items = Resources.LoadAll<Item>("");
        
        Debug.Log($"[ItemDatabase] 在Resources文件夾中找到 {items.Length} 個物品");
        
        // 將物品添加到數據庫
        foreach (Item item in items)
        {
            AddItemToDatabase(item);
        }
        
        Debug.Log($"[ItemDatabase] 自動加載完成：添加了 {items.Length} 個物品");
    }
    
    /// <summary>
    /// 將物品添加到數據庫
    /// </summary>
    private void AddItemToDatabase(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ItemDatabase] 嘗試添加null物品到數據庫");
            return;
        }
        
        // 添加到物品列表
        allItems.Add(item);
        
        // 按ID索引（如果物品有ID的話）
        if (item.Id > 0)
        {
            if (itemsById.ContainsKey(item.Id))
            {
                Debug.LogWarning($"[ItemDatabase] 物品ID衝突: ID {item.Id} 已存在 ({itemsById[item.Id].Name} vs {item.Name})");
            }
            else
            {
                itemsById[item.Id] = item;
            }
        }
        
        // 按名稱索引
        string itemName = item.Name;
        if (!string.IsNullOrEmpty(itemName))
        {
            if (itemsByName.ContainsKey(itemName))
            {
                Debug.LogWarning($"[ItemDatabase] 物品名稱衝突: \"{itemName}\" 已存在");
            }
            else
            {
                itemsByName[itemName] = item;
            }
        }
        
        // 同時按ScriptableObject的name索引（用於向後兼容）
        string assetName = item.name;
        if (!string.IsNullOrEmpty(assetName) && assetName != itemName)
        {
            if (!itemsByName.ContainsKey(assetName))
            {
                itemsByName[assetName] = item;
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[ItemDatabase] 添加物品: ID={item.Id}, Name=\"{itemName}\", AssetName=\"{assetName}\"");
        }
    }
    
    /// <summary>
    /// 通過ID查找物品
    /// </summary>
    public Item GetItemById(int itemId)
    {
        if (!isLoaded)
        {
            Debug.LogError("[ItemDatabase] 數據庫尚未加載，無法查找物品");
            return null;
        }
        
        if (itemId <= 0)
        {
            Debug.LogWarning($"[ItemDatabase] 無效的物品ID: {itemId}");
            return null;
        }
        
        itemsById.TryGetValue(itemId, out Item item);
        
        if (item == null && debugMode)
        {
            Debug.LogWarning($"[ItemDatabase] 找不到ID為 {itemId} 的物品");
        }
        
        return item;
    }
    
    /// <summary>
    /// 通過名稱查找物品
    /// </summary>
    public Item GetItemByName(string itemName)
    {
        if (!isLoaded)
        {
            Debug.LogError("[ItemDatabase] 數據庫尚未加載，無法查找物品");
            return null;
        }
        
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("[ItemDatabase] 物品名稱為空");
            return null;
        }
        
        itemsByName.TryGetValue(itemName, out Item item);
        
        if (item == null && debugMode)
        {
            Debug.LogWarning($"[ItemDatabase] 找不到名為 \"{itemName}\" 的物品");
        }
        
        return item;
    }
    
    /// <summary>
    /// 智能查找物品 - 優先使用ID，備選使用名稱
    /// </summary>
    public Item GetItem(int itemId, string itemName = null)
    {
        // 優先使用ID查找
        if (itemId > 0)
        {
            Item item = GetItemById(itemId);
            if (item != null)
            {
                return item;
            }
        }
        
        // 如果ID查找失敗，嘗試名稱查找（向後兼容）
        if (!string.IsNullOrEmpty(itemName))
        {
            Item item = GetItemByName(itemName);
            if (item != null)
            {
                Debug.LogWarning($"[ItemDatabase] 通過名稱找到物品 \"{itemName}\"，但ID {itemId} 無效，建議更新存檔格式");
                return item;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 獲取指定類型的所有物品
    /// </summary>
    public List<Item> GetItemsByType(ItemType itemType)
    {
        if (!isLoaded)
        {
            Debug.LogError("[ItemDatabase] 數據庫尚未加載，無法查找物品");
            return new List<Item>();
        }
        
        return allItems.Where(item => item.Type == itemType).ToList();
    }
    
    /// <summary>
    /// 獲取指定稀有度的所有物品
    /// </summary>
    public List<Item> GetItemsByRarity(ItemRarity rarity)
    {
        if (!isLoaded)
        {
            Debug.LogError("[ItemDatabase] 數據庫尚未加載，無法查找物品");
            return new List<Item>();
        }
        
        return allItems.Where(item => item.Rarity == rarity).ToList();
    }
    
    /// <summary>
    /// 清空數據庫
    /// </summary>
    private void ClearDatabase()
    {
        allItems.Clear();
        itemsById.Clear();
        itemsByName.Clear();
        isLoaded = false;
    }
    
    /// <summary>
    /// 重新加載數據庫（主要用於開發時）
    /// </summary>
    [ContextMenu("重新加載物品數據庫")]
    public void ReloadDatabase()
    {
        Debug.Log("[ItemDatabase] 重新加載物品數據庫...");
        ClearDatabase();
        LoadAllItems();
    }
    
    /// <summary>
    /// 輸出數據庫內容到控制台
    /// </summary>
    [ContextMenu("輸出數據庫內容")]
    public void LogDatabaseContents()
    {
        if (!isLoaded)
        {
            Debug.LogWarning("[ItemDatabase] 數據庫未加載");
            return;
        }
        
        Debug.Log($"[ItemDatabase] 數據庫內容 ({allItems.Count} 個物品):");
        
        foreach (Item item in allItems)
        {
            Debug.Log($"  ID: {item.Id}, Name: \"{item.Name}\", AssetName: \"{item.name}\", Type: {item.Type}, Rarity: {item.Rarity}");
        }
    }
    
    /// <summary>
    /// 獲取數據庫統計信息
    /// </summary>
    public string GetDatabaseStats()
    {
        if (!isLoaded)
        {
            return "數據庫未加載";
        }
        
        var typeGroups = allItems.GroupBy(item => item.Type);
        var rarityGroups = allItems.GroupBy(item => item.Rarity);
        
        string stats = $"物品數據庫統計:\n";
        stats += $"總物品數: {allItems.Count}\n";
        stats += $"有效ID數: {itemsById.Count}\n";
        stats += $"按類型分組:\n";
        
        foreach (var group in typeGroups)
        {
            stats += $"  {group.Key}: {group.Count()}\n";
        }
        
        stats += $"按稀有度分組:\n";
        foreach (var group in rarityGroups)
        {
            stats += $"  {group.Key}: {group.Count()}\n";
        }
        
        return stats;
    }
    
    private void OnDestroy()
    {
        // 清理 3D 預覽系統
        Clear3DPreviewSystem();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // ==================== 3D 預覽系統功能 ====================
    
    #region 3D Preview System
    
    /// <summary>
    /// 初始化 3D 預覽系統（僅模型池）
    /// </summary>
    private void Initialize3DPreviewSystem()
    {
        // 創建全域模型容器
        GameObject poolObject = new GameObject("GlobalItemModelPool");
        poolContainer = poolObject.transform;
        poolContainer.SetParent(transform);
        // 保持容器活動，通過個別模型的 SetActive() 控制顯示
        
        Debug.Log("[ItemDatabase] 3D 預覽系統模型池初始化完成");
    }
    
    /// <summary>
    /// 載入所有物品的 3D 模型
    /// </summary>
    private void Load3DModelsForItems()
    {
        if (!isLoaded)
        {
            Debug.LogWarning("[ItemDatabase] 物品數據庫尚未載入，無法載入 3D 模型");
            return;
        }
        
        Debug.Log($"[ItemDatabase] 開始載入 {allItems.Count} 個道具的 3D 模型");
        
        int loadedCount = 0;
        foreach (Item item in allItems)
        {
            if (item.ItemPrefab3D != null)
            {
                // 實例化模型
                GameObject model = Instantiate(item.ItemPrefab3D, poolContainer);
                model.name = $"Preview_{item.Name}_{item.Id}";
                model.SetActive(false);
                
                // 應用預設的預覽設定
                ApplyItemPreviewSettings(model, item);
                
                // 移除物理組件（預覽不需要物理）
                RemovePhysicsComponents(model);
                
                // 加入全域池
                globalModelPool[item.Id] = model;
                loadedCount++;
                
                if (debugMode)
                {
                    Debug.Log($"[ItemDatabase] 載入 3D 模型: {item.Name} (ID: {item.Id})");
                }
            }
        }
        
        Debug.Log($"[ItemDatabase] 3D 模型載入完成，共載入 {loadedCount} 個模型");
    }
    
    /// <summary>
    /// 應用道具的預覽設定
    /// </summary>
    private void ApplyItemPreviewSettings(GameObject model, Item item)
    {
        model.transform.localScale = item.PreviewScale;
        model.transform.localRotation = Quaternion.Euler(item.PreviewRotation);
        model.transform.localPosition = item.PreviewPosition;
    }
    
    /// <summary>
    /// 移除預覽模型上不需要的物理組件
    /// </summary>
    private void RemovePhysicsComponents(GameObject model)
    {
        // 禁用所有碰撞器
        Collider[] colliders = model.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // 移除或設定剛體為 kinematic
        Rigidbody[] rigidbodies = model.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }
    
    /// <summary>
    /// 顯示指定道具的 3D 預覽
    /// </summary>
    public void ShowItemPreview(Item item)
    {
        if (item == null)
        {
            HidePreview();
            return;
        }
        
        // 隱藏當前模型
        HideCurrentModel();
        
        // 從全域池中獲取模型
        if (globalModelPool.TryGetValue(item.Id, out GameObject model))
        {
            // 直接在相機前方固定位置顯示
            model.SetActive(true);
            currentActiveModel = model;
            
            // 設定旋轉
            if (item.EnableAutoRotation)
            {
                currentRotationSpeed = item.RotationSpeed;
                isRotationEnabled = true;
            }
            else
            {
                currentRotationSpeed = Vector3.zero;
                isRotationEnabled = false;
            }
            
            if (debugMode)
            {
                Debug.Log($"[ItemDatabase] 顯示 3D 預覽: {item.Name}");
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.LogWarning($"[ItemDatabase] 找不到道具 3D 模型: {item.Name} (ID: {item.Id})");
            }
        }
    }
    
    /// <summary>
    /// 隱藏當前的預覽
    /// </summary>
    public void HidePreview()
    {
        HideCurrentModel();
        currentRotationSpeed = Vector3.zero;
    }
    
    /// <summary>
    /// 隱藏當前激活的模型
    /// </summary>
    private void HideCurrentModel()
    {
        if (currentActiveModel != null)
        {
            currentActiveModel.SetActive(false);
            // 模型保持在原本的池容器中，不需要移動
            currentActiveModel = null;
        }
    }
    
    /// <summary>
    /// 設定旋轉啟用狀態
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        isRotationEnabled = enabled;
    }
    
    /// <summary>
    /// 清理 3D 預覽系統
    /// </summary>
    private void Clear3DPreviewSystem()
    {
        HidePreview();
        
        foreach (var kvp in globalModelPool)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
            }
        }
        
        globalModelPool.Clear();
        
        Debug.Log("[ItemDatabase] 3D 預覽系統模型池已清理");
    }
    
    /// <summary>
    /// 獲取 3D 預覽系統狀態信息
    /// </summary>
    public string GetPreviewSystemInfo()
    {
        return $"載入 3D 模型數量: {globalModelPool.Count}, 當前預覽: {(currentActiveModel?.name ?? "無")}";
    }
    
    #endregion
    
    /// <summary>
    /// 驗證數據庫完整性
    /// </summary>
    [ContextMenu("驗證數據庫完整性")]
    public void ValidateDatabase()
    {
        if (!isLoaded)
        {
            Debug.LogWarning("[ItemDatabase] 數據庫未加載，無法驗證");
            return;
        }
        
        Debug.Log("[ItemDatabase] 開始驗證數據庫完整性...");
        
        int invalidIdCount = 0;
        int duplicateNameCount = 0;
        
        foreach (Item item in allItems)
        {
            // 檢查ID
            if (item.Id <= 0)
            {
                Debug.LogWarning($"[ItemDatabase] 物品 \"{item.Name}\" 沒有有效的ID");
                invalidIdCount++;
            }
            
            // 檢查名稱重複
            var sameNameItems = allItems.Where(i => i.Name == item.Name).ToList();
            if (sameNameItems.Count > 1)
            {
                Debug.LogWarning($"[ItemDatabase] 發現重複名稱: \"{item.Name}\" ({sameNameItems.Count} 個)");
                duplicateNameCount++;
            }
        }
        
        Debug.Log($"[ItemDatabase] 驗證完成 - 無效ID: {invalidIdCount}, 重複名稱: {duplicateNameCount}");
    }
    
    // ==================== Inspector 輔助功能 ====================
    
    /// <summary>
    /// 從Resources自動填充手動物品列表
    /// </summary>
    [ContextMenu("從Resources自動填充手動列表")]
    public void PopulateManualListFromResources()
    {
        Debug.Log("[ItemDatabase] 開始從Resources自動填充手動物品列表...");
        
        // 從Resources加載所有物品
        Item[] items = Resources.LoadAll<Item>("");
        
        // 清空現有列表
        if (manualItemList == null)
        {
            manualItemList = new List<Item>();
        }
        else
        {
            manualItemList.Clear();
        }
        
        // 添加到手動列表
        foreach (Item item in items)
        {
            if (item != null && !manualItemList.Contains(item))
            {
                manualItemList.Add(item);
            }
        }
        
        Debug.Log($"[ItemDatabase] 自動填充完成，手動列表中現有 {manualItemList.Count} 個物品");
        
        // 自動切換到手動模式
        useManualItemList = true;
        
        Debug.Log("[ItemDatabase] 已自動切換到手動物品列表模式");
        
#if UNITY_EDITOR
        // 標記為已修改，確保Inspector更新
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    /// <summary>
    /// 驗證手動物品列表
    /// </summary>
    [ContextMenu("驗證手動物品列表")]
    public void ValidateManualList()
    {
        if (manualItemList == null)
        {
            Debug.LogWarning("[ItemDatabase] 手動物品列表為null");
            return;
        }
        
        Debug.Log("[ItemDatabase] 開始驗證手動物品列表...");
        
        int nullCount = 0;
        int duplicateCount = 0;
        int validCount = 0;
        HashSet<Item> uniqueItems = new HashSet<Item>();
        
        foreach (Item item in manualItemList)
        {
            if (item == null)
            {
                nullCount++;
            }
            else if (uniqueItems.Contains(item))
            {
                duplicateCount++;
                Debug.LogWarning($"[ItemDatabase] 發現重複物品: {item.Name}");
            }
            else
            {
                uniqueItems.Add(item);
                validCount++;
            }
        }
        
        Debug.Log($"[ItemDatabase] 手動列表驗證完成:");
        Debug.Log($"  總數: {manualItemList.Count}");
        Debug.Log($"  有效: {validCount}");
        Debug.Log($"  空引用: {nullCount}");
        Debug.Log($"  重複: {duplicateCount}");
        
        if (nullCount > 0 || duplicateCount > 0)
        {
            Debug.LogWarning($"[ItemDatabase] 手動列表存在問題，建議清理");
        }
    }
    
    /// <summary>
    /// 清理手動物品列表（移除null和重複項）
    /// </summary>
    [ContextMenu("清理手動物品列表")]
    public void CleanupManualList()
    {
        if (manualItemList == null)
        {
            Debug.LogWarning("[ItemDatabase] 手動物品列表為null，無需清理");
            return;
        }
        
        Debug.Log("[ItemDatabase] 開始清理手動物品列表...");
        
        int originalCount = manualItemList.Count;
        
        // 移除null項目和重複項目
        HashSet<Item> uniqueItems = new HashSet<Item>();
        List<Item> cleanedList = new List<Item>();
        
        foreach (Item item in manualItemList)
        {
            if (item != null && !uniqueItems.Contains(item))
            {
                uniqueItems.Add(item);
                cleanedList.Add(item);
            }
        }
        
        manualItemList = cleanedList;
        
        int cleanedCount = manualItemList.Count;
        int removedCount = originalCount - cleanedCount;
        
        Debug.Log($"[ItemDatabase] 清理完成：移除了 {removedCount} 個無效項目，剩餘 {cleanedCount} 個有效物品");
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    /// <summary>
    /// 切換加載模式
    /// </summary>
    [ContextMenu("切換加載模式")]
    public void ToggleLoadingMode()
    {
        useManualItemList = !useManualItemList;
        string mode = useManualItemList ? "手動物品列表" : "自動掃描Resources";
        Debug.Log($"[ItemDatabase] 已切換到 {mode} 模式");
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    /// <summary>
    /// 獲取當前配置信息
    /// </summary>
    [ContextMenu("顯示配置信息")]
    public void ShowConfigurationInfo()
    {
        string info = "[ItemDatabase] 當前配置信息:\n";
        info += $"  加載模式: {(useManualItemList ? "手動物品列表" : "自動掃描Resources")}\n";
        info += $"  在Awake時加載: {loadOnAwake}\n";
        info += $"  調試模式: {debugMode}\n";
        info += $"  數據庫已加載: {isLoaded}\n";
        
        if (useManualItemList)
        {
            int manualCount = manualItemList?.Count ?? 0;
            int nullCount = manualItemList?.Count(item => item == null) ?? 0;
            info += $"  手動列表物品數: {manualCount}\n";
            info += $"  手動列表空引用: {nullCount}\n";
        }
        
        if (isLoaded)
        {
            info += $"  已緩存物品數: {allItems.Count}\n";
            info += $"  按ID索引數: {itemsById.Count}\n";
            info += $"  按名稱索引數: {itemsByName.Count}\n";
        }
        
        Debug.Log(info);
    }
}