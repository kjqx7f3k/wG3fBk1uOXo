using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 玩家遊戲設置 UI 控制器
/// 管理影像設定和音訊設定的 UI 組件與互動邏輯
/// </summary>
public class PlayerGameSettingsUI : UIPanel
{
    public static PlayerGameSettingsUI Instance { get; private set; }
    
    [Header("影像設定")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown fpsLimitDropdown;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private TMP_Dropdown antiAliasingDropdown;
    
    [Header("音訊設定")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    
    [Header("語言設定")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    
    [Header("UI 控制")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;
    
    // 設定值快取
    private int currentResolutionIndex = 0;
    private int currentFPSLimit = 60;
    private bool currentVSync = true;
    private int currentAntiAliasing = 2;
    private float currentMasterVolume = 1.0f;
    private int currentLanguageIndex = 0;
    
    public bool IsSettingsUIOpen => IsOpen;
    
    private void Awake()
    {
        // 設定UIPanel屬性
        pauseGameWhenOpen = true;   // 設定UI暫停遊戲
        blockCharacterMovement = true;  // 阻擋角色移動
        canCloseWithEscape = true;  // 可用ESC關閉
        
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
            
            InitializeUIComponents();
            LoadCurrentSettings();
            SetupEventListeners();
            
            Debug.Log("PlayerGameSettingsUI初始化完成");
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
    }
    
    /// <summary>
    /// 處理自定義輸入 - 重寫UIPanel方法
    /// </summary>
    protected override void HandleCustomInput()
    {
        // 設定UI暫不需要額外的輸入處理
        // ESC鍵已由UIPanel基類處理
    }
    
    /// <summary>
    /// 初始化 UI 組件
    /// </summary>
    private void InitializeUIComponents()
    {
        // 初始化解析度下拉選單
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(new System.Collections.Generic.List<string> 
            {
                "1920x1080",
                "1600x900",
                "1366x768",
                "1280x720",
                "1024x768"
            });
        }
        
        // 初始化 FPS 限制下拉選單
        if (fpsLimitDropdown != null)
        {
            fpsLimitDropdown.ClearOptions();
            fpsLimitDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "30 FPS",
                "60 FPS",
                "120 FPS",
                "144 FPS",
                "無限制"
            });
        }
        
        // 初始化抗鋸齒下拉選單
        if (antiAliasingDropdown != null)
        {
            antiAliasingDropdown.ClearOptions();
            antiAliasingDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "關閉",
                "FXAA",
                "TAA",
                "SMAA",
                "MSAA 2x",
                "MSAA 4x",
                "MSAA 8x"
            });
        }
        
        // 初始化語言下拉選單
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "繁體中文",
                "簡體中文",
                "English",
                "日本語"
            });
        }
    }
    
    /// <summary>
    /// 載入當前設定值
    /// </summary>
    private void LoadCurrentSettings()
    {
        // 從 GameSettings 載入語言設定
        if (GameSettings.Instance != null)
        {
            string currentLanguage = GameSettings.Instance.CurrentLanguage;
            currentLanguageIndex = GetLanguageIndex(currentLanguage);
        }
        
        // 載入影像設定
        if (resolutionDropdown != null)
            resolutionDropdown.value = currentResolutionIndex;
            
        if (fpsLimitDropdown != null)
            fpsLimitDropdown.value = GetFPSLimitIndex(currentFPSLimit);
            
        if (vSyncToggle != null)
            vSyncToggle.isOn = currentVSync;
            
        if (antiAliasingDropdown != null)
            antiAliasingDropdown.value = currentAntiAliasing;
        
        // 載入音訊設定
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = currentMasterVolume;
            
        if (masterVolumeText != null)
            masterVolumeText.text = $"{(currentMasterVolume * 100):F0}%";
        
        // 載入語言設定
        if (languageDropdown != null)
            languageDropdown.value = currentLanguageIndex;
    }
    
    /// <summary>
    /// 設定事件監聽器
    /// </summary>
    private void SetupEventListeners()
    {
        // 影像設定事件
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            
        if (fpsLimitDropdown != null)
            fpsLimitDropdown.onValueChanged.AddListener(OnFPSLimitChanged);
            
        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            
        if (antiAliasingDropdown != null)
            antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
        
        // 音訊設定事件
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        
        // 語言設定事件
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        
        // 按鈕事件
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplySettings);
            
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetSettings);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseSettings);
    }
    
    #region 影像設定回調
    private void OnResolutionChanged(int index)
    {
        currentResolutionIndex = index;
        Debug.Log($"解析度變更為: {resolutionDropdown.options[index].text}");
    }
    
    private void OnFPSLimitChanged(int index)
    {
        currentFPSLimit = GetFPSLimitValue(index);
        Debug.Log($"FPS限制變更為: {currentFPSLimit}");
    }
    
    private void OnVSyncChanged(bool enabled)
    {
        currentVSync = enabled;
        Debug.Log($"垂直同步: {(enabled ? "開啟" : "關閉")}");
    }
    
    private void OnAntiAliasingChanged(int index)
    {
        currentAntiAliasing = index;
        Debug.Log($"抗鋸齒變更為: {antiAliasingDropdown.options[index].text}");
    }
    #endregion
    
    #region 音訊設定回調
    private void OnMasterVolumeChanged(float volume)
    {
        currentMasterVolume = volume;
        if (masterVolumeText != null)
            masterVolumeText.text = $"{(volume * 100):F0}%";
        Debug.Log($"總音量變更為: {volume:F2}");
    }
    #endregion
    
    #region 語言設定回調
    private void OnLanguageChanged(int index)
    {
        currentLanguageIndex = index;
        string languageCode = GetLanguageCode(index);
        
        // 更新 GameSettings
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.CurrentLanguage = languageCode;
        }
        
        // 切換語言
        if (LocalizedUIHelper.Instance != null)
        {
            LocalizedUIHelper.Instance.ChangeLanguage(languageCode);
        }
        
        Debug.Log($"語言變更為: {languageDropdown.options[index].text} ({languageCode})");
    }
    #endregion
    
    #region 按鈕回調
    private void OnApplySettings()
    {
        Debug.Log("套用設定");
        
        // 儲存設定到 GameSettings
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SaveSettings();
        }
    }
    
    private void OnResetSettings()
    {
        Debug.Log("重置設定");
        
        // 重置到預設值
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ResetToDefaults();
        }
        
        LoadCurrentSettings();
    }
    
    private void OnCloseSettings()
    {
        Debug.Log("關閉設定");
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
        Debug.Log("設定UI已開啟");
    }
    
    /// <summary>
    /// 面板關閉時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnClosed()
    {
        Debug.Log("設定UI已關閉，返回GameMenu");
        
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
        if (resolutionDropdown != null) resolutionDropdown.interactable = interactable;
        if (fpsLimitDropdown != null) fpsLimitDropdown.interactable = interactable;
        if (vSyncToggle != null) vSyncToggle.interactable = interactable;
        if (antiAliasingDropdown != null) antiAliasingDropdown.interactable = interactable;
        if (masterVolumeSlider != null) masterVolumeSlider.interactable = interactable;
        if (languageDropdown != null) languageDropdown.interactable = interactable;
        if (applyButton != null) applyButton.interactable = interactable;
        if (resetButton != null) resetButton.interactable = interactable;
        if (closeButton != null) closeButton.interactable = interactable;
    }
    #endregion
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
