using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;

/// <summary>
/// 玩家遊戲設置 UI 控制器
/// 管理影像設定和音訊設定的 UI 組件與互動邏輯
/// </summary>
public class PlayerGameSettingsUI : UIPanel
{
    public static PlayerGameSettingsUI Instance { get; private set; }
    
    [Header("影像設定")]
    [SerializeField] private OptionSwitcher resolutionSwitcher;
    [SerializeField] private OptionSwitcher fpsLimitSwitcher;
    [SerializeField] private OptionSwitcher vSyncSwitcher;
    [SerializeField] private OptionSwitcher antiAliasingSwitcher;
    
    [Header("音訊設定")]
    [SerializeField] private OptionSwitcher masterVolumeSwitcher;
    
    [Header("語言設定")]
    [SerializeField] private OptionSwitcher languageSwitcher;
    
    [Header("UI 控制")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;
    
    [Header("本地化組件")]
    [SerializeField] private TextMeshProUGUI resolutionLabel;
    [SerializeField] private TextMeshProUGUI fpsLimitLabel;
    [SerializeField] private TextMeshProUGUI vSyncLabel;
    [SerializeField] private TextMeshProUGUI antiAliasingLabel;
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI languageLabel;
    [SerializeField] private TextMeshProUGUI resetButtonText;
    [SerializeField] private TextMeshProUGUI closeButtonText;
    
    // 設定值快取
    private int currentResolutionIndex = 0;
    private int currentFPSLimit = 60;
    private bool currentVSync = true;
    private int currentAntiAliasing = 2;
    private float currentMasterVolume = 1.0f;
    private int currentLanguageIndex = 0;
    
    // 輸入導航控制
    private float lastNavigationTime = 0f;
    
    // 鍵盤導航系統
    private enum NavigationItemType { OptionSwitcher, Button }
    private struct NavigationItem
    {
        public NavigationItemType type;
        public OptionSwitcher optionSwitcher;
        public Button button;
        public string name;
        
        public NavigationItem(OptionSwitcher switcher, string itemName)
        {
            type = NavigationItemType.OptionSwitcher;
            optionSwitcher = switcher;
            button = null;
            name = itemName;
        }
        
        public NavigationItem(Button btn, string itemName)
        {
            type = NavigationItemType.Button;
            optionSwitcher = null;
            button = btn;
            name = itemName;
        }
    }
    
    private List<NavigationItem> navigableItems = new List<NavigationItem>();
    private int currentNavIndex = -1;
    
    [Header("導航設定")]
    [SerializeField] private float navigationCooldown = 0.2f;
    [SerializeField] private string selectedPrefix = "> ";
    
    public bool IsSettingsUIOpen => IsOpen;
    
    protected override void Awake()
    {
        // 調用基類的 Awake 方法
        base.Awake();
        
        // 設定UIPanel屬性
        pauseGameWhenOpen = true;   // 設定UI暫停遊戲
        blockCharacterMovement = true;  // 阻擋角色移動
        canCloseWithEscape = true;  // 可用ESC關閉
        debugMode = true;  // 啟用調試模式以監控UI狀態變化
        
        // 檢查是否已經有Instance且不是當前物件
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PlayerGameSettingsUI已存在，銷毀重複實例");
            Destroy(gameObject);
            return;
        }
        
        // 如果Instance為null或是當前物件，則設置為Instance
        if (Instance == null)
        {
            Instance = this;
            
            // 安全地調用DontDestroyOnLoad
            try
            {
                DontDestroyOnLoad(gameObject);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"DontDestroyOnLoad失敗: {e.Message}");
            }
        }
    }

    private void Start()
    {
        // 確保面板初始為關閉狀態
        if (panelCanvas != null)
        {
            panelCanvas.enabled = false;
        }
        isOpen = false;
        
        // 延遲初始化以確保所有 OptionSwitcher 組件都已準備好
        StartCoroutine(DelayedInitialization());
    }
    
    /// <summary>
    /// 延遲初始化以確保組件順序正確
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // 等待一幀確保所有 OptionSwitcher 的 Awake 都已執行
        yield return null;
        
        // 在 Start() 中進行 UI 初始化，確保組件引用已正確設置
        InitializeUIComponents();
        LoadCurrentSettings();
        SetupEventListeners();
        
        // 初始化導航系統
        InitializeNavigationSystem();
        
        // 初始化本地化系統
        InitializeLocalization();
    }
    
    /// <summary>
    /// 處理自定義輸入 - 重寫UIPanel方法
    /// </summary>
    protected override void HandleCustomInput()
    {
        HandleKeyboardNavigation();
    }
    
    /// <summary>
    /// 處理鍵盤導航輸入
    /// </summary>
    private void HandleKeyboardNavigation()
    {
        if (InputSystemWrapper.Instance == null)
        {
            Debug.LogError("[PlayerGameSettingsUI] InputSystemWrapper instance not found!");
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
                
                // 上下鍵：導航不同項目
                if (navigation.y > 0.5f)
                {
                    Navigate(-1); // 向上
                }
                else if (navigation.y < -0.5f)
                {
                    Navigate(1); // 向下
                }
                // 左右鍵：調整當前項目的設置
                else if (navigation.x < -0.5f)
                {
                    AdjustCurrentSetting(-1); // 向左/減少
                }
                else if (navigation.x > 0.5f)
                {
                    AdjustCurrentSetting(1); // 向右/增加
                }
            }
            // 如果在冷卻時間內，忽略導航輸入
        }
        
        // 確認輸入不受冷卻時間影響
        if (confirmInput)
        {
            ExecuteSelectedItem();
        }
    }
    
    /// <summary>
    /// 初始化 UI 組件
    /// </summary>
    private void InitializeUIComponents()
    {
        // 初始化解析度選項切換器
        if (resolutionSwitcher != null)
        {
            // 獲取系統支援的解析度並建立選項
            Resolution[] availableResolutions = Screen.resolutions;
            List<string> resolutionOptions = new List<string>();
            
            // 添加常見解析度選項
            string[] commonResolutions = { "1920x1080", "1600x900", "1366x768", "1280x720", "1024x768" };
            foreach (string resolution in commonResolutions)
            {
                resolutionOptions.Add(resolution);
            }
            
            resolutionSwitcher.SetOptions(resolutionOptions);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] resolutionSwitcher 引用為 null！請在 Inspector 中設置。");
        }
        
        // 初始化 FPS 限制選項切換器
        if (fpsLimitSwitcher != null)
        {
            // 選項文字將由本地化系統設定，這裡先設定臨時選項
            fpsLimitSwitcher.SetOptions(new System.Collections.Generic.List<string>
            {
                "30 FPS", "60 FPS", "120 FPS", "144 FPS", "無限制"
            });
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] fpsLimitSwitcher 引用為 null！請在 Inspector 中設置。");
        }
        
        // 初始化垂直同步選項切換器
        if (vSyncSwitcher != null)
        {
            // 選項文字將由本地化系統設定，這裡先設定臨時選項
            vSyncSwitcher.SetOptions(new System.Collections.Generic.List<string>
            {
                "關閉", "開啟"
            });
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] vSyncSwitcher 引用為 null！請在 Inspector 中設置。");
        }
        
        // 初始化抗鋸齒選項切換器
        if (antiAliasingSwitcher != null)
        {
            // 選項文字將由本地化系統設定，這裡先設定臨時選項
            antiAliasingSwitcher.SetOptions(new System.Collections.Generic.List<string>
            {
                "關閉", "MSAA 2x", "MSAA 4x", "MSAA 8x"
            });
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] antiAliasingSwitcher 引用為 null！請在 Inspector 中設置。");
        }
        
        // 初始化音量選項切換器
        if (masterVolumeSwitcher != null)
        {
            masterVolumeSwitcher.SetOptions(new System.Collections.Generic.List<string>
            {
                "0%", "10%", "20%", "30%", "40%", "50%", 
                "60%", "70%", "80%", "90%", "100%"
            });
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] masterVolumeSwitcher 引用為 null！請在 Inspector 中設置。");
        }
        
        // 初始化語言選項切換器
        if (languageSwitcher != null)
        {
            // 選項文字將由本地化系統設定，這裡先設定臨時選項
            languageSwitcher.SetOptions(new System.Collections.Generic.List<string>
            {
                "繁體中文", "簡體中文", "English", "日本語"
            });
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] languageSwitcher 引用為 null！請在 Inspector 中設置。");
        }
        
        Debug.Log("[PlayerGameSettingsUI] UI 組件初始化完成");
    }
    
    /// <summary>
    /// 載入當前設定值
    /// </summary>
    private void LoadCurrentSettings()
    {
        if (GameSettings.Instance == null)
        {
            Debug.LogWarning("[PlayerGameSettingsUI] GameSettings.Instance 為 null，使用預設設定");
            return;
        }
        
        // 從 GameSettings 載入所有設定
        currentResolutionIndex = GameSettings.Instance.ResolutionIndex;
        currentFPSLimit = GameSettings.Instance.FPSLimit;
        currentVSync = GameSettings.Instance.VSyncEnabled;
        currentAntiAliasing = GameSettings.Instance.AntiAliasingLevel;
        currentMasterVolume = GameSettings.Instance.MasterVolume;
        string currentLanguage = GameSettings.Instance.Language;
        currentLanguageIndex = GetLanguageIndex(currentLanguage);
        
        // 更新 UI 組件顯示
        if (resolutionSwitcher != null)
        {
            resolutionSwitcher.CurrentIndex = currentResolutionIndex;
        }
            
        if (fpsLimitSwitcher != null)
        {
            int fpsIndex = GetFPSLimitIndex(currentFPSLimit);
            fpsLimitSwitcher.CurrentIndex = fpsIndex;
        }
            
        if (vSyncSwitcher != null)
        {
            int vSyncIndex = GetVSyncIndex(currentVSync);
            vSyncSwitcher.CurrentIndex = vSyncIndex;
        }
            
        if (antiAliasingSwitcher != null)
        {
            antiAliasingSwitcher.CurrentIndex = currentAntiAliasing;
        }
        
        if (masterVolumeSwitcher != null)
        {
            int volumeIndex = GetVolumeIndex(currentMasterVolume);
            masterVolumeSwitcher.CurrentIndex = volumeIndex;
        }
        
        if (languageSwitcher != null)
        {
            languageSwitcher.CurrentIndex = currentLanguageIndex;
        }
        
    }
    
    /// <summary>
    /// 設定事件監聽器
    /// </summary>
    private void SetupEventListeners()
    {
        // 影像設定事件
        if (resolutionSwitcher != null)
        {
            resolutionSwitcher.OnValueChanged.AddListener(OnResolutionChanged);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 resolutionSwitcher 設定事件監聽器（引用為 null）");
        }
            
        if (fpsLimitSwitcher != null)
        {
            fpsLimitSwitcher.OnValueChanged.AddListener(OnFPSLimitChanged);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 fpsLimitSwitcher 設定事件監聽器（引用為 null）");
        }
            
        if (vSyncSwitcher != null)
        {
            vSyncSwitcher.OnValueChanged.AddListener(OnVSyncChanged);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 vSyncSwitcher 設定事件監聽器（引用為 null）");
        }
            
        if (antiAliasingSwitcher != null)
        {
            antiAliasingSwitcher.OnValueChanged.AddListener(OnAntiAliasingChanged);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 antiAliasingSwitcher 設定事件監聽器（引用為 null）");
        }
        
        // 音訊設定事件
        if (masterVolumeSwitcher != null)
        {
            masterVolumeSwitcher.OnValueChanged.AddListener(OnMasterVolumeChanged);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 masterVolumeSwitcher 設定事件監聽器（引用為 null）");
        }
        
        // 語言設定事件
        if (languageSwitcher != null)
        {
            languageSwitcher.OnValueChanged.AddListener(OnLanguageChanged);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 languageSwitcher 設定事件監聽器（引用為 null）");
        }
        
        // 按鈕事件
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetSettings);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 resetButton 設定事件監聽器（引用為 null）");
        }
            
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseSettings);
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法為 closeButton 設定事件監聽器（引用為 null）");
        }
    }
    
    #region 影像設定回調
    private void OnResolutionChanged(int index)
    {
        currentResolutionIndex = index;
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ResolutionIndex = index;
        }
    }
    
    private void OnFPSLimitChanged(int index)
    {
        currentFPSLimit = GetFPSLimitValue(index);
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.FPSLimit = currentFPSLimit;
        }
    }
    
    private void OnVSyncChanged(int index)
    {
        currentVSync = GetVSyncValue(index);
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.VSyncEnabled = currentVSync;
        }
    }
    
    private void OnAntiAliasingChanged(int index)
    {
        currentAntiAliasing = index;
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.AntiAliasingLevel = index;
        }
    }
    #endregion
    
    #region 音訊設定回調
    private void OnMasterVolumeChanged(int index)
    {
        currentMasterVolume = GetVolumeValue(index);
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.MasterVolume = currentMasterVolume;
        }
    }
    #endregion
    
    #region 語言設定回調
    private void OnLanguageChanged(int index)
    {
        currentLanguageIndex = index;
        string languageCode = GetLanguageCode(index);
        
        // 只需要設定GameSettings的語言屬性，這會自動觸發OnLanguageChanged事件
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.Language = languageCode;
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] GameSettings.Instance 為 null！");
        }
    }
    #endregion
    
    #region 按鈕回調
    private void OnResetSettings()
    {
        // 重置到預設值
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ResetToDefaults();
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] GameSettings.Instance 為 null，無法重置設定");
            return;
        }
        
        // 重新載入設定到 UI
        LoadCurrentSettings();
        
        // 確保UI狀態保持正確（面板應該保持開啟且遊戲暫停）
        if (IsOpen && pauseGameWhenOpen && Time.timeScale != 0f)
        {
            Debug.LogWarning("[PlayerGameSettingsUI] 修正：重置後遊戲意外恢復，重新暫停遊戲");
            Time.timeScale = 0f;
        }
        
        Debug.Log($"[PlayerGameSettingsUI] 重置後UI狀態: 開啟={IsOpen}, 遊戲暫停={Time.timeScale == 0f}");
    }
    
    private void OnCloseSettings()
    {
        CloseSettings();
    }
    #endregion
    
    #region 公開UI控制方法
    /// <summary>
    /// 開啟設定UI
    /// </summary>
    public void OpenSettings()
    {
        Open(); // 使用UIPanel的Open方法
    }
    
    /// <summary>
    /// 關閉設定UI並返回GameMenu
    /// </summary>
    public void CloseSettings()
    {
        Close(); // 使用UIPanel的Close方法
    }
    
    /// <summary>
    /// 面板開啟時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnOpened()
    {
        // 設定UI已開啟
        
        // 重新初始化導航系統
        if (navigableItems.Count > 0)
        {
            SetDefaultSelection();
        }
    }
    
    /// <summary>
    /// 面板關閉時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnClosed()
    {
        // 清除所有視覺選中狀態
        ResetAllItemsVisual();
        
        // 返回到GameMenu
        if (GameMenuManager.Instance != null)
        {
            GameMenuManager.Instance.OpenGameMenu();
        }
    }
    
    /// <summary>
    /// 處理ESC鍵邏輯 - 重寫UIPanel方法
    /// </summary>
    protected override void HandleEscapeKey()
    {
        // ESC鍵關閉設定並返回GameMenu
        CloseSettings();
    }
    #endregion
    
    #region 輔助方法
    private int GetFPSLimitIndex(int fps)
    {
        switch (fps)
        {
            case 30: return 0;
            case 60: return 1;
            case 120: return 2;
            case 144: return 3;
            default: return 4; // 無限制
        }
    }
    
    private int GetFPSLimitValue(int index)
    {
        switch (index)
        {
            case 0: return 30;
            case 1: return 60;
            case 2: return 120;
            case 3: return 144;
            default: return -1; // 無限制
        }
    }
    
    /// <summary>
    /// 根據 VSync 布爾值獲取下拉選單索引
    /// </summary>
    private int GetVSyncIndex(bool enabled)
    {
        return enabled ? 1 : 0; // 0: 關閉, 1: 開啟
    }
    
    /// <summary>
    /// 根據下拉選單索引獲取 VSync 布爾值
    /// </summary>
    private bool GetVSyncValue(int index)
    {
        return index == 1; // 0: 關閉, 1: 開啟
    }
    
    /// <summary>
    /// 根據音量浮點值獲取下拉選單索引
    /// </summary>
    private int GetVolumeIndex(float volume)
    {
        // 將 0.0-1.0 範圍轉換為 0-10 索引
        return Mathf.RoundToInt(volume * 10f);
    }
    
    /// <summary>
    /// 根據下拉選單索引獲取音量浮點值
    /// </summary>
    private float GetVolumeValue(int index)
    {
        // 將 0-10 索引轉換為 0.0-1.0 範圍
        return index / 10f;
    }
    
    /// <summary>
    /// 根據下拉選單索引獲取語言代碼
    /// </summary>
    private string GetLanguageCode(int index)
    {
        switch (index)
        {
            case 0: return "zh-TW"; // 繁體中文
            case 1: return "zh-CN"; // 簡體中文
            case 2: return "en";    // English
            case 3: return "ja";    // 日本語
            default: return "zh-TW";
        }
    }
    
    /// <summary>
    /// 根據語言代碼獲取下拉選單索引
    /// </summary>
    private int GetLanguageIndex(string languageCode)
    {
        switch (languageCode)
        {
            case "zh-TW": return 0;
            case "zh-CN": return 1;
            case "en": return 2;
            case "ja": return 3;
            default: return 0;
        }
    }
    #endregion
    
    #region 公開方法
    /// <summary>
    /// 取得當前影像設定
    /// </summary>
    public (int resolutionIndex, int fpsLimit, bool vSync, int antiAliasing) GetGraphicsSettings()
    {
        return (currentResolutionIndex, currentFPSLimit, currentVSync, currentAntiAliasing);
    }
    
    /// <summary>
    /// 取得當前音訊設定
    /// </summary>
    public float GetAudioSettings()
    {
        return currentMasterVolume;
    }
    
    /// <summary>
    /// 取得當前語言設定
    /// </summary>
    public int GetLanguageSettings()
    {
        return currentLanguageIndex;
    }
    
    /// <summary>
    /// 設定所有 UI 組件的可互動狀態
    /// </summary>
    public void SetUIInteractable(bool interactable)
    {
        if (resolutionSwitcher != null) resolutionSwitcher.SetInteractable(interactable);
        if (fpsLimitSwitcher != null) fpsLimitSwitcher.SetInteractable(interactable);
        if (vSyncSwitcher != null) vSyncSwitcher.SetInteractable(interactable);
        if (antiAliasingSwitcher != null) antiAliasingSwitcher.SetInteractable(interactable);
        if (masterVolumeSwitcher != null) masterVolumeSwitcher.SetInteractable(interactable);
        if (languageSwitcher != null) languageSwitcher.SetInteractable(interactable);
        if (resetButton != null) resetButton.interactable = interactable;
        if (closeButton != null) closeButton.interactable = interactable;
    }
    #endregion
    
    #region 鍵盤導航系統
    
    /// <summary>
    /// 初始化導航系統
    /// </summary>
    private void InitializeNavigationSystem()
    {
        BuildNavigableItems();
        SetDefaultSelection();
    }
    
    /// <summary>
    /// 建立可導航項目列表
    /// </summary>
    private void BuildNavigableItems()
    {
        navigableItems.Clear();
        
        // 添加所有設置項目（按順序）
        if (resolutionSwitcher != null)
            navigableItems.Add(new NavigationItem(resolutionSwitcher, "解析度"));
        if (fpsLimitSwitcher != null)
            navigableItems.Add(new NavigationItem(fpsLimitSwitcher, "FPS限制"));
        if (vSyncSwitcher != null)
            navigableItems.Add(new NavigationItem(vSyncSwitcher, "垂直同步"));
        if (antiAliasingSwitcher != null)
            navigableItems.Add(new NavigationItem(antiAliasingSwitcher, "抗鋸齒"));
        if (masterVolumeSwitcher != null)
            navigableItems.Add(new NavigationItem(masterVolumeSwitcher, "音量"));
        if (languageSwitcher != null)
            navigableItems.Add(new NavigationItem(languageSwitcher, "語言"));
        
        // 添加按鈕項目
        if (resetButton != null)
            navigableItems.Add(new NavigationItem(resetButton, "重置"));
        if (closeButton != null)
            navigableItems.Add(new NavigationItem(closeButton, "返回"));
    }
    
    /// <summary>
    /// 設置預設選中項目
    /// </summary>
    private void SetDefaultSelection()
    {
        if (navigableItems.Count > 0)
        {
            currentNavIndex = 0; // 預設選中第一個項目
            UpdateSelectionVisuals();
        }
        else
        {
            currentNavIndex = -1;
        }
    }
    
    /// <summary>
    /// 導航到上一個或下一個項目
    /// </summary>
    /// <param name="direction">-1 為向上, 1 為向下</param>
    private void Navigate(int direction)
    {
        if (navigableItems.Count <= 1) return;
        
        // 取消當前項目的選中狀態
        SetCurrentItemSelected(false);
        
        // 移動到新項目
        currentNavIndex += direction;
        
        // 循環導航
        if (currentNavIndex < 0) currentNavIndex = navigableItems.Count - 1;
        if (currentNavIndex >= navigableItems.Count) currentNavIndex = 0;
        
        // 更新選中狀態
        UpdateSelectionVisuals();
    }
    
    /// <summary>
    /// 調整當前選中項目的設置
    /// </summary>
    /// <param name="direction">-1 為左/減少, 1 為右/增加</param>
    private void AdjustCurrentSetting(int direction)
    {
        if (currentNavIndex < 0 || currentNavIndex >= navigableItems.Count) return;
        
        NavigationItem currentItem = navigableItems[currentNavIndex];
        
        // 只有 OptionSwitcher 項目可以調整
        if (currentItem.type == NavigationItemType.OptionSwitcher && currentItem.optionSwitcher != null)
        {
            currentItem.optionSwitcher.AdjustValue(direction);
        }
    }
    
    /// <summary>
    /// 執行當前選中的項目
    /// </summary>
    private void ExecuteSelectedItem()
    {
        if (currentNavIndex < 0 || currentNavIndex >= navigableItems.Count) return;
        
        NavigationItem currentItem = navigableItems[currentNavIndex];
        
        // 只有按鈕項目可以執行
        if (currentItem.type == NavigationItemType.Button && currentItem.button != null)
        {
            if (currentItem.button.interactable)
            {
                currentItem.button.onClick.Invoke();
            }
        }
    }
    
    /// <summary>
    /// 設置當前項目的選中狀態
    /// </summary>
    /// <param name="selected">是否選中</param>
    private void SetCurrentItemSelected(bool selected)
    {
        if (currentNavIndex < 0 || currentNavIndex >= navigableItems.Count) return;
        
        NavigationItem currentItem = navigableItems[currentNavIndex];
        
        if (currentItem.type == NavigationItemType.OptionSwitcher && currentItem.optionSwitcher != null)
        {
            currentItem.optionSwitcher.SetSelected(selected);
        }
    }
    
    /// <summary>
    /// 更新選中視覺狀態
    /// </summary>
    private void UpdateSelectionVisuals()
    {
        // 重置所有項目的視覺狀態
        ResetAllItemsVisual();
        
        // 設置當前選中項目的視覺狀態
        if (currentNavIndex >= 0 && currentNavIndex < navigableItems.Count)
        {
            NavigationItem currentItem = navigableItems[currentNavIndex];
            
            if (currentItem.type == NavigationItemType.OptionSwitcher && currentItem.optionSwitcher != null)
            {
                // OptionSwitcher 顯示箭頭
                currentItem.optionSwitcher.SetSelected(true);
            }
            else if (currentItem.type == NavigationItemType.Button && currentItem.button != null)
            {
                // Button 顯示前綴
                AddPrefixToButton(currentItem.button, selectedPrefix);
            }
        }
    }
    
    /// <summary>
    /// 重置所有項目的視覺狀態
    /// </summary>
    private void ResetAllItemsVisual()
    {
        foreach (NavigationItem item in navigableItems)
        {
            if (item.type == NavigationItemType.OptionSwitcher && item.optionSwitcher != null)
            {
                item.optionSwitcher.SetSelected(false);
            }
            else if (item.type == NavigationItemType.Button && item.button != null)
            {
                RemovePrefixFromButton(item.button, selectedPrefix);
            }
        }
    }
    
    /// <summary>
    /// 為按鈕添加前綴
    /// </summary>
    /// <param name="button">目標按鈕</param>
    /// <param name="prefix">前綴文字</param>
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
    
    /// <summary>
    /// 從按鈕移除前綴
    /// </summary>
    /// <param name="button">目標按鈕</param>
    /// <param name="prefix">要移除的前綴</param>
    private void RemovePrefixFromButton(Button button, string prefix)
    {
        if (button == null) return;
        
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            string currentText = text.text;
            if (currentText.StartsWith(prefix))
            {
                text.text = currentText.Substring(prefix.Length);
            }
        }
    }
    
    #endregion
    
    #region 本地化系統
    
    /// <summary>
    /// 初始化本地化系統
    /// </summary>
    private void InitializeLocalization()
    {
        // 檢查所有本地化組件引用
        CheckLocalizationComponentReferences();
        
        // 等待 GameSettings 本地化系統初始化完成，然後更新 UI 文字
        StartCoroutine(WaitForLocalizationAndUpdateUI());
        
        // 註冊語言變更事件
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged += OnLocalizationLanguageChanged;
        }
        else
        {
            Debug.LogError("[PlayerGameSettingsUI] 無法註冊語言變更事件 - GameSettings.Instance 為 null");
        }
    }
    
    /// <summary>
    /// 檢查所有本地化組件引用
    /// </summary>
    private void CheckLocalizationComponentReferences()
    {
        int nullCount = 0;
        if (resolutionLabel == null) nullCount++;
        if (fpsLimitLabel == null) nullCount++;
        if (vSyncLabel == null) nullCount++;
        if (antiAliasingLabel == null) nullCount++;
        if (masterVolumeLabel == null) nullCount++;
        if (languageLabel == null) nullCount++;
        if (resetButtonText == null) nullCount++;
        if (closeButtonText == null) nullCount++;
        
        if (nullCount > 0)
        {
            Debug.LogError($"[PlayerGameSettingsUI] 發現 {nullCount} 個本地化組件引用為 null！請在 Inspector 中設置這些引用。");
        }
    }
    
    /// <summary>
    /// 等待本地化系統初始化並更新 UI
    /// </summary>
    private System.Collections.IEnumerator WaitForLocalizationAndUpdateUI()
    {
        Debug.Log("[PlayerGameSettingsUI] 等待本地化系統初始化...");
        
        float startTime = Time.unscaledTime;
        float timeout = 10f; // 10秒超時
        
        // 等待 GameSettings 本地化系統初始化完成
        while (GameSettings.Instance == null || !GameSettings.Instance.IsLocalizationInitialized())
        {
            if (Time.unscaledTime - startTime > timeout)
            {
                Debug.LogError("[PlayerGameSettingsUI] 本地化系統初始化超時！請檢查 GameSettings 是否存在於場景中。");
                yield break;
            }
            
            if (Time.unscaledTime - startTime > 1f) // 1秒後開始顯示等待訊息
            {
                Debug.LogWarning($"[PlayerGameSettingsUI] 仍在等待本地化系統初始化... ({Time.unscaledTime - startTime:F1}s)");
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // 更新所有 UI 文字
        UpdateAllLocalizedTexts();
        UpdateLocalizedOptionTexts();
    }
    
    /// <summary>
    /// 語言變更事件處理
    /// </summary>
    private void OnLocalizationLanguageChanged(string languageCode)
    {
        if (GameSettings.Instance == null)
        {
            Debug.LogError("[PlayerGameSettingsUI] 語言變更時 GameSettings.Instance 為 null！");
            return;
        }
        
        if (!GameSettings.Instance.IsLocalizationInitialized())
        {
            Debug.LogError("[PlayerGameSettingsUI] 語言變更時本地化系統尚未初始化！");
            return;
        }
        
        // 更新所有本地化文字
        UpdateAllLocalizedTexts();
        UpdateLocalizedOptionTexts();
    }
    
    /// <summary>
    /// 更新所有標籤的本地化文字
    /// </summary>
    private void UpdateAllLocalizedTexts()
    {
        if (GameSettings.Instance == null || !GameSettings.Instance.IsLocalizationInitialized())
        {
            Debug.LogWarning("[PlayerGameSettingsUI] UpdateAllLocalizedTexts: GameSettings 本地化系統尚未初始化");
            return;
        }
        
        
        // 更新設定標籤
        if (resolutionLabel != null)
        {
            GameSettings.Instance.UpdateLocalizedText(resolutionLabel, "UI_Tables", "settings.graphics.resolution");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] resolutionLabel 為 null，跳過");
        }
        
        if (fpsLimitLabel != null)
        {
            GameSettings.Instance.UpdateLocalizedText(fpsLimitLabel, "UI_Tables", "settings.graphics.fps_limit");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] fpsLimitLabel 為 null，跳過");
        }
        
        if (vSyncLabel != null)
        {
            GameSettings.Instance.UpdateLocalizedText(vSyncLabel, "UI_Tables", "settings.graphics.vsync");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] vSyncLabel 為 null，跳過");
        }
        
        if (antiAliasingLabel != null)
        {
            GameSettings.Instance.UpdateLocalizedText(antiAliasingLabel, "UI_Tables", "settings.graphics.antialiasing");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] antiAliasingLabel 為 null，跳過");
        }
        
        if (masterVolumeLabel != null)
        {
            GameSettings.Instance.UpdateLocalizedText(masterVolumeLabel, "UI_Tables", "settings.audio.volume");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] masterVolumeLabel 為 null，跳過");
        }
        
        if (languageLabel != null)
        {
            GameSettings.Instance.UpdateLocalizedText(languageLabel, "UI_Tables", "settings.language.title");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] languageLabel 為 null，跳過");
        }
        
        // 更新按鈕文字
        if (resetButtonText != null)
        {
            GameSettings.Instance.UpdateLocalizedText(resetButtonText, "UI_Tables", "settings.button.reset");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] resetButtonText 為 null，跳過");
        }
        
        if (closeButtonText != null)
        {
            GameSettings.Instance.UpdateLocalizedText(closeButtonText, "UI_Tables", "settings.button.back");
        }
        else
        {
            Debug.LogWarning("[PlayerGameSettingsUI] closeButtonText 為 null，跳過");
        }
        
    }
    
    /// <summary>
    /// 更新選項的本地化文字
    /// </summary>
    private void UpdateLocalizedOptionTexts()
    {
        if (GameSettings.Instance == null || !GameSettings.Instance.IsLocalizationInitialized())
        {
            return;
        }
        
        
        // 更新 FPS 限制選項
        UpdateFPSLimitOptions();
        
        // 更新垂直同步選項
        UpdateVSyncOptions();
        
        // 更新抗鋸齒選項
        UpdateAntiAliasingOptions();
        
        // 更新語言選項
        UpdateLanguageOptions();
    }
    
    /// <summary>
    /// 更新 FPS 限制選項的本地化文字
    /// </summary>
    private void UpdateFPSLimitOptions()
    {
        if (fpsLimitSwitcher == null) return;
        
        var localizedOptions = new List<string>();
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.fps.30"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.fps.60"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.fps.120"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.fps.144"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.options.unlimited"));
        
        int currentIndex = fpsLimitSwitcher.CurrentIndex;
        fpsLimitSwitcher.SetOptions(localizedOptions);
        fpsLimitSwitcher.CurrentIndex = currentIndex;
    }
    
    /// <summary>
    /// 更新垂直同步選項的本地化文字
    /// </summary>
    private void UpdateVSyncOptions()
    {
        if (vSyncSwitcher == null) return;
        
        var localizedOptions = new List<string>();
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.options.disabled"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.options.enabled"));
        
        int currentIndex = vSyncSwitcher.CurrentIndex;
        vSyncSwitcher.SetOptions(localizedOptions);
        vSyncSwitcher.CurrentIndex = currentIndex;
    }
    
    /// <summary>
    /// 更新抗鋸齒選項的本地化文字
    /// </summary>
    private void UpdateAntiAliasingOptions()
    {
        if (antiAliasingSwitcher == null) return;
        
        var localizedOptions = new List<string>();
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.antialiasing.off"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.antialiasing.2x"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.antialiasing.4x"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.antialiasing.8x"));
        
        int currentIndex = antiAliasingSwitcher.CurrentIndex;
        antiAliasingSwitcher.SetOptions(localizedOptions);
        antiAliasingSwitcher.CurrentIndex = currentIndex;
    }
    
    /// <summary>
    /// 更新語言選項的本地化文字
    /// </summary>
    private void UpdateLanguageOptions()
    {
        if (languageSwitcher == null) return;
        
        var localizedOptions = new List<string>();
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.language.zh_tw"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.language.zh_cn"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.language.en"));
        localizedOptions.Add(GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.language.ja"));
        
        int currentIndex = languageSwitcher.CurrentIndex;
        languageSwitcher.SetOptions(localizedOptions);
        languageSwitcher.CurrentIndex = currentIndex;
    }
    
    /// <summary>
    /// 手動觸發本地化文字更新（用於除錯）
    /// </summary>
    [ContextMenu("手動更新本地化文字")]
    public void ForceUpdateLocalization()
    {
        
        if (GameSettings.Instance == null)
        {
            Debug.LogError("[PlayerGameSettingsUI] GameSettings.Instance 為 null！請確保場景中存在 GameSettings。");
            return;
        }
        
        if (!GameSettings.Instance.IsLocalizationInitialized())
        {
            Debug.LogError("[PlayerGameSettingsUI] GameSettings 本地化系統尚未初始化！請等待初始化完成。");
            return;
        }
        
        Debug.Log($"[PlayerGameSettingsUI] 當前語言: {GameSettings.Instance.GetCurrentLanguageCode()}");
        
        // 檢查組件引用
        CheckLocalizationComponentReferences();
        
        // 強制更新所有本地化文字
        UpdateAllLocalizedTexts();
        UpdateLocalizedOptionTexts();
        
    }
    
    /// <summary>
    /// 檢查本地化系統狀態（用於除錯）
    /// </summary>
    [ContextMenu("檢查本地化系統狀態")]
    public void CheckLocalizationSystemStatus()
    {
        Debug.Log($"GameSettings.Instance 存在: {GameSettings.Instance != null}");
        
        if (GameSettings.Instance != null)
        {
            Debug.Log($"GameSettings 本地化已初始化: {GameSettings.Instance.IsLocalizationInitialized()}");
            Debug.Log($"當前語言: {GameSettings.Instance.GetCurrentLanguageCode()}");
            var availableLanguages = GameSettings.Instance.GetAvailableLanguages();
            Debug.Log($"可用語言: {string.Join(", ", availableLanguages)}");
            
            // 測試獲取本地化文字
            string testText = GameSettings.Instance.GetLocalizedString("UI_Tables", "settings.graphics.resolution");
            Debug.Log($"測試獲取本地化文字 'settings.graphics.resolution': '{testText}'");
        }
        
        CheckLocalizationComponentReferences();
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            // 取消註冊本地化事件
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnLanguageChanged -= OnLocalizationLanguageChanged;
            }
            
            Instance = null;
        }
    }
}
