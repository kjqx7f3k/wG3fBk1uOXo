using UnityEngine;
using TMPro;

/// <summary>
/// 本地化字體更新器 - 附加到 TextMeshProUGUI 組件以自動處理字體切換
/// 提供比全域 FontManager 更精細的控制
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedFontUpdater : MonoBehaviour
{
    [Header("字體設定")]
    [SerializeField] private bool useGlobalFontManager = true;
    [SerializeField] private string overrideLanguageCode = ""; // 如果設定，則使用指定語言而非當前語言
    
    [Header("自定義字體配置")]
    [SerializeField] private bool useCustomFontConfig = false;
    [SerializeField] private FontManager.LanguageFontConfig[] customFontConfigs;
    
    private TextMeshProUGUI textComponent;
    private System.Collections.Generic.Dictionary<string, FontManager.LanguageFontConfig> customConfigDict;
    private bool isInitialized = false;
    
    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        InitializeCustomConfigs();
    }
    
    private void Start()
    {
        InitializeFontUpdater();
    }
    
    private void OnEnable()
    {
        if (isInitialized)
        {
            RegisterEvents();
            UpdateFont();
        }
    }
    
    private void OnDisable()
    {
        UnregisterEvents();
    }
    
    /// <summary>
    /// 初始化字體更新器
    /// </summary>
    private void InitializeFontUpdater()
    {
        if (textComponent == null)
        {
            Debug.LogWarning($"[LocalizedFontUpdater] 找不到 TextMeshProUGUI 組件: {gameObject.name}");
            return;
        }
        
        RegisterEvents();
        
        // 如果使用全域管理器，註冊到 FontManager
        if (useGlobalFontManager && FontManager.Instance != null)
        {
            FontManager.Instance.RegisterTextComponent(textComponent);
        }
        
        // 立即更新字體
        UpdateFont();
        
        isInitialized = true;
    }
    
    /// <summary>
    /// 初始化自定義字體配置
    /// </summary>
    private void InitializeCustomConfigs()
    {
        if (useCustomFontConfig && customFontConfigs != null)
        {
            customConfigDict = new System.Collections.Generic.Dictionary<string, FontManager.LanguageFontConfig>();
            foreach (var config in customFontConfigs)
            {
                if (!string.IsNullOrEmpty(config.languageCode) && config.fontAsset != null)
                {
                    customConfigDict[config.languageCode] = config;
                }
            }
        }
    }
    
    /// <summary>
    /// 註冊事件
    /// </summary>
    private void RegisterEvents()
    {
        // 註冊 FontManager 的字體變更事件
        if (FontManager.Instance != null)
        {
            FontManager.Instance.OnFontChanged += OnFontChanged;
        }
        
        // 註冊 LocalizedUIHelper 的語言變更事件
        if (LocalizedUIHelper.Instance != null)
        {
            LocalizedUIHelper.Instance.OnLanguageChanged += OnLanguageChanged;
        }
        
        // 註冊 GameSettings 的語言變更事件
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged += OnGameSettingsLanguageChanged;
        }
    }
    
    /// <summary>
    /// 取消註冊事件
    /// </summary>
    private void UnregisterEvents()
    {
        if (FontManager.Instance != null)
        {
            FontManager.Instance.OnFontChanged -= OnFontChanged;
        }
        
        if (LocalizedUIHelper.Instance != null)
        {
            LocalizedUIHelper.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
        
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged -= OnGameSettingsLanguageChanged;
        }
    }
    
    /// <summary>
    /// 字體變更事件處理
    /// </summary>
    private void OnFontChanged(string languageCode)
    {
        if (!useGlobalFontManager)
        {
            UpdateFont();
        }
    }
    
    /// <summary>
    /// 語言變更事件處理（來自 LocalizedUIHelper）
    /// </summary>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        if (newLocale != null)
        {
            UpdateFont();
        }
    }
    
    /// <summary>
    /// 語言變更事件處理（來自 GameSettings）
    /// </summary>
    private void OnGameSettingsLanguageChanged(string languageCode)
    {
        UpdateFont();
    }
    
    /// <summary>
    /// 更新字體
    /// </summary>
    public void UpdateFont()
    {
        if (textComponent == null)
        {
            return;
        }
        
        string targetLanguage = GetTargetLanguageCode();
        var fontConfig = GetFontConfigForLanguage(targetLanguage);
        
        if (fontConfig != null)
        {
            ApplyFontConfig(fontConfig);
        }
        else if (FontManager.Instance != null)
        {
            // 如果找不到自定義配置，嘗試使用全域配置
            FontManager.Instance.UpdateTextComponentFont(textComponent, targetLanguage);
        }
    }
    
    /// <summary>
    /// 獲取目標語言代碼
    /// </summary>
    /// <returns>語言代碼</returns>
    private string GetTargetLanguageCode()
    {
        // 如果設定了覆蓋語言代碼，使用它
        if (!string.IsNullOrEmpty(overrideLanguageCode))
        {
            return overrideLanguageCode;
        }
        
        // 優先使用 FontManager 的當前語言
        if (FontManager.Instance != null)
        {
            string fontManagerLanguage = FontManager.Instance.GetCurrentLanguageCode();
            if (!string.IsNullOrEmpty(fontManagerLanguage))
            {
                return fontManagerLanguage;
            }
        }
        
        // 其次使用 LocalizedUIHelper 的當前語言
        if (LocalizedUIHelper.Instance != null)
        {
            return LocalizedUIHelper.Instance.GetCurrentLanguageCode();
        }
        
        // 最後使用 GameSettings 的語言設定
        if (GameSettings.Instance != null)
        {
            return GameSettings.Instance.CurrentLanguage;
        }
        
        return "zh-TW"; // 預設值
    }
    
    /// <summary>
    /// 獲取指定語言的字體配置
    /// </summary>
    /// <param name="languageCode">語言代碼</param>
    /// <returns>字體配置</returns>
    private FontManager.LanguageFontConfig GetFontConfigForLanguage(string languageCode)
    {
        // 如果使用自定義配置，優先查找自定義配置
        if (useCustomFontConfig && customConfigDict != null)
        {
            if (customConfigDict.TryGetValue(languageCode, out var customConfig))
            {
                return customConfig;
            }
        }
        
        // 如果沒有自定義配置或找不到，使用全域配置
        if (useGlobalFontManager && FontManager.Instance != null)
        {
            return FontManager.Instance.GetFontConfigForLanguage(languageCode);
        }
        
        return null;
    }
    
    /// <summary>
    /// 應用字體配置
    /// </summary>
    /// <param name="fontConfig">字體配置</param>
    private void ApplyFontConfig(FontManager.LanguageFontConfig fontConfig)
    {
        if (textComponent == null || fontConfig == null || fontConfig.fontAsset == null)
        {
            return;
        }
        
        // 保存原始設定（如果尚未保存）
        var originalSize = GetComponent<OriginalFontSize>();
        if (originalSize == null)
        {
            originalSize = gameObject.AddComponent<OriginalFontSize>();
            originalSize.originalSize = textComponent.fontSize;
            originalSize.originalLineSpacing = textComponent.lineSpacing;
        }
        
        // 應用字體
        textComponent.font = fontConfig.fontAsset;
        
        // 應用字體大小調整
        if (fontConfig.fontSizeMultiplier != 1.0f)
        {
            textComponent.fontSize = originalSize.originalSize * fontConfig.fontSizeMultiplier;
        }
        
        // 應用行距調整
        if (fontConfig.lineSpacingAdjustment != 0f)
        {
            textComponent.lineSpacing = originalSize.originalLineSpacing + fontConfig.lineSpacingAdjustment;
        }
        
        Debug.Log($"[LocalizedFontUpdater] 已更新 {gameObject.name} 的字體: {fontConfig.fontAsset.name}");
    }
    
    /// <summary>
    /// 設定覆蓋語言代碼
    /// </summary>
    /// <param name="languageCode">語言代碼</param>
    public void SetOverrideLanguageCode(string languageCode)
    {
        overrideLanguageCode = languageCode;
        UpdateFont();
    }
    
    /// <summary>
    /// 清除覆蓋語言代碼
    /// </summary>
    public void ClearOverrideLanguageCode()
    {
        overrideLanguageCode = "";
        UpdateFont();
    }
    
    /// <summary>
    /// 設定是否使用全域字體管理器
    /// </summary>
    /// <param name="useGlobal">是否使用全域管理器</param>
    public void SetUseGlobalFontManager(bool useGlobal)
    {
        if (useGlobalFontManager != useGlobal)
        {
            useGlobalFontManager = useGlobal;
            
            if (useGlobal && FontManager.Instance != null)
            {
                FontManager.Instance.RegisterTextComponent(textComponent);
            }
            else if (!useGlobal && FontManager.Instance != null)
            {
                FontManager.Instance.UnregisterTextComponent(textComponent);
            }
            
            UpdateFont();
        }
    }
    
    private void OnDestroy()
    {
        // 從 FontManager 取消註冊
        if (FontManager.Instance != null && textComponent != null)
        {
            FontManager.Instance.UnregisterTextComponent(textComponent);
        }
        
        UnregisterEvents();
    }
}