using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using UnityEngine;

/// <summary>
/// 對話數據載入器 - 統一處理文件載入、JSON解析和DialogLine轉換
/// 整合了文件讀取、解析和緩存管理功能
/// 支援多語言對話系統
/// </summary>
public static class DialogDataLoader
{
    #region 本地化系統數據結構
    
    /// <summary>
    /// 語言-對話映射：Dictionary&lt;語言代碼, Dictionary&lt;對話ID, DialogData&gt;&gt;
    /// </summary>
    private static Dictionary<string, Dictionary<string, BaseDialogManager.DialogData>> languageDialogMap = new Dictionary<string, Dictionary<string, BaseDialogManager.DialogData>>();
    
    /// <summary>
    /// 當前語言代碼
    /// </summary>
    private static string currentLanguage = "en";
    
    /// <summary>
    /// 是否已初始化本地化系統
    /// </summary>
    private static bool isLocalizationInitialized = false;
    
    #endregion
    /// <summary>
    /// 載入對話數據的結果結構
    /// </summary>
    public struct LoadResult
    {
        public Dictionary<int, BaseDialogManager.DialogLine> dialogLines;
        public BaseDialogManager.DialogData dialogData;
        public bool success;
        
        public LoadResult(Dictionary<int, BaseDialogManager.DialogLine> dialogLines, BaseDialogManager.DialogData dialogData, bool success)
        {
            this.dialogLines = dialogLines;
            this.dialogData = dialogData;
            this.success = success;
        }
    }
    
    /// <summary>
    /// 載入對話數據 - 主要入口點，整合緩存檢查
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="forceReload">是否強制重新載入（忽略緩存）</param>
    /// <returns>載入結果</returns>
    public static LoadResult LoadDialogData(string fileName, bool forceReload = false)
    {
        // 開始測量總載入時間
        System.Diagnostics.Stopwatch totalStopwatch = new System.Diagnostics.Stopwatch();
        totalStopwatch.Start();
        
        // 檢查緩存（除非強制重新載入）
        if (!forceReload && DialogCacheManager.IsDialogCached(fileName))
        {
            var cachedDialogData = DialogCacheManager.GetCachedDialogData(fileName);
            
            if (cachedDialogData != null)
            {
                // 每次都重新生成DialogLine以確保條件檢查是最新的
                Dictionary<int, BaseDialogManager.DialogLine> dialogLines = ConvertToDialogLines(cachedDialogData, fileName);
                
                totalStopwatch.Stop();
                Debug.Log($"從緩存重新生成對話總耗時：{totalStopwatch.Elapsed.TotalMilliseconds} 毫秒 - {fileName}");
                return new LoadResult(dialogLines, cachedDialogData, true);
            }
        }
        
        // 從文件載入
        LoadResult result = LoadFromFile(fileName);
        
        // 如果載入成功，緩存DialogData（不緩存DialogLine）
        if (result.success)
        {
            DialogCacheManager.CacheDialog(fileName, result.dialogData);
        }
        
        totalStopwatch.Stop();
        Debug.Log($"載入對話總耗時：{totalStopwatch.Elapsed.TotalMilliseconds} 毫秒 - {fileName}");
        
        return result;
    }
    
    /// <summary>
    /// 從文件載入對話數據
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>載入結果</returns>
    private static LoadResult LoadFromFile(string fileName)
    {
        // 載入文件內容
        string fileContent = LoadFileContent(fileName);
        if (fileContent == null)
        {
            return new LoadResult(new Dictionary<int, BaseDialogManager.DialogLine>(), null, false);
        }
        
        // 檢查文件格式
        if (!IsJsonFile(fileName))
        {
            Debug.LogWarning($"未知的對話文件格式: {GetFileExtension(fileName)}，默認使用Json格式處理 - {fileName}");
        }
        
        // 解析JSON內容
        BaseDialogManager.DialogData dialogData = ParseJsonContent(fileContent, fileName);
        if (dialogData == null)
        {
            return new LoadResult(new Dictionary<int, BaseDialogManager.DialogLine>(), null, false);
        }
        
        // 轉換為DialogLine字典
        Dictionary<int, BaseDialogManager.DialogLine> dialogLines = ConvertToDialogLines(dialogData, fileName);
        
        return new LoadResult(dialogLines, dialogData, true);
    }
    
    /// <summary>
    /// 載入文件內容
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件內容字符串，失敗時返回null</returns>
    private static string LoadFileContent(string fileName)
    {
        string filePath = GetDialogFilePath(fileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"對話文件不存在: {filePath}");
            return null;
        }
        
        try
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            string fileContent = File.ReadAllText(filePath);
            
            stopwatch.Stop();
            Debug.Log($"文件載入耗時：{stopwatch.Elapsed.TotalMilliseconds} 毫秒 - {fileName}");
            
            return fileContent;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"載入對話文件時發生錯誤 {fileName}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 解析JSON內容為DialogData
    /// </summary>
    /// <param name="jsonContent">JSON內容字符串</param>
    /// <param name="fileName">文件名（用於錯誤報告）</param>
    /// <returns>解析後的DialogData，失敗時返回null</returns>
    private static BaseDialogManager.DialogData ParseJsonContent(string jsonContent, string fileName)
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            Debug.LogError($"JSON內容為空: {fileName}");
            return null;
        }
        
        try
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            BaseDialogManager.DialogData dialogData = JsonUtility.FromJson<BaseDialogManager.DialogData>(jsonContent);
            
            stopwatch.Stop();
            Debug.Log($"JSON解析耗時：{stopwatch.Elapsed.TotalMilliseconds} 毫秒 - {fileName}");
            
            if (dialogData == null || dialogData.dialogs == null)
            {
                Debug.LogError($"JSON對話文件格式錯誤: {fileName}");
                return null;
            }
            
            Debug.Log($"已解析JSON對話: {dialogData.dialogName} (版本: {dialogData.version}) - {fileName}");
            return dialogData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析JSON對話文件時發生錯誤 {fileName}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 將DialogData轉換為DialogLine字典
    /// </summary>
    /// <param name="dialogData">對話數據</param>
    /// <param name="fileName">文件名（用於錯誤報告）</param>
    /// <returns>DialogLine字典</returns>
    private static Dictionary<int, BaseDialogManager.DialogLine> ConvertToDialogLines(BaseDialogManager.DialogData dialogData, string fileName)
    {
        Dictionary<int, BaseDialogManager.DialogLine> dialogLines = new Dictionary<int, BaseDialogManager.DialogLine>();
        
        if (dialogData?.dialogs == null)
        {
            Debug.LogError($"DialogData或dialogs為null: {fileName}");
            return dialogLines;
        }
        
        try
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            foreach (BaseDialogManager.DialogEntry entry in dialogData.dialogs)
            {
                BaseDialogManager.DialogLine dialogLine = new BaseDialogManager.DialogLine(
                    entry.id, 
                    entry.nextId, 
                    entry.expressionId, 
                    entry.text ?? "", 
                    entry.events
                );
                
                // 處理選項（需要根據條件過濾）
                if (entry.options != null)
                {
                    ProcessDialogOptions(entry.options, dialogLine);
                }
                
                dialogLines[entry.id] = dialogLine;
            }
            
            stopwatch.Stop();
            Debug.Log($"DialogLine轉換耗時：{stopwatch.Elapsed.TotalMilliseconds} 毫秒 - {fileName}");
            Debug.Log($"成功轉換 {dialogLines.Count} 行對話從文件: {fileName}");
            
            return dialogLines;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"轉換DialogLine時發生錯誤 {fileName}: {e.Message}");
            return new Dictionary<int, BaseDialogManager.DialogLine>();
        }
    }
    
    /// <summary>
    /// 處理對話選項（包含條件檢查）
    /// </summary>
    /// <param name="optionEntries">選項條目數組</param>
    /// <param name="dialogLine">目標對話行</param>
    private static void ProcessDialogOptions(BaseDialogManager.DialogOptionEntry[] optionEntries, BaseDialogManager.DialogLine dialogLine)
    {
        foreach (BaseDialogManager.DialogOptionEntry optionEntry in optionEntries)
        {
            // 檢查選項是否應該顯示
            if (optionEntry.condition == null || DialogConditionChecker.CheckCondition(optionEntry.condition))
            {
                dialogLine.options.Add(new BaseDialogManager.DialogOption(optionEntry.text, optionEntry.nextId));
            }
            else
            {
                // 條件不滿足，可以選擇顯示failText或完全隱藏
                if (!string.IsNullOrEmpty(optionEntry.failText))
                {
                    // 顯示failText作為不可選擇的選項
                    dialogLine.options.Add(new BaseDialogManager.DialogOption($"[{optionEntry.failText}]", -1));
                }
            }
        }
    }
    
    #region 條件檢查系統
    
    
    #endregion
    
    #region 文件工具方法
    
    /// <summary>
    /// 獲取對話文件的完整路徑
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>完整文件路徑</returns>
    public static string GetDialogFilePath(string fileName)
    {
        return Path.Combine(Application.dataPath, fileName);
    }
    
    /// <summary>
    /// 獲取文件擴展名
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件擴展名（小寫）</returns>
    public static string GetFileExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLower();
    }
    
    /// <summary>
    /// 檢查文件是否為JSON格式
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>是否為JSON文件</returns>
    public static bool IsJsonFile(string fileName)
    {
        return GetFileExtension(fileName) == ".json";
    }
    
    /// <summary>
    /// 檢查對話文件是否存在
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件是否存在</returns>
    public static bool DialogFileExists(string fileName)
    {
        string filePath = GetDialogFilePath(fileName);
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// 獲取文件信息（用於調試）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件信息字符串</returns>
    public static string GetFileInfo(string fileName)
    {
        string filePath = GetDialogFilePath(fileName);
        
        if (!File.Exists(filePath))
        {
            return $"文件不存在: {fileName}";
        }
        
        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return $"文件: {fileName}, 大小: {fileInfo.Length} bytes, 修改時間: {fileInfo.LastWriteTime}";
        }
        catch (System.Exception e)
        {
            return $"無法獲取文件信息: {e.Message}";
        }
    }
    
    #endregion
    
    #region 二進制序列化功能
    
    /// <summary>
    /// 將 DialogData 序列化並保存為二進制文件
    /// </summary>
    /// <param name="dialogData">要保存的對話數據</param>
    /// <param name="fileName">文件名（不含副檔名）</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveDialogDataToBinary(BaseDialogManager.DialogData dialogData, string fileName)
    {
        if (dialogData == null)
        {
            Debug.LogError("DialogData 為 null，無法保存為二進制文件");
            return false;
        }
        
        string filePath = GetDialogBinaryFilePath(fileName);
        
        try
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            // 確保目錄存在
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                formatter.Serialize(stream, dialogData);
            }
            
            stopwatch.Stop();
            Debug.Log($"DialogData 二進制保存完成，耗時：{stopwatch.Elapsed.TotalMilliseconds} 毫秒 - {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存 DialogData 二進制文件時發生錯誤 {fileName}: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 從二進制文件載入 DialogData
    /// </summary>
    /// <param name="fileName">文件名（不含副檔名）</param>
    /// <returns>載入的 DialogData，失敗時返回 null</returns>
    public static BaseDialogManager.DialogData LoadDialogDataFromBinary(string fileName)
    {
        string filePath = GetDialogBinaryFilePath(fileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"二進制對話文件不存在: {filePath}");
            return null;
        }
        
        try
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                BaseDialogManager.DialogData dialogData = (BaseDialogManager.DialogData)formatter.Deserialize(stream);
                
                stopwatch.Stop();
                Debug.Log($"DialogData 二進制載入完成，耗時：{stopwatch.Elapsed.TotalMilliseconds} 毫秒 - {filePath}");
                Debug.Log($"載入對話: {dialogData.dialogName} (版本: {dialogData.version}) - {fileName}");
                
                return dialogData;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"載入 DialogData 二進制文件時發生錯誤 {fileName}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 獲取二進制對話文件的完整路徑
    /// </summary>
    /// <param name="fileName">文件名（不含副檔名）</param>
    /// <returns>完整文件路徑</returns>
    public static string GetDialogBinaryFilePath(string fileName)
    {
        // 確保文件有 .ekqolt 副檔名
        if (!fileName.EndsWith(".ekqolt"))
        {
            fileName += ".ekqolt";
        }
        return Path.Combine(Application.persistentDataPath, "DialogCache", fileName);
    }
    
    /// <summary>
    /// 檢查二進制對話文件是否存在
    /// </summary>
    /// <param name="fileName">文件名（不含副檔名）</param>
    /// <returns>文件是否存在</returns>
    public static bool DialogBinaryFileExists(string fileName)
    {
        string filePath = GetDialogBinaryFilePath(fileName);
        return File.Exists(filePath);
    }
    
    
    #endregion
    
    
    /// <summary>
    /// 獲取指定目錄下特定擴展名的對話文件列表
    /// </summary>
    /// <param name="directoryPath">目錄路徑</param>
    /// <param name="searchPattern">搜尋模式（如 "*.json", "*.ekqolt"）</param>
    /// <returns>文件路徑列表</returns>
    private static List<string> GetDialogFileList(string directoryPath, string searchPattern)
    {
        List<string> files = new List<string>();
        
        if (!Directory.Exists(directoryPath))
        {
            Debug.LogWarning($"目錄不存在: {directoryPath}");
            return files;
        }
        
        try
        {
            string[] foundFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
            files.AddRange(foundFiles);
            
            Debug.Log($"在目錄 {directoryPath} 中找到 {files.Count} 個 {searchPattern} 文件");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"搜尋文件時發生錯誤 {directoryPath}: {e.Message}");
        }
        
        return files;
    }
    
    
    #region 本地化系統核心功能
    
    /// <summary>
    /// 載入所有語言資料夾下的對話文件，建立語言-對話映射
    /// </summary>
    /// <param name="basePath">對話文件的基礎路徑，如 "StreamingAssets/Dialogs"</param>
    /// <returns>成功載入的語言數量</returns>
    public static int LoadAllLanguageDialogs(string basePath)
    {
        System.Diagnostics.Stopwatch totalStopwatch = new System.Diagnostics.Stopwatch();
        totalStopwatch.Start();
        
        // 清空現有映射
        languageDialogMap.Clear();
        
        string fullBasePath = Path.Combine(Application.dataPath, basePath);
        
        if (!Directory.Exists(fullBasePath))
        {
            Debug.LogError($"本地化對話基礎路徑不存在: {fullBasePath}");
            return 0;
        }
        
        // 掃描所有語言資料夾
        string[] languageFolders = Directory.GetDirectories(fullBasePath);
        
        if (languageFolders.Length == 0)
        {
            Debug.LogWarning($"在路徑中未找到任何語言資料夾: {fullBasePath}");
            return 0;
        }
        
        Debug.Log($"開始載入 {languageFolders.Length} 個語言的對話文件...");
        
        int successLanguageCount = 0;
        int totalDialogCount = 0;
        
        foreach (string languageFolderPath in languageFolders)
        {
            string languageCode = Path.GetFileName(languageFolderPath);
            int dialogCount = LoadLanguageDialogs(languageCode, languageFolderPath);
            
            if (dialogCount > 0)
            {
                successLanguageCount++;
                totalDialogCount += dialogCount;
                Debug.Log($"語言 '{languageCode}' 載入完成，共 {dialogCount} 個對話");
            }
            else
            {
                Debug.LogWarning($"語言 '{languageCode}' 載入失敗或無對話文件");
            }
        }
        
        totalStopwatch.Stop();
        
        if (successLanguageCount > 0)
        {
            isLocalizationInitialized = true;
            Debug.Log($"本地化系統初始化完成！成功載入 {successLanguageCount} 種語言，總計 {totalDialogCount} 個對話，耗時: {totalStopwatch.Elapsed.TotalMilliseconds} 毫秒");
        }
        else
        {
            Debug.LogError("本地化系統初始化失敗：沒有成功載入任何語言");
        }
        
        return successLanguageCount;
    }
    
    /// <summary>
    /// 載入單一語言資料夾下的所有對話文件
    /// </summary>
    /// <param name="languageCode">語言代碼</param>
    /// <param name="languageFolderPath">語言資料夾完整路徑</param>
    /// <returns>成功載入的對話文件數量</returns>
    private static int LoadLanguageDialogs(string languageCode, string languageFolderPath)
    {
        if (!Directory.Exists(languageFolderPath))
        {
            Debug.LogWarning($"語言資料夾不存在: {languageFolderPath}");
            return 0;
        }
        
        // 獲取該語言資料夾下的所有 JSON 文件
        List<string> jsonFiles = GetDialogFileList(languageFolderPath, "*.json");
        
        if (jsonFiles.Count == 0)
        {
            Debug.LogWarning($"語言 '{languageCode}' 資料夾中未找到任何 JSON 對話文件: {languageFolderPath}");
            return 0;
        }
        
        // 為該語言創建對話字典
        Dictionary<string, BaseDialogManager.DialogData> dialogDict = new Dictionary<string, BaseDialogManager.DialogData>();
        int successCount = 0;
        
        foreach (string filePath in jsonFiles)
        {
            try
            {
                // 載入文件內容
                string fileContent = File.ReadAllText(filePath);
                
                if (string.IsNullOrEmpty(fileContent))
                {
                    Debug.LogWarning($"對話文件內容為空: {filePath}");
                    continue;
                }
                
                // 解析 JSON 內容
                BaseDialogManager.DialogData dialogData = ParseJsonContent(fileContent, Path.GetFileName(filePath));
                
                if (dialogData != null)
                {
                    // 使用文件名（不含副檔名）作為對話ID
                    string dialogId = Path.GetFileNameWithoutExtension(filePath);
                    dialogDict[dialogId] = dialogData;
                    successCount++;
                    
                    Debug.Log($"載入對話成功: [{languageCode}] {dialogId}");
                }
                else
                {
                    Debug.LogWarning($"解析對話文件失敗: {filePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"載入對話文件時發生錯誤 {filePath}: {e.Message}");
            }
        }
        
        // 將該語言的對話字典存入主映射
        if (successCount > 0)
        {
            languageDialogMap[languageCode] = dialogDict;
        }
        
        return successCount;
    }
    
    /// <summary>
    /// 設定當前語言
    /// </summary>
    /// <param name="languageCode">語言代碼，如 "en", "zh-TW"</param>
    /// <returns>是否設定成功</returns>
    public static bool SetCurrentLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            Debug.LogWarning("語言代碼不能為空");
            return false;
        }
        
        if (!isLocalizationInitialized)
        {
            Debug.LogWarning("本地化系統尚未初始化，請先調用 LoadAllLanguageDialogs");
            return false;
        }
        
        if (!languageDialogMap.ContainsKey(languageCode))
        {
            Debug.LogWarning($"不支援的語言代碼: {languageCode}。可用語言: {string.Join(", ", languageDialogMap.Keys)}");
            return false;
        }
        
        string previousLanguage = currentLanguage;
        currentLanguage = languageCode;
        
        Debug.Log($"語言已切換: {previousLanguage} → {currentLanguage}");
        return true;
    }
    
    /// <summary>
    /// 根據當前語言獲取對話數據
    /// </summary>
    /// <param name="dialogId">對話ID</param>
    /// <returns>對話數據，找不到則返回null</returns>
    public static BaseDialogManager.DialogData GetLocalizedDialogData(string dialogId)
    {
        return GetLocalizedDialogData(currentLanguage, dialogId);
    }
    
    /// <summary>
    /// 根據指定語言獲取對話數據
    /// </summary>
    /// <param name="languageCode">語言代碼</param>
    /// <param name="dialogId">對話ID</param>
    /// <returns>對話數據，找不到則返回null</returns>
    public static BaseDialogManager.DialogData GetLocalizedDialogData(string languageCode, string dialogId)
    {
        if (!isLocalizationInitialized)
        {
            Debug.LogWarning("本地化系統尚未初始化，返回null");
            return null;
        }
        
        if (string.IsNullOrEmpty(languageCode) || string.IsNullOrEmpty(dialogId))
        {
            Debug.LogWarning("語言代碼或對話ID不能為空");
            return null;
        }
        
        if (!languageDialogMap.ContainsKey(languageCode))
        {
            Debug.LogWarning($"不支援的語言代碼: {languageCode}");
            return null;
        }
        
        var dialogDict = languageDialogMap[languageCode];
        if (dialogDict.ContainsKey(dialogId))
        {
            return dialogDict[dialogId];
        }
        
        Debug.LogWarning($"在語言 '{languageCode}' 中找不到對話 '{dialogId}'");
        return null;
    }
    
    /// <summary>
    /// 載入本地化對話數據 - 擴展現有方法
    /// </summary>
    /// <param name="dialogId">對話ID</param>
    /// <param name="languageCode">語言代碼（可選，使用當前語言）</param>
    /// <param name="forceReload">是否強制重新載入</param>
    /// <returns>載入結果</returns>
    public static LoadResult LoadLocalizedDialogData(string dialogId, string languageCode = null, bool forceReload = false)
    {
        // 使用指定語言或當前語言
        string targetLanguage = string.IsNullOrEmpty(languageCode) ? currentLanguage : languageCode;
        
        // 嘗試從本地化映射獲取
        BaseDialogManager.DialogData dialogData = GetLocalizedDialogData(targetLanguage, dialogId);
        
        if (dialogData != null)
        {
            // 轉換為 DialogLine
            Dictionary<int, BaseDialogManager.DialogLine> dialogLines = ConvertToDialogLines(dialogData, $"[{targetLanguage}] {dialogId}");
            return new LoadResult(dialogLines, dialogData, true);
        }
        
        // 如果本地化失敗，fallback 到原有系統
        Debug.LogWarning($"本地化載入失敗，嘗試原有載入方式: {dialogId}");
        return LoadDialogData(dialogId, forceReload);
    }
    
    /// <summary>
    /// 獲取當前語言代碼
    /// </summary>
    /// <returns>當前語言代碼</returns>
    public static string GetCurrentLanguage()
    {
        return currentLanguage;
    }
    
    /// <summary>
    /// 獲取所有支援的語言代碼
    /// </summary>
    /// <returns>語言代碼數組</returns>
    public static string[] GetSupportedLanguages()
    {
        if (!isLocalizationInitialized)
        {
            return new string[0];
        }
        
        return languageDialogMap.Keys.ToArray();
    }
    
    /// <summary>
    /// 檢查是否支援指定語言
    /// </summary>
    /// <param name="languageCode">語言代碼</param>
    /// <returns>是否支援該語言</returns>
    public static bool IsLanguageSupported(string languageCode)
    {
        return isLocalizationInitialized && languageDialogMap.ContainsKey(languageCode);
    }
    
    /// <summary>
    /// 檢查本地化系統是否已初始化
    /// </summary>
    /// <returns>是否已初始化</returns>
    public static bool IsLocalizationInitialized()
    {
        return isLocalizationInitialized;
    }
    
    /// <summary>
    /// 獲取本地化系統統計信息
    /// </summary>
    /// <returns>統計信息字符串</returns>
    public static string GetLocalizationStatistics()
    {
        if (!isLocalizationInitialized)
        {
            return "本地化系統尚未初始化";
        }
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"當前語言: {currentLanguage}");
        sb.AppendLine($"支援語言數量: {languageDialogMap.Count}");
        
        foreach (var kvp in languageDialogMap)
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value.Count} 個對話");
        }
        
        return sb.ToString();
    }
    
    #endregion
}