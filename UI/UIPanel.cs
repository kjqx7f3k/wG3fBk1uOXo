using UnityEngine;

/// <summary>
/// UI面板基礎抽象類
/// 提供統一的輸入處理、狀態管理和時間控制
/// </summary>
public abstract class UIPanel : MonoBehaviour
{
    [Header("UI面板設定")]
    [SerializeField] protected Canvas panelCanvas; // 新增：統一的 Canvas 控制器
    [SerializeField] protected bool pauseGameWhenOpen = false;
    [SerializeField] protected bool blockCharacterMovement = true;
    [SerializeField] protected bool canCloseWithEscape = true;
    [SerializeField] protected bool debugMode = false;
    
    protected bool isOpen = false;
    
    protected virtual void Awake()
    {
        // 確保所有子類別都設定了 Canvas
        if (panelCanvas == null)
        {
            // 嘗試自動獲取
            panelCanvas = GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                Debug.LogError($"[{GetType().Name}] 未在 Inspector 中設定 panelCanvas，且無法在自身找到 Canvas 組件！", this);
            }
        }
    }
    
    /// <summary>
    /// 面板是否開啟
    /// </summary>
    public bool IsOpen => isOpen;
    
    /// <summary>
    /// 是否暫停遊戲
    /// </summary>
    public bool PausesGame => pauseGameWhenOpen;
    
    /// <summary>
    /// 是否阻擋角色移動
    /// </summary>
    public bool BlocksCharacterMovement => blockCharacterMovement;
    
    protected virtual void Update()
    {
        if (!isOpen) return;
        
        HandleInput();
    }
    
    /// <summary>
    /// 處理輸入
    /// </summary>
    protected virtual void HandleInput()
    {
        if (InputSystemWrapper.Instance == null) return;
        
        // 處理ESC鍵
        if (canCloseWithEscape && InputSystemWrapper.Instance.GetUICancelDown())
        {
            HandleEscapeKey();
        }
        
        // 處理其他輸入
        HandleCustomInput();
    }
    
    /// <summary>
    /// 處理ESC鍵邏輯 - 子類可重寫
    /// </summary>
    protected virtual void HandleEscapeKey()
    {
        Close();
    }
    
    /// <summary>
    /// 處理自定義輸入 - 子類實現
    /// </summary>
    protected virtual void HandleCustomInput()
    {
        // 子類實現具體的輸入邏輯
    }
    
    /// <summary>
    /// 開啟面板
    /// </summary>
    public virtual void Open()
    {
        if (isOpen) return;
        isOpen = true;
        
        if (panelCanvas != null) panelCanvas.enabled = true;
        
        // 處理時間控制
        if (pauseGameWhenOpen)
        {
            if (debugMode)
            {
                Debug.Log($"[{GetType().Name}] 暫停遊戲 (timeScale: {Time.timeScale} -> 0)");
            }
            Time.timeScale = 0f;
        }
        
        OnOpened();
        
        if (debugMode)
        {
            Debug.Log($"[{GetType().Name}] 面板已開啟");
        }
    }
    
    /// <summary>
    /// 關閉面板
    /// </summary>
    public virtual void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        
        if (panelCanvas != null) panelCanvas.enabled = false;
        
        // 處理時間控制 - 加強保護邏輯
        if (pauseGameWhenOpen)
        {
            bool hasOtherPausingUI = HasOtherPausingUI();
            if (debugMode)
            {
                Debug.Log($"[{GetType().Name}] 檢查其他暫停UI: {hasOtherPausingUI}");
            }
            
            if (!hasOtherPausingUI)
            {
                if (debugMode)
                {
                    Debug.Log($"[{GetType().Name}] 恢復遊戲 (timeScale: {Time.timeScale} -> 1)");
                }
                Time.timeScale = 1f;
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log($"[{GetType().Name}] 保持暫停狀態，因為有其他暫停UI開啟");
                }
            }
        }
        
        OnClosed();
        
        if (debugMode)
        {
            Debug.Log($"[{GetType().Name}] 面板已關閉");
        }
    }
    
    /// <summary>
    /// 檢查是否有其他暫停遊戲的UI開啟
    /// </summary>
    /// <returns>如果有其他暫停UI返回true</returns>
    private bool HasOtherPausingUI()
    {
        // 檢查常見的暫停UI
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.IsOpen && GameMenuManager.Instance.PausesGame && GameMenuManager.Instance != this)
            return true;
            
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen && SaveUIController.Instance.PausesGame && SaveUIController.Instance != this)
            return true;
            
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen && PlayerGameSettingsUI.Instance.PausesGame && PlayerGameSettingsUI.Instance != this)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// 面板開啟時調用 - 子類重寫
    /// </summary>
    protected virtual void OnOpened()
    {
        // 子類實現
    }
    
    /// <summary>
    /// 面板關閉時調用 - 子類重寫
    /// </summary>
    protected virtual void OnClosed()
    {
        // 子類實現
    }
}
