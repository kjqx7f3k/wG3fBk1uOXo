using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 簡單的本地化管理器 - 直接從CSV讀取多語言數據
/// 替代複雜的Unity Localization系統，實現用戶期望的簡單邏輯：
/// 語言改變 → 觸發事件 → UI更新文字
/// </summary>
public class SimpleLocalizationManager : MonoBehaviour
{
    public static SimpleLocalizationManager Instance { get; private set; }
    
    [Header("本地化設定")]
    [SerializeField] private bool enableDebugLog = false;
    [SerializeField] private string csvFileName = "UI.csv";
    
    // 本地化數據：[key][languageCode] = localizedText
    private Dictionary<string, Dictionary<string, string>> localizationData;
    private List<string> availableLanguages;
    
    private bool isInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLocalization();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 初始化本地化系統
    /// </summary>
    private void InitializeLocalization()
    {
        Debug.Log("[SimpleLocalizationManager] ===== 開始初始化簡單本地化系統 =====");
        
        localizationData = new Dictionary<string, Dictionary<string, string>>();
        availableLanguages = new List<string>();
        
        LoadLocalizationFromCSV();
        
        isInitialized = true;
        Debug.Log($"[SimpleLocalizationManager] ===== 本地化系統初始化完成，支持 {availableLanguages.Count} 種語言 =====");
    }
    
    /// <summary>
    /// 從CSV文件載入本地化數據
    /// </summary>
    private void LoadLocalizationFromCSV()
    {
        string csvPath = Path.Combine(Application.dataPath, "Localization", csvFileName);
        
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[SimpleLocalizationManager] 找不到本地化CSV文件: {csvPath}");
            return;
        }
        
        try
        {
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                Debug.LogError("[SimpleLocalizationManager] CSV文件格式錯誤：至少需要標題行和一行數據");
                return;
            }
            
            // 解析標題行，獲取支持的語言
            ParseHeaderLine(lines[0]);
            
            // 解析數據行
            int loadedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                if (ParseDataLine(lines[i], i + 1))
                {
                    loadedCount++;
                }
            }
            
            Debug.Log($"[SimpleLocalizationManager] 從CSV載入了 {loadedCount} 個本地化條目");
            
            if (enableDebugLog)
            {
                LogLoadedData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimpleLocalizationManager] 載入CSV文件失敗: {e.Message}");
        }
    }
    
    /// <summary>
    /// 解析CSV標題行，獲取支持的語言
    /// </summary>
    private void ParseHeaderLine(string headerLine)
    {
        string[] headers = ParseCSVLine(headerLine);
        
        if (headers.Length < 3)
        {
            Debug.LogError("[SimpleLocalizationManager] CSV標題行格式錯誤，至少需要: Key,Id,Language1");
            return;
        }
        
        // 跳過Key和Id列，從第3列開始是語言
        for (int i = 2; i < headers.Length; i++)
        {
            string header = headers[i].Trim();
            
            // 提取語言代碼，格式如 "English(en)" -> "en"
            string languageCode = ExtractLanguageCode(header);
            if (!string.IsNullOrEmpty(languageCode))
            {
                availableLanguages.Add(languageCode);
                if (enableDebugLog)
                {
                    Debug.Log($"[SimpleLocalizationManager] 發現支持語言: {languageCode} ({header})");
                }
            }
        }
        
        Debug.Log($"[SimpleLocalizationManager] 支持的語言: {string.Join(", ", availableLanguages)}");
    }
    
    /// <summary>
    /// 解析數據行
    /// </summary>
    private bool ParseDataLine(string dataLine, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(dataLine))
        {
            return false;
        }
        
        string[] values = ParseCSVLine(dataLine);
        
        if (values.Length < 3)
        {
            Debug.LogWarning($"[SimpleLocalizationManager] CSV第{lineNumber}行數據不足: {dataLine}");
            return false;
        }
        
        string key = values[0].Trim();
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[SimpleLocalizationManager] CSV第{lineNumber}行缺少Key: {dataLine}");
            return false;
        }
        
        // 為這個key創建語言字典
        if (!localizationData.ContainsKey(key))
        {
            localizationData[key] = new Dictionary<string, string>();
        }
        
        // 讀取每種語言的翻譯
        for (int i = 2; i < values.Length && i - 2 < availableLanguages.Count; i++)
        {
            string languageCode = availableLanguages[i - 2];
            string localizedText = values[i].Trim();
            
            if (!string.IsNullOrEmpty(localizedText))
            {
                localizationData[key][languageCode] = localizedText;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 解析CSV行，正確處理引號和逗號
    /// </summary>
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        result.Add(currentField);
        return result.ToArray();
    }
    
    /// <summary>
    /// 從標題中提取語言代碼
    /// </summary>
    private string ExtractLanguageCode(string header)
    {
        // 處理格式如 "English(en)", "Chinese (Traditional)(zh-TW)" 等
        int startIndex = header.LastIndexOf('(');
        int endIndex = header.LastIndexOf(')');
        
        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            return header.Substring(startIndex + 1, endIndex - startIndex - 1);
        }
        
        // 如果沒有括號，直接返回（可能就是語言代碼）
        return header.ToLower();
    }
    
    /// <summary>
    /// 獲取本地化文字
    /// </summary>
    public string GetLocalizedString(string key, string languageCode)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[SimpleLocalizationManager] 系統尚未初始化，返回原始鍵值: {key}");
            return key;
        }
        
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(languageCode))
        {
            return key;
        }
        
        if (localizationData.TryGetValue(key, out var languageDict))
        {
            if (languageDict.TryGetValue(languageCode, out string localizedText))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[SimpleLocalizationManager] 找到本地化文字: {key}[{languageCode}] = '{localizedText}'");
                }
                return localizedText;
            }
            else
            {
                // 嘗試使用英語作為fallback
                if (languageCode != "en" && languageDict.TryGetValue("en", out string fallbackText))
                {
                    Debug.LogWarning($"[SimpleLocalizationManager] 找不到 {key}[{languageCode}]，使用英語fallback: '{fallbackText}'");
                    return fallbackText;
                }
                
                Debug.LogWarning($"[SimpleLocalizationManager] 找不到本地化文字: {key}[{languageCode}]");
            }
        }
        else
        {
            Debug.LogWarning($"[SimpleLocalizationManager] 找不到本地化鍵值: {key}");
        }
        
        return key; // 返回鍵值作為fallback
    }
    
    /// <summary>
    /// 獲取支持的語言列表
    /// </summary>
    public List<string> GetAvailableLanguages()
    {
        return new List<string>(availableLanguages);
    }
    
    /// <summary>
    /// 檢查是否支持指定語言
    /// </summary>
    public bool IsLanguageSupported(string languageCode)
    {
        return availableLanguages.Contains(languageCode);
    }
    
    /// <summary>
    /// 重新載入本地化數據（用於熱更新）
    /// </summary>
    [ContextMenu("重新載入本地化數據")]
    public void ReloadLocalizationData()
    {
        Debug.Log("[SimpleLocalizationManager] 重新載入本地化數據...");
        
        localizationData.Clear();
        availableLanguages.Clear();
        
        LoadLocalizationFromCSV();
        
        Debug.Log("[SimpleLocalizationManager] 本地化數據重新載入完成");
    }
    
    /// <summary>
    /// 記錄載入的數據（用於除錯）
    /// </summary>
    private void LogLoadedData()
    {
        Debug.Log($"[SimpleLocalizationManager] ===== 載入的本地化數據 =====");
        Debug.Log($"支持的語言: {string.Join(", ", availableLanguages)}");
        Debug.Log($"載入的鍵值數量: {localizationData.Count}");
        
        if (localizationData.Count > 0)
        {
            var first5Keys = localizationData.Keys.Take(5);
            foreach (string key in first5Keys)
            {
                var translations = localizationData[key];
                var translationInfo = string.Join(", ", translations.Select(kv => $"{kv.Key}:'{kv.Value}'"));
                Debug.Log($"  {key} -> {translationInfo}");
            }
            
            if (localizationData.Count > 5)
            {
                Debug.Log($"  ... 還有 {localizationData.Count - 5} 個鍵值");
            }
        }
        
        Debug.Log($"[SimpleLocalizationManager] ===== 數據載入日誌結束 =====");
    }
    
    /// <summary>
    /// 獲取系統狀態（用於除錯）
    /// </summary>
    [ContextMenu("顯示系統狀態")]
    public void LogSystemStatus()
    {
        Debug.Log($"[SimpleLocalizationManager] ===== 系統狀態 =====");
        Debug.Log($"已初始化: {isInitialized}");
        Debug.Log($"支持的語言: {string.Join(", ", availableLanguages)}");
        Debug.Log($"載入的鍵值數量: {localizationData?.Count ?? 0}");
        Debug.Log($"除錯模式: {enableDebugLog}");
        Debug.Log($"CSV文件名: {csvFileName}");
        Debug.Log($"[SimpleLocalizationManager] ===== 狀態結束 =====");
    }
}