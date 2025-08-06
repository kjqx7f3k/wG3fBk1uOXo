using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // for ToList() in OnValidate
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

#if UNITY_EDITOR // 僅在編輯器環境下編譯以下程式碼
using UnityEditor; // 為了使用 EditorUtility
#endif

[System.Serializable]
public class PlayerTag
{
    public string tagId;        // 核心：識別用
    public string displayName;  // 可選：給玩家看的
    public int value;          // 核心：數值

    public PlayerTag(string id, string name = "", int val = 0)
    {
        tagId = id;
        displayName = string.IsNullOrEmpty(name) ? id : name;
        value = val;
    }
}

public class TagSystem : MonoBehaviour
{
    // 實際用於遊戲邏輯的字典
    private Dictionary<string, PlayerTag> playerTags = new Dictionary<string, PlayerTag>();

    // Unity 編輯器序列化用的列表 (Dictionary 無法直接序列化)
    [SerializeField]
    private List<PlayerTag> _serializedPlayerTags = new List<PlayerTag>();

    // 單例模式
    private static TagSystem instance;
    public static TagSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TagSystem>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TagSystem");
                    instance = go.AddComponent<TagSystem>();
                }
                // 確保物件在場景切換時不會被銷毀
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTagsFromSerializedList(); // 在 Awake 時載入序列化列表中的標籤
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 當腳本在編輯器中被載入或值被修改時呼叫
    // 用於確保在編輯器中對 _serializedPlayerTags 的修改會反映到 playerTags
    private void OnValidate()
    {
        LoadTagsFromSerializedList();
        // 如果想在編輯器中也能編輯 playerTags 字典並保存，則需要反向操作
        // SaveTagsToSerializedList(); // 如果需要將字典更改同步回序列化列表，取消註釋此行
    }

    // 當物件被禁用或銷毀時，將當前標籤狀態儲存到序列化列表中
    private void OnDisable()
    {
        SaveTagsToSerializedList();
    }

    // 將序列化列表的數據載入到字典中
    private void LoadTagsFromSerializedList()
    {
        playerTags.Clear();
        foreach (PlayerTag tag in _serializedPlayerTags)
        {
            if (!playerTags.ContainsKey(tag.tagId))
            {
                playerTags.Add(tag.tagId, tag);
            }
            else
            {
                // 如果序列化列表中有重複的 ID，則覆蓋現有標籤
                Debug.LogWarning($"[TagSystem] Duplicate tagId '{tag.tagId}' found in serialized list. Overwriting existing tag.");
                playerTags[tag.tagId] = tag;
            }
        }
    }

    // 將字典的數據儲存到序列化列表中
    private void SaveTagsToSerializedList()
    {
        _serializedPlayerTags.Clear();
        _serializedPlayerTags.AddRange(playerTags.Values);
    }

    #region 基本操作

    /// <summary>
    /// 添加或更新標籤
    /// </summary>
    public void SetTag(string tagId, int value = 0, string displayName = "")
    {
        
        playerTags[tagId].value = value;
        if (!string.IsNullOrEmpty(displayName))
            playerTags[tagId].displayName = displayName;
        Debug.Log($"[TagSystem] 標籤設定: {tagId} = {playerTags[tagId].value} (display: {playerTags[tagId].displayName})");
    }

    /// <summary>
    /// 獲取標籤數值
    /// </summary>
    public int GetTagValue(string tagId)
    {
        // 假設 tagId 必然存在
        return playerTags[tagId].value;
    }

    /// <summary>
    /// 獲取標籤物件
    /// </summary>
    public PlayerTag GetTag(string tagId)
    {
        // 假設 tagId 必然存在
        return playerTags[tagId];
    }


    /// <summary>
    /// 增加標籤數值
    /// </summary>
    public void IncrementTag(string tagId, int amount = 1)
    {
        // 判斷是否需要新增標籤 (保留 ContainsKey)
        if (playerTags.ContainsKey(tagId))
        {
            playerTags[tagId].value += amount;
            Debug.Log($"[TagSystem] 標籤增加: {tagId} += {amount} (當前值: {playerTags[tagId].value})");
        }
        else
        {
            // 在「標籤不會動態增加」的設計中，此分支應謹慎使用或不應被觸發
            SetTag(tagId, amount); // 如果不存在，則建立並設定初始值
            Debug.LogWarning($"[TagSystem] 標籤 '{tagId}' 不存在，已建立並設定初始值 {amount}。請檢查設計，如果標籤不應動態增加。");
        }
    }

    /// <summary>
    /// 減少標籤數值
    /// </summary>
    public void DecrementTag(string tagId, int amount = 1)
    {
        IncrementTag(tagId, -amount);
    }

    #endregion

    /// <summary>
    /// 檢查標籤數值是否大於等於指定值
    /// </summary>
    public bool CheckTagValue(string tagId, int minValue)
    {
        // 假設 tagId 必然存在
        bool result = playerTags[tagId].value >= minValue;
        Debug.Log($"[TagSystem] 檢查標籤數值 '{tagId}' ({playerTags[tagId].value} >= {minValue}): {result}");
        return result;
    }

    #region 數據管理

    /// <summary>
    /// 清空所有標籤
    /// </summary>
    public void ClearAllTags()
    {
        playerTags.Clear();
        SaveTagsToSerializedList(); // 清空後同步到序列化列表
        Debug.Log("[TagSystem] 所有標籤已清空");
    }

    /// <summary>
    /// 從CSV檔案載入標籤 (Resources資料夾)
    /// </summary>
    public void LoadTagsFromCSV(string fileName)
    {
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);
        if (csvFile == null)
        {
            Debug.LogError($"[TagSystem] 找不到CSV檔案: {fileName}");
            return;
        }

        LoadTagsFromCSVText(csvFile.text);
    }

    /// <summary>
    /// 從CSV文字內容載入標籤
    /// </summary>
    public void LoadTagsFromCSVText(string csvText)
    {
        try
        {
            playerTags.Clear(); // 清空現有標籤

            string[] lines = csvText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries); // 更健壯的分隔方式
            int loadedCount = 0;

            // 跳過標題行
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] values = line.Split(',');
                if (values.Length >= 3)
                {
                    string tagId = values[0].Trim();
                    string displayName = values[1].Trim();

                    // CSV 載入時仍需要檢查，因為 CSV 內容可能會有重複或新標籤
                    if (int.TryParse(values[2].Trim(), out int value))
                    {
                        if (playerTags.ContainsKey(tagId))
                        {
                            Debug.LogWarning($"[TagSystem] CSV中存在重複的Tag ID: {tagId}。將覆蓋舊值。");
                        }
                        playerTags[tagId] = new PlayerTag(tagId, displayName, value);
                        loadedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[TagSystem] CSV行數據格式錯誤，無法解析數值: {line}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[TagSystem] CSV行數據列數不足 (至少3列): {line}");
                }
            }
            SaveTagsToSerializedList(); // 從 CSV 載入後，同步到序列化列表
            Debug.Log($"[TagSystem] 從CSV載入了 {loadedCount} 個標籤");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TagSystem] 載入CSV標籤失敗: {e.Message}");
        }
    }

    /// <summary>
    /// 導出標籤為CSV格式
    /// </summary>
    public string SaveTagsToCSV()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // CSV標題行
        sb.AppendLine("tagId,displayName,value");

        // 標籤資料
        foreach (var tag in playerTags.Values)
        {
            // 確保 displayName 不包含逗號，否則需要加引號處理
            string display = tag.displayName.Contains(",") ? $"\"{tag.displayName}\"" : tag.displayName;
            sb.AppendLine($"{tag.tagId},{display},{tag.value}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 從二進制檔案載入標籤 (persistentDataPath)
    /// </summary>
    public void LoadTagsFromBinary(string fileName)
    {
        string filePath = GetBinaryFilePath(fileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[TagSystem] 找不到二進制檔案: {filePath}");
            return;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(filePath, FileMode.Open))
        {
            List<PlayerTag> loadedTags = (List<PlayerTag>)formatter.Deserialize(stream);
            
            playerTags.Clear();
            int loadedCount = 0;
            
            foreach (PlayerTag tag in loadedTags)
            {
                if (playerTags.ContainsKey(tag.tagId))
                {
                    Debug.LogWarning($"[TagSystem] 二進制檔案中存在重複的Tag ID: {tag.tagId}。將覆蓋舊值。");
                }
                playerTags[tag.tagId] = tag;
                loadedCount++;
            }
            
            SaveTagsToSerializedList(); // 載入後同步到序列化列表
            Debug.Log($"[TagSystem] 從二進制檔案載入了 {loadedCount} 個標籤");
        }
    }

    /// <summary>
    /// 儲存標籤到二進制檔案 (persistentDataPath)
    /// </summary>
    public void SaveTagsToBinary(string fileName)
    {
        string filePath = GetBinaryFilePath(fileName);
        
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            List<PlayerTag> tagsToSave = new List<PlayerTag>(playerTags.Values);
            formatter.Serialize(stream, tagsToSave);
        }
        
        Debug.Log($"[TagSystem] 已儲存 {playerTags.Count} 個標籤到二進制檔案: {filePath}");
    }

    /// <summary>
    /// 取得二進制檔案的完整路徑
    /// </summary>
    private string GetBinaryFilePath(string fileName)
    {
        // 確保檔案有 .dat 副檔名
        if (!fileName.EndsWith(".dat"))
        {
            fileName += ".dat";
        }
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    #endregion

    #region 調試功能

    /// <summary>
    /// 打印所有標籤狀態
    /// </summary>
    [ContextMenu("Print All Tags")]
    public void PrintAllTags()
    {
        Debug.Log("=== 玩家標籤狀態 ===");
        if (playerTags.Count == 0)
        {
            Debug.Log("目前沒有任何標籤。");
            return;
        }
        foreach (var kvp in playerTags)
        {
            Debug.Log($"{kvp.Key}: Value={kvp.Value.value}, Display='{kvp.Value.displayName}'");
        }
        Debug.Log($"總共 {playerTags.Count} 個標籤");
    }

    /// <summary>
    /// 測試：載入二進制標籤檔案 (player_tags.dat)
    /// </summary>
    [ContextMenu("Test Load Binary Tags")]
    public void TestLoadBinaryTags()
    {
        LoadTagsFromBinary("player_tags");
    }

    /// <summary>
    /// 測試：儲存二進制標籤檔案 (player_tags.dat)
    /// </summary>
    [ContextMenu("Test Save Binary Tags")]
    public void TestSaveBinaryTags()
    {
        SaveTagsToBinary("player_tags");
    }


    // --- 新增的功能 ---

        /// <summary>
        /// 在編輯器運行時點擊按鈕，重新從序列化數據載入標籤。
        /// 這會刷新 _serializedPlayerTags 列表中顯示的數據到實際的 playerTags 字典。
        /// </summary>
        [ContextMenu("Refresh Tags From Serialized Data")]
        public void Editor_RefreshTagsFromSerializedData()
        {
    #if UNITY_EDITOR // 確保此代碼只在編輯器中執行
            // 確保在遊戲運行時才能使用此功能
            if (Application.isPlaying)
            {
                Debug.Log($"<color=yellow>[PlayerTagSystem]</color> **正在從 Inspector 中的序列化數據刷新標籤...**");
                LoadTagsFromSerializedList(); // 重新載入字典
                PrintAllTags(); // 可選：刷新後打印所有標籤以確認
                Debug.Log($"<color=yellow>[PlayerTagSystem]</color> **標籤刷新完成。**");
            }
            else
            {
                Debug.LogWarning("[PlayerTagSystem] 'Refresh Tags From Serialized Data' 只能在遊戲運行時點擊。");
            }
    #else
            Debug.LogWarning("[PlayerTagSystem] 'Refresh Tags From Serialized Data' 僅供編輯器使用。");
    #endif
        }

        /// <summary>
        /// 在編輯器運行時點擊按鈕，強制將當前 playerTags 字典的內容保存到序列化數據。
        /// 這會將遊戲內修改的數據同步到 Inspector 的 _serializedPlayerTags 列表。
        /// </summary>
        [ContextMenu("Save Current Tags To Serialized Data")]
        public void Editor_SaveCurrentTagsToSerializedData()
        {
    #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Debug.Log($"<color=yellow>[PlayerTagSystem]</color> **正在將當前標籤保存到 Inspector 的序列化數據...**");
                SaveTagsToSerializedList(); // 將字典內容保存到列表
                // 強制編輯器刷新 Inspector，使其顯示最新的 _serializedPlayerTags
                EditorUtility.SetDirty(this); // 標記此對象已修改
                AssetDatabase.SaveAssets(); // 如果需要保存為資產 (例如 Prefab)，可使用此行
                Debug.Log($"<color=yellow>[PlayerTagSystem]</color> **標籤保存完成。**");
            }
            else
            {
                Debug.LogWarning("[PlayerTagSystem] 'Save Current Tags To Serialized Data' 只能在遊戲運行時點擊。");
            }
    #else
            Debug.LogWarning("[PlayerTagSystem] 'Save Current Tags To Serialized Data' 僅供編輯器使用。");
    #endif
        }

    #endregion
}