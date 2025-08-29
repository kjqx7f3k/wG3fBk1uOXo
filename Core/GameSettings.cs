using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 遊戲全局設定管理器 - 管理遊戲中的全局設定和常數
/// 整合了原GameGlobals、GameSettings和LocalizedUIHelper的功能
/// 作為語言設定和本地化的單一真實來源
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }
    
    [Header("本地化設定")]
    [SerializeField] private string currentLanguage = "zh-TW";
    [SerializeField] private bool initializeLocalizationOnAwake = true;
    
    [Header("圖形設定")]
    [SerializeField] private int resolutionIndex = 0;
    [SerializeField] private int fpsLimit = 60;
    [SerializeField] private bool vSyncEnabled = true;
    [SerializeField] private int antiAliasingLevel = 2;
    
    [Header("音訊設定")]
    [SerializeField] private float masterVolume = 1.0f;
    
    [Header("物理設定")]
    [SerializeField] private float gravityConstant = 1.0f;
    [SerializeField] private float defaultMass = 1.0f;
    
    [Header("遊戲設定")]
    [SerializeField] private float gameSpeed = 1.0f;
    [SerializeField] private bool debugMode = false;
    
    [Header("UI設定")]
    [SerializeField] private float uiAnimationSpeed = 1.0f;
    [SerializeField] private bool enableUIAnimations = true;
    
    // 公開屬性
    public string Language 
    { 
        get => currentLanguage; 
        set 
        { 
            if (currentLanguage != value)
            {
                currentLanguage = value;
                // 只有在初始化完成後才觸發事件，避免初始化時的循環
                if (isInitialized)
                {
                    OnLanguageChanged?.Invoke(currentLanguage);
                    Debug.Log($"[GameSettings] 語言已變更為: {currentLanguage}");
                }
            }
        } 
    }
    
    public int ResolutionIndex 
    { 
        get => resolutionIndex; 
        set 
        { 
            resolutionIndex = value;
            OnGraphicsSettingsChanged?.Invoke();
        } 
    }
    
    public int FPSLimit 
    { 
        get => fpsLimit; 
        set 
        { 
            fpsLimit = value;
            OnGraphicsSettingsChanged?.Invoke();
            // 即時套用 FPS 設定
            if (isInitialized)
            {
                ApplyGraphicsSettings();
            }
        } 
    }
    
    public bool VSyncEnabled 
    { 
        get => vSyncEnabled; 
        set 
        { 
            vSyncEnabled = value;
            OnGraphicsSettingsChanged?.Invoke();
            // 即時套用垂直同步設定
            if (isInitialized)
            {
                ApplyGraphicsSettings();
            }
        } 
    }
    
    public int AntiAliasingLevel 
    { 
        get => antiAliasingLevel; 
        set 
        { 
            antiAliasingLevel = value;
            OnGraphicsSettingsChanged?.Invoke();
            // 即時套用抗鋸齒設定
            if (isInitialized)
            {
                ApplyGraphicsSettings();
            }
        } 
    }
    
    public float MasterVolume 
    { 
        get => masterVolume; 
        set 
        { 
            masterVolume = Mathf.Clamp01(value);
            OnAudioSettingsChanged?.Invoke();
            // 即時套用音量設定
            if (isInitialized)
            {
                ApplyAudioSettings();
            }
        } 
    }
    
    public float GravityConstant 
    { 
        get => gravityConstant; 
        set 
        { 
            gravityConstant = value;
            OnGravityConstantChanged?.Invoke(gravityConstant);
        } 
    }
    
    public float DefaultMass 
    { 
        get => defaultMass; 
        set => defaultMass = value; 
    }
    
    public float GameSpeed 
    { 
        get => gameSpeed; 
        set 
        { 
            gameSpeed = value;
            Time.timeScale = gameSpeed;
        } 
    }
    
    public bool DebugMode 
    { 
        get => debugMode; 
        set => debugMode = value; 
    }
    
    public float UIAnimationSpeed 
    { 
        get => uiAnimationSpeed; 
        set => uiAnimationSpeed = value; 
    }
    
    public bool EnableUIAnimations 
    { 
        get => enableUIAnimations; 
        set => enableUIAnimations = value; 
    }
    
    // 本地化系統變數（簡化版本）
    private bool isLocalizationInitialized = false;
    
    // 事件
    public System.Action<string> OnLanguageChanged;
    public System.Action OnGraphicsSettingsChanged;
    public System.Action OnAudioSettingsChanged;
    public System.Action<float> OnGravityConstantChanged;
    public System.Action<float> OnGameSpeedChanged;
    public System.Action<bool> OnDebugModeChanged;
    public System.Action OnLocalizationInitialized;
    
    // 初始化狀態追蹤
    private bool isInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化設定
            InitializeSettings();
            
            // 初始化本地化系統
            if (initializeLocalizationOnAwake)
            {
                StartCoroutine(InitializeLocalization());
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 初始化設定
    /// </summary>
    private void InitializeSettings()
    {
        // 應用初始遊戲速度
        Time.timeScale = gameSpeed;
        
        // 載入儲存的設定
        LoadSettings();
        
        // 應用圖形和音訊設定
        ApplyGraphicsSettings();
        ApplyAudioSettings();
        
        // 標記為已初始化，允許屬性變更時即時套用
        isInitialized = true;
        
        Debug.Log($"GameSettings 初始化完成:");
        Debug.Log($"  當前語言: {currentLanguage}");
        Debug.Log($"  解析度索引: {resolutionIndex}");
        Debug.Log($"  FPS限制: {fpsLimit}");
        Debug.Log($"  垂直同步: {vSyncEnabled}");
        Debug.Log($"  抗鋸齒: {antiAliasingLevel}");
        Debug.Log($"  主音量: {masterVolume}");
        Debug.Log($"  重力常數: {gravityConstant}");
        Debug.Log($"  預設質量: {defaultMass}");
        Debug.Log($"  遊戲速度: {gameSpeed}");
        Debug.Log($"  除錯模式: {debugMode}");
    }
    
    /// <summary>
    /// 重置所有設定為預設值
    /// </summary>
    [ContextMenu("重置為預設值")]
    public void ResetToDefaults()
    {
        Debug.Log("[GameSettings] 開始重置所有設定為預設值");
        
        // 優化語言重置邏輯 - 只在語言實際變更時觸發事件
        string targetLanguage = "zh-TW";
        if (currentLanguage != targetLanguage)
        {
            Debug.Log($"[GameSettings] 語言從 {currentLanguage} 重置為 {targetLanguage}");
            Language = targetLanguage;
        }
        else
        {
            Debug.Log($"[GameSettings] 語言已經是預設值 {targetLanguage}，跳過語言變更");
        }
        
        // 重置其他設定（直接設定欄位避免觸發多餘事件）
        ResolutionIndex = 0;
        FPSLimit = 60;
        VSyncEnabled = true;
        AntiAliasingLevel = 2;
        MasterVolume = 1.0f;
        GravityConstant = 1.0f;
        DefaultMass = 1.0f;
        GameSpeed = 1.0f;
        DebugMode = false;
        UIAnimationSpeed = 1.0f;
        EnableUIAnimations = true;
        
        ApplyGraphicsSettings();
        ApplyAudioSettings();
        
        Debug.Log("[GameSettings] 重置完成 - 所有設定已恢復為預設值");
    }
    
    /// <summary>
    /// 儲存設定到PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetString("GameSettings_CurrentLanguage", currentLanguage);
        PlayerPrefs.SetInt("GameSettings_ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("GameSettings_FPSLimit", fpsLimit);
        PlayerPrefs.SetInt("GameSettings_VSyncEnabled", vSyncEnabled ? 1 : 0);
        PlayerPrefs.SetInt("GameSettings_AntiAliasingLevel", antiAliasingLevel);
        PlayerPrefs.SetFloat("GameSettings_MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("GameSettings_GravityConstant", gravityConstant);
        PlayerPrefs.SetFloat("GameSettings_DefaultMass", defaultMass);
        PlayerPrefs.SetFloat("GameSettings_GameSpeed", gameSpeed);
        PlayerPrefs.SetInt("GameSettings_DebugMode", debugMode ? 1 : 0);
        PlayerPrefs.SetFloat("GameSettings_UIAnimationSpeed", uiAnimationSpeed);
        PlayerPrefs.SetInt("GameSettings_EnableUIAnimations", enableUIAnimations ? 1 : 0);
        
        PlayerPrefs.Save();
        Debug.Log("GameSettings 設定已儲存");
    }
    
    /// <summary>
    /// 從PlayerPrefs載入設定
    /// </summary>
    public void LoadSettings()
    {
        // 優先載入新的GameSettings設定，如果沒有則嘗試載入舊GameGlobals設定
        if (PlayerPrefs.HasKey("GameSettings_CurrentLanguage"))
        {
            // 載入語言設定 - 使用屬性setter確保觸發事件
            string savedLanguage = PlayerPrefs.GetString("GameSettings_CurrentLanguage", "zh-TW");
            Language = savedLanguage; // 使用屬性而非直接設定欄位
            
            resolutionIndex = PlayerPrefs.GetInt("GameSettings_ResolutionIndex", 0);
            fpsLimit = PlayerPrefs.GetInt("GameSettings_FPSLimit", 60);
            vSyncEnabled = PlayerPrefs.GetInt("GameSettings_VSyncEnabled", 1) == 1;
            antiAliasingLevel = PlayerPrefs.GetInt("GameSettings_AntiAliasingLevel", 2);
            masterVolume = PlayerPrefs.GetFloat("GameSettings_MasterVolume", 1.0f);
            gravityConstant = PlayerPrefs.GetFloat("GameSettings_GravityConstant", 1.0f);
            defaultMass = PlayerPrefs.GetFloat("GameSettings_DefaultMass", 1.0f);
            gameSpeed = PlayerPrefs.GetFloat("GameSettings_GameSpeed", 1.0f);
            debugMode = PlayerPrefs.GetInt("GameSettings_DebugMode", 0) == 1;
            uiAnimationSpeed = PlayerPrefs.GetFloat("GameSettings_UIAnimationSpeed", 1.0f);
            enableUIAnimations = PlayerPrefs.GetInt("GameSettings_EnableUIAnimations", 1) == 1;
            
            Debug.Log("GameSettings 設定已載入");
        }
        else if (PlayerPrefs.HasKey("GameGlobals_GravityConstant"))
        {
            // 相容性：載入舊GameGlobals設定，新圖形音訊設定使用預設值
            // 語言設定使用預設值
            Language = "zh-TW";
            
            gravityConstant = PlayerPrefs.GetFloat("GameGlobals_GravityConstant", 1.0f);
            defaultMass = PlayerPrefs.GetFloat("GameGlobals_DefaultMass", 1.0f);
            gameSpeed = PlayerPrefs.GetFloat("GameGlobals_GameSpeed", 1.0f);
            debugMode = PlayerPrefs.GetInt("GameGlobals_DebugMode", 0) == 1;
            uiAnimationSpeed = PlayerPrefs.GetFloat("GameGlobals_UIAnimationSpeed", 1.0f);
            enableUIAnimations = PlayerPrefs.GetInt("GameGlobals_EnableUIAnimations", 1) == 1;
            
            Debug.Log("已載入舊GameGlobals設定，將自動轉換為GameSettings格式");
            SaveSettings(); // 立即保存為新格式
        }
        else
        {
            // 使用預設值，包括語言
            Language = "zh-TW";
            Debug.Log("沒有找到儲存的設定，使用預設值");
        }
    }
    
    /// <summary>
    /// 獲取設定資訊字串
    /// </summary>
    /// <returns>設定資訊</returns>
    public string GetSettingsInfo()
    {
        return $"GameSettings 設定:\n" +
               $"  當前語言: {currentLanguage}\n" +
               $"  解析度索引: {resolutionIndex}\n" +
               $"  FPS限制: {fpsLimit}\n" +
               $"  垂直同步: {vSyncEnabled}\n" +
               $"  抗鋸齒等級: {antiAliasingLevel}\n" +
               $"  主音量: {masterVolume:F2}\n" +
               $"  重力常數: {gravityConstant}\n" +
               $"  預設質量: {defaultMass}\n" +
               $"  遊戲速度: {gameSpeed}\n" +
               $"  除錯模式: {debugMode}\n" +
               $"  UI動畫速度: {uiAnimationSpeed}\n" +
               $"  啟用UI動畫: {enableUIAnimations}";
    }
    
    /// <summary>
    /// 獲取當前 Unity 系統狀態資訊
    /// </summary>
    /// <returns>系統狀態資訊</returns>
    public string GetUnitySystemStatus()
    {
        return $"Unity 系統狀態:\n" +
               $"  當前解析度: {Screen.currentResolution.width}x{Screen.currentResolution.height}\n" +
               $"  targetFrameRate: {Application.targetFrameRate}\n" +
               $"  vSyncCount: {QualitySettings.vSyncCount}\n" +
               $"  antiAliasing: {QualitySettings.antiAliasing}\n" +
               $"  AudioListener.volume: {AudioListener.volume:F2}\n" +
               $"  Time.timeScale: {Time.timeScale:F2}\n" +
               #if UNITY_EDITOR
               $"  編輯器模式: 是\n" +
               $"  當前品質等級: {QualitySettings.GetQualityLevel()}\n" +
               $"  品質等級名稱: {QualitySettings.names[QualitySettings.GetQualityLevel()]}";
               #else
               $"  編輯器模式: 否";
               #endif
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 編輯器專用：強制重新套用所有設定
    /// </summary>
    [UnityEngine.ContextMenu("強制重新套用所有設定")]
    public void ForceReapplyAllSettings()
    {
        Debug.Log("[GameSettings] 編輯器強制重新套用所有設定...");
        Debug.Log($"套用前狀態:\n{GetUnitySystemStatus()}");
        
        ApplyGraphicsSettings();
        ApplyAudioSettings();
        
        Debug.Log($"套用後狀態:\n{GetUnitySystemStatus()}");
        Debug.Log("[GameSettings] 強制重新套用完成");
    }
    
    /// <summary>
    /// 編輯器專用：顯示當前所有設定和系統狀態
    /// </summary>
    [UnityEngine.ContextMenu("顯示設定和系統狀態")]
    public void ShowAllStatus()
    {
        Debug.Log($"=== GameSettings 完整狀態報告 ===");
        Debug.Log($"{GetSettingsInfo()}");
        Debug.Log($"");
        Debug.Log($"{GetUnitySystemStatus()}");
        Debug.Log($"=== 狀態報告結束 ===");
    }
    
    /// <summary>
    /// 編輯器專用：測試 FPS 限制功能
    /// </summary>
    [UnityEngine.ContextMenu("測試 FPS 限制")]
    public void TestFPSLimit()
    {
        Debug.Log("[GameSettings] 開始 FPS 限制測試...");
        Debug.Log("將在接下來的 10 秒內依序測試不同 FPS 設定");
        Debug.Log("請觀察 Unity Profiler 或 Game View Stats 中的 FPS 變化");
        
        StartCoroutine(FPSTestCoroutine());
    }
    
    /// <summary>
    /// FPS 測試協程
    /// </summary>
    private System.Collections.IEnumerator FPSTestCoroutine()
    {
        int[] testFPS = { 30, 60, 120, -1 }; // -1 表示無限制
        string[] testNames = { "30 FPS", "60 FPS", "120 FPS", "無限制" };
        
        // 確保 VSync 關閉以便測試 FPS 限制
        bool originalVSync = vSyncEnabled;
        VSyncEnabled = false;
        Debug.Log("已暫時關閉 VSync 以便測試 FPS 限制");
        
        for (int i = 0; i < testFPS.Length; i++)
        {
            Debug.Log($"[FPS測試] 第 {i + 1}/{testFPS.Length} 步: 設定為 {testNames[i]}");
            
            if (testFPS[i] > 0)
            {
                FPSLimit = testFPS[i];
            }
            else
            {
                FPSLimit = -1;
            }
            
            Debug.Log($"[FPS測試] 當前 targetFrameRate: {Application.targetFrameRate}");
            Debug.Log($"[FPS測試] 請觀察接下來 2.5 秒的 FPS 表現");
            
            yield return new UnityEngine.WaitForSeconds(2.5f);
        }
        
        // 恢復原始 VSync 設定
        VSyncEnabled = originalVSync;
        Debug.Log($"[FPS測試] 測試完成，已恢復原始 VSync 設定: {originalVSync}");
        Debug.Log("[FPS測試] 如果沒有看到 FPS 變化，請確認:");
        Debug.Log("  1. Unity Profiler 是否已開啟 (Window → Analysis → Profiler)");
        Debug.Log("  2. Game View 中的 Stats 是否已啟用");
        Debug.Log("  3. 是否有其他因素限制了 FPS (如複雜的場景渲染)");
    }
    
    /// <summary>
    /// 編輯器專用：測試抗鋸齒效果
    /// </summary>
    [UnityEngine.ContextMenu("測試抗鋸齒效果")]
    public void TestAntiAliasing()
    {
        Debug.Log("[GameSettings] 開始抗鋸齒效果測試...");
        Debug.Log("將在接下來的 12 秒內依序測試不同抗鋸齒等級");
        Debug.Log("請觀察 Game View 中物體邊緣的平滑程度變化");
        Debug.Log("建議測試場景：包含直線、圓形、斜線的 3D 物體");
        
        StartCoroutine(AntiAliasingTestCoroutine());
    }
    
    /// <summary>
    /// 抗鋸齒測試協程
    /// </summary>
    private System.Collections.IEnumerator AntiAliasingTestCoroutine()
    {
        int[] testAALevels = { 0, 1, 2, 3 }; // 對應 關閉, MSAA 2x, MSAA 4x, MSAA 8x
        string[] testNames = { "關閉 (0x)", "MSAA 2x", "MSAA 4x", "MSAA 8x" };
        int[] actualValues = { 0, 2, 4, 8 };
        
        // 保存原始設定
        int originalAALevel = antiAliasingLevel;
        Debug.Log($"已保存原始抗鋸齒設定: {testNames[originalAALevel]} ({actualValues[originalAALevel]})");
        
        for (int i = 0; i < testAALevels.Length; i++)
        {
            Debug.Log($"[抗鋸齒測試] 第 {i + 1}/{testAALevels.Length} 步: 設定為 {testNames[i]}");
            Debug.Log($"[抗鋸齒測試] 觀察重點:");
            Debug.Log($"  - 3D 物體的邊緣線條是否平滑");
            Debug.Log($"  - 特別注意直線與背景的交界處");
            Debug.Log($"  - 圓形或曲線邊緣的鋸齒狀況");
            
            AntiAliasingLevel = testAALevels[i];
            
            Debug.Log($"[抗鋸齒測試] 當前 QualitySettings.antiAliasing: {QualitySettings.antiAliasing}");
            Debug.Log($"[抗鋸齒測試] 請仔細觀察接下來 3 秒的視覺效果");
            
            // 檢查渲染管線相容性
            #if UNITY_EDITOR
            var renderPipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (renderPipeline != null)
            {
                Debug.Log($"[抗鋸齒測試] 目前使用渲染管線: {renderPipeline.GetType().Name}");
                if (actualValues[i] > 0)
                {
                    Debug.Log($"[抗鋸齒測試] 注意: 某些渲染管線可能不支援傳統 MSAA，效果可能有限");
                }
            }
            else
            {
                Debug.Log($"[抗鋸齒測試] 使用內建渲染管線，MSAA 應該正常工作");
            }
            #endif
            
            yield return new UnityEngine.WaitForSeconds(3f);
        }
        
        // 恢復原始設定
        AntiAliasingLevel = originalAALevel;
        Debug.Log($"[抗鋸齒測試] 測試完成，已恢復原始設定: {testNames[originalAALevel]}");
        Debug.Log("[抗鋸齒測試] 測試總結:");
        Debug.Log("  - 如果沒有看到明顯差異，可能原因:");
        Debug.Log("    1. 場景中缺少適合觀察的幾何體");
        Debug.Log("    2. 當前渲染管線不支援 MSAA");
        Debug.Log("    3. 顯示器解析度過高，鋸齒本身就不明顯");
        Debug.Log("  - 建議在 Edit → Project Settings → Quality 中確認設定");
        Debug.Log("  - 可嘗試建立包含細線條或小圓形的測試場景");
    }
    #endif
    
    // ========== 本地化系統相關方法 (整合自LocalizedUIHelper) ==========
    
    /// <summary>
    /// 初始化本地化系統（簡化版本）
    /// </summary>
    public IEnumerator InitializeLocalization()
    {
        if (isLocalizationInitialized)
            yield break;
            
        Debug.Log("[GameSettings] ===== 正在初始化簡化本地化系統... =====");
        
        // 等待SimpleLocalizationManager初始化
        while (SimpleLocalizationManager.Instance == null)
        {
            yield return null;
        }
        
        // 檢查當前語言是否被支持
        if (!SimpleLocalizationManager.Instance.IsLanguageSupported(currentLanguage))
        {
            Debug.LogWarning($"[GameSettings] 當前語言 {currentLanguage} 不被支持，切換到英語");
            currentLanguage = "en";
        }
        
        isLocalizationInitialized = true;
        
        Debug.Log($"[GameSettings] ===== 簡化本地化系統初始化完成，當前語言: {currentLanguage} =====");
    }
    
    // 已移除：PreloadCommonStringTables 方法因不再使用Unity Localization而刪除
    
    // 已移除：Unity Localization 相關的事件處理方法因不再使用而刪除
    
    // 已移除：舊版Unity Localization相關的本地化方法已移除，請使用文件末尾的簡化版本
    
    
    /// <summary>
    /// 獲取本地化文字（簡化版本）
    /// </summary>
    public string GetLocalizedString(string tableName, string entryKey)
    {
        if (!isLocalizationInitialized)
        {
            Debug.LogWarning($"[GameSettings] 本地化系統尚未初始化，返回原始鍵值: {entryKey}");
            return entryKey;
        }
        
        if (SimpleLocalizationManager.Instance == null)
        {
            Debug.LogWarning($"[GameSettings] SimpleLocalizationManager 未初始化，返回原始鍵值: {entryKey}");
            return entryKey;
        }
        
        return SimpleLocalizationManager.Instance.GetLocalizedString(entryKey, currentLanguage);
    }
    
    /// <summary>
    /// 獲取本地化文字（異步，保留兼容性）
    /// </summary>
    public void GetLocalizedStringAsync(string tableName, string entryKey, System.Action<string> callback)
    {
        string result = GetLocalizedString(tableName, entryKey);
        callback?.Invoke(result);
    }
    
    /// <summary>
    /// 更新TextMeshProUGUI的本地化文字（簡化版本）
    /// </summary>
    public void UpdateLocalizedText(TextMeshProUGUI textComponent, string tableName, string entryKey)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("[GameSettings] TextMeshProUGUI 組件為 null");
            return;
        }
        
        string localizedText = GetLocalizedString(tableName, entryKey);
        textComponent.text = localizedText;
    }
    
    /// <summary>
    /// 切換語言（簡化版本）
    /// </summary>
    public bool ChangeLanguage(string localeCode)
    {
        if (!isLocalizationInitialized)
        {
            Debug.LogWarning("[GameSettings] 本地化系統尚未初始化");
            return false;
        }
        
        if (SimpleLocalizationManager.Instance == null)
        {
            Debug.LogWarning("[GameSettings] SimpleLocalizationManager 未初始化");
            return false;
        }
        
        if (!SimpleLocalizationManager.Instance.IsLanguageSupported(localeCode))
        {
            Debug.LogWarning($"[GameSettings] 找不到語言: {localeCode}");
            return false;
        }
        
        Language = localeCode; // 透過屬性觸發事件
        return true;
    }
    
    /// <summary>
    /// 獲取所有可用語言
    /// </summary>
    public List<string> GetAvailableLanguages()
    {
        if (SimpleLocalizationManager.Instance != null)
        {
            return SimpleLocalizationManager.Instance.GetAvailableLanguages();
        }
        
        return new List<string> { "zh-TW", "en", "zh-CN", "ja" }; // 默認支持的語言
    }
    
    /// <summary>
    /// 獲取當前語言代碼
    /// </summary>
    /// <returns>當前語言代碼</returns>
    public string GetCurrentLanguageCode()
    {
        return currentLanguage;
    }
    
    /// <summary>
    /// 檢查本地化系統是否已初始化
    /// </summary>
    public bool IsLocalizationInitialized()
    {
        return isLocalizationInitialized;
    }
    
    // ========== 結束本地化系統方法 ==========
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnApplicationQuit()
    {
        // 應用程式退出時自動儲存設定
        if (Instance == this)
        {
            SaveSettings();
            Instance = null;
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // 應用程式暫停時儲存設定（主要用於移動平台）
        if (pauseStatus && Instance == this)
        {
            SaveSettings();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // 應用程式失去焦點時儲存設定
        if (!hasFocus && Instance == this)
        {
            SaveSettings();
        }
    }
    
    /// <summary>
    /// 套用圖形設定到 Unity 系統
    /// </summary>
    private void ApplyGraphicsSettings()
    {
        try
        {
            Debug.Log($"[GameSettings] 開始套用圖形設定 - FPS: {fpsLimit}, VSync: {vSyncEnabled}");
            
            #if UNITY_EDITOR
            Debug.Log($"[GameSettings] 運行於 Unity 編輯器中 - 某些設定可能有限制");
            #endif
            
            // 套用解析度設定
            string[] resolutionOptions = { "1920x1080", "1600x900", "1366x768", "1280x720", "1024x768" };
            if (resolutionIndex >= 0 && resolutionIndex < resolutionOptions.Length)
            {
                string[] resolution = resolutionOptions[resolutionIndex].Split('x');
                if (resolution.Length == 2 && int.TryParse(resolution[0], out int width) && int.TryParse(resolution[1], out int height))
                {
                    #if UNITY_EDITOR
                    Debug.Log($"[GameSettings] 編輯器中解析度設定: {width}x{height} (注意: Screen.SetResolution 在編輯器中不生效，需要手動調整 Game View 大小來測試)");
                    #else
                    Screen.SetResolution(width, height, Screen.fullScreen);
                    Debug.Log($"套用解析度: {width}x{height}");
                    #endif
                }
                else
                {
                    Debug.LogError($"無效的解析度格式: {resolutionOptions[resolutionIndex]}");
                }
            }
            else
            {
                Debug.LogWarning($"解析度索引超出範圍: {resolutionIndex}");
            }
            
            // 記錄當前 Unity 系統狀態（套用前）
            Debug.Log($"[GameSettings] 套用前狀態 - 當前 targetFrameRate: {Application.targetFrameRate}, vSyncCount: {QualitySettings.vSyncCount}");
            
            // 重要：先處理垂直同步，再處理 FPS 限制
            // Unity 中 VSync 會覆蓋 targetFrameRate 設定
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
            Debug.Log($"套用垂直同步: {(vSyncEnabled ? "開啟" : "關閉")} (vSyncCount = {QualitySettings.vSyncCount})");
            
            // 套用 FPS 限制（只在 VSync 關閉時有效）
            if (!vSyncEnabled)
            {
                // VSync 關閉，可以自由設定 FPS
                if (fpsLimit > 0)
                {
                    Application.targetFrameRate = fpsLimit;
                    Debug.Log($"套用 FPS 限制: {fpsLimit} (VSync 已關閉, targetFrameRate = {Application.targetFrameRate})");
                    
                    #if UNITY_EDITOR
                    Debug.Log($"[GameSettings] 編輯器測試提示: 請打開 Window → Analysis → Profiler 或在 Game View 中啟用 Stats 來查看實際 FPS");
                    #endif
                }
                else
                {
                    Application.targetFrameRate = -1; // 無限制
                    Debug.Log($"套用 FPS 限制: 無限制 (VSync 已關閉, targetFrameRate = {Application.targetFrameRate})");
                }
            }
            else
            {
                // VSync 開啟，FPS 會被顯示器刷新率限制
                Application.targetFrameRate = -1; // 讓 VSync 控制幀率
                Debug.Log($"VSync 開啟，FPS 將由顯示器刷新率控制 (忽略 FPS 限制設定: {fpsLimit}, targetFrameRate = {Application.targetFrameRate})");
                
                #if UNITY_EDITOR
                Debug.Log($"[GameSettings] 編輯器中 VSync 可能受到編輯器設定影響，建議在 Build 版本中測試最終效果");
                #endif
            }
            
            // 套用抗鋸齒設定
            // PlayerGameSettingsUI 的選項: "關閉", "MSAA 2x", "MSAA 4x", "MSAA 8x"
            // 直接對應到 Unity 的 MSAA 值: 0, 2, 4, 8
            int previousAA = QualitySettings.antiAliasing;
            int[] antiAliasingValues = { 0, 2, 4, 8 }; // 對應上述選項
            if (antiAliasingLevel >= 0 && antiAliasingLevel < antiAliasingValues.Length)
            {
                QualitySettings.antiAliasing = antiAliasingValues[antiAliasingLevel];
                string[] antiAliasingNames = { "關閉", "MSAA 2x", "MSAA 4x", "MSAA 8x" };
                string currentAAName = (antiAliasingLevel < antiAliasingNames.Length) ? antiAliasingNames[antiAliasingLevel] : "未知";
                Debug.Log($"套用抗鋸齒: {currentAAName} (從 MSAA {previousAA}x 變更為 MSAA {antiAliasingValues[antiAliasingLevel]}x)");
                
                #if UNITY_EDITOR
                if (previousAA != QualitySettings.antiAliasing)
                {
                    Debug.Log($"[GameSettings] 抗鋸齒設定已變更！觀察方法:");
                    Debug.Log($"  1. 在 Game View 中觀察 3D 物體的邊緣是否更平滑");
                    Debug.Log($"  2. 特別注意直線、圓形、斜線的邊緣鋸齒狀況");
                    Debug.Log($"  3. 可在 Edit → Project Settings → Quality 中確認設定");
                    Debug.Log($"  4. 使用右鍵選單「測試抗鋸齒效果」進行對比測試");
                }
                else
                {
                    Debug.Log($"[GameSettings] 抗鋸齒設定未變更 (維持 MSAA {QualitySettings.antiAliasing}x)");
                }
                #endif
            }
            else
            {
                Debug.LogWarning($"[GameSettings] 抗鋸齒索引超出範圍: {antiAliasingLevel}");
            }
            
            // 記錄最終狀態
            Debug.Log($"[GameSettings] 套用後狀態 - targetFrameRate: {Application.targetFrameRate}, vSyncCount: {QualitySettings.vSyncCount}, antiAliasing: {QualitySettings.antiAliasing}");
            
            #if UNITY_EDITOR
            Debug.Log($"[GameSettings] 編輯器測試總結:");
            Debug.Log($"  - 解析度: 需要手動調整 Game View 大小測試");
            Debug.Log($"  - FPS: 使用 Profiler 或 Game View Stats 查看");
            Debug.Log($"  - VSync: 可能受編輯器影響，Build 版本更準確");
            Debug.Log($"  - 抗鋸齒: 應該在 Game View 中立即可見");
            #endif
            
            Debug.Log($"[GameSettings] 圖形設定套用完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"套用圖形設定時發生錯誤: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 套用音訊設定到 Unity 系統
    /// </summary>
    private void ApplyAudioSettings()
    {
        try
        {
            // 套用主音量
            AudioListener.volume = masterVolume;
            Debug.Log($"套用主音量: {masterVolume:F2}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"套用音訊設定時發生錯誤: {e.Message}");
        }
    }
}
