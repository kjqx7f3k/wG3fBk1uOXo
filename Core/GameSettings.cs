using UnityEngine;

/// <summary>
/// 遊戲全局設定管理器 - 管理遊戲中的全局設定和常數
/// 整合了原GameGlobals和GameSettings的功能
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }
    
    [Header("本地化設定")]
    [SerializeField] private string currentLanguage = "zh-TW";
    
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
    public string CurrentLanguage 
    { 
        get => currentLanguage; 
        set 
        { 
            currentLanguage = value;
            OnLanguageChanged?.Invoke(currentLanguage);
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
    
    // 事件
    public System.Action<string> OnLanguageChanged;
    public System.Action<float> OnGravityConstantChanged;
    public System.Action<float> OnGameSpeedChanged;
    public System.Action<bool> OnDebugModeChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化設定
            InitializeSettings();
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
        
        Debug.Log($"GameSettings 初始化完成:");
        Debug.Log($"  當前語言: {currentLanguage}");
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
        CurrentLanguage = "zh-TW";
        GravityConstant = 1.0f;
        DefaultMass = 1.0f;
        GameSpeed = 1.0f;
        DebugMode = false;
        UIAnimationSpeed = 1.0f;
        EnableUIAnimations = true;
        
        Debug.Log("GameSettings 已重置為預設值");
    }
    
    /// <summary>
    /// 儲存設定到PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetString("GameSettings_CurrentLanguage", currentLanguage);
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
            CurrentLanguage = PlayerPrefs.GetString("GameSettings_CurrentLanguage", "zh-TW");
            GravityConstant = PlayerPrefs.GetFloat("GameSettings_GravityConstant", 1.0f);
            DefaultMass = PlayerPrefs.GetFloat("GameSettings_DefaultMass", 1.0f);
            GameSpeed = PlayerPrefs.GetFloat("GameSettings_GameSpeed", 1.0f);
            DebugMode = PlayerPrefs.GetInt("GameSettings_DebugMode", 0) == 1;
            UIAnimationSpeed = PlayerPrefs.GetFloat("GameSettings_UIAnimationSpeed", 1.0f);
            EnableUIAnimations = PlayerPrefs.GetInt("GameSettings_EnableUIAnimations", 1) == 1;
            
            Debug.Log("GameSettings 設定已載入");
        }
        else if (PlayerPrefs.HasKey("GameGlobals_GravityConstant"))
        {
            // 相容性：載入舊GameGlobals設定
            GravityConstant = PlayerPrefs.GetFloat("GameGlobals_GravityConstant", 1.0f);
            DefaultMass = PlayerPrefs.GetFloat("GameGlobals_DefaultMass", 1.0f);
            GameSpeed = PlayerPrefs.GetFloat("GameGlobals_GameSpeed", 1.0f);
            DebugMode = PlayerPrefs.GetInt("GameGlobals_DebugMode", 0) == 1;
            UIAnimationSpeed = PlayerPrefs.GetFloat("GameGlobals_UIAnimationSpeed", 1.0f);
            EnableUIAnimations = PlayerPrefs.GetInt("GameGlobals_EnableUIAnimations", 1) == 1;
            
            Debug.Log("已載入舊GameGlobals設定，將自動轉換為GameSettings格式");
            SaveSettings(); // 立即保存為新格式
        }
        else
        {
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
               $"  重力常數: {gravityConstant}\n" +
               $"  預設質量: {defaultMass}\n" +
               $"  遊戲速度: {gameSpeed}\n" +
               $"  除錯模式: {debugMode}\n" +
               $"  UI動畫速度: {uiAnimationSpeed}\n" +
               $"  啟用UI動畫: {enableUIAnimations}";
    }
    
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
}
