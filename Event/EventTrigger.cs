using UnityEngine;

/// <summary>
/// 事件觸發器 - 基於各種條件觸發遊戲事件
/// 重用 BaseDialogManager.DialogEvent 格式，與對話系統使用統一的事件處理
/// </summary>
public class EventTrigger : MonoBehaviour
{
    [Header("觸發條件")]
    [SerializeField] private TriggerType triggerType = TriggerType.OnSceneEnter;
    [SerializeField] private float triggerValue = 0f; // 時間值、距離值等
    [SerializeField] private Transform playerTransform; // 玩家位置參考
    [SerializeField] private string tagConditionId = ""; // 標籤條件ID (用於TagCondition類型)
    [SerializeField] private int tagConditionValue = 1; // 標籤條件值
    
    [Header("事件配置 (統一事件格式)")]
    [SerializeField] private GameEvent[] eventsToTrigger;
    
    [Header("觸發器設定")]
    [SerializeField] private bool triggerOnce = true; // 是否只觸發一次
    [SerializeField] private bool debugMode = false; // 調試模式
    [SerializeField] private float onSceneEnterDelay = 0f; // OnSceneEnter 延遲觸發時間（秒）
    
    public enum TriggerType
    {
        OnSceneEnter,           // 場景進入時立即觸發
        DelayAfterSceneEnter,   // 場景進入後延遲觸發 (triggerValue = 延遲秒數)
        PlayerProximity,        // 玩家接近時觸發 (triggerValue = 距離)
        TagCondition,           // 標籤條件滿足時觸發 (使用 tagConditionId 和 tagConditionValue)
        TimeInScene,            // 在場景中停留時間觸發 (triggerValue = 停留秒數)
        GameTime                // 遊戲總時間觸發 (triggerValue = 遊戲總秒數)
    }
    
    private bool hasTriggered = false;
    private float sceneEnterTime;
    private float gameStartTime;
    
    // 性能優化：避免每幀執行 FindWithTag
    private bool hasSearchedForPlayer = false;
    
    // PlayerProximity 狀態追蹤
    private bool playerWasInRange = false;
    
    private void Start()
    {
        sceneEnterTime = Time.time;
        gameStartTime = Time.unscaledTime; // 使用不受 timeScale 影響的時間
        
        Debug.Log($"[EventTrigger] {name} Start() 被調用，觸發類型: {triggerType}");
        Debug.Log($"[EventTrigger] {name} sceneEnterTime: {sceneEnterTime}, gameStartTime: {gameStartTime}");
        
        // 立即檢查場景進入觸發
        if (triggerType == TriggerType.OnSceneEnter)
        {
            Debug.Log($"[EventTrigger] {name} 偵測到 OnSceneEnter 類型，延遲時間: {onSceneEnterDelay}秒");
            
            if (onSceneEnterDelay <= 0f)
            {
                Debug.Log($"[EventTrigger] {name} 立即觸發（無延遲）");
                TriggerEvents();
            }
            else
            {
                Debug.Log($"[EventTrigger] {name} 開始延遲觸發協程");
                StartCoroutine(DelayedOnSceneEnterTrigger());
            }
        }
        else
        {
            Debug.Log($"[EventTrigger] {name} 觸發類型不是 OnSceneEnter，跳過立即觸發");
        }
    }
    
    /// <summary>
    /// 延遲的場景進入觸發
    /// </summary>
    private System.Collections.IEnumerator DelayedOnSceneEnterTrigger()
    {
        Debug.Log($"[EventTrigger] {name} 延遲觸發開始，等待 {onSceneEnterDelay} 秒");
        yield return new WaitForSeconds(onSceneEnterDelay);
        
        Debug.Log($"[EventTrigger] {name} 延遲觸發時間到，執行事件");
        TriggerEvents();
    }
    
    private void Update()
    {
        if (hasTriggered && triggerOnce) return;
        
        CheckTriggerCondition();
    }
    
    /// <summary>
    /// 檢查觸發條件
    /// </summary>
    private void CheckTriggerCondition()
    {
        bool shouldTrigger = false;
        
        switch (triggerType)
        {
            case TriggerType.DelayAfterSceneEnter:
                shouldTrigger = (Time.time - sceneEnterTime) >= triggerValue;
                break;
                
            case TriggerType.PlayerProximity:
                shouldTrigger = CheckPlayerProximity();
                break;
                
            case TriggerType.TagCondition:
                shouldTrigger = CheckTagCondition();
                break;
                
            case TriggerType.TimeInScene:
                shouldTrigger = (Time.time - sceneEnterTime) >= triggerValue;
                break;
                
            case TriggerType.GameTime:
                shouldTrigger = Time.unscaledTime >= triggerValue;
                break;
        }
        
        if (shouldTrigger)
        {
            TriggerEvents();
        }
    }
    
    /// <summary>
    /// 檢查玩家接近條件
    /// </summary>
    private bool CheckPlayerProximity()
    {
        if (playerTransform == null && !hasSearchedForPlayer)
        {
            // 只搜尋一次，避免每幀執行 FindWithTag
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                if (debugMode)
                    Debug.LogWarning($"[EventTrigger] {name}: 找不到玩家物件，無法檢查接近條件");
            }
            hasSearchedForPlayer = true;
        }
        
        // 如果仍然沒有玩家引用，返回 false
        if (playerTransform == null)
        {
            return false;
        }
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool isInRange = distance <= triggerValue;
        
        // 重複觸發邏輯：只有從範圍外進入範圍內時才觸發
        if (!triggerOnce)
        {
            if (isInRange && !playerWasInRange)
            {
                // 玩家剛進入範圍
                playerWasInRange = true;
                return true;
            }
            else if (!isInRange && playerWasInRange)
            {
                // 玩家離開範圍，重置狀態
                playerWasInRange = false;
                hasTriggered = false;
            }
            return false;
        }
        
        // 一次性觸發：直接返回是否在範圍內
        return isInRange;
    }
    
    /// <summary>
    /// 檢查標籤條件
    /// </summary>
    private bool CheckTagCondition()
    {
        if (string.IsNullOrEmpty(tagConditionId))
        {
            if (debugMode)
                Debug.LogWarning($"[EventTrigger] {name}: 標籤條件ID為空");
            return false;
        }
        
        if (TagSystem.Instance == null)
        {
            if (debugMode)
                Debug.LogWarning($"[EventTrigger] {name}: TagSystem.Instance 為 null");
            return false;
        }
        
        int currentTagValue = TagSystem.Instance.GetTagValue(tagConditionId);
        return currentTagValue >= tagConditionValue;
    }
    
    /// <summary>
    /// 觸發事件
    /// </summary>
    private void TriggerEvents()
    {
        Debug.Log($"[EventTrigger] {name} TriggerEvents() 被調用，觸發類型: {triggerType}");
        
        if (eventsToTrigger == null || eventsToTrigger.Length == 0)
        {
            Debug.LogWarning($"[EventTrigger] {name}: 沒有配置事件 - eventsToTrigger 為 {(eventsToTrigger == null ? "null" : "空陣列")}");
            return;
        }
        
        Debug.Log($"[EventTrigger] {name} 準備觸發 {eventsToTrigger.Length} 個事件");
        
        // 檢查依賴的單例系統狀態
        CheckSystemDependencies();
        
        try
        {
            // 使用統一的事件處理器處理事件
            Debug.Log($"[EventTrigger] {name} 調用 EventProcessor.ProcessEvents");
            EventProcessor.ProcessEvents(eventsToTrigger);
            Debug.Log($"[EventTrigger] {name} EventProcessor.ProcessEvents 執行完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EventTrigger] {name} 處理事件時發生錯誤: {e.Message}\n{e.StackTrace}");
        }
        
        hasTriggered = true;
        Debug.Log($"[EventTrigger] {name} 事件觸發完成，hasTriggered 設為 true");
    }
    
    /// <summary>
    /// 檢查系統依賴狀態
    /// </summary>
    private void CheckSystemDependencies()
    {
        Debug.Log($"[EventTrigger] {name} 檢查系統依賴狀態：");
        
        // 檢查 TagSystem
        if (TagSystem.Instance == null)
        {
            Debug.LogWarning($"[EventTrigger] {name} TagSystem.Instance 為 null");
        }
        else
        {
            Debug.Log($"[EventTrigger] {name} TagSystem.Instance 可用");
        }
        
        // 檢查 InventoryManager
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning($"[EventTrigger] {name} InventoryManager.Instance 為 null");
        }
        else
        {
            Debug.Log($"[EventTrigger] {name} InventoryManager.Instance 可用");
        }
        
        // 檢查 NarrationDialogManager
        if (NarrationDialogManager.Instance == null)
        {
            Debug.LogWarning($"[EventTrigger] {name} NarrationDialogManager.Instance 為 null");
        }
        else
        {
            Debug.Log($"[EventTrigger] {name} NarrationDialogManager.Instance 可用");
        }
        
        // 檢查 GameSceneManager
        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning($"[EventTrigger] {name} GameSceneManager.Instance 為 null");
        }
        else
        {
            Debug.Log($"[EventTrigger] {name} GameSceneManager.Instance 可用");
        }
    }
    
    /// <summary>
    /// 手動觸發事件（供外部調用）
    /// </summary>
    public void ManualTrigger()
    {
        if (debugMode)
            Debug.Log($"[EventTrigger] {name} 手動觸發事件");
            
        TriggerEvents();
    }
    
    /// <summary>
    /// 重置觸發器狀態
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        sceneEnterTime = Time.time;
        hasSearchedForPlayer = false;
        playerWasInRange = false;
        
        if (debugMode)
            Debug.Log($"[EventTrigger] {name} 重置觸發器狀態");
    }
    
    // Editor 輔助方法
    private void OnDrawGizmosSelected()
    {
        if (triggerType == TriggerType.PlayerProximity)
        {
            // 繪製玩家接近範圍
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, triggerValue);
        }
    }
}