using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 本地化 UI 輔助類 - 提供程式碼中使用本地化文字的便捷接口
/// 整合 Unity Localization Package 與現有 UI 系統
/// </summary>
public class LocalizedUIHelper : MonoBehaviour
{
    public static LocalizedUIHelper Instance { get; private set; }
    
    [Header("本地化設定")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private string defaultLocaleCode = "zh-TW";
    
    // 本地化狀態
    private bool isInitialized = false;
    private Dictionary<string, StringTable> cachedStringTables = new Dictionary<string, StringTable>();
    
    // 事件
    public System.Action OnLocalizationInitialized;
    public System.Action<Locale> OnLanguageChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (initializeOnAwake)
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
    /// 初始化本地化系統
    /// </summary>
    public IEnumerator InitializeLocalization()
    {
        if (isInitialized)
            yield break;
            
        Debug.Log("[LocalizedUIHelper] 正在初始化本地化系統...");
        
        // 等待 LocalizationSettings 初始化完成
        yield return LocalizationSettings.InitializationOperation;
        
        // 設定預設語言
        if (!string.IsNullOrEmpty(defaultLocaleCode))
        {
            var defaultLocale = LocalizationSettings.AvailableLocales.GetLocale(defaultLocaleCode);
            if (defaultLocale != null)
            {
                LocalizationSettings.SelectedLocale = defaultLocale;
            }
        }
        
        // 預載入常用 String Table
        yield return PreloadCommonStringTables();
        
        // 註冊語言變更事件
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        
        isInitialized = true;
        OnLocalizationInitialized?.Invoke();
        
        Debug.Log($"[LocalizedUIHelper] 本地化系統初始化完成，當前語言: {LocalizationSettings.SelectedLocale?.LocaleName}");
    }
    
    /// <summary>
    /// 預載入常用的 String Table
    /// </summary>
    private IEnumerator PreloadCommonStringTables()
    {
        string[] commonTables = { "UI_Tables", "Dialog_Tables", "System_Tables", "Items_Tables" };
        
        foreach (string tableName in commonTables)
        {
            var loadOperation = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
            yield return loadOperation;
            
            if (loadOperation.Result != null)
            {
                cachedStringTables[tableName] = loadOperation.Result;
                Debug.Log($"[LocalizedUIHelper] 預載入 String Table: {tableName}");
            }
            else
            {
                Debug.LogWarning($"[LocalizedUIHelper] 無法載入 String Table: {tableName}");
            }
        }
    }
    
    /// <summary>
    /// 語言變更事件處理
    /// </summary>
    private void OnSelectedLocaleChanged(Locale newLocale)
    {
        Debug.Log($"[LocalizedUIHelper] 語言已切換至: {newLocale.LocaleName}");
        
        // 清除緩存的 String Table，強制重新載入
        cachedStringTables.Clear();
        StartCoroutine(PreloadCommonStringTables());
        
        OnLanguageChanged?.Invoke(newLocale);
    }
    
    /// <summary>
    /// 獲取本地化文字（同步）
    /// </summary>
    /// <param name="tableName">String Table 名稱</param>
    /// <param name="entryKey">文字鍵值</param>
    /// <returns>本地化文字，找不到時返回鍵值</returns>
    public string GetLocalizedString(string tableName, string entryKey)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[LocalizedUIHelper] 本地化系統尚未初始化，返回原始鍵值: {entryKey}");
            return entryKey;
        }
        
        // 嘗試從緩存獲取
        if (cachedStringTables.TryGetValue(tableName, out StringTable table))
        {
            var entry = table.GetEntry(entryKey);
            if (entry != null)
            {
                return entry.GetLocalizedString();
            }
        }
        
        // 如果緩存中沒有，嘗試即時載入
        var stringTable = LocalizationSettings.StringDatabase.GetTable(tableName);
        if (stringTable != null)
        {
            var entry = stringTable.GetEntry(entryKey);
            if (entry != null)
            {
                return entry.GetLocalizedString();
            }
        }
        
        Debug.LogWarning($"[LocalizedUIHelper] 找不到本地化文字: {tableName}.{entryKey}");
        return entryKey; // 返回鍵值作為 fallback
    }
    
    /// <summary>
    /// 獲取本地化文字（異步）
    /// </summary>
    /// <param name="tableName">String Table 名稱</param>
    /// <param name="entryKey">文字鍵值</param>
    /// <param name="callback">回調函數</param>
    public void GetLocalizedStringAsync(string tableName, string entryKey, System.Action<string> callback)
    {
        StartCoroutine(GetLocalizedStringCoroutine(tableName, entryKey, callback));
    }
    
    private IEnumerator GetLocalizedStringCoroutine(string tableName, string entryKey, System.Action<string> callback)
    {
        if (!isInitialized)
        {
            yield return new WaitUntil(() => isInitialized);
        }
        
        var loadOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, entryKey);
        yield return loadOperation;
        
        if (loadOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            callback?.Invoke(loadOperation.Result);
        }
        else
        {
            Debug.LogWarning($"[LocalizedUIHelper] 異步載入本地化文字失敗: {tableName}.{entryKey}");
            callback?.Invoke(entryKey);
        }
    }
    
    /// <summary>
    /// 更新 TextMeshProUGUI 組件的本地化文字
    /// </summary>
    /// <param name="textComponent">文字組件</param>
    /// <param name="tableName">String Table 名稱</param>
    /// <param name="entryKey">文字鍵值</param>
    public void UpdateLocalizedText(TextMeshProUGUI textComponent, string tableName, string entryKey)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("[LocalizedUIHelper] TextMeshProUGUI 組件為 null");
            return;
        }
        
        GetLocalizedStringAsync(tableName, entryKey, (localizedText) =>
        {
            if (textComponent != null) // 確保組件仍然存在
            {
                textComponent.text = localizedText;
            }
        });
    }
    
    /// <summary>
    /// 切換語言
    /// </summary>
    /// <param name="localeCode">語言代碼 (如: zh-TW, en, ja)</param>
    /// <returns>是否切換成功</returns>
    public bool ChangeLanguage(string localeCode)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[LocalizedUIHelper] 本地化系統尚未初始化");
            return false;
        }
        
        var targetLocale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (targetLocale == null)
        {
            Debug.LogWarning($"[LocalizedUIHelper] 找不到語言: {localeCode}");
            return false;
        }
        
        LocalizationSettings.SelectedLocale = targetLocale;
        return true;
    }
    
    /// <summary>
    /// 獲取當前語言代碼
    /// </summary>
    /// <returns>當前語言代碼</returns>
    public string GetCurrentLanguageCode()
    {
        if (!isInitialized || LocalizationSettings.SelectedLocale == null)
        {
            return defaultLocaleCode;
        }
        
        return LocalizationSettings.SelectedLocale.Identifier.Code;
    }
    
    /// <summary>
    /// 獲取所有可用語言
    /// </summary>
    /// <returns>語言代碼列表</returns>
    public List<string> GetAvailableLanguages()
    {
        List<string> languages = new List<string>();
        
        if (!isInitialized)
        {
            return languages;
        }
        
        var locales = LocalizationSettings.AvailableLocales.Locales;
        foreach (var locale in locales)
        {
            languages.Add(locale.Identifier.Code);
        }
        
        return languages;
    }
    
    /// <summary>
    /// 檢查本地化系統是否已初始化
    /// </summary>
    /// <returns>是否已初始化</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            // 取消註冊事件
            if (isInitialized)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            }
            
            Instance = null;
        }
    }
}