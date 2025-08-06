using UnityEngine;

/// <summary>
/// 雙對話管理器系統測試腳本
/// 用於測試 InteractiveDialogManager 和 NarrationDialogManager 是否正常工作
/// </summary>
public class DualDialogManagerTest : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private string testDialogFile = "testDialog";
    [SerializeField] private string testNarrationText = "這是一個測試旁白";
    [SerializeField] private float narrationAutoHideDelay = 3f;
    
    void Start()
    {
        if (testOnStart)
        {
            StartTest();
        }
    }
    
    void Update()
    {
        // 按 T 鍵測試交互對話
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestInteractiveDialog();
        }
        
        // 按 N 鍵測試旁白
        if (Input.GetKeyDown(KeyCode.N))
        {
            TestNarration();
        }
        
        // 按 H 鍵隱藏旁白
        if (Input.GetKeyDown(KeyCode.H))
        {
            HideNarration();
        }
        
        // 按 I 鍵顯示系統狀態
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowSystemInfo();
        }
    }
    
    [ContextMenu("Start Test")]
    public void StartTest()
    {
        Debug.Log("=== 雙對話管理器系統測試開始 ===");
        
        // 測試單例是否正確創建
        TestSingletonInstances();
        
        // 測試向後兼容性
        TestBackwardCompatibility();
        
        Debug.Log("=== 測試完成 ===");
    }
    
    private void TestSingletonInstances()
    {
        Debug.Log("--- 測試單例實例 ---");
        
        // 測試 InteractiveDialogManager
        bool interactiveExists = InteractiveDialogManager.Instance != null;
        Debug.Log($"InteractiveDialogManager.Instance 存在: {interactiveExists}");
        
        // 測試 NarrationDialogManager
        bool narrationExists = NarrationDialogManager.Instance != null;
        Debug.Log($"NarrationDialogManager.Instance 存在: {narrationExists}");
        
        // 測試向後兼容性
        bool dialogManagerExists = DialogManager.Instance != null;
        Debug.Log($"DialogManager.Instance (向後兼容) 存在: {dialogManagerExists}");
        
        if (dialogManagerExists && interactiveExists)
        {
            bool compatibilityWorks = DialogManager.Instance == InteractiveDialogManager.Instance;
            Debug.Log($"向後兼容性正常: {compatibilityWorks}");
        }
    }
    
    private void TestBackwardCompatibility()
    {
        Debug.Log("--- 測試向後兼容性 ---");
        
        if (DialogManager.Instance != null)
        {
            Debug.Log("可以通過 DialogManager.Instance 訪問");
            Debug.Log($"對話狀態: {DialogManager.Instance.IsInDialog}");
        }
        else
        {
            Debug.LogError("向後兼容性測試失敗：DialogManager.Instance 為 null");
        }
    }
    
    [ContextMenu("Test Interactive Dialog")]
    public void TestInteractiveDialog()
    {
        Debug.Log("測試交互式對話");
        
        if (InteractiveDialogManager.Instance != null)
        {
            // 使用向後兼容的方式調用
            DialogManager.Instance.LoadDialog(testDialogFile);
            Debug.Log($"啟動交互式對話: {testDialogFile}");
        }
        else
        {
            Debug.LogError("InteractiveDialogManager.Instance 不存在");
        }
    }
    
    [ContextMenu("Test Narration")]
    public void TestNarration()
    {
        Debug.Log("測試旁白顯示");
        
        if (NarrationDialogManager.Instance != null)
        {
            NarrationDialogManager.Instance.ShowNarration(testNarrationText, narrationAutoHideDelay);
            Debug.Log($"顯示旁白: {testNarrationText} (自動隱藏延遲: {narrationAutoHideDelay}s)");
        }
        else
        {
            Debug.LogError("NarrationDialogManager.Instance 不存在");
        }
    }
    
    [ContextMenu("Hide Narration")]
    public void HideNarration()
    {
        Debug.Log("隱藏旁白");
        
        if (NarrationDialogManager.Instance != null)
        {
            NarrationDialogManager.Instance.HideNarration();
        }
        else
        {
            Debug.LogError("NarrationDialogManager.Instance 不存在");
        }
    }
    
    [ContextMenu("Show System Info")]
    public void ShowSystemInfo()
    {
        Debug.Log("=== 雙對話管理器系統狀態 ===");
        
        // InteractiveDialogManager 狀態
        if (InteractiveDialogManager.Instance != null)
        {
            bool interactiveInDialog = InteractiveDialogManager.Instance.IsInDialog;
            Debug.Log($"交互式對話狀態: {(interactiveInDialog ? "進行中" : "空閒")}");
        }
        
        // NarrationDialogManager 狀態  
        if (NarrationDialogManager.Instance != null)
        {
            bool narrationInDialog = NarrationDialogManager.Instance.IsShowingNarration();
            Debug.Log($"旁白顯示狀態: {(narrationInDialog ? "顯示中" : "隱藏")}");
        }
        
        // 檢查是否可以同時運行
        bool bothActive = false;
        if (InteractiveDialogManager.Instance != null && NarrationDialogManager.Instance != null)
        {
            bothActive = InteractiveDialogManager.Instance.IsInDialog && 
                        NarrationDialogManager.Instance.IsShowingNarration();
        }
        Debug.Log($"雙對話同時運行: {bothActive}");
        
        Debug.Log("=== 狀態檢查完成 ===");
    }
}