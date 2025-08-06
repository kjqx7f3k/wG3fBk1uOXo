using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : UIPanel
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("UI 組件引用")]
    [SerializeField] private ScrollRect itemListScrollRect;
    [SerializeField] private Transform itemListContent;
    [SerializeField] private Transform itemPreviewArea;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button equipButton;
    
    [Header("Prefab設定")]
    [SerializeField] private GameObject itemSlotPrefab;
    
    [Header("分頁設定")]
    [SerializeField] private int itemsPerPage = 5;
    [SerializeField] private TextMeshProUGUI pageInfoText;
    
    [Header("佈局設定")]
    [SerializeField] private RectOffset layoutPadding;
    [SerializeField] private float layoutSpacing = 5f;
    
    [Header("測試物品")]
    [SerializeField] private List<Item> testItems = new List<Item>();
    
    // UI數據
    private List<GameObject> itemListUIElements = new List<GameObject>();
    private int selectedItemIndex = -1;
    private GameObject currentPreviewObject;
    private ControllableCreature currentCreature;
    
    // 分頁相關變數
    private List<(Item item, int count)> allItems = new List<(Item, int)>();
    private int currentPage = 0;
    private int totalPages = 0;
    
    // 導航系統變數（參考SaveUI）
    private List<Button> navigableButtons = new List<Button>();
    private int currentNavIndex = -1;
    private bool isItemSelected = false; // 是否已選中道具進入操作模式
    
    [Header("導航設定")]
    [SerializeField] private float navigationCooldown = 0.2f; // 導航冷卻時間（秒）
    private float lastNavigationTime = 0f; // 上次導航的時間
    
    // 事件
    public System.Action<Item> OnItemSelected;
    public System.Action<Item> OnItemUsed;
    public System.Action<Item> OnItemDiscarded;
    public System.Action<Item, bool> OnItemEquipped; // Item, isEquipped
    
    public ControllableCreature CurrentCreature => currentCreature;
    public CreatureInventory CurrentInventory => currentCreature?.Inventory;
    public Item SelectedItem 
    { 
        get 
        {
            if (CurrentInventory == null || allItems.Count == 0) return null;
            if (selectedItemIndex >= 0 && selectedItemIndex < allItems.Count)
            {
                return allItems[selectedItemIndex].item;
            }
            return null;
        }
    }
    public int SelectedItemIndex => selectedItemIndex;
    
    protected override void Awake()
    {
        base.Awake(); // 呼叫基底類別的Awake
        
        // 設定UIPanel屬性
        pauseGameWhenOpen = false;  // 道具欄不暫停遊戲
        blockCharacterMovement = true;  // 但阻擋角色移動
        canCloseWithEscape = true;  // 可用ESC關閉
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 確保列表已初始化
            if (itemListUIElements == null)
                itemListUIElements = new List<GameObject>();
            if (testItems == null)
                testItems = new List<Item>();
            
            // 初始化layoutPadding（如果還沒有設置的話）
            if (layoutPadding == null)
                layoutPadding = new RectOffset(10, 10, 10, 10);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            // 清理UI元素
            ClearItemListUI();
            ClearPreviewArea();
            
            // 重置單例引用
            Instance = null;
            
            Debug.Log("InventoryManager 已清理");
        }
    }
    
    private void OnApplicationQuit()
    {
        // 應用程式退出時清理
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void Start()
    {
        // 設置UI組件
        SetupUI();
        
        // 確保面板初始為關閉狀態
        if (panelCanvas != null)
        {
            panelCanvas.enabled = false;
        }
        isOpen = false;

        // 直接開始延遲初始化
        StartCoroutine(DelayedInitialization());
    }
    
    private void OnEnable()
    {
        // 當GameObject變為active時，檢查是否需要執行延遲初始化
    }
    
    /// <summary>
    /// 延遲初始化協程
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // 等待一幀確保所有系統都已初始化
        yield return null;
        
        Debug.Log("開始延遲初始化背包系統");
        
        InitializeWithCurrentCreature();
        
        // 再等待一幀確保生物背包已經初始化
        yield return null;
        
        AddTestItemsToCurrentCreature();
    }
    
    protected override void Update()
    {
        // 檢查Tab鍵輸入（只檢查一次）
        bool tabPressed = InputSystemWrapper.Instance != null && InputSystemWrapper.Instance.GetUITabDown();
        
        if (tabPressed)
        {
            Debug.Log($"[InventoryManager] Tab鍵被按下，當前道具欄狀態: {isOpen}");
        }
        
        // 根據當前狀態決定如何處理Tab鍵
        if (tabPressed)
        {
            if (isOpen)
            {
                // 道具欄已開啟，Tab鍵用於關閉道具欄
                Debug.Log("[InventoryManager] 道具欄已開啟，Tab鍵關閉道具欄");
                CloseInventory();
            }
            else
            {
                // 道具欄未開啟，檢查是否可以開啟道具欄
                HandleTabToOpenInventory();
            }
        }
        
        // 處理道具欄內部的非Tab輸入（當道具欄開啟時）
        if (isOpen)
        {
            // 檢查ESC鍵 (假設 InputSystemWrapper.Instance.GetUIBackDown() 對應 ESC)
            if (canCloseWithEscape && InputSystemWrapper.Instance != null && InputSystemWrapper.Instance.GetUICancelDown())
            {
                HandleEscapeKey();
                return; // 處理完ESC後，直接返回，避免同一幀內處理其他輸入
            }

            HandleInventoryNavigation();
        }
    }
    
    /// <summary>
    /// 處理Tab鍵開啟道具欄的邏輯
    /// </summary>
    private void HandleTabToOpenInventory()
    {
        bool anyUIOpen = IsTabBlocked();
        Debug.Log($"[InventoryManager] 檢查是否可開啟道具欄，其他UI開啟狀態: {anyUIOpen}");
        
        if (!anyUIOpen)
        {
            Debug.Log("[InventoryManager] 沒有其他UI開啟，開啟道具欄");
            OpenInventory();
        }
        else
        {
            Debug.Log("[InventoryManager] 有其他UI開啟，無法開啟道具欄");
        }
    }
    
    /// <summary>
    /// 處理道具欄的導航輸入（不包括Tab鍵）
    /// </summary>
    private void HandleInventoryNavigation()
    {
        // 處理道具欄導航輸入
        HandleInventoryInput();
    }
    
    /// <summary>
    /// 檢查Tab鍵是否被其他UI阻擋
    /// </summary>
    /// <returns>如果被阻擋返回true</returns>
    private bool IsTabBlocked()
    {
        // 對話中時阻擋
        if (DialogManager.Instance != null && DialogManager.Instance.IsInDialog)
            return true;
            
        // 遊戲選單開啟時阻擋
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.IsOpen)
            return true;
            
        // 存檔UI開啟時阻擋
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen)
            return true;
            
        // 設定UI開啟時阻擋
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// 處理ESC鍵邏輯 - 重寫UIPanel方法
    /// </summary>
    protected override void HandleEscapeKey()
    {
        // ESC鍵關閉道具欄並開啟遊戲選單
        CloseInventory();
        
        if (GameMenuManager.Instance != null)
        {
            GameMenuManager.Instance.OpenGameMenu();
        }
    }
    
    /// <summary>
    /// 處理自定義輸入 - 重寫UIPanel方法
    /// 由於我們在 Update 中直接處理所有輸入，這裡什麼都不做
    /// </summary>
    protected override void HandleCustomInput()
    {
        // 什麼都不做 - 所有輸入都在 Update 中處理
    }
    
    /// <summary>
    /// 處理物品欄的鍵盤輸入
    /// </summary>
    private void HandleInventoryInput()
    {
        // 只有在物品欄開啟時才處理輸入
        if (!IsOpen)
        {
            return;
        }
        
        if (InputSystemWrapper.Instance == null)
        {
            Debug.LogError("[InventoryManager] InputSystemWrapper instance not found!");
            return;
        }
        
        Vector2 navigation = InputSystemWrapper.Instance.GetUINavigationInput();
        bool confirmInput = InputSystemWrapper.Instance.GetUIConfirmDown();
        
        // 檢查是否有導航輸入並應用冷卻時間
        bool hasNavigationInput = Mathf.Abs(navigation.y) > 0.5f || Mathf.Abs(navigation.x) > 0.5f;
        
        if (hasNavigationInput)
        {
            // 只有當前時間超過了 (上次導航時間 + 冷卻時間) 才執行導航
            if (Time.unscaledTime > lastNavigationTime + navigationCooldown)
            {
                lastNavigationTime = Time.unscaledTime; // 更新上次導航時間
                
                // 上下箭頭鍵或W/S鍵導航
                if (navigation.y > 0.5f)
                {
                    Navigate(-1);
                }
                else if (navigation.y < -0.5f)
                {
                    Navigate(1);
                }
                // 左右箭頭鍵或A/D鍵切換模式
                else if (navigation.x < -0.5f)
                {
                    SwitchToItemListNavigation();
                }
                else if (navigation.x > 0.5f)
                {
                    SwitchToActionButtonsNavigation();
                }
            }
            // 如果在冷卻時間內，忽略導航輸入（不輸出調試訊息以避免日誌洪水）
        }
        
        // 確認輸入不受冷卻時間影響
        if (confirmInput)
        {
            ExecuteSelectedButton();
        }
        
        // 滑鼠滾輪滾動（只在道具列表模式下有效）
        if (!isItemSelected)
        {
            Vector2 scroll = InputSystemWrapper.Instance.GetUIScrollInput();
            
            // 滾輪輸入也應該有冷卻機制
            if (Mathf.Abs(scroll.y) > 0f)
            {
                if (Time.unscaledTime > lastNavigationTime + navigationCooldown)
                {
                    lastNavigationTime = Time.unscaledTime;
                    
                    if (scroll.y > 0f)
                    {
                        Navigate(-1);
                    }
                    else if (scroll.y < 0f)
                    {
                        Navigate(1);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 切換物品欄顯示
    /// </summary>
    public void ToggleInventory()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }
    
    /// <summary>
    /// 開啟物品欄
    /// </summary>
    public void OpenInventory()
    {
        Open();
    }
    
    /// <summary>
    /// 關閉物品欄
    /// </summary>
    public void CloseInventory()
    {
        Close();
    }
    
    /// <summary>
    /// 檢查是否有其他暫停遊戲的UI開啟
    /// </summary>
    /// <returns>如果有其他暫停UI返回true</returns>
    private bool HasOtherPausingUI()
    {
        // 檢查常見的暫停UI
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.IsOpen && GameMenuManager.Instance.PausesGame)
            return true;
            
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen && SaveUIController.Instance.PausesGame)
            return true;
            
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen && PlayerGameSettingsUI.Instance.PausesGame)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// 面板開啟時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnOpened()
    {
        base.OnOpened();
        Debug.Log("物品欄已開啟");
        
        // 更新UI以反映當前狀態
        UpdateAllUI();
    }
    
    /// <summary>
    /// 面板關閉時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnClosed()
    {
        base.OnClosed();
        Debug.Log("物品欄已關閉");
    }
    
    /// <summary>
    /// 設置UI組件
    /// </summary>
    private void SetupUI()
    {
        // 檢查必要的UI組件是否已設置
        if (itemListContent == null)
        {
            Debug.LogError("itemListContent 未設置！請在Inspector中指定itemListContent引用。");
        }
        
        // 設置按鈕事件
        if (useButton != null)
            useButton.onClick.AddListener(OnUseButtonClicked);
        if (discardButton != null)
            discardButton.onClick.AddListener(OnDiscardButtonClicked);
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        
        // 初始化UI狀態
        UpdateItemListUI();
        UpdateButtonStates();
    }
    
    /// <summary>
    /// 檢查是否應該阻止遊戲輸入
    /// </summary>
    // ==================== 多生物背包系統 ====================
    
    /// <summary>
    /// 初始化當前控制的生物
    /// </summary>
    private void InitializeWithCurrentCreature()
    {
        // 從SceneCreatureManager獲取當前控制的生物
        if (SceneCreatureManager.Instance != null)
        {
            IControllable controllable = SceneCreatureManager.Instance.CurrentControlledCreature;
            if (controllable is ControllableCreature creature)  SetCurrentCreature(creature);
        }
    }
    
    /// <summary>
    /// 設置當前顯示的生物背包
    /// </summary>
    /// <param name="creature">要顯示背包的生物</param>
    public void SetCurrentCreature(ControllableCreature creature)
    {
        currentCreature = creature;
        selectedItemIndex = -1; // 重置選擇
        UpdateAllUI();
        
        if (creature != null)
        {
            Debug.Log($"切換到 {creature.name} 的背包");
        }
        else
        {
            Debug.Log("沒有設置當前生物");
        }
    }
    
    /// <summary>
    /// 刷新當前背包UI（由Creature調用）
    /// </summary>
    public void RefreshCurrentInventoryUI()
    {
        UpdateAllUI();
    }
    
    /// <summary>
    /// 添加測試物品到當前生物
    /// </summary>
    private void AddTestItemsToCurrentCreature()
    {
        if (currentCreature?.Inventory == null)
        {
            Debug.LogWarning("當前生物或其背包為null，無法添加測試物品");
            return;
        }
        
        // 檢查背包是否已經有物品（來自序列化的起始物品）
        var existingItems = currentCreature.Inventory.GetAllItemsWithCounts();
        if (existingItems.Count > 0)
        {
            Debug.Log($"{currentCreature.name} 背包已有 {existingItems.Count} 個物品，跳過添加測試物品");
            return;
        }
        
        if (testItems == null || testItems.Count == 0)
        {
            Debug.LogWarning("測試物品列表為空，請在Inspector中設置testItems");
            return;
        }
        
        Debug.Log($"開始為 {currentCreature.name} 添加測試物品，共 {testItems.Count} 種物品");
        
        foreach (Item item in testItems)
        {
            if (item != null)
            {
                // 隨機數量 1-3
                int count = Random.Range(1, 4);
                int actualAdded = currentCreature.Inventory.AddItem(item, count);
                Debug.Log($"添加物品: {item.Name} x{count} (實際添加: {actualAdded})");
            }
            else
            {
                Debug.LogWarning("測試物品列表中有null項目");
            }
        }
        
        Debug.Log($"完成為 {currentCreature.name} 添加測試物品");
        
        // 強制更新UI
        UpdateAllUI();
    }
    
    /// <summary>
    /// 手動添加測試物品（公開方法，用於運行時測試）
    /// </summary>
    [ContextMenu("添加測試物品")]
    public void ManuallyAddTestItems()
    {
        AddTestItemsToCurrentCreature();
    }
    
    // ==================== 委託給當前生物背包的方法 ====================
    
    /// <summary>
    /// 添加物品到當前生物背包
    /// </summary>
    public int AddItem(Item item, int count)
    {
        if (CurrentInventory == null) return 0;
        return CurrentInventory.AddItem(item, count);
    }
    
    /// <summary>
    /// 從當前生物背包移除物品
    /// </summary>
    public int RemoveItem(Item item, int count)
    {
        if (CurrentInventory == null) return 0;
        return CurrentInventory.RemoveItem(item, count);
    }
    
    /// <summary>
    /// 檢查當前生物背包是否有足夠物品
    /// </summary>
    public bool HasItem(Item item, int count)
    {
        if (CurrentInventory == null) return false;
        return CurrentInventory.HasItem(item, count);
    }
    
    /// <summary>
    /// 獲取當前生物背包中物品數量
    /// </summary>
    public int GetItemCount(Item item)
    {
        if (CurrentInventory == null) return 0;
        return CurrentInventory.GetItemCount(item);
    }
    
    /// <summary>
    /// 使用當前生物背包中的物品
    /// </summary>
    public bool UseItem(Item item, GameObject user)
    {
        if (CurrentInventory == null) return false;
        return CurrentInventory.UseItem(item, user);
    }
    
    /// <summary>
    /// 格子點擊事件處理
    /// </summary>
    /// <param name="slot">被點擊的格子</param>
    /// <param name="isLeftClick">是否為左鍵點擊</param>
    public void OnSlotClicked(InventorySlot slot, bool isLeftClick)
    {
        if (slot == null) return;
        
        if (isLeftClick)
        {
            // 左鍵點擊 - 選擇格子
            SelectSlot(slot);
        }
        else
        {
            // 右鍵點擊 - 使用物品
            if (!slot.IsEmpty && slot.CurrentItem.IsUsable)
            {
                // 找到玩家物件
                GameObject player = FindPlayerObject();
                if (player != null)
                {
                    UseItem(slot.CurrentItem, player);
                }
            }
        }
    }
    
    /// <summary>
    /// 選擇格子（已棄用，保留用於向後兼容）
    /// </summary>
    /// <param name="slot">要選擇的格子</param>
    private void SelectSlot(InventorySlot slot)
    {
        // 這個方法已棄用，新架構使用索引選擇
        // 保留用於向後兼容
        Debug.LogWarning("SelectSlot方法已棄用，請使用新的索引選擇系統");
    }
    
    /// <summary>
    /// 清空當前生物背包
    /// </summary>
    public void ClearInventory()
    {
        if (CurrentInventory != null)
        {
            CurrentInventory.ClearInventory();
        }
        
        selectedItemIndex = -1;
        UpdateAllUI();
        Debug.Log("當前生物背包已清空");
    }
    
    /// <summary>
    /// 獲取空格子數量
    /// </summary>
    /// <returns>空格子數量</returns>
    public int GetEmptySlotCount()
    {
        if (CurrentInventory == null) return 0;
        return CurrentInventory.GetEmptySlotCount();
    }
    
    /// <summary>
    /// 檢查物品欄是否已滿
    /// </summary>
    /// <returns>是否已滿</returns>
    public bool IsInventoryFull()
    {
        if (CurrentInventory == null) return true;
        return CurrentInventory.IsInventoryFull();
    }
    
    /// <summary>
    /// 尋找玩家物件
    /// </summary>
    /// <returns>玩家物件</returns>
    private GameObject FindPlayerObject()
    {
        // 嘗試從SceneCreatureManager獲取當前控制的creature
        if (SceneCreatureManager.Instance != null && SceneCreatureManager.Instance.CurrentControlledCreature != null)
        {
            return SceneCreatureManager.Instance.CurrentControlledCreature.GetTransform().gameObject;
        }
        
        // 備用方案：尋找帶有Player標籤的物件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player;
        
        // 最後備用方案：尋找ControllableCreature組件
        ControllableCreature creature = FindFirstObjectByType<ControllableCreature>();
        if (creature != null) return creature.gameObject;
        
        return null;
    }
    
    /// <summary>
    /// 獲取物品欄狀態信息
    /// </summary>
    /// <returns>狀態信息字符串</returns>
    public string GetInventoryInfo()
    {
        if (CurrentInventory == null) return "沒有當前生物";
        return CurrentInventory.GetInventoryInfo();
    }
    
    // ==================== 新架構的四區域功能 ====================
    
    #region 1. 道具列表區域
    
    /// <summary>
    /// 更新道具列表UI
    /// </summary>
    private void UpdateItemListUI()
    {
        Debug.Log("UpdateItemListUI 被調用");
        
        if (itemListContent == null)
        {
            Debug.LogError("itemListContent 為 null！請在Inspector中設置itemListContent引用");
            return;
        }
        
        Debug.Log($"itemListContent 設置正確: {itemListContent.name}");
        
        // 確保Content有VerticalLayoutGroup組件
        EnsureLayoutGroup();
        
        // 清除舊的UI元素
        ClearItemListUI();
        
        // 獲取所有非空的物品
        var itemsWithCounts = GetAllItemsWithCounts();
        Debug.Log($"獲取到 {itemsWithCounts.Count} 個物品");
        
        // 添加詳細的物品信息調試
        if (currentCreature != null)
        {
            Debug.Log($"當前生物: {currentCreature.name}");
            Debug.Log($"生物背包: {currentCreature.Inventory != null}");
            if (currentCreature.Inventory != null)
            {
                Debug.Log($"背包槽位數: {currentCreature.Inventory.InventorySlots.Count}");
                foreach (var slot in currentCreature.Inventory.InventorySlots)
                {
                    if (!slot.IsEmpty)
                    {
                        Debug.Log($"  槽位有物品: {slot.CurrentItem.Name} x{slot.ItemCount}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("currentCreature 為 null");
        }
        
        // 更新allItems列表
        allItems.Clear();
        allItems.AddRange(itemsWithCounts);
        
        // 計算總頁數
        totalPages = allItems.Count > 0 ? Mathf.CeilToInt((float)allItems.Count / itemsPerPage) : 1;
        
        // 確保當前頁數在有效範圍內
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
        
        // 顯示當前頁的內容
        DisplayCurrentPage();
        
        // 更新分頁UI信息
        UpdatePaginationUI();
        
        Debug.Log($"UpdateItemListUI 完成，共 {allItems.Count} 個物品，分 {totalPages} 頁，當前第 {currentPage + 1} 頁");
    }
    
    /// <summary>
    /// 顯示當前頁的內容
    /// </summary>
    private void DisplayCurrentPage()
    {
        // 先清除現有的UI元素
        ClearItemListUI();
        
        if (allItems.Count == 0)
        {
            Debug.LogWarning("沒有物品可顯示！檢查當前生物背包是否有物品");
            Debug.Log($"當前生物: {currentCreature?.name ?? "null"}");
            Debug.Log($"當前背包: {CurrentInventory != null}");
            return;
        }
        
        // 計算當前頁要顯示的項目範圍
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, allItems.Count);
        
        // 創建當前頁的物品UI元素
        for (int i = startIndex; i < endIndex; i++)
        {
            var itemData = allItems[i];
            Debug.Log($"處理物品 {i}: {itemData.item.Name} x{itemData.count}");
            
            GameObject itemUI = CreateItemListElement(itemData.item, itemData.count, i);
            if (itemUI != null)
            {
                itemListUIElements.Add(itemUI);
                Debug.Log($"成功添加UI元素到列表，當前列表大小: {itemListUIElements.Count}");
            }
            else
            {
                Debug.LogError($"創建物品UI失敗: {itemData.item.Name}");
            }
        }
        
        // 強制刷新佈局
        ForceLayoutRefresh();
        
        // 更新導航按鈕列表（如果不在操作按鈕模式）
        if (!isItemSelected)
        {
            BuildNavigableButtons();
        }
        
        // 更新選中狀態
        UpdateItemListSelection();
    }
    
    /// <summary>
    /// 更新分頁UI狀態
    /// </summary>
    private void UpdatePaginationUI()
    {
        // 更新頁面信息文字
        if (pageInfoText != null)
        {
            if (allItems.Count == 0)
            {
                pageInfoText.text = "無物品";
            }
            else
            {
                pageInfoText.text = $"第 {currentPage + 1} 頁 / 共 {totalPages} 頁";
            }
        }
        
        Debug.Log($"[InventoryManager] 當前頁: {currentPage + 1}/{totalPages}, 總物品數: {allItems.Count}");
    }
    
    /// <summary>
    /// 確保Content有正確的佈局組件
    /// </summary>
    private void EnsureLayoutGroup()
    {
        if (itemListContent == null) return;
        
        // 檢查是否已有VerticalLayoutGroup
        VerticalLayoutGroup layoutGroup = itemListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            // 添加VerticalLayoutGroup組件
            layoutGroup = itemListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        // 設置佈局參數
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        
        // 使用Inspector中設置的spacing和padding
        layoutGroup.spacing = layoutSpacing;
        layoutGroup.padding = layoutPadding;
        
        // 確保有ContentSizeFitter來自動調整Content大小
        ContentSizeFitter sizeFitter = itemListContent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = itemListContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }
    
    /// <summary>
    /// 創建道具列表元素（使用prefab）
    /// </summary>
    private GameObject CreateItemListElement(Item item, int count, int index)
    {
        if (itemListContent == null || item == null || itemSlotPrefab == null) return null;
        
        // 實例化prefab
        GameObject itemUI = Instantiate(itemSlotPrefab, itemListContent);
        itemUI.name = $"Item_{index}_{item.Name}";
        
        // 修復scale問題 - 確保scale為(1,1,1)
        itemUI.transform.localScale = Vector3.one;
        
        // 修復RectTransform設置
        RectTransform rectTransform = itemUI.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 重置位置
            rectTransform.localPosition = Vector3.zero;
            
            // 設置錨點和pivot以確保正確的佈局
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            
            // 如果沒有設置高度，給一個預設高度
            if (rectTransform.sizeDelta.y <= 0)
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 50f);
            }
        }
        
        // 獲取Button組件並設置點擊事件
        Button button = itemUI.GetComponent<Button>();
        if (button == null)
        {
            button = itemUI.GetComponentInChildren<Button>();
        }
        
        if (button != null)
        {
            // 清除舊的事件監聽器
            button.onClick.RemoveAllListeners();
            // 添加新的點擊事件
            int itemIndex = index;
            button.onClick.AddListener(() => OnItemListClicked(itemIndex));
        }
        
        // 獲取文字組件並設置內容
        TextMeshProUGUI nameText = itemUI.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            string displayText = count > 1 ? $"{item.Name} x{count}" : item.Name;
            nameText.text = displayText;
        }
        
        // 獲取圖標組件並設置圖片（如果有的話）
        Image[] images = itemUI.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            // 假設第一個不是Button的Image是圖標
            if (img.gameObject != button?.gameObject && img.name.ToLower().Contains("icon"))
            {
                if (item.Icon != null)
                {
                    img.sprite = item.Icon;
                    img.gameObject.SetActive(true);
                }
                else
                {
                    img.gameObject.SetActive(false);
                }
                break;
            }
        }
        
        return itemUI;
    }
    
    /// <summary>
    /// 道具列表點擊事件
    /// </summary>
    private void OnItemListClicked(int index)
    {
        SelectItemByIndex(index);
    }
    
    /// <summary>
    /// 導航物品（支援自動分頁）
    /// </summary>
    /// <param name="direction">-1 向上，1 向下</param>
    private void NavigateItems(int direction)
    {
        if (allItems.Count == 0) return;

        // 計算當前頁有多少個項目
        int startIndex = currentPage * itemsPerPage;
        int itemsOnCurrentPage = Mathf.Min(itemsPerPage, allItems.Count - startIndex);
        
        // 如果總頁數只有一頁，則在頁內循環
        if (totalPages <= 1)
        {
            if (itemsOnCurrentPage <= 1) return; // 如果只有一個項目且只有一頁，不導航
            
            int currentIndexInPage = selectedItemIndex - startIndex;
            currentIndexInPage = (currentIndexInPage + direction + itemsOnCurrentPage) % itemsOnCurrentPage;
            selectedItemIndex = startIndex + currentIndexInPage;
        }
        else // --- 多頁情況下的換頁邏輯 ---
        {
            int currentIndexInPage = selectedItemIndex - startIndex;
            int newIndexInPage = currentIndexInPage + direction;

            if (newIndexInPage < 0) // 向上超出當前頁範圍
            {
                currentPage = (currentPage - 1 + totalPages) % totalPages;
                // 計算新頁面的起始索引
                int newPageStartIndex = currentPage * itemsPerPage;
                // 計算新頁面有多少項目
                int itemsOnNewPage = Mathf.Min(itemsPerPage, allItems.Count - newPageStartIndex);
                // 選中新頁面的最後一個項目
                selectedItemIndex = newPageStartIndex + itemsOnNewPage - 1;
                DisplayCurrentPage();
            }
            else if (newIndexInPage >= itemsOnCurrentPage) // 向下超出當前頁範圍
            {
                currentPage = (currentPage + 1) % totalPages;
                // 選中新頁面的第一個項目
                selectedItemIndex = currentPage * itemsPerPage;
                DisplayCurrentPage();
            }
            else // 正常在頁內導航
            {
                selectedItemIndex += direction;
            }
        }

        // 更新UI和狀態
        if (!isItemSelected)
        {
            // 在道具欄模式下，我們需要更新 currentNavIndex 來匹配頁內索引
            currentNavIndex = selectedItemIndex - (currentPage * itemsPerPage);
        }
        
        UpdateAllUIAfterNavigation();
    }

    /// <summary>
    /// 導航後統一更新所有相關UI
    /// </summary>
    private void UpdateAllUIAfterNavigation()
    {
        UpdateItemListSelection();
        UpdateItemPreview();
        UpdateItemDescription();
        UpdateButtonStates();
        UpdatePaginationUI();
        UpdateSelectionVisuals(); // 確保視覺選中狀態正確
        
        // 觸發選中事件
        if (SelectedItem != null)
        {
            OnItemSelected?.Invoke(SelectedItem);
        }
        
        Debug.Log($"[NavigateItems] 最終: 索引 {selectedItemIndex}, 頁面 {currentPage + 1}/{totalPages}");
    }
    
    /// <summary>
    /// 選擇上一個物品（保留用於向後兼容）
    /// </summary>
    private void SelectPreviousItem()
    {
        NavigateItems(-1);
    }
    
    /// <summary>
    /// 選擇下一個物品（保留用於向後兼容）
    /// </summary>
    private void SelectNextItem()
    {
        NavigateItems(1);
    }
    
    /// <summary>
    /// 根據索引選擇物品
    /// </summary>
    private void SelectItemByIndex(int index)
    {
        var items = GetAllItemsWithCounts();
        if (index < 0 || index >= items.Count) return;
        
        selectedItemIndex = index;
        UpdateAllUI();
    }
    
    /// <summary>
    /// 更新道具列表選中狀態
    /// </summary>
    private void UpdateItemListSelection()
    {
        // 計算當前頁要顯示的項目範圍
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, allItems.Count);
        
        // 更新當前頁顯示項目的文字
        for (int i = 0; i < itemListUIElements.Count; i++)
        {
            GameObject itemUI = itemListUIElements[i];
            if (itemUI == null) continue;
            
            int globalIndex = startIndex + i;
            if (globalIndex >= allItems.Count) break;
            
            var itemData = allItems[globalIndex];
            TextMeshProUGUI nameText = itemUI.GetComponentInChildren<TextMeshProUGUI>();
            
            if (nameText != null)
            {
                string displayText = itemData.count > 1 ? $"{itemData.item.Name} x{itemData.count}" : itemData.item.Name;
                
                // 如果是選中的項目，在前面加上">"符號
                if (globalIndex == selectedItemIndex)
                {
                    displayText = "> " + displayText;
                }
                
                nameText.text = displayText;
            }
        }
        
        // 滾動到選中項目（在當前頁面中的位置）
        ScrollToSelectedItemInCurrentPage();
    }
    
    /// <summary>
    /// 滾動到選中項目在當前頁面中的位置
    /// </summary>
    private void ScrollToSelectedItemInCurrentPage()
    {
        if (itemListScrollRect == null || selectedItemIndex < 0) return;
        
        // 計算選中項目在當前頁面中的索引
        int startIndex = currentPage * itemsPerPage;
        int selectedIndexInPage = selectedItemIndex - startIndex;
        
        // 如果選中項目不在當前頁面，不需要滾動
        if (selectedIndexInPage < 0 || selectedIndexInPage >= itemListUIElements.Count) return;
        
        // 計算滾動位置
        if (itemListUIElements.Count > 1)
        {
            float normalizedPosition = 1f - (float)selectedIndexInPage / (itemListUIElements.Count - 1);
            itemListScrollRect.verticalNormalizedPosition = normalizedPosition;
        }
    }
    
    /// <summary>
    /// 滾動到選中的物品（舊版本，保留向後兼容）
    /// </summary>
    private void ScrollToSelectedItem()
    {
        ScrollToSelectedItemInCurrentPage();
    }
    
    /// <summary>
    /// 清除道具列表UI
    /// </summary>
    private void ClearItemListUI()
    {
        if (itemListUIElements == null)
        {
            itemListUIElements = new List<GameObject>();
            return;
        }
        
        foreach (GameObject ui in itemListUIElements)
        {
            if (ui != null)
                DestroyImmediate(ui);
        }
        itemListUIElements.Clear();
    }
    
    /// <summary>
    /// 強制刷新佈局
    /// </summary>
    private void ForceLayoutRefresh()
    {
        if (itemListContent == null) return;
        
        // 強制重建佈局
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemListContent.GetComponent<RectTransform>());
        
        // 如果有ScrollRect，也刷新它
        if (itemListScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            itemListScrollRect.Rebuild(CanvasUpdate.PostLayout);
        }
        
        Debug.Log("強制刷新佈局完成");
    }
    
    #endregion
    
    #region 2. 道具預覽區域
    
    /// <summary>
    /// 更新道具預覽
    /// </summary>
    private void UpdateItemPreview()
    {
        if (itemPreviewArea == null) return;
        
        // 清除舊的預覽物件
        ClearPreviewArea();
        
        Item selectedItem = SelectedItem;
        if (selectedItem == null) return;
        
        // 暫時跳過3D預覽功能，因為Item類還沒有ItemPrefab屬性
        // 可以在未來添加ItemPrefab屬性到Item類後啟用此功能
        
        // TODO: 在Item類中添加ItemPrefab屬性後啟用以下代碼
        /*
        if (selectedItem.ItemPrefab != null)
        {
            currentPreviewObject = Instantiate(selectedItem.ItemPrefab, itemPreviewArea);
            
            // 調整預覽物件的位置和大小
            currentPreviewObject.transform.localPosition = Vector3.zero;
            currentPreviewObject.transform.localRotation = Quaternion.identity;
            currentPreviewObject.transform.localScale = Vector3.one;
            
            // 移除可能的碰撞器和剛體（預覽不需要物理）
            Collider[] colliders = currentPreviewObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            Rigidbody[] rigidbodies = currentPreviewObject.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.isKinematic = true;
            }
        }
        */
    }
    
    /// <summary>
    /// 清除預覽區域
    /// </summary>
    private void ClearPreviewArea()
    {
        if (currentPreviewObject != null)
        {
            DestroyImmediate(currentPreviewObject);
            currentPreviewObject = null;
        }
    }
    
    #endregion
    
    #region 3. 說明欄區域
    
    /// <summary>
    /// 更新物品說明
    /// </summary>
    private void UpdateItemDescription()
    {
        if (itemDescriptionText == null) return;
        
        Item selectedItem = SelectedItem;
        if (selectedItem == null)
        {
            itemDescriptionText.text = "";
            return;
        }
        
        // 構建說明文字
        string description = $"<b>{selectedItem.Name}</b>\n\n";
        description += $"{selectedItem.Description}\n\n";
        description += $"類型: {GetItemTypeString(selectedItem)}\n";
        description += $"數量: {GetItemCount(selectedItem)}\n";
        
        if (selectedItem.MaxStackSize > 1)
        {
            description += $"最大堆疊: {selectedItem.MaxStackSize}\n";
        }
        
        if (selectedItem.IsUsable)
        {
            description += "可使用\n";
        }
        
        if (selectedItem.IsConsumable)
        {
            description += "消耗品\n";
        }
        
        itemDescriptionText.text = description;
    }
    
    /// <summary>
    /// 獲取物品類型字串
    /// </summary>
    private string GetItemTypeString(Item item)
    {
        // 這裡可以根據Item的類型屬性返回對應的字串
        // 暫時返回基本描述
        if (item.IsConsumable) return "消耗品";
        if (item.IsUsable) return "可使用物品";
        return "一般物品";
    }
    
    #endregion
    
    #region 4. 選項欄區域
    
    /// <summary>
    /// 更新按鈕狀態
    /// </summary>
    private void UpdateButtonStates()
    {
        Item selectedItem = SelectedItem;
        bool hasSelectedItem = selectedItem != null;
        
        // 使用按鈕
        if (useButton != null)
        {
            useButton.interactable = hasSelectedItem && selectedItem.IsUsable;
            TextMeshProUGUI useButtonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
            if (useButtonText != null)
            {
                useButtonText.text = "使用 (U)";
            }
        }
        
        // 丟棄按鈕
        if (discardButton != null)
        {
            discardButton.interactable = hasSelectedItem;
            TextMeshProUGUI discardButtonText = discardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (discardButtonText != null)
            {
                discardButtonText.text = "丟棄 (D)";
            }
        }
        
        // 裝備/脫下按鈕
        if (equipButton != null)
        {
            bool isEquippable = hasSelectedItem && IsItemEquippable(selectedItem);
            bool isEquipped = hasSelectedItem && IsItemEquipped(selectedItem);
            
            equipButton.interactable = isEquippable;
            TextMeshProUGUI equipButtonText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (equipButtonText != null)
            {
                equipButtonText.text = isEquipped ? "脫下 (E)" : "裝備 (E)";
            }
        }
    }
    
    /// <summary>
    /// 使用按鈕點擊事件
    /// </summary>
    private void OnUseButtonClicked()
    {
        Item selectedItem = SelectedItem;
        if (selectedItem == null || !selectedItem.IsUsable) return;
        
        GameObject player = FindPlayerObject();
        if (player != null)
        {
            bool success = UseItem(selectedItem, player);
            if (success)
            {
                OnItemUsed?.Invoke(selectedItem);
                UpdateAllUI(); // 更新UI以反映物品數量變化
            }
        }
    }
    
    /// <summary>
    /// 丟棄按鈕點擊事件
    /// </summary>
    private void OnDiscardButtonClicked()
    {
        Item selectedItem = SelectedItem;
        if (selectedItem == null) return;
        
        // 丟棄一個物品
        int removed = RemoveItem(selectedItem, 1);
        if (removed > 0)
        {
            OnItemDiscarded?.Invoke(selectedItem);
            UpdateAllUI();
            Debug.Log($"丟棄了 {selectedItem.Name}");
        }
    }
    
    /// <summary>
    /// 裝備/脫下按鈕點擊事件
    /// </summary>
    private void OnEquipButtonClicked()
    {
        Item selectedItem = SelectedItem;
        if (selectedItem == null || !IsItemEquippable(selectedItem)) return;
        
        bool isCurrentlyEquipped = IsItemEquipped(selectedItem);
        
        // 切換裝備狀態
        SetItemEquipped(selectedItem, !isCurrentlyEquipped);
        
        OnItemEquipped?.Invoke(selectedItem, !isCurrentlyEquipped);
        UpdateButtonStates();
        
        Debug.Log($"{(!isCurrentlyEquipped ? "裝備" : "脫下")}了 {selectedItem.Name}");
    }
    
    #endregion
    
    #region 導航系統（參考SaveUI）
    
    /// <summary>
    /// 根據當前狀態建立可導航的按鈕列表
    /// </summary>
    private void BuildNavigableButtons()
    {
        navigableButtons.Clear();
        
        if (isItemSelected)
        {
            // 狀態2: 已選中道具，可導航按鈕為操作按鈕
            if (useButton != null) navigableButtons.Add(useButton);
            if (discardButton != null) navigableButtons.Add(discardButton);
            if (equipButton != null) navigableButtons.Add(equipButton);
        }
        else
        {
            // 狀態1: 預設狀態，可導航按鈕為道具項目按鈕
            foreach (var itemUI in itemListUIElements)
            {
                if (itemUI != null)
                {
                    Button button = itemUI.GetComponent<Button>();
                    if (button == null) button = itemUI.GetComponentInChildren<Button>();
                    if (button != null)
                    {
                        navigableButtons.Add(button);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 設定預設選中的項目
    /// </summary>
    private void SetDefaultSelection()
    {
        BuildNavigableButtons();
        
        if (navigableButtons.Count > 0)
        {
            currentNavIndex = 0;
        }
        else
        {
            currentNavIndex = -1;
        }
        
        UpdateSelectionVisuals();
    }
    
    /// <summary>
    /// 導航到上一個或下一個按鈕
    /// </summary>
    /// <param name="direction">-1 為上, 1 為下</param>
    private void Navigate(int direction)
    {
        if (isItemSelected)
        {
            // 在操作按鈕間導航
            NavigateActionButtons(direction);
        }
        else
        {
            // 在道具列表間導航（支援自動分頁）
            NavigateItems(direction);
        }
    }
    
    /// <summary>
    /// 在操作按鈕間導航
    /// </summary>
    private void NavigateActionButtons(int direction)
    {
        if (navigableButtons.Count <= 1) return;
        
        currentNavIndex += direction;
        
        // 循環導航
        if (currentNavIndex < 0) currentNavIndex = navigableButtons.Count - 1;
        if (currentNavIndex >= navigableButtons.Count) currentNavIndex = 0;
        
        UpdateSelectionVisuals();
    }
    
    /// <summary>
    /// 執行當前選中的按鈕
    /// </summary>
    private void ExecuteSelectedButton()
    {
        if (isItemSelected)
        {
            // 在操作按鈕模式下，執行選中的操作按鈕
            if (currentNavIndex >= 0 && currentNavIndex < navigableButtons.Count)
            {
                Button selectedButton = navigableButtons[currentNavIndex];
                if (selectedButton != null && selectedButton.interactable)
                {
                    selectedButton.onClick.Invoke();
                }
            }
        }
        else
        {
            // 在道具列表模式下，選中道具並切換到操作按鈕模式
            if (allItems.Count > 0)
            {
                SwitchToActionButtonsNavigation();
            }
        }
    }
    
    /// <summary>
    /// 切換到道具列表導航模式
    /// </summary>
    private void SwitchToItemListNavigation()
    {
        if (!isItemSelected || allItems.Count == 0) return;
        
        Debug.Log("[Navigation] 切換到道具列表導航模式");
        
        isItemSelected = false;
        SetDefaultSelection();
    }
    
    /// <summary>
    /// 切換到操作按鈕導航模式
    /// </summary>
    private void SwitchToActionButtonsNavigation()
    {
        if (isItemSelected || allItems.Count == 0) return;
        
        Debug.Log("[Navigation] 切換到操作按鈕導航模式");
        
        isItemSelected = true;
        currentNavIndex = 0; // 預設選中第一個操作按鈕
        BuildNavigableButtons();
        UpdateSelectionVisuals();
    }
    
    /// <summary>
    /// 更新所有按鈕的視覺表現
    /// </summary>
    private void UpdateSelectionVisuals()
    {
        // 1. 重置所有道具項目的文字為原始狀態
        UpdateItemListSelection();
        
        // 2. 重置所有操作按鈕的文字為原始狀態
        ResetActionButtonTexts();
        
        // 3. 為當前導航選中的按鈕/項目加上前綴
        if (isItemSelected)
        {
            // 操作按鈕模式：為選中的操作按鈕加上前綴
            if (currentNavIndex >= 0 && currentNavIndex < navigableButtons.Count)
            {
                Button selectedButton = navigableButtons[currentNavIndex];
                AddPrefixToButton(selectedButton, "> ");
            }
        }
        // 道具列表模式的選中狀態已在UpdateItemListSelection中處理
    }
    
    /// <summary>
    /// 重置操作按鈕的文字
    /// </summary>
    private void ResetActionButtonTexts()
    {
        if (useButton != null)
        {
            var text = useButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = "使用 (U)";
        }
        
        if (discardButton != null)
        {
            var text = discardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = "丟棄 (D)";
        }
        
        if (equipButton != null)
        {
            var text = equipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                Item selectedItem = SelectedItem;
                bool isEquipped = selectedItem != null && IsItemEquipped(selectedItem);
                text.text = isEquipped ? "脫下 (E)" : "裝備 (E)";
            }
        }
    }
    
    /// <summary>
    /// 為按鈕添加前綴
    /// </summary>
    private void AddPrefixToButton(Button button, string prefix)
    {
        if (button == null) return;
        
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            string currentText = text.text;
            if (!currentText.StartsWith(prefix))
            {
                text.text = prefix + currentText;
            }
        }
    }
    
    #endregion
    
    #region 輔助方法
    
    /// <summary>
    /// 獲取所有物品及其數量
    /// </summary>
    private List<(Item item, int count)> GetAllItemsWithCounts()
    {
        if (CurrentInventory == null)
        {
            return new List<(Item item, int count)>();
        }
        
        return CurrentInventory.GetAllItemsWithCounts();
    }
    
    /// <summary>
    /// 更新所有UI
    /// </summary>
    private void UpdateAllUI()
    {
        UpdateItemListUI();
        UpdateItemPreview();
        UpdateItemDescription();
        UpdateButtonStates();
        
        // 初始化導航狀態
        isItemSelected = false;
        SetDefaultSelection();
        
        // 觸發選中事件
        if (SelectedItem != null)
        {
            OnItemSelected?.Invoke(SelectedItem);
        }
    }
    
    /// <summary>
    /// 檢查物品是否可裝備
    /// </summary>
    private bool IsItemEquippable(Item item)
    {
        // 這裡可以根據物品的屬性判斷是否可裝備
        // 暫時簡單判斷
        return item != null && !item.IsConsumable;
    }
    
    /// <summary>
    /// 檢查物品是否已裝備
    /// </summary>
    private bool IsItemEquipped(Item item)
    {
        // 這裡需要實現裝備系統的邏輯
        // 暫時返回false
        return false;
    }
    
    /// <summary>
    /// 設置物品裝備狀態
    /// </summary>
    private void SetItemEquipped(Item item, bool equipped)
    {
        // 這裡需要實現裝備系統的邏輯
        // 暫時只是記錄
    }
    
    #endregion
}
