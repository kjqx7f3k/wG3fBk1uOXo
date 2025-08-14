using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Collections;

#if UNITY_EDITOR
 using UnityEditor;
#endif

[System.Serializable]
public struct s_vector3
{
    public float x, y, z;

    public s_vector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
}


[System.Serializable]
public struct s_quaternion
{
    public float x, y, z, w;

    public s_quaternion(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }
}

/// <summary>
/// 遊戲保存數據結構
/// </summary>
[System.Serializable]
public class GameSaveData
{
    public long saveTime;
    public string gameVersion;
    public string currentSceneName;
    public AllCreaturesInventoryData allCreaturesInventoryData;
    public List<CreatureData> creaturesData;
    public TagSystemData tagSystemData;
    public GameSettingsData gameSettingsData;
}

/// <summary>
/// 所有可控制生物背包數據
/// </summary>
[System.Serializable]
public class AllCreaturesInventoryData
{
    public List<CreatureInventoryData> creaturesInventories;
    
    public AllCreaturesInventoryData()
    {
        creaturesInventories = new List<CreatureInventoryData>();
    }
}

/// <summary>
/// 單個生物背包數據
/// </summary>
[System.Serializable]
public class CreatureInventoryData
{
    public string creatureName;
    public int inventorySize;
    public string ownerName;
    public List<InventorySlotData> inventorySlots;
    
    public CreatureInventoryData()
    {
        inventorySlots = new List<InventorySlotData>();
    }
}

/// <summary>
/// 背包格子數據
/// </summary>
[System.Serializable]
public class InventorySlotData
{
    public bool hasItem;
    public int itemId;           // 新增：物品ID（主要標識符）
    public string itemName;      // 保留：物品名稱（向後兼容）
    public int itemCount;
}

/// <summary>
/// 生物數據
/// </summary>
[System.Serializable]
public class CreatureData
{
    public string creatureName;
    public bool isControllable;
    public bool isPlayerControlled;
    public s_vector3 position;
    public s_quaternion rotation;
    public s_vector3 scale;
    public int currentHealth;
    public int maxHealth;
    public bool isDead;
}

/// <summary>
/// 標籤系統數據
/// </summary>
[System.Serializable]
public class TagSystemData
{
    public string tagsCSVData;
}

/// <summary>
/// 遊戲全局變數數據
/// </summary>
[System.Serializable]
public class GameSettingsData
{
    public float gravityConstant;
    public float defaultMass;
    public float gameSpeed;
    public bool debugMode;
    public float uiAnimationSpeed;
    public bool enableUIAnimations;
}

/// <summary>
/// 遊戲保存管理器 - 處理遊戲數據的二進制保存和加載
/// </summary> 
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    [Header("保存設定")]
    [SerializeField] private string saveFileName = "GameSave.eqg";
    [SerializeField] private bool enableAutoSave = false;
    [SerializeField] private bool enableUnFocusSave = false;
    [SerializeField] private bool enablePauseSave = false;
    [SerializeField] private float autoSaveInterval = 300f; // 5分鐘自動保存
    
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    private float lastAutoSaveTime;
    
    // 事件
    public System.Action OnSaveStarted;
    public System.Action OnSaveCompleted;
    public System.Action OnLoadStarted;
    public System.Action OnLoadCompleted;
    public System.Action<string> OnSaveError;
    public System.Action<string> OnLoadError;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            lastAutoSaveTime = Time.time;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // 自動保存檢查
        if (enableAutoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            SaveGame();
            lastAutoSaveTime = Time.time;
        }
    }
    
    /// <summary>
    /// 保存遊戲數據到指定檔名
    /// </summary>
    /// <param name="customFileName">自定義檔名（不包含副檔名）</param>
    /// <returns>是否保存成功</returns>
    public bool SaveGameWithCustomFileName(string customFileName)
    {
        if (string.IsNullOrEmpty(customFileName))
        {
            Debug.LogError("[SaveManager] 自定義檔名不能為空");
            return false;
        }
        
        try
        {
            OnSaveStarted?.Invoke();
            Debug.Log($"[SaveManager] 開始保存遊戲到: {customFileName}.eqg");
            
            // 創建保存數據
            GameSaveData saveData = CreateSaveData();
            
            // 使用自定義檔名
            string customSaveFilePath = Path.Combine(Application.persistentDataPath, $"{customFileName}.eqg");
            
            // 序列化並寫入文件
            using (FileStream fileStream = new FileStream(customSaveFilePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, saveData);
            }
            
            Debug.Log($"[SaveManager] 遊戲保存成功: {customSaveFilePath}");
            OnSaveCompleted?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            string errorMsg = $"保存遊戲失敗: {e.Message}";
            Debug.LogError($"[SaveManager] {errorMsg}");
            OnSaveError?.Invoke(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// 保存遊戲數據
    /// </summary>
    /// <returns>是否保存成功</returns>
    public bool SaveGame()
    {
        try
        {
            OnSaveStarted?.Invoke();
            Debug.Log("[SaveManager] 開始保存遊戲...");
            
            // 創建保存數據
            GameSaveData saveData = CreateSaveData();
            
            // 序列化並寫入文件
            using (FileStream fileStream = new FileStream(SaveFilePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, saveData);
            }
            
            Debug.Log($"[SaveManager] 遊戲保存成功: {SaveFilePath}");
            OnSaveCompleted?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            string errorMsg = $"保存遊戲失敗: {e.Message}";
            Debug.LogError($"[SaveManager] {errorMsg}");
            OnSaveError?.Invoke(errorMsg);
            return false;
        }
    }
    
    /// <summary>
    /// 加載指定檔名的遊戲數據
    /// </summary>
    /// <param name="customFileName">自定義檔名（不包含副檔名）</param>
    /// <returns>是否加載成功</returns>
    public bool LoadGameWithCustomFileName(string customFileName)
    {
        if (string.IsNullOrEmpty(customFileName))
        {
            Debug.LogError("[SaveManager] 自定義檔名不能為空");
            return false;
        }
        
        string customSaveFilePath = Path.Combine(Application.persistentDataPath, $"{customFileName}.eqg");
        
        if (!File.Exists(customSaveFilePath))
        {
            Debug.LogWarning($"[SaveManager] 保存文件不存在: {customSaveFilePath}");
            return false;
        }
        
        // 開始協程進行帶載入畫面的加載
        StartCoroutine(LoadGameWithLoadingScreenCustom(customSaveFilePath));
        return true;
    }
    
    /// <summary>
    /// 帶載入畫面的自定義文件遊戲加載協程
    /// </summary>
    private IEnumerator LoadGameWithLoadingScreenCustom(string customFilePath)
    {
        OnLoadStarted?.Invoke();
        Debug.Log($"[SaveManager] 開始加載遊戲: {customFilePath}");
        
        // 詳細記錄當前系統狀態
        Debug.Log($"[SaveManager] 當前UI狀態 - DialogManager存在: {DialogManager.Instance != null}");
        if (DialogManager.Instance != null)
        {
            Debug.Log($"[SaveManager] DialogManager在對話中: {DialogManager.Instance.IsInDialog}");
        }
        Debug.Log($"[SaveManager] LoadingScreenManager存在: {LoadingScreenManager.Instance != null}");
        // UI系統狀態檢查
        Debug.Log($"[SaveManager] InventoryManager開啟: {InventoryManager.Instance?.IsOpen ?? false}");
        Debug.Log($"[SaveManager] GameMenuManager開啟: {GameMenuManager.Instance?.IsOpen ?? false}");
        Debug.Log($"[SaveManager] SaveUIController開啟: {SaveUIController.Instance?.IsOpen ?? false}");
        Debug.Log($"[SaveManager] PlayerGameSettingsUI開啟: {PlayerGameSettingsUI.Instance?.IsOpen ?? false}");
        
        // 檢查並關閉任何活躍的對話，防止阻塞加載過程
        if (DialogManager.Instance != null && DialogManager.Instance.IsInDialog)
        {
            Debug.Log("[SaveManager] 檢測到活躍對話，強制關閉以避免加載阻塞");
            DialogManager.Instance.ForceCloseDialog();
            yield return new WaitForSeconds(0.1f); // 等待對話關閉完成
            Debug.Log("[SaveManager] 對話已關閉，繼續加載過程");
        }
        
        // 顯示載入畫面
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingUI();
            LoadingScreenManager.Instance.SetSceneName("載入存檔");
            LoadingScreenManager.Instance.ShowMessage("正在讀取存檔文件...");
            yield return new WaitForSeconds(0.1f); // 讓UI有時間顯示
        }
        
        // 嘗試加載存檔數據
        GameSaveData saveData = null;
        string errorMessage = null;
        
        // 模擬進度：讀取文件 (0-20%)
        UpdateLoadingProgress(0.1f, "正在讀取存檔文件...");
        yield return new WaitForSeconds(0.2f);
        
        try
        {
            // 反序列化文件（純同步操作）
            using (FileStream fileStream = new FileStream(customFilePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                saveData = (GameSaveData)formatter.Deserialize(fileStream);
            }
        }
        catch (Exception e)
        {
            errorMessage = $"讀取存檔文件失敗: {e.Message}";
            Debug.LogError($"[SaveManager] {errorMessage}");
        }
        
        UpdateLoadingProgress(0.2f, "存檔文件讀取完成");
        yield return new WaitForSeconds(0.1f);
        
        // 如果讀取失敗，顯示錯誤並退出
        if (saveData == null || !string.IsNullOrEmpty(errorMessage))
        {
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowMessage($"加載失敗: {errorMessage}");
                yield return new WaitForSeconds(2f);
                LoadingScreenManager.Instance.HideLoadingUI();
            }
            
            OnLoadError?.Invoke(errorMessage);
            yield break;
        }
        
        // 應用保存數據
        yield return StartCoroutine(ApplySaveDataWithProgress(saveData));
        
        // 完成加載
        UpdateLoadingProgress(1.0f, "遊戲加載完成！");
        yield return new WaitForSeconds(0.5f);
        
        // 隱藏載入畫面
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingUI();
        }
        
        Debug.Log($"[SaveManager] 遊戲加載成功: {customFilePath}");
        OnLoadCompleted?.Invoke();
    }

    /// <summary>
    /// 加載遊戲數據
    /// </summary>
    /// <returns>是否加載成功</returns>
    public bool LoadGame()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning("[SaveManager] 保存文件不存在");
            return false;
        }
        
        // 開始協程進行帶載入畫面的加載
        StartCoroutine(LoadGameWithLoadingScreen());
        return true;
    }
    
    /// <summary>
    /// 帶載入畫面的遊戲加載協程
    /// </summary>
    private IEnumerator LoadGameWithLoadingScreen()
    {
        OnLoadStarted?.Invoke();
        Debug.Log("[SaveManager] 開始加載遊戲...");
        
        // 檢查並關閉任何活躍的對話，防止阻塞加載過程
        if (DialogManager.Instance != null && DialogManager.Instance.IsInDialog)
        {
            Debug.Log("[SaveManager] 檢測到活躍對話，強制關閉以避免加載阻塞");
            DialogManager.Instance.ForceCloseDialog();
            yield return new WaitForSeconds(0.1f); // 等待對話關閉完成
            Debug.Log("[SaveManager] 對話已關閉，繼續加載過程");
        }
        
        // 顯示載入畫面
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingUI();
            LoadingScreenManager.Instance.SetSceneName("載入存檔");
            LoadingScreenManager.Instance.ShowMessage("正在讀取存檔文件...");
            yield return new WaitForSeconds(0.1f); // 讓UI有時間顯示
        }
        
        // 嘗試加載存檔數據
        GameSaveData saveData = null;
        string errorMessage = null;
        
        // 模擬進度：讀取文件 (0-20%)
        UpdateLoadingProgress(0.1f, "正在讀取存檔文件...");
        yield return new WaitForSeconds(0.2f);
        
        try
        {
            // 反序列化文件（純同步操作）
            using (FileStream fileStream = new FileStream(SaveFilePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                saveData = (GameSaveData)formatter.Deserialize(fileStream);
            }
        }
        catch (Exception e)
        {
            errorMessage = $"讀取存檔文件失敗: {e.Message}";
            Debug.LogError($"[SaveManager] {errorMessage}");
        }
        
        UpdateLoadingProgress(0.2f, "存檔文件讀取完成");
        yield return new WaitForSeconds(0.1f);
        
        // 如果讀取失敗，顯示錯誤並退出
        if (saveData == null || !string.IsNullOrEmpty(errorMessage))
        {
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.ShowMessage($"加載失敗: {errorMessage}");
                yield return new WaitForSeconds(2f);
                LoadingScreenManager.Instance.HideLoadingUI();
            }
            
            OnLoadError?.Invoke(errorMessage);
            yield break;
        }
        
        // 應用保存數據
        yield return StartCoroutine(ApplySaveDataWithProgress(saveData));
        
        // 完成加載
        UpdateLoadingProgress(1.0f, "遊戲加載完成！");
        yield return new WaitForSeconds(0.5f);
        
        // 隱藏載入畫面
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingUI();
        }
        
        Debug.Log("[SaveManager] 遊戲加載成功");
        OnLoadCompleted?.Invoke();
    }
    
    /// <summary>
    /// 帶進度的應用保存數據
    /// </summary>
    /// <param name="saveData">保存數據</param>
    private IEnumerator ApplySaveDataWithProgress(GameSaveData saveData)
    {
        Debug.Log($"[SaveManager] 應用保存數據 - 保存時間: {DateTime.FromBinary(saveData.saveTime)}");
        Debug.Log($"[SaveManager] 開始應用存檔數據，場景: {saveData.currentSceneName}");
        
        // 加載標籤系統數據 (20-35%)
        UpdateLoadingProgress(0.25f, "正在加載標籤系統數據...");
        Debug.Log("[SaveManager] 開始加載標籤系統數據");
        LoadTagSystemData(saveData.tagSystemData);
        Debug.Log("[SaveManager] 標籤系統數據加載完成");
        yield return new WaitForSeconds(0.2f);
        
        // 加載遊戲全局變數 (35-50%)
        UpdateLoadingProgress(0.4f, "正在加載遊戲設定...");
        Debug.Log("[SaveManager] 開始加載遊戲設定");
        LoadGameSettingsData(saveData.gameSettingsData);
        Debug.Log("[SaveManager] 遊戲設定加載完成");
        yield return new WaitForSeconds(0.2f);
        
        // 加載所有可控制生物背包數據 (50-75%)
        UpdateLoadingProgress(0.6f, "正在加載生物背包數據...");
        Debug.Log("[SaveManager] 開始加載生物背包數據");
        LoadAllCreaturesInventory(saveData.allCreaturesInventoryData);
        Debug.Log("[SaveManager] 生物背包數據加載完成");
        yield return new WaitForSeconds(0.3f);
        
        // 加載生物數據 (75-95%)
        UpdateLoadingProgress(0.85f, "正在加載生物狀態...");
        Debug.Log("[SaveManager] 開始加載生物狀態");
        LoadCreaturesData(saveData.creaturesData);
        Debug.Log("[SaveManager] 生物狀態加載完成");
        yield return new WaitForSeconds(0.2f);
        
        UpdateLoadingProgress(0.95f, "數據加載完成，正在初始化...");
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log("[SaveManager] 所有數據加載完成");
    }
    
    /// <summary>
    /// 更新載入進度
    /// </summary>
    /// <param name="progress">進度 (0-1)</param>
    /// <param name="message">顯示訊息</param>
    private void UpdateLoadingProgress(float progress, string message)
    {
        if (LoadingScreenManager.Instance != null)
        {
            // 模擬進度更新事件
            LoadingScreenManager.Instance.ShowMessage(message);
            
            // 手動設置進度（因為LoadingScreenManager主要是為場景加載設計的）
            // 我們需要直接調用其內部方法或創建自定義進度更新
            Debug.Log($"[SaveManager] 載入進度: {progress * 100:F0}% - {message}");
        }
    }
    
    /// <summary>
    /// 檢查是否存在保存文件
    /// </summary>
    /// <returns>是否存在保存文件</returns>
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }
    
    /// <summary>
    /// 刪除保存文件
    /// </summary>
    /// <returns>是否刪除成功</returns>
    public bool DeleteSaveFile()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveManager] 保存文件已刪除");
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 刪除保存文件失敗: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 創建保存數據
    /// </summary>
    /// <returns>遊戲保存數據</returns>
    private GameSaveData CreateSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        
        // 保存時間戳
        saveData.saveTime = DateTime.Now.ToBinary();
        saveData.gameVersion = Application.version;
        
        // 保存所有可控制生物背包數據
        saveData.allCreaturesInventoryData = SaveAllCreaturesInventory();
        
        // 保存場景中的生物數據
        saveData.creaturesData = SaveCreaturesData();
        
        // 保存標籤系統數據
        saveData.tagSystemData = SaveTagSystemData();
        
        // 保存遊戲全局變數
        saveData.gameSettingsData = SaveGameSettingsData();
        
        // 保存當前場景名稱
        saveData.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        Debug.Log($"[SaveManager] 保存數據創建完成 - 場景: {saveData.currentSceneName}");
        return saveData;
    }
    
    /// <summary>
    /// 應用保存數據
    /// </summary>
    /// <param name="saveData">保存數據</param>
    private void ApplySaveData(GameSaveData saveData)
    {
        Debug.Log($"[SaveManager] 應用保存數據 - 保存時間: {DateTime.FromBinary(saveData.saveTime)}");
        
        // 加載標籤系統數據
        LoadTagSystemData(saveData.tagSystemData);
        
        // 加載遊戲全局變數
        LoadGameSettingsData(saveData.gameSettingsData);
        
        // 加載所有可控制生物背包數據
        LoadAllCreaturesInventory(saveData.allCreaturesInventoryData);
        
        // 加載生物數據
        LoadCreaturesData(saveData.creaturesData);
        
        Debug.Log("[SaveManager] 所有數據加載完成");
    }
    
    /// <summary>
    /// 保存所有可控制生物背包數據
    /// </summary>
    /// <returns>所有可控制生物背包數據</returns>
    private AllCreaturesInventoryData SaveAllCreaturesInventory()
    {
        AllCreaturesInventoryData allInventoryData = new AllCreaturesInventoryData();
        
        // 查找所有ControllableCreature
        ControllableCreature[] allControllableCreatures = FindObjectsByType<ControllableCreature>(FindObjectsSortMode.None); // 包含未激活 (等同於舊版的 true)
        
        foreach (ControllableCreature creature in allControllableCreatures)
        {
            if (creature.Inventory != null)
            {
                CreatureInventoryData creatureInventoryData = new CreatureInventoryData();
                CreatureInventory inventory = creature.Inventory;
                
                creatureInventoryData.creatureName = creature.name;
                creatureInventoryData.inventorySize = inventory.InventorySize;
                creatureInventoryData.ownerName = inventory.OwnerName;
                creatureInventoryData.inventorySlots = new List<InventorySlotData>();
                
                foreach (InventorySlot slot in inventory.InventorySlots)
                {
                    InventorySlotData slotData = new InventorySlotData();
                    if (!slot.IsEmpty)
                    {
                        slotData.itemId = slot.CurrentItem.Id;           // 使用物品ID
                        slotData.itemName = slot.CurrentItem.name;       // 保留名稱用於向後兼容
                        slotData.itemCount = slot.ItemCount;
                        slotData.hasItem = true;
                    }
                    else
                    {
                        slotData.hasItem = false;
                    }
                    creatureInventoryData.inventorySlots.Add(slotData);
                }
                
                allInventoryData.creaturesInventories.Add(creatureInventoryData);
            }
        }
        
        Debug.Log($"[SaveManager] 所有可控制生物背包數據保存完成 - {allInventoryData.creaturesInventories.Count} 個生物背包");
        return allInventoryData;
    }
    
    /// <summary>
    /// 加載所有可控制生物背包數據
    /// </summary>
    /// <param name="allInventoryData">所有背包數據</param>
    private void LoadAllCreaturesInventory(AllCreaturesInventoryData allInventoryData)
    {
        if (allInventoryData == null || allInventoryData.creaturesInventories == null)
        {
            Debug.LogWarning("[SaveManager] 沒有可控制生物背包數據可加載");
            return;
        }
        
        // 查找所有ControllableCreature
        ControllableCreature[] allControllableCreatures = FindObjectsByType<ControllableCreature>(FindObjectsSortMode.None);
        
        foreach (CreatureInventoryData creatureInventoryData in allInventoryData.creaturesInventories)
        {
            // 通過名稱查找對應的生物
            ControllableCreature creature = System.Array.Find(allControllableCreatures, c => c.name == creatureInventoryData.creatureName);
            
            if (creature != null && creature.Inventory != null)
            {
                CreatureInventory inventory = creature.Inventory;
                
                // 清空現有背包
                inventory.ClearInventory();
                
                // 重新初始化背包大小
                if (creatureInventoryData.inventorySize > 0)
                {
                    inventory.Initialize(creatureInventoryData.inventorySize, creatureInventoryData.ownerName);
                }
                
                // 加載物品
                for (int i = 0; i < creatureInventoryData.inventorySlots.Count && i < inventory.InventorySlots.Count; i++)
                {
                    InventorySlotData slotData = creatureInventoryData.inventorySlots[i];
                    if (slotData.hasItem)
                    {
                        // 確保 ItemDatabase 已加載
                        if (ItemDatabase.Instance == null)
                        {
                            Debug.LogError($"[SaveManager] ItemDatabase 未初始化，無法加載物品");
                            continue;
                        }
                        
                        if (!ItemDatabase.Instance.IsLoaded)
                        {
                            Debug.LogWarning($"[SaveManager] ItemDatabase 尚未加載，正在加載...");
                            ItemDatabase.Instance.LoadAllItems();
                        }
                        
                        // 使用 ItemDatabase 智能查找物品（優先ID，備選名稱）
                        Item item = ItemDatabase.Instance.GetItem(slotData.itemId, slotData.itemName);
                        
                        if (item != null)
                        {
                            inventory.InventorySlots[i].SetItem(item, slotData.itemCount);
                            Debug.Log($"[SaveManager] 成功加載物品: {item.Name} x{slotData.itemCount} (ID: {item.Id})");
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveManager] 找不到物品: ID={slotData.itemId}, Name=\"{slotData.itemName}\"");
                        }
                    }
                }
                
                Debug.Log($"[SaveManager] {creature.name} 背包數據加載完成");
            }
            else
            {
                Debug.LogWarning($"[SaveManager] 找不到可控制生物或其背包: {creatureInventoryData.creatureName}");
            }
        }
        
        Debug.Log("[SaveManager] 所有可控制生物背包數據加載完成");
    }
    
    /// <summary>
    /// 保存場景中的生物數據
    /// </summary>
    /// <returns>生物數據列表</returns>
    private List<CreatureData> SaveCreaturesData()
    {
        List<CreatureData> creaturesData = new List<CreatureData>();
        
        // 查找所有Creature和ControllableCreature
        Creature[] allCreatures = FindObjectsByType<Creature>(FindObjectsSortMode.None);
        
        foreach (Creature creature in allCreatures)
        {
            CreatureData creatureData = new CreatureData();
            
            // 基本信息
            creatureData.creatureName = creature.name;
            creatureData.isControllable = creature is ControllableCreature;
            
            // 位置和旋轉
            s_vector3 s_position = new s_vector3(creature.transform.position);
            s_vector3 s_scale = new s_vector3(creature.transform.localScale);
            s_quaternion s_rotation = new s_quaternion(creature.transform.rotation);


            creatureData.position = s_position;
            creatureData.rotation = s_rotation;
            creatureData.scale = s_scale;
            
            // 生命值數據
            creatureData.currentHealth = creature.CurrentHealth;
            creatureData.maxHealth = creature.MaxHealth;
            creatureData.isDead = creature.IsDead;
            
            // 如果是可控制生物，保存額外數據
            if (creature is ControllableCreature controllable)
            {
                // 檢查是否為當前正在被玩家控制的生物
                creatureData.isPlayerControlled = CreatureController.Instance != null && 
                                                 CreatureController.Instance.CurrentControlledCreature == controllable;
            }
            
            creaturesData.Add(creatureData);
        }
        
        Debug.Log($"[SaveManager] 生物數據保存完成 - {creaturesData.Count} 個生物");
        return creaturesData;
    }
    
    /// <summary>
    /// 加載生物數據
    /// </summary>
    /// <param name="creaturesData">生物數據列表</param>
    private void LoadCreaturesData(List<CreatureData> creaturesData)
    {
        Creature[] allCreatures = FindObjectsByType<Creature>(FindObjectsSortMode.None);
        
        foreach (CreatureData creatureData in creaturesData)
        {
            // 通過名稱查找對應的生物
            Creature creature = System.Array.Find(allCreatures, c => c.name == creatureData.creatureName);
            
            if (creature != null)
            {
                Debug.Log("[SaveManager] 尋找到生物: " + creature.name);
                // 恢復位置和旋轉
                creature.transform.position = new Vector3(creatureData.position.x, creatureData.position.y, creatureData.position.z);
                creature.transform.rotation = new Quaternion(creatureData.rotation.x, creatureData.rotation.y, creatureData.rotation.z, creatureData.rotation.w);
                creature.transform.localScale = new Vector3(creatureData.scale.x, creatureData.scale.y, creatureData.scale.z);
                
                // 恢復生命值
                creature.SetMaxHealth(creatureData.maxHealth);
                if (creatureData.isDead)
                {
                    creature.TakeDamage(creature.CurrentHealth); // 讓生物死亡
                }
                else
                {
                    int healthDifference = creatureData.currentHealth - creature.CurrentHealth;
                    if (healthDifference > 0)
                    {
                        creature.Heal(healthDifference);
                    }
                    else if (healthDifference < 0)
                    {
                        creature.TakeDamage(-healthDifference);
                    }
                }
                
            }
            else
            {
                Debug.LogWarning($"[SaveManager] 找不到生物: {creatureData.creatureName}");
            }
        }
        
        Debug.Log("[SaveManager] 生物數據加載完成");
    }
    
    
    /// <summary>
    /// 保存標籤系統數據
    /// </summary>
    /// <returns>標籤系統數據</returns>
    private TagSystemData SaveTagSystemData()
    {
        TagSystemData tagData = new TagSystemData();
        
        if (TagSystem.Instance != null)
        {
            // 獲取所有標籤的CSV格式數據
            string csvData = TagSystem.Instance.SaveTagsToCSV();
            tagData.tagsCSVData = csvData;
        }
        
        Debug.Log("[SaveManager] 標籤系統數據保存完成");
        return tagData;
    }
    
    /// <summary>
    /// 加載標籤系統數據
    /// </summary>
    /// <param name="tagData">標籤數據</param>
    private void LoadTagSystemData(TagSystemData tagData)
    {
        if (TagSystem.Instance != null && !string.IsNullOrEmpty(tagData.tagsCSVData))
        {
            TagSystem.Instance.LoadTagsFromCSVText(tagData.tagsCSVData);
        }
        
        Debug.Log("[SaveManager] 標籤系統數據加載完成");
    }
    
    /// <summary>
    /// 保存遊戲全局變數數據
    /// </summary>
    /// <returns>全局變數數據</returns>
    private GameSettingsData SaveGameSettingsData()
    {
        GameSettingsData settingsData = new GameSettingsData();
        
        if (GameSettings.Instance != null)
        {
            settingsData.gravityConstant = GameSettings.Instance.GravityConstant;
            settingsData.defaultMass = GameSettings.Instance.DefaultMass;
            settingsData.gameSpeed = GameSettings.Instance.GameSpeed;
            settingsData.debugMode = GameSettings.Instance.DebugMode;
            settingsData.uiAnimationSpeed = GameSettings.Instance.UIAnimationSpeed;
            settingsData.enableUIAnimations = GameSettings.Instance.EnableUIAnimations;
        }
        
        Debug.Log("[SaveManager] 遊戲設定數據保存完成");
        return settingsData;
    }
    
    /// <summary>
    /// 加載遊戲設定數據
    /// </summary>
    /// <param name="settingsData">設定數據</param>
    private void LoadGameSettingsData(GameSettingsData settingsData)
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.GravityConstant = settingsData.gravityConstant;
            GameSettings.Instance.DefaultMass = settingsData.defaultMass;
            GameSettings.Instance.GameSpeed = settingsData.gameSpeed;
            GameSettings.Instance.DebugMode = settingsData.debugMode;
            GameSettings.Instance.UIAnimationSpeed = settingsData.uiAnimationSpeed;
            GameSettings.Instance.EnableUIAnimations = settingsData.enableUIAnimations;
        }
        
        Debug.Log("[SaveManager] 遊戲設定數據加載完成");
    }
    
    
    /// <summary>
    /// 獲取保存文件信息
    /// </summary>
    /// <returns>保存文件信息</returns>
    public string GetSaveFileInfo()
    {
        if (!HasSaveFile())
        {
            return "沒有保存文件";
        }
        
        try
        {
            FileInfo fileInfo = new FileInfo(SaveFilePath);
            return $"保存文件: {saveFileName}\n" +
                   $"大小: {fileInfo.Length / 1024f:F2} KB\n" +
                   $"修改時間: {fileInfo.LastWriteTime}";
        }
        catch (Exception e)
        {
            return $"無法讀取保存文件信息: {e.Message}";
        }
    }
    
    /// <summary>
    /// 設置自動保存
    /// </summary>
    /// <param name="enabled">是否啟用</param>
    /// <param name="interval">間隔時間（秒）</param>
    public void SetAutoSave(bool enabled, float interval = 300f)
    {
        enableAutoSave = enabled;
        autoSaveInterval = interval;
        lastAutoSaveTime = Time.time;
        
        Debug.Log($"[SaveManager] 自動保存設定: {(enabled ? "啟用" : "停用")}, 間隔: {interval}秒");
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // 應用暫停時自動保存
        if (pauseStatus && enablePauseSave)
        {
            SaveGame();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // 應用失去焦點時自動保存
        if (!hasFocus && enableUnFocusSave)
        {
            SaveGame();
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    [ContextMenu("Save Game")]
    public void Editor_SaveGame()
    {
#if UNITY_EDITOR // 確保此代碼只在編輯器中執行
       bool result = SaveGame();
       if(result)
       {
           Debug.Log("[SaveManager] 遊戲已保存 (編輯器模式)");
       }
       else
       {
           Debug.LogError("[SaveManager] 遊戲保存失敗 (編輯器模式)");
       }
#else
        Debug.LogWarning("[SaveManager] SaveGame 方法只能在編輯器中使用");
#endif
    }

    [ContextMenu("Load Game")]
    public void Editor_LoadGame()
    {
#if UNITY_EDITOR // 確保此代碼只在編輯器中執行
        bool result = LoadGame();
        if(result)
        {
            Debug.Log("[SaveManager] 遊戲已加載 (編輯器模式)");
        }
        else
        {
            Debug.LogError("[SaveManager] 遊戲加載失敗 (編輯器模式)");
        }
#else
        Debug.LogWarning("[SaveManager] LoadGame 方法只能在編輯器中使用");
#endif
    }
}
