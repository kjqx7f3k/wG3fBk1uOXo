using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Unity Input System 包裝器
/// 負責管理所有 UI 相關的輸入處理，整合 Unity Input System
/// </summary>
public class InputSystemWrapper : MonoBehaviour
{
    public static InputSystemWrapper Instance { get; private set; }
    
    [Header("輸入設定")]
    [SerializeField] private bool enableDebugLog = false;
    
    // Input Actions
    private InputSystem_Actions inputActions;
    private InputSystem_Actions.UIActions uiActions;
    private InputSystem_Actions.PlayerActions playerActions;  // 新增：遊戲輸入
    
    // UI 輸入事件
    public event Action<Vector2> OnUINavigate;
    public event Action OnUIConfirm;
    public event Action OnUICancel;
    public event Action OnUITab;
    public event Action<Vector2> OnUIScroll;
    public event Action OnMenuToggle;
    
    // 遊戲輸入事件
    public event Action<Vector2> OnPlayerMove;
    public event Action OnPlayerJump;
    public event Action OnPlayerAttack;
    public event Action OnPlayerInteract;
    public event Action OnPlayerSwitchCreature;
    public event Action OnPlayerShowInfo;
    public event Action<int> OnPlayerNumberKey;
    
    // 輸入狀態
    private bool isUIInputEnabled = false;  // 修復：初始化為false，等待正確啟用
    private bool isPlayerInputEnabled = false;  // 新增：遊戲輸入狀態
    private bool isNavigating = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInputSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeInputSystem()
    {
        try
        {
            // 創建 Input Actions 實例
            inputActions = new InputSystem_Actions();
            uiActions = inputActions.UI;
            playerActions = inputActions.Player;  // 新增：遊戲輸入
            
            // 訂閱所有 UI 輸入事件
            uiActions.UINavigate.performed += OnUINavigatePerformed;
            uiActions.UINavigate.canceled += OnUINavigateCanceled;
            uiActions.UIConfirm.performed += OnUIConfirmPerformed;
            uiActions.Cancel.performed += OnUICancelPerformed;
            uiActions.UITab.performed += OnUITabPerformed;
            uiActions.UIScroll.performed += OnUIScrollPerformed;
            
            // 新增：訂閱遊戲輸入事件
            playerActions.Move.performed += OnPlayerMovePerformed;
            playerActions.Move.canceled += OnPlayerMoveCanceled;
            playerActions.Jump.performed += OnPlayerJumpPerformed;
            playerActions.Attack.performed += OnPlayerAttackPerformed;
            playerActions.Interact.performed += OnPlayerInteractPerformed;
            playerActions.SwitchCreature.performed += OnPlayerSwitchCreaturePerformed;
            playerActions.ShowInfo.performed += OnPlayerShowInfoPerformed;
            playerActions.NumberKey.performed += OnPlayerNumberKeyPerformed;
            
            if (enableDebugLog)
                Debug.Log("[InputSystemWrapper] 輸入系統初始化完成（UI 和遊戲 Actions 已配置）");
                
            // 確保輸入立即啟用
            EnableUIInput();
            EnablePlayerInput();  // 新增：啟用遊戲輸入
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InputSystemWrapper] 初始化輸入系統失敗: {e.Message}");
        }
    }
    
    private void OnEnable()
    {
        EnableUIInput();
    }
    
    private void OnDisable()
    {
        DisableUIInput();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            DisableUIInput();
            inputActions?.Dispose();
            Instance = null;
        }
    }
    
    /// <summary>
    /// 啟用 UI 輸入
    /// </summary>
    public void EnableUIInput()
    {
        if (inputActions != null)
        {
            if (!isUIInputEnabled)
            {
                uiActions.Enable();
                isUIInputEnabled = true;
                
                if (enableDebugLog)
                    Debug.Log("[InputSystemWrapper] UI 輸入已啟用");
            }
            else
            {
                if (enableDebugLog)
                    Debug.Log("[InputSystemWrapper] UI 輸入已經處於啟用狀態");
            }
        }
        else
        {
            Debug.LogError("[InputSystemWrapper] inputActions 為 null，無法啟用 UI 輸入");
        }
    }
    
    /// <summary>
    /// 停用 UI 輸入
    /// </summary>
    public void DisableUIInput()
    {
        if (inputActions != null)
        {
            if (isUIInputEnabled)
            {
                uiActions.Disable();
                isUIInputEnabled = false;
                
                if (enableDebugLog)
                    Debug.Log("[InputSystemWrapper] UI 輸入已停用");
            }
            else
            {
                if (enableDebugLog)
                    Debug.Log("[InputSystemWrapper] UI 輸入已經處於停用狀態");
            }
        }
        else
        {
            Debug.LogWarning("[InputSystemWrapper] inputActions 為 null，無法停用 UI 輸入");
        }
    }
    
    /// <summary>
    /// 檢查 UI 輸入是否啟用
    /// </summary>
    public bool IsUIInputEnabled()
    {
        return isUIInputEnabled && inputActions != null;
    }
    
    /// <summary>
    /// 強制重新初始化輸入系統（用於故障排除）
    /// </summary>
    [ContextMenu("重新初始化輸入系統")]
    public void ForceReinitialize()
    {
        Debug.Log("[InputSystemWrapper] 強制重新初始化輸入系統");
        
        // 先清理現有的輸入
        if (inputActions != null)
        {
            DisableUIInput();
            inputActions.Dispose();
        }
        
        // 重新初始化
        InitializeInputSystem();
    }
    
    /// <summary>
    /// 獲取當前輸入系統狀態（用於調試）
    /// </summary>
    public string GetInputSystemStatus()
    {
        return $"InputActions: {(inputActions != null ? "已創建" : "null")}, " +
               $"UI輸入啟用: {isUIInputEnabled}, " +
               $"Instance: {(Instance != null ? "存在" : "null")}";
    }
    
    // ==================== Input Action 回調函數 ====================
    
    private void OnUINavigatePerformed(InputAction.CallbackContext context)
    {
        if (!isUIInputEnabled) return;
        
        // 讀取 Vector2 導航輸入（WASD + 方向鍵組合）
        Vector2 navigation = context.ReadValue<Vector2>();
        isNavigating = navigation != Vector2.zero;
        
        OnUINavigate?.Invoke(navigation);
        
        if (enableDebugLog && navigation != Vector2.zero)
            Debug.Log($"[InputSystemWrapper] UI 導航: {navigation}");
    }
    
    private void OnUINavigateCanceled(InputAction.CallbackContext context)
    {
        isNavigating = false;
        OnUINavigate?.Invoke(Vector2.zero);
    }
    
    private void OnUIConfirmPerformed(InputAction.CallbackContext context)
    {
        if (!isUIInputEnabled) return;
        
        OnUIConfirm?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] UI 確認");
    }
    
    private void OnUICancelPerformed(InputAction.CallbackContext context)
    {
        if (!isUIInputEnabled) 
        {
            Debug.LogWarning("[InputSystemWrapper] Cancel事件觸發但UI輸入未啟用");
            return;
        }
        
        Debug.Log("[InputSystemWrapper] *** Cancel事件觸發，準備調用 OnUICancel 事件 ***");
        OnUICancel?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] UI 取消");
    }
    
    private void OnUITabPerformed(InputAction.CallbackContext context)
    {
        if (!isUIInputEnabled) 
        {
            Debug.LogWarning("[InputSystemWrapper] Tab事件觸發但UI輸入未啟用");
            return;
        }
        
        Debug.Log("[InputSystemWrapper] *** Tab事件觸發，準備調用 OnUITab 事件 ***");
        OnUITab?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] UI Tab");
    }
    
    private void OnUIScrollPerformed(InputAction.CallbackContext context)
    {
        if (!isUIInputEnabled) return;
        
        Vector2 scroll = context.ReadValue<Vector2>();
        OnUIScroll?.Invoke(scroll);
        
        if (enableDebugLog && scroll != Vector2.zero)
            Debug.Log($"[InputSystemWrapper] UI 滾動: {scroll}");
    }
    
    private void OnMenuTogglePerformed(InputAction.CallbackContext context)
    {
        OnMenuToggle?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] 選單切換");
    }
    
    // ==================== 公用輔助方法 ====================
    
    /// <summary>
    /// 檢查是否正在導航
    /// </summary>
    public bool IsNavigating()
    {
        return isNavigating;
    }
    
    /// <summary>
    /// 獲取當前 UI 導航輸入值
    /// </summary>
    public Vector2 GetUINavigationInput()
    {
        if (!isUIInputEnabled) return Vector2.zero;
        
        // 直接讀取 Vector2 導航輸入（WASD + 方向鍵組合）
        return uiActions.UINavigate.ReadValue<Vector2>();
    }
    
    /// <summary>
    /// 檢查 UI 確認鍵是否按下
    /// </summary>
    public bool GetUIConfirmDown()
    {
        if (!isUIInputEnabled) return false;
        return uiActions.UIConfirm.WasPressedThisFrame();
    }
    
    /// <summary>
    /// 檢查 UI 取消鍵是否按下
    /// </summary>
    public bool GetUICancelDown()
    {
        if (!isUIInputEnabled) return false;
        return uiActions.Cancel.WasPressedThisFrame();
    }
    
    /// <summary>
    /// 檢查 UI Tab 鍵是否按下
    /// </summary>
    public bool GetUITabDown()
    {
        if (!isUIInputEnabled) return false;
        return uiActions.UITab.WasPressedThisFrame();
    }
    
    /// <summary>
    /// 獲取滾輪輸入
    /// </summary>
    public Vector2 GetUIScrollInput()
    {
        if (!isUIInputEnabled) return Vector2.zero;
        
        // 直接讀取 Vector2 滾輪輸入
        return uiActions.UIScroll.ReadValue<Vector2>();
    }
    
    /// <summary>
    /// 設定除錯日誌開關
    /// </summary>
    public void SetDebugLog(bool enabled)
    {
        enableDebugLog = enabled;
    }
    
    // ==================== 遊戲輸入管理 ====================
    
    /// <summary>
    /// 啟用遊戲輸入
    /// </summary>
    public void EnablePlayerInput()
    {
        if (inputActions != null)
        {
            if (!isPlayerInputEnabled)
            {
                playerActions.Enable();
                isPlayerInputEnabled = true;
                
                if (enableDebugLog)
                    Debug.Log("[InputSystemWrapper] 遊戲輸入已啟用");
            }
        }
        else
        {
            Debug.LogError("[InputSystemWrapper] inputActions 為 null，無法啟用遊戲輸入");
        }
    }
    
    /// <summary>
    /// 停用遊戲輸入
    /// </summary>
    public void DisablePlayerInput()
    {
        if (inputActions != null)
        {
            if (isPlayerInputEnabled)
            {
                playerActions.Disable();
                isPlayerInputEnabled = false;
                
                if (enableDebugLog)
                    Debug.Log("[InputSystemWrapper] 遊戲輸入已停用");
            }
        }
    }
    
    /// <summary>
    /// 檢查遊戲輸入是否啟用
    /// </summary>
    public bool IsPlayerInputEnabled()
    {
        return isPlayerInputEnabled && inputActions != null;
    }
    
    // ==================== 遊戲輸入回調函數 ====================
    
    private void OnPlayerMovePerformed(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        Vector2 movement = context.ReadValue<Vector2>();
        OnPlayerMove?.Invoke(movement);
        
        if (enableDebugLog && movement != Vector2.zero)
            Debug.Log($"[InputSystemWrapper] 玩家移動: {movement}");
    }
    
    private void OnPlayerMoveCanceled(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        OnPlayerMove?.Invoke(Vector2.zero);
    }
    
    private void OnPlayerJumpPerformed(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        OnPlayerJump?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] 玩家跳躍");
    }
    
    private void OnPlayerAttackPerformed(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        OnPlayerAttack?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] 玩家攻擊");
    }
    
    private void OnPlayerInteractPerformed(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        OnPlayerInteract?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] 玩家交互");
    }
    
    private void OnPlayerSwitchCreaturePerformed(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        OnPlayerSwitchCreature?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] 切換生物");
    }
    
    private void OnPlayerShowInfoPerformed(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        OnPlayerShowInfo?.Invoke();
        
        if (enableDebugLog)
            Debug.Log("[InputSystemWrapper] 顯示資訊");
    }
    
    private void OnPlayerNumberKeyPerformed(InputAction.CallbackContext context)
    {
        if (!isPlayerInputEnabled) return;
        
        int numKeyValue;
        if (int.TryParse(context.control.name, out numKeyValue))
        {
            OnPlayerNumberKey?.Invoke(numKeyValue);
            
            if (enableDebugLog)
                Debug.Log($"[InputSystemWrapper] 數字鍵: {numKeyValue}");
        }
    }
    
    // ==================== 事件連接方法 ====================
    
    /// <summary>
    /// 連接到 CreatureController
    /// </summary>
    public void ConnectToCreatureController()
    {
        var creatureController = CreatureController.Instance;
        if (creatureController != null)
        {
            OnPlayerMove += creatureController.HandleMovementInput;
            OnPlayerJump += creatureController.HandleJumpInput;
            OnPlayerAttack += creatureController.HandleAttackInput;
            OnPlayerInteract += creatureController.HandleInteractInput;
            OnPlayerNumberKey += creatureController.HandleNumberKeyInput;
            OnPlayerSwitchCreature += creatureController.HandleSwitchCreatureInput;
            OnPlayerShowInfo += creatureController.HandleShowInfoInput;
            
            Debug.Log("[InputSystemWrapper] 已連接到 CreatureController");
        }
        else
        {
            Debug.LogError("[InputSystemWrapper] 無法連接到 CreatureController - 實例不存在");
        }
    }
    
    /// <summary>
    /// 初始化所有輸入連接
    /// </summary>
    [ContextMenu("初始化輸入連接")]
    public void InitializeInputConnections()
    {
        ConnectToCreatureController();
    }
}