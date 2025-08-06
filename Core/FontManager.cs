using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 字體管理器 - 管理不同語言對應的字體配置
/// 與 Unity Localization Package 整合，支援自動字體切換
/// </summary>
public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }
    
    [System.Serializable]
    public class LanguageFontConfig
    {
        public string languageCode;
        public TMP_FontAsset fontAsset;
        public float fontSizeMultiplier = 1.0f;
        public float lineSpacingAdjustment = 0f;
    }
    
    [Header("字體配置")]
    [SerializeField] private LanguageFontConfig[] languageFontConfigs;
    [SerializeField] private TMP_FontAsset fallbackFont;
    
    [Header("自動更新設定")]
    [SerializeField] private bool autoUpdateAllTextComponents = true;
    [SerializeField] private bool updateOnLanguageChange = true;
    
    // 緩存
    private Dictionary<string, LanguageFontConfig> fontConfigDict = new Dictionary<string, LanguageFontConfig>();
    private List<TextMeshProUGUI> registeredTextComponents = new List<TextMeshProUGUI>();
    private string currentLanguageCode = "";
    
    // 事件
    public System.Action<string> OnFontChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFontManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 註冊語言變更事件
        if (updateOnLanguageChange && LocalizedUIHelper.Instance != null)
        {
            LocalizedUIHelper.Instance.OnLanguageChanged += OnLanguageChanged;
        }
        
        // 如果 GameSettings 存在，也註冊其語言變更事件
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged += OnGameSettingsLanguageChanged;
        }
    }
    
    /// <summary>
    /// 初始化字體管理器
    /// </summary>
    private void InitializeFontManager()
    {
        // 建立語言代碼到字體配置的字典
        fontConfigDict.Clear();
        foreach (var config in languageFontConfigs)
        {
            if (!string.IsNullOrEmpty(config.languageCode) && config.fontAsset != null)
            {
                fontConfigDict[config.languageCode] = config;
            }
        }
        
        // 設定預設字體為 fallback
        if (fallbackFont == null && languageFontConfigs.Length > 0)
        {
            fallbackFont = languageFontConfigs[0].fontAsset;
        }
        
        Debug.Log($"[FontManager] 初始化完成，載入 {fontConfigDict.Count} 個字體配置");
    }
    
    /// <summary>
    /// 語言變更事件處理（來自 LocalizedUIHelper）
    /// </summary>
    private void OnLanguageChanged(Locale newLocale)
    {
        if (newLocale != null)
        {
            ApplyFontForLanguage(newLocale.Identifier.Code);
        }
    }
    
    /// <summary>
    /// 語言變更事件處理（來自 GameSettings）
    /// </summary>
    private void OnGameSettingsLanguageChanged(string languageCode)
    {
        ApplyFontForLanguage(languageCode);
    }
    
    /// <summary>
    /// 為指定語言應用字體
    /// </summary>
    /// <param name="languageCode">語言代碼</param>
    public void ApplyFontForLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || languageCode == currentLanguageCode)
        {
            return;
        }
        
        currentLanguageCode = languageCode;
        
        // 獲取對應的字體配置
        var fontConfig = GetFontConfigForLanguage(languageCode);
        if (fontConfig == null)
        {
            Debug.LogWarning($"[FontManager] 找不到語言 {languageCode} 的字體配置，使用 fallback 字體");
            fontConfig = new LanguageFontConfig 
            { 
                languageCode = languageCode, 
                fontAsset = fallbackFont, 
                fontSizeMultiplier = 1.0f 
            };
        }
        
        // 更新所有註冊的文字組件
        if (autoUpdateAllTextComponents)
        {
            UpdateAllRegisteredTextComponents(fontConfig);
        }
        
        // 如果啟用全域更新，尋找場景中所有 TextMeshProUGUI 組件
        if (autoUpdateAllTextComponents)
        {
            UpdateAllSceneTextComponents(fontConfig);
        }
        
        OnFontChanged?.Invoke(languageCode);
        Debug.Log($"[FontManager] 已切換到語言 {languageCode} 的字體: {fontConfig.fontAsset?.name}");
    }
    
    /// <summary>
    /// 獲取指定語言的字體配置
    /// </summary>
    /// <param name="languageCode">語言代碼</param>
    /// <returns>字體配置，找不到時返回 null</returns>
    public LanguageFontConfig GetFontConfigForLanguage(string languageCode)
    {
        fontConfigDict.TryGetValue(languageCode, out LanguageFontConfig config);
        return config;
    }
    
    /// <summary>
    /// 註冊文字組件以便自動更新字體
    /// </summary>
    /// <param name="textComponent">要註冊的文字組件</param>
    public void RegisterTextComponent(TextMeshProUGUI textComponent)
    {
        if (textComponent != null && !registeredTextComponents.Contains(textComponent))
        {
            registeredTextComponents.Add(textComponent);
            
            // 立即應用當前語言的字體
            if (!string.IsNullOrEmpty(currentLanguageCode))
            {
                var fontConfig = GetFontConfigForLanguage(currentLanguageCode);
                if (fontConfig != null)
                {
                    ApplyFontToTextComponent(textComponent, fontConfig);
                }
            }
        }
    }
    
    /// <summary>
    /// 取消註冊文字組件
    /// </summary>
    /// <param name="textComponent">要取消註冊的文字組件</param>
    public void UnregisterTextComponent(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            registeredTextComponents.Remove(textComponent);
        }
    }
    
    /// <summary>
    /// 手動更新指定文字組件的字體
    /// </summary>
    /// <param name="textComponent">文字組件</param>
    /// <param name="languageCode">語言代碼（可選，不指定則使用當前語言）</param>
    public void UpdateTextComponentFont(TextMeshProUGUI textComponent, string languageCode = "")
    {
        if (textComponent == null)
        {
            return;
        }
        
        string targetLanguage = string.IsNullOrEmpty(languageCode) ? currentLanguageCode : languageCode;
        var fontConfig = GetFontConfigForLanguage(targetLanguage);
        
        if (fontConfig != null)
        {
            ApplyFontToTextComponent(textComponent, fontConfig);
        }
    }
    
    /// <summary>
    /// 應用字體配置到文字組件
    /// </summary>
    /// <param name="textComponent">文字組件</param>
    /// <param name="fontConfig">字體配置</param>
    private void ApplyFontToTextComponent(TextMeshProUGUI textComponent, LanguageFontConfig fontConfig)
    {
        if (textComponent == null || fontConfig == null || fontConfig.fontAsset == null)
        {
            return;
        }
        
        // 保存原始字體大小（如果是第一次設定）
        if (!textComponent.gameObject.GetComponent<OriginalFontSize>())
        {
            var originalSize = textComponent.gameObject.AddComponent<OriginalFontSize>();
            originalSize.originalSize = textComponent.fontSize;
            originalSize.originalLineSpacing = textComponent.lineSpacing;
        }
        
        var originalSizeComponent = textComponent.gameObject.GetComponent<OriginalFontSize>();
        
        // 應用字體
        textComponent.font = fontConfig.fontAsset;
        
        // 應用字體大小調整
        if (fontConfig.fontSizeMultiplier != 1.0f)
        {
            textComponent.fontSize = originalSizeComponent.originalSize * fontConfig.fontSizeMultiplier;
        }
        
        // 應用行距調整
        if (fontConfig.lineSpacingAdjustment != 0f)
        {
            textComponent.lineSpacing = originalSizeComponent.originalLineSpacing + fontConfig.lineSpacingAdjustment;
        }
    }
    
    /// <summary>
    /// 更新所有註冊的文字組件
    /// </summary>
    /// <param name="fontConfig">字體配置</param>
    private void UpdateAllRegisteredTextComponents(LanguageFontConfig fontConfig)
    {
        // 清理已被銷毀的組件
        registeredTextComponents.RemoveAll(text => text == null);
        
        foreach (var textComponent in registeredTextComponents)
        {
            ApplyFontToTextComponent(textComponent, fontConfig);
        }
    }
    
    /// <summary>
    /// 更新場景中所有 TextMeshProUGUI 組件
    /// </summary>
    /// <param name="fontConfig">字體配置</param>
    private void UpdateAllSceneTextComponents(LanguageFontConfig fontConfig)
    {
        var allTextComponents = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var textComponent in allTextComponents)
        {
            // 檢查是否有 LocalizedFontUpdater 組件（如果有，則跳過自動更新）
            if (textComponent.GetComponent<LocalizedFontUpdater>() == null)
            {
                ApplyFontToTextComponent(textComponent, fontConfig);
            }
        }
    }
    
    /// <summary>
    /// 獲取當前語言代碼
    /// </summary>
    /// <returns>當前語言代碼</returns>
    public string GetCurrentLanguageCode()
    {
        return currentLanguageCode;
    }
    
    /// <summary>
    /// 獲取可用的語言列表
    /// </summary>
    /// <returns>語言代碼列表</returns>
    public List<string> GetAvailableLanguages()
    {
        return new List<string>(fontConfigDict.Keys);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            // 取消註冊事件
            if (LocalizedUIHelper.Instance != null)
            {
                LocalizedUIHelper.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
            
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnLanguageChanged -= OnGameSettingsLanguageChanged;
            }
            
            Instance = null;
        }
    }
}

/// <summary>
/// 用於保存文字組件原始字體大小的輔助組件
/// </summary>
public class OriginalFontSize : MonoBehaviour
{
    public float originalSize;
    public float originalLineSpacing;
}