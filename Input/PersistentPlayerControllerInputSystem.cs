using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Linq;

public class PersistentPlayerControllerInputSystem : MonoBehaviour
{
    private static PersistentPlayerControllerInputSystem _instance;
    private static bool _isQuitting = false;
    
    public static PersistentPlayerControllerInputSystem Instance
    {
        get
        {
            // 如果應用程式正在退出或場景正在切換，不要創建新實例
            if (_isQuitting || _instance == null)
            {
                if (_instance == null && !_isQuitting)
                {
                    // 嘗試找到現有的實例
                    _instance = FindFirstObjectByType<PersistentPlayerControllerInputSystem>();
                    
                    // 如果沒有找到，創建一個新的
                    if (_instance == null)
                    {
                        GameObject controllerObj = new GameObject("PersistentPlayerControllerInputSystem");
                        _instance = controllerObj.AddComponent<PersistentPlayerControllerInputSystem>();
                    }
                }
            }
            return _instance;
        }
    }
    
    [Header("攝影機設定")]
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -5);
    
    // Input System
    private InputSystem_Actions inputActions;
    
    // 當前控制的生物
    private IControllable currentControlledCreature;
    
    // 可控制的生物列表
    private List<IControllable> controllableCreatures = new List<IControllable>();
    private int currentCreatureIndex = 0;
    
    // 攝影機引用（每個場景可能不同）
    private Camera currentCamera;
    
    // 輸入狀態
    private Vector2 movementInput;
    
    // 事件
    public event Action<IControllable> OnCreatureSwitched;
    public event Action OnSceneChanged;
    
    // 場景傳送相關
    [System.Serializable]
    public class CreatureTransferData
    {
        public string creatureName;
        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public float maxHealth;
        public bool isControlled;
        public string creatureType;
        
        public CreatureTransferData(IControllable creature)
        {
            Transform transform = creature.GetTransform();
            creatureName = transform.name;
            position = transform.position;
            rotation = transform.rotation;
            
            // 如果是Creature類型，保存更多資料
            if (creature is ControllableCreature creatureComponent)
            {
                health = creatureComponent.CurrentHealth;
                maxHealth = creatureComponent.MaxHealth;
                creatureType = creatureComponent.GetType().Name;
            }
            
            isControlled = (creature == Instance.currentControlledCreature);
        }
    }
    
    private List<CreatureTransferData> transferData = new List<CreatureTransferData>();
    private bool shouldTransferCreatures = false;
    
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
            
            // 初始化 Input System
            InitializeInputSystem();
            
            // 監聽場景載入事件
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void InitializeInputSystem()
    {
        inputActions = new InputSystem_Actions();
        
        // 綁定輸入事件
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Attack.performed += OnAttackPerformed;
        inputActions.Player.Interact.performed += OnInteractPerformed;
        inputActions.Player.SwitchCreature.performed += OnSwitchCreaturePerformed;
        inputActions.Player.ShowInfo.performed += OnShowInfoPerformed;
        inputActions.Player.NumberKey.performed += OnNumberKeyPerformed;
        
        // 啟用輸入
        inputActions.Player.Enable();
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            // 取消事件監聽
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // 清理 Input System
            if (inputActions != null)
            {
                inputActions.Player.Disable();
                inputActions.Dispose();
                inputActions = null;
            }
            
            // 清理引用
            currentControlledCreature = null;
            controllableCreatures.Clear();
            currentCamera = null;
            
            // 重置單例引用
            _instance = null;
            
            Debug.Log("PersistentPlayerControllerInputSystem 已清理");
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
    
    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Enable();
        }
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }
    
    // Input System 事件處理
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        movementInput = Vector2.zero;
    }
    
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (currentControlledCreature != null)
        {
            currentControlledCreature.HandleJumpInput(true);
        }
    }
    
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (currentControlledCreature != null)
        {
            currentControlledCreature.HandleAttackInput(true);
        }
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (currentControlledCreature != null)
        {
            currentControlledCreature.HandleInteractInput(true);
        }
    }
    
    // 新增數字鍵處理
    private void OnNumberKeyPerformed(InputAction.CallbackContext context)
    {
        if (currentControlledCreature != null)
        {

            int numKeyValue; 
            int.TryParse(context.control.name, out numKeyValue);
            Debug.Log("[NumberKey] 按下數字鍵: " + numKeyValue);
            currentControlledCreature.HandleNumberKeyInput(numKeyValue);
        }
    }

    private void OnSwitchCreaturePerformed(InputAction.CallbackContext context)
    {
        SwitchToNextCreature();
    }
    
    private void OnShowInfoPerformed(InputAction.CallbackContext context)
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
        
        // 自動註冊新場景中的生物
        AutoRegisterCreaturesInScene();
        
        // 恢復傳送的生物
        RestoreTransferredCreatures();
        
        // 觸發場景變更事件
        OnSceneChanged?.Invoke();
    }
    
    private void Update()
    {
        SendInputToControlledCreature();
        UpdateCamera();
    }
    
    private void SendInputToControlledCreature()
    {
        if (currentControlledCreature == null) return;
        
        // 發送移動輸入到當前控制的生物
        currentControlledCreature.HandleMovementInput(movementInput);
    }
    
    private void UpdateCamera()
    {
        if (currentControlledCreature == null || currentCamera == null) return;
        
        Transform targetTransform = currentControlledCreature.GetTransform();
        if (targetTransform == null) return;
        
        Vector3 targetPosition = targetTransform.position + cameraOffset;
        currentCamera.transform.position = Vector3.Lerp(
            currentCamera.transform.position, 
            targetPosition, 
            cameraFollowSpeed * Time.deltaTime
        );
        
        // 讓攝影機看向目標
        Vector3 lookDirection = targetTransform.position - currentCamera.transform.position;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            currentCamera.transform.rotation = Quaternion.Lerp(
                currentCamera.transform.rotation,
                targetRotation,
                cameraFollowSpeed * Time.deltaTime
            );
        }
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
    
    // private void AutoRegisterCreaturesInScene()
    // {
    //     // 自動註冊場景中所有的 Creature 組件
    //     Creature[] creatures = FindObjectsByType<Creature>();
        
    //     foreach (Creature creature in creatures)
    //     {
    //         RegisterControllableCreature(creature);
    //     }
        
    //     Debug.Log($"自動註冊了 {creatures.Length} 個生物");
    // }
    
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
    
    // 手動設定攝影機
    public void SetCamera(Camera camera)
    {
        currentCamera = camera;
        Debug.Log($"手動設定攝影機: {camera.name}");
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
    
    // 設定攝影機參數
    public void SetCameraSettings(float followSpeed, Vector3 offset)
    {
        cameraFollowSpeed = followSpeed;
        cameraOffset = offset;
    }
    
    // 獲取當前輸入值（供其他系統使用）
    public Vector2 GetMovementInput()
    {
        return movementInput;
    }
    
    // 檢查特定動作是否被按下
    public bool IsActionPressed(string actionName)
    {
        switch (actionName.ToLower())
        {
            case "jump":
                return inputActions.Player.Jump.IsPressed();
            case "attack":
                return inputActions.Player.Attack.IsPressed();
            case "interact":
                return inputActions.Player.Interact.IsPressed();
            case "sprint":
                return inputActions.Player.Sprint.IsPressed();
            default:
                return false;
        }
    }
    
    // === 生物傳送功能（使用DontDestroyOnLoad） ===
    
    // 持久化的生物列表（跨場景保持）
    private List<GameObject> persistentCreatures = new List<GameObject>();
    
    // 攝影機傳送設定
    public enum CameraTransferMode
    {
        FollowPlayer,       // 跟隨玩家（預設）
        UseSceneCamera,     // 使用場景中的固定攝影機
        SetFixedPosition    // 設定固定位置
    }
    
    [System.Serializable]
    public class CameraTransferSettings
    {
        public CameraTransferMode mode;
        public bool setCameraPosition;
        public Vector3 cameraPosition;
        public Vector3 cameraRotation;
        public bool followPlayer;
        public string sceneCameraTag;
        public string sceneCameraName;
        
        public CameraTransferSettings(Vector3 position, Vector3 rotation, bool follow)
        {
            mode = CameraTransferMode.SetFixedPosition;
            setCameraPosition = true;
            cameraPosition = position;
            cameraRotation = rotation;
            followPlayer = follow;
        }
        
        public CameraTransferSettings(CameraTransferMode transferMode, string cameraTag, string cameraName)
        {
            mode = transferMode;
            setCameraPosition = true;
            sceneCameraTag = cameraTag;
            sceneCameraName = cameraName;
            followPlayer = false;
        }
    }
    
    private CameraTransferSettings cameraTransferSettings;
    
    /// <summary>
    /// 檢查是否有生物需要傳送
    /// </summary>
    public bool HasCreaturesToTransfer()
    {
        return shouldTransferCreatures && transferData.Count > 0;
    }
    
    /// <summary>
    /// 獲取傳送生物的數量
    /// </summary>
    public int GetTransferCreatureCount()
    {
        return transferData.Count;
    }
    
    /// <summary>
    /// 準備生物傳送（使用DontDestroyOnLoad）
    /// </summary>
    /// <param name="spawnPosition">生成位置</param>
    public void PrepareCreatureTransfer(Vector3 spawnPosition)
    {
        // 清理之前的傳送資料
        transferData.Clear();
        persistentCreatures.Clear();
        
        // 保存當前所有可控制生物的資料
        foreach (IControllable creature in controllableCreatures)
        {
            if (creature != null)
            {
                try
                {
                    CreatureTransferData data = new CreatureTransferData(creature);
                    data.position = spawnPosition; // 設置新的生成位置
                    transferData.Add(data);
                    
                    // 將生物設為持久化（DontDestroyOnLoad）
                    GameObject creatureObj = creature.GetTransform().gameObject;
                    DontDestroyOnLoad(creatureObj);
                    persistentCreatures.Add(creatureObj);
                    
                    Debug.Log($"設置生物 {creatureObj.name} 為持久化");
                }
                catch (MissingReferenceException)
                {
                    Debug.LogWarning("嘗試設置已銷毀的生物為持久化，跳過");
                }
            }
        }
        
        shouldTransferCreatures = persistentCreatures.Count > 0;
        Debug.Log($"成功設置 {persistentCreatures.Count} 個生物為持久化");
    }
    
    /// <summary>
    /// 清理持久化生物列表
    /// </summary>
    private void CleanupPersistentCreatures()
    {
        // 移除已被銷毀的持久化生物
        persistentCreatures.RemoveAll(obj => obj == null);
    }
    
    /// <summary>
    /// 在新場景中恢復傳送的生物（處理DontDestroyOnLoad的生物）
    /// </summary>
    private void RestoreTransferredCreatures()
    {
        if (!shouldTransferCreatures) return;
        
        Debug.Log($"開始恢復持久化生物，持久化生物數量: {persistentCreatures.Count}，傳送資料數量: {transferData.Count}");
        
        // 處理持久化的生物（使用DontDestroyOnLoad的生物）
        CleanupPersistentCreatures();
        
        foreach (GameObject persistentCreatureObj in persistentCreatures)
        {
            if (persistentCreatureObj != null)
            {
                ControllableCreature creature = persistentCreatureObj.GetComponent<ControllableCreature>();
                if (creature != null)
                {
                    // 重新註冊持久化的生物
                    RegisterControllableCreature(creature);
                    
                    // 檢查是否有對應的傳送資料來更新位置
                    CreatureTransferData matchingData = transferData.Find(data => data.creatureName == creature.name);
                    if (matchingData != null)
                    {
                        // 更新位置到新的生成點
                        creature.transform.position = matchingData.position;
                        creature.transform.rotation = matchingData.rotation;
                        
                        // 如果這個生物之前被控制，重新設為控制對象
                        if (matchingData.isControlled)
                        {
                            SetControlledCreature(creature);
                        }
                        
                        Debug.Log($"恢復持久化生物: {creature.name} 到位置: {matchingData.position}");
                    }
                    else
                    {
                        Debug.Log($"重新註冊持久化生物: {creature.name}");
                    }
                }
            }
        }
        
        // 處理沒有對應持久化生物的傳送資料（創建新生物）
        foreach (CreatureTransferData data in transferData)
        {
            bool foundPersistentCreature = persistentCreatures.Any(obj => 
                obj != null && obj.name == data.creatureName);
            
            if (!foundPersistentCreature)
            {
                // 嘗試在新場景中找到同名的生物
                ControllableCreature existingCreature = FindCreatureByName(data.creatureName);
                
                if (existingCreature != null)
                {
                    // 恢復生物狀態
                    RestoreCreatureState(existingCreature, data);
                }
                else
                {
                    // 如果找不到同名生物，嘗試創建新的生物實例
                    CreateTransferredCreature(data);
                }
            }
        }
        
        // 應用攝影機傳送設定
        ApplyCameraTransferSettings();
        
        // 清理傳送資料
        transferData.Clear();
        shouldTransferCreatures = false;
        
        Debug.Log($"生物傳送恢復完成，當前可控制生物數量: {controllableCreatures.Count}");
    }
    
    /// <summary>
    /// 根據名稱尋找生物
    /// </summary>
    private ControllableCreature FindCreatureByName(string creatureName)
    {
        ControllableCreature[] allCreatures = FindObjectsByType<ControllableCreature>(FindObjectsSortMode.None);
        
        foreach (ControllableCreature creature in allCreatures)
        {
            if (creature.name == creatureName)
            {
                return creature;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 恢復生物狀態
    /// </summary>
    private void RestoreCreatureState(ControllableCreature creature, CreatureTransferData data)
    {
        // 設置位置和旋轉
        creature.transform.position = data.position;
        creature.transform.rotation = data.rotation;
        
        // 確保Creature完全初始化（等待一幀讓Awake和Start執行）
        StartCoroutine(RestoreCreatureStateDelayed(creature, data));
    }
    
    /// <summary>
    /// 延遲恢復生物狀態（確保初始化完成）
    /// </summary>
    private System.Collections.IEnumerator RestoreCreatureStateDelayed(ControllableCreature creature, CreatureTransferData data)
    {
        // 等待一幀確保Awake和Start方法執行完畢
        yield return null;
        
        // 再等待一幀確保StateMachine完全初始化
        yield return null;
        
        // 恢復生命值
        if (data.health > 0)
        {
            // 直接設置生命值，避免觸發狀態變更
            SetCreatureHealthDirectly(creature, (int)data.health);
        }
        
        // 註冊為可控制生物
        RegisterControllableCreature(creature);
        
        // 如果這個生物之前被控制，重新設為控制對象
        if (data.isControlled)
        {
            SetControlledCreature(creature);
        }
        
        Debug.Log($"恢復生物狀態: {data.creatureName} 位置: {data.position} 生命值: {data.health}");
    }
    
    /// <summary>
    /// 直接設置生物生命值（避免觸發狀態變更）
    /// </summary>
    private void SetCreatureHealthDirectly(Creature creature, int targetHealth)
    {
        try
        {
            // 使用反射直接設置私有字段，避免觸發狀態變更
            var currentHealthField = typeof(Creature).GetField("currentHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentHealthField != null)
            {
                // 確保目標生命值在有效範圍內
                int clampedHealth = Mathf.Clamp(targetHealth, 0, creature.MaxHealth);
                currentHealthField.SetValue(creature, clampedHealth);
                
                // 注意：無法直接觸發事件，因為事件只能在類內部觸發
                // 這裡省略事件觸發，生命值已正確設置
                
                Debug.Log($"直接設置 {creature.name} 生命值為 {clampedHealth}");
            }
            else
            {
                Debug.LogWarning($"無法找到currentHealth字段，使用備用方法");
                // 備用方法：如果反射失敗，嘗試使用Heal方法
                if (targetHealth < creature.MaxHealth)
                {
                    int damageToTake = creature.MaxHealth - targetHealth;
                    creature.TakeDamage(damageToTake);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"設置生物生命值時發生錯誤: {e.Message}");
            // 最後的備用方法
            if (targetHealth < creature.CurrentHealth)
            {
                int damageToTake = creature.CurrentHealth - targetHealth;
                creature.TakeDamage(damageToTake);
            }
            else if (targetHealth > creature.CurrentHealth)
            {
                int healAmount = targetHealth - creature.CurrentHealth;
                creature.Heal(healAmount);
            }
        }
    }
    
    /// <summary>
    /// 創建傳送的生物（如果新場景中沒有同名生物）
    /// </summary>
    private void CreateTransferredCreature(CreatureTransferData data)
    {
        Debug.Log($"嘗試創建傳送的生物: {data.creatureName}");
        
        // 嘗試創建一個基本的生物實例
        GameObject creatureObj = CreateBasicCreature(data.creatureName, data.position, data.rotation);
        
        if (creatureObj != null)
        {
            ControllableCreature creature = creatureObj.GetComponent<ControllableCreature>();
            if (creature != null)
            {
                // 恢復生物狀態
                RestoreCreatureState(creature, data);
                Debug.Log($"成功創建並恢復生物: {data.creatureName}");
            }
            else
            {
                Debug.LogError($"創建的物件 {data.creatureName} 沒有Creature組件");
                Destroy(creatureObj);
            }
        }
        else
        {
            Debug.LogWarning($"無法創建生物 {data.creatureName}，將嘗試使用場景中的任意生物");
            // 嘗試使用場景中的第一個生物作為替代
            TryUseAlternativeCreature(data);
        }
    }
    
    /// <summary>
    /// 創建基本的生物實例
    /// </summary>
    private GameObject CreateBasicCreature(string creatureName, Vector3 position, Quaternion rotation)
    {
        try
        {
            // 創建一個基本的生物GameObject
            GameObject creatureObj = new GameObject(creatureName);
            creatureObj.transform.position = position;
            creatureObj.transform.rotation = rotation;
            
            // 添加必要的組件
            ControllableCreature creature = creatureObj.AddComponent<ControllableCreature>();
            
            // 添加Rigidbody用於物理
            Rigidbody rb = creatureObj.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 5f;
            rb.angularDamping = 5f;
            
            // 添加Collider用於碰撞檢測
            CapsuleCollider collider = creatureObj.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0, 1f, 0);
            
            // 創建視覺表示（簡單的膠囊體）
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(creatureObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            
            // 移除visual的collider，因為我們已經在父物件上有了
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                DestroyImmediate(visualCollider);
            }
            
            // 設置標籤和層級
            creatureObj.tag = "Player"; // 或者適當的標籤
            creatureObj.layer = 0; // Default layer
            
            Debug.Log($"成功創建基本生物: {creatureName}");
            return creatureObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"創建生物時發生錯誤: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 嘗試使用場景中的替代生物
    /// </summary>
    private void TryUseAlternativeCreature(CreatureTransferData data)
    {
        // 尋找場景中任何可用的生物
        ControllableCreature[] allCreatures = FindObjectsByType<ControllableCreature>(FindObjectsSortMode.None);
        
        if (allCreatures.Length > 0)
        {
            // 找一個還沒有被註冊的生物，或者使用第一個
            ControllableCreature availableCreature = null;
            
            foreach (ControllableCreature creature in allCreatures)
            {
                if (!controllableCreatures.Contains(creature))
                {
                    availableCreature = creature;
                    break;
                }
            }
            
            // 如果沒有未註冊的生物，使用第一個
            if (availableCreature == null && allCreatures.Length > 0)
            {
                availableCreature = allCreatures[0];
            }
            
            if (availableCreature != null)
            {
                // 重命名生物以匹配原始名稱
                string originalName = availableCreature.name;
                availableCreature.name = data.creatureName;
                
                // 恢復狀態
                RestoreCreatureState(availableCreature, data);
                
                Debug.Log($"使用替代生物 '{originalName}' 作為 '{data.creatureName}'");
            }
        }
        else
        {
            Debug.LogWarning($"場景中沒有任何生物可用於恢復 {data.creatureName}");
        }
    }
    
    /// <summary>
    /// 設置生物傳送的生成位置
    /// </summary>
    /// <param name="position">生成位置</param>
    public void SetCreatureSpawnPosition(Vector3 position)
    {
        foreach (CreatureTransferData data in transferData)
        {
            data.position = position;
        }
        Debug.Log($"設置生物生成位置: {position}");
    }
    
    /// <summary>
    /// 自動註冊場景中所有的 Creature 組件
    /// </summary>
    private void AutoRegisterCreaturesInScene()
    {
        // 獲取場景中所有的 Creature 組件
        ControllableCreature[] creatures = FindObjectsByType<ControllableCreature>(FindObjectsSortMode.None);
        
        // 獲取持久化生物的名稱列表
        HashSet<string> persistentCreatureNames = new HashSet<string>();
        foreach (GameObject persistentObj in persistentCreatures)
        {
            if (persistentObj != null)
            {
                persistentCreatureNames.Add(persistentObj.name);
            }
        }
        
        int registeredCount = 0;
        int skippedCount = 0;
        
        foreach (ControllableCreature creature in creatures)
        {
            // 檢查這個生物是否是持久化生物
            bool isPersistentCreature = persistentCreatures.Contains(creature.gameObject);
            
            // 檢查是否有同名的持久化生物存在
            bool hasPersistentDuplicate = !isPersistentCreature && persistentCreatureNames.Contains(creature.name);
            
            if (hasPersistentDuplicate)
            {
                // 如果場景中有同名的持久化生物，銷毀場景中的重複生物
                Debug.Log($"發現重複生物 {creature.name}，銷毀場景中的副本");
                Destroy(creature.gameObject);
                skippedCount++;
            }
            else
            {
                // 註冊生物
                RegisterControllableCreature(creature);
                registeredCount++;
            }
        }
        
        Debug.Log($"自動註冊了 {registeredCount} 個生物，跳過 {skippedCount} 個重複生物");
    }
    
    /// <summary>
    /// 取消生物傳送
    /// </summary>
    public void CancelCreatureTransfer()
    {
        transferData.Clear();
        shouldTransferCreatures = false;
        cameraTransferSettings = null;
        Debug.Log("取消生物傳送");
    }
    
    /// <summary>
    /// 設置攝影機傳送設定
    /// </summary>
    /// <param name="cameraPosition">攝影機位置</param>
    /// <param name="cameraRotation">攝影機旋轉</param>
    /// <param name="followPlayer">是否跟隨玩家</param>
    public void SetCameraTransferSettings(Vector3 cameraPosition, Vector3 cameraRotation, bool followPlayer)
    {
        cameraTransferSettings = new CameraTransferSettings(cameraPosition, cameraRotation, followPlayer);
        Debug.Log($"設置攝影機傳送設定 - 位置: {cameraPosition}, 旋轉: {cameraRotation}, 跟隨玩家: {followPlayer}");
    }
    
    /// <summary>
    /// 設置攝影機傳送模式
    /// </summary>
    /// <param name="mode">攝影機模式</param>
    /// <param name="cameraTag">攝影機標籤</param>
    /// <param name="cameraName">攝影機名稱</param>
    public void SetCameraTransferMode(CameraTransferMode mode, string cameraTag, string cameraName)
    {
        cameraTransferSettings = new CameraTransferSettings(mode, cameraTag, cameraName);
        Debug.Log($"設置攝影機傳送模式 - 模式: {mode}, 標籤: {cameraTag}, 名稱: {cameraName}");
    }
    
    /// <summary>
    /// 應用攝影機傳送設定
    /// </summary>
    private void ApplyCameraTransferSettings()
    {
        if (cameraTransferSettings == null || !cameraTransferSettings.setCameraPosition) return;
        
        switch (cameraTransferSettings.mode)
        {
            case CameraTransferMode.FollowPlayer:
                // 跟隨玩家模式，使用預設攝影機跟隨
                Debug.Log("攝影機設定為跟隨玩家模式");
                break;
                
            case CameraTransferMode.UseSceneCamera:
                // 使用場景中的固定攝影機
                Camera sceneCamera = FindSceneCamera(cameraTransferSettings.sceneCameraTag, cameraTransferSettings.sceneCameraName);
                if (sceneCamera != null)
                {
                    // 切換到場景攝影機
                    SetCamera(sceneCamera);
                    DisableCameraFollow(); // 停用跟隨功能
                    Debug.Log($"切換到場景攝影機: {sceneCamera.name}");
                }
                else
                {
                    Debug.LogWarning($"找不到場景攝影機 - 標籤: {cameraTransferSettings.sceneCameraTag}, 名稱: {cameraTransferSettings.sceneCameraName}");
                }
                break;
                
            case CameraTransferMode.SetFixedPosition:
                // 設定固定位置
                if (currentCamera != null)
                {
                    currentCamera.transform.position = cameraTransferSettings.cameraPosition;
                    currentCamera.transform.rotation = Quaternion.Euler(cameraTransferSettings.cameraRotation);
                    
                    // 根據設定決定是否跟隨玩家
                    if (!cameraTransferSettings.followPlayer)
                    {
                        DisableCameraFollow();
                    }
                    
                    Debug.Log($"應用攝影機固定位置設定 - 位置: {cameraTransferSettings.cameraPosition}, 旋轉: {cameraTransferSettings.cameraRotation}");
                }
                else
                {
                    Debug.LogWarning("無法應用攝影機設定：找不到攝影機");
                }
                break;
        }
        
        // 清理設定
        cameraTransferSettings = null;
    }
    
    /// <summary>
    /// 尋找場景中的攝影機
    /// </summary>
    /// <param name="cameraTag">攝影機標籤</param>
    /// <param name="cameraName">攝影機名稱（可選）</param>
    /// <returns>找到的攝影機，如果沒找到則返回null</returns>
    private Camera FindSceneCamera(string cameraTag, string cameraName)
    {
        // 獲取場景中所有的攝影機
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        
        Debug.Log($"場景中找到 {allCameras.Length} 個攝影機");
        
        // 如果指定了名稱，優先按名稱搜尋
        if (!string.IsNullOrEmpty(cameraName))
        {
            foreach (Camera cam in allCameras)
            {
                if (cam.name == cameraName)
                {
                    Debug.Log($"按名稱找到攝影機: {cam.name}");
                    return cam;
                }
            }
            Debug.LogWarning($"按名稱 '{cameraName}' 找不到攝影機");
        }
        
        // 按標籤搜尋
        if (!string.IsNullOrEmpty(cameraTag))
        {
            foreach (Camera cam in allCameras)
            {
                if (cam.CompareTag(cameraTag))
                {
                    Debug.Log($"按標籤找到攝影機: {cam.name} (標籤: {cameraTag})");
                    return cam;
                }
            }
            Debug.LogWarning($"按標籤 '{cameraTag}' 找不到攝影機");
        }
        
        // 如果都找不到，嘗試找MainCamera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log($"使用主攝影機作為備用: {mainCamera.name}");
            return mainCamera;
        }
        
        // 最後嘗試使用第一個找到的攝影機
        if (allCameras.Length > 0)
        {
            Debug.Log($"使用第一個找到的攝影機作為備用: {allCameras[0].name}");
            return allCameras[0];
        }
        
        Debug.LogError("場景中沒有找到任何攝影機！");
        return null;
    }
    
    /// <summary>
    /// 暫時停用攝影機跟隨功能
    /// </summary>
    private System.Collections.IEnumerator DisableCameraFollowTemporarily()
    {
        // 保存原始的跟隨設定
        float originalFollowSpeed = cameraFollowSpeed;
        Vector3 originalOffset = cameraOffset;
        
        // 暫時停用跟隨
        cameraFollowSpeed = 0f;
        
        // 等待一段時間後恢復（可以根據需要調整）
        yield return new WaitForSeconds(2f);
        
        // 恢復原始設定
        cameraFollowSpeed = originalFollowSpeed;
        cameraOffset = originalOffset;
        
        Debug.Log("攝影機跟隨功能已恢復");
    }
    
    /// <summary>
    /// 手動啟用攝影機跟隨
    /// </summary>
    public void EnableCameraFollow()
    {
        // 停止任何正在運行的停用協程
        StopAllCoroutines();
        
        // 恢復攝影機跟隨
        if (cameraFollowSpeed <= 0)
        {
            cameraFollowSpeed = 5f; // 預設值
        }
        
        Debug.Log("手動啟用攝影機跟隨");
    }
    
    /// <summary>
    /// 手動停用攝影機跟隨
    /// </summary>
    public void DisableCameraFollow()
    {
        cameraFollowSpeed = 0f;
        Debug.Log("手動停用攝影機跟隨");
    }
    
    /// <summary>
    /// 設置攝影機為固定位置模式
    /// </summary>
    /// <param name="position">固定位置</param>
    /// <param name="rotation">固定旋轉</param>
    public void SetCameraFixedMode(Vector3 position, Vector3 rotation)
    {
        if (currentCamera != null)
        {
            currentCamera.transform.position = position;
            currentCamera.transform.rotation = Quaternion.Euler(rotation);
            DisableCameraFollow();
            
            Debug.Log($"設置攝影機為固定模式 - 位置: {position}, 旋轉: {rotation}");
        }
    }
}
