using UnityEngine;
using System.Collections.Generic;
using System;

public class CreatureController : MonoBehaviour
{
    private static CreatureController _instance;
    private static bool _isQuitting = false;
    
    public static CreatureController Instance
    {
        get
        {
            // 如果應用程式正在退出，直接返回 null
            if (_isQuitting) return null;
            
            // 如果實例不存在，嘗試找到現有的或創建新的
            if (_instance == null)
            {
                // 嘗試找到現有的實例
                _instance = FindFirstObjectByType<CreatureController>();
                
                // 如果沒有找到且應用程式未退出，創建一個新的
                if (_instance == null && !_isQuitting)
                {
                    GameObject controllerObj = new GameObject("CreatureController");
                    _instance = controllerObj.AddComponent<CreatureController>();
                }
            }
            
            return _instance;
        }
    }
    
    // 當前控制的生物
    private IControllable currentControlledCreature;
    
    // 可控制的生物列表
    private List<IControllable> controllableCreatures = new List<IControllable>();
    private int currentCreatureIndex = 0;
    
    // 攝影機引用（每個場景可能不同）
    private Camera currentCamera;
    
    // 輸入接口 - 供 InputSystemWrapper 調用
    private Vector2 currentMovementInput;
    
    [Header("場景設定")]
    [SerializeField] private bool autoRegisterOnStart = true;
    [SerializeField] private bool registerOnlyTaggedCreatures = false;
    [SerializeField] private string creatureTag = "Controllable";
    
    // 重試連接相關字段
    private int retryCount = 0;
    private const int MAX_RETRY_COUNT = 3;
    private bool isRetrying = false; // 防止並發重試
    
    // 事件
    public event Action<IControllable> OnCreatureSwitched;
    public event Action OnSceneChanged;
    
    public IControllable CurrentControlledCreature => currentControlledCreature;
    public int ControllableCreatureCount => controllableCreatures.Count;
    
    /// <summary>
    /// 獲取當前正在控制的生物（方法形式，供其他管理器調用）
    /// </summary>
    /// <returns>當前控制的生物</returns>
    public IControllable GetCurrentControlledCreature()
    {
        return currentControlledCreature;
    }
    
    private void Awake()
    {
        // 實現 Singleton 模式
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 監聽場景載入事件
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    
    private void OnApplicationQuit()
    {
        // 應用程式退出時清理
        _isQuitting = true;
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    // 供 InputSystemWrapper 調用的公共接口
    public void HandleMovementInput(Vector2 input)
    {
        currentMovementInput = input;
    }
    
    public void HandleJumpInput()
    {
        if (currentControlledCreature != null)
        {
            currentControlledCreature.HandleJumpInput(true);
        }
    }
    
    public void HandleAttackInput()
    {
        if (currentControlledCreature != null)
        {
            currentControlledCreature.HandleAttackInput(true);
        }
    }
    
    public void HandleInteractInput()
    {
        if (currentControlledCreature != null)
        {
            currentControlledCreature.HandleInteractInput(true);
        }
    }
    
    public void HandleNumberKeyInput(int numKeyValue)
    {
        if (currentControlledCreature != null)
        {
            Debug.Log($"[NumberKey] 按下數字鍵: {numKeyValue}");
            currentControlledCreature.HandleNumberKeyInput(numKeyValue);
        }
    }

    public void HandleSwitchCreatureInput()
    {
        SwitchToNextCreature();
    }
    
    public void HandleShowInfoInput()
    {
        ShowControlInfo();
    }
    
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"場景載入: {scene.name}");
        
        // 清除舊場景的生物引用
        ClearInvalidCreatures();
        
        // 重新設定攝影機
        SetupCamera();
        
        // 自動註冊新場景中的生物（如果啟用）
        if (autoRegisterOnStart)
        {
            RegisterCreaturesInScene();
        }
        
        // 初始化輸入連接
        InitializeInputConnections();
        
        // 觸發場景變更事件
        OnSceneChanged?.Invoke();
    }
    
    private void Update()
    {
        // 只在有控制生物且有輸入時才處理
        if (currentControlledCreature != null && currentMovementInput != Vector2.zero)
        {
            SendInputToControlledCreature();
        }
    }
    
    private void SendInputToControlledCreature()
    {
        // 發送移動輸入到當前控制的生物
        currentControlledCreature.HandleMovementInput(currentMovementInput);
    }
    
    private void SetupCamera()
    {
        // 尋找主攝影機
        currentCamera = Camera.main;
        
        if (currentCamera == null)
        {
            // 如果沒有主攝影機，尋找任何攝影機
            currentCamera = FindFirstObjectByType<Camera>();
        }
        
        if (currentCamera != null)
        {
            Debug.Log($"設定攝影機: {currentCamera.name}");
        }
        else
        {
            Debug.LogWarning("場景中沒有找到攝影機！");
        }
    }
    
    private void ClearInvalidCreatures()
    {
        // 移除已被銷毀或無效的生物引用
        controllableCreatures.RemoveAll(creature => 
        {
            if (creature == null) return true;
            
            try
            {
                // 安全地檢查Transform是否存在
                Transform transform = creature.GetTransform();
                return transform == null;
            }
            catch (MissingReferenceException)
            {
                // 如果拋出MissingReferenceException，表示物件已被銷毀
                return true;
            }
            catch (System.Exception)
            {
                // 其他異常也視為無效
                return true;
            }
        });
        
        // 如果當前控制的生物無效，清除引用
        if (currentControlledCreature != null)
        {
            bool isCurrentCreatureValid = false;
            try
            {
                Transform currentTransform = currentControlledCreature.GetTransform();
                isCurrentCreatureValid = currentTransform != null && controllableCreatures.Contains(currentControlledCreature);
            }
            catch (MissingReferenceException)
            {
                isCurrentCreatureValid = false;
            }
            catch (System.Exception)
            {
                isCurrentCreatureValid = false;
            }
            
            if (!isCurrentCreatureValid)
            {
                currentControlledCreature = null;
                currentCreatureIndex = 0;
            }
        }
        
        Debug.Log($"清理後剩餘生物數量: {controllableCreatures.Count}");
    }
    
    private void RegisterCreaturesInScene()
    {
        if (registerOnlyTaggedCreatures)
        {
            // 只註冊有特定標籤的生物
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(creatureTag);
            int registeredCount = 0;
            
            foreach (GameObject obj in taggedObjects)
            {
                IControllable controllable = obj.GetComponent<IControllable>();
                if (controllable != null)
                {
                    RegisterControllableCreature(controllable);
                    registeredCount++;
                }
            }
            
            Debug.Log($"註冊了 {registeredCount} 個標籤為 '{creatureTag}' 的可控制物件");
        }
        else
        {
            // 註冊所有 ControllableCreature 組件
            ControllableCreature[] creatures = FindObjectsByType<ControllableCreature>(FindObjectsSortMode.None);
            int registeredCount = 0;
            
            foreach (ControllableCreature creature in creatures)
            {
                // 避免重複註冊
                if (!controllableCreatures.Contains(creature))
                {
                    RegisterControllableCreature(creature);
                    registeredCount++;
                }
            }
            
            Debug.Log($"註冊了 {registeredCount} 個新的 ControllableCreature 組件");
        }
    }
    
    private void ShowControlInfo()
    {
        if (currentControlledCreature != null)
        {
            string currentName = currentControlledCreature.GetTransform().name;
            string[] allNames = GetControllableCreatureNames();
            
            Debug.Log($"當前控制: {currentName}");
            Debug.Log($"可控制生物總數: {ControllableCreatureCount}");
            Debug.Log($"所有可控制生物: {string.Join(", ", allNames)}");
        }
        else
        {
            Debug.Log("目前沒有控制任何生物");
        }
    }
    
    // 註冊可控制的生物
    public void RegisterControllableCreature(IControllable creature)
    {
        if (creature == null || controllableCreatures.Contains(creature)) return;
        
        controllableCreatures.Add(creature);
        
        // 如果這是第一個生物，自動設為當前控制對象
        if (currentControlledCreature == null)
        {
            SetControlledCreature(creature);
        }
        
        Debug.Log($"註冊可控制生物: {creature.GetTransform().name}");
    }
    
    // 取消註冊可控制的生物
    public void UnregisterControllableCreature(IControllable creature)
    {
        if (creature == null || !controllableCreatures.Contains(creature)) return;
        
        // 如果正在控制這個生物，切換到其他生物
        if (currentControlledCreature == creature)
        {
            SwitchToNextCreature();
        }
        
        controllableCreatures.Remove(creature);
        Debug.Log($"取消註冊可控制生物: {creature.GetTransform().name}");
    }
    
    // 切換到下一個生物
    public void SwitchToNextCreature()
    {
        if (controllableCreatures.Count == 0) return;
        
        // 找到下一個可控制的生物
        int startIndex = currentCreatureIndex;
        do
        {
            currentCreatureIndex = (currentCreatureIndex + 1) % controllableCreatures.Count;
            
            if (controllableCreatures[currentCreatureIndex].IsControllable)
            {
                SetControlledCreature(controllableCreatures[currentCreatureIndex]);
                return;
            }
        }
        while (currentCreatureIndex != startIndex);
        
        Debug.LogWarning("沒有可控制的生物！");
    }
    
    // 切換到指定的生物
    public void SetControlledCreature(IControllable creature)
    {
        if (creature == null || !creature.IsControllable) return;
        
        // 通知舊生物失去控制
        if (currentControlledCreature != null)
        {
            currentControlledCreature.OnControlLost();
        }
        
        // 設定新的控制對象
        currentControlledCreature = creature;
        currentCreatureIndex = controllableCreatures.IndexOf(creature);
        
        // 通知新生物獲得控制
        currentControlledCreature.OnControlGained();
        
        // 通知 CameraController 新的目標
        NotifyCameraControllerOfTarget();
        
        // 通知InventoryManager切換到新生物的背包
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null && creature is ControllableCreature controllableCreature)
        {
            try
            {
                inventoryManager.SetCurrentCreature(controllableCreature);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CreatureController] 設定生物背包時發生錯誤: {e.Message}");
            }
        }
        
        // 觸發事件
        OnCreatureSwitched?.Invoke(currentControlledCreature);
        
        Debug.Log($"切換控制到: {creature.GetTransform().name}");
    }
    
    // 根據名稱切換生物
    public void SetControlledCreatureByName(string creatureName)
    {
        IControllable targetCreature = controllableCreatures.Find(c => 
            c.GetTransform().name == creatureName);
            
        if (targetCreature != null)
        {
            SetControlledCreature(targetCreature);
        }
        else
        {
            Debug.LogWarning($"找不到名為 '{creatureName}' 的可控制生物！");
        }
    }
    
    // 獲取所有可控制生物的名稱
    public string[] GetControllableCreatureNames()
    {
        string[] names = new string[controllableCreatures.Count];
        for (int i = 0; i < controllableCreatures.Count; i++)
        {
            names[i] = controllableCreatures[i].GetTransform().name;
        }
        return names;
    }
    
    // 手動設定攝影機（保留為適配性接口）
    public void SetCamera(Camera camera)
    {
        currentCamera = camera;
        Debug.Log($"手動設定攝影機: {camera.name}");
        
        // 通知 CameraController 設定新的目標
        NotifyCameraControllerOfTarget();
    }
    
    // 通知 CameraController 當前控制的生物
    private void NotifyCameraControllerOfTarget()
    {
        if (currentCamera != null && currentControlledCreature != null)
        {
            CameraController cameraController = currentCamera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetPlayerTarget(currentControlledCreature.GetTransform());
            }
        }
    }
    
    // 清除所有註冊的生物（用於完全重置）
    public void ClearAllCreatures()
    {
        if (currentControlledCreature != null)
        {
            currentControlledCreature.OnControlLost();
        }
        
        controllableCreatures.Clear();
        currentControlledCreature = null;
        currentCreatureIndex = 0;
        
        Debug.Log("清除所有註冊的生物");
    }
    
    // 獲取當前場景名稱
    public string GetCurrentSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    
    // 獲取當前輸入值（供其他系統使用）
    public Vector2 GetMovementInput()
    {
        return currentMovementInput;
    }
    
    /// <summary>
    /// 初始化輸入連接
    /// </summary>
    private void InitializeInputConnections()
    {
        // 防止並發調用
        if (isRetrying) return;
        
        var inputWrapper = InputSystemWrapper.Instance;
        if (inputWrapper != null)
        {
            // 確保 InputSystemWrapper 已經連接到各個系統
            inputWrapper.InitializeInputConnections();
            Debug.Log("[CreatureController] 輸入連接初始化完成");
            
            // 重置重試狀態
            retryCount = 0;
            isRetrying = false;
        }
        else
        {
            Debug.LogWarning("[CreatureController] InputSystemWrapper 實例不存在，將在稍後重試");
            // 延遲重試
            if (!isRetrying)
            {
                isRetrying = true;
                Invoke(nameof(RetryInputConnections), 0.5f);
            }
        }
    }
    
    /// <summary>
    /// 重試輸入連接初始化（帶重試次數限制）
    /// </summary>
    private void RetryInputConnections()
    {
        // 防止物件被銷毀後繼續執行
        if (this == null || gameObject == null)
        {
            isRetrying = false;
            return;
        }
        
        retryCount++;
        var inputWrapper = InputSystemWrapper.Instance;
        
        if (inputWrapper != null)
        {
            try
            {
                inputWrapper.InitializeInputConnections();
                Debug.Log("[CreatureController] 延遲輸入連接初始化完成");
                
                // 成功時重置所有重試狀態
                retryCount = 0;
                isRetrying = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CreatureController] 輸入連接初始化失敗: {e.Message}");
                isRetrying = false;
            }
        }
        else if (retryCount < MAX_RETRY_COUNT)
        {
            Debug.LogWarning($"[CreatureController] 重試輸入連接初始化 ({retryCount}/{MAX_RETRY_COUNT})");
            Invoke(nameof(RetryInputConnections), 1.0f); // 延長重試間隔
        }
        else
        {
            Debug.LogError($"[CreatureController] 達到最大重試次數，無法建立與 InputSystemWrapper 的連接");
            isRetrying = false; // 重置重試狀態
        }
    }
    
    private void OnDestroy()
    {
        // 清理Invoke調用
        CancelInvoke();
        
        if (_instance == this)
        {
            // 取消事件監聽
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // 清理引用
            currentControlledCreature = null;
            controllableCreatures.Clear();
            currentCamera = null;
            
            // 重置單例引用
            _instance = null;
            
            Debug.Log("CreatureController 已清理");
        }
    }
    
    /// <summary>
    /// 重新註冊所有生物（用於動態生成生物後）
    /// </summary>
    public void RefreshCreatureRegistration()
    {
        RegisterCreaturesInScene();
    }
}