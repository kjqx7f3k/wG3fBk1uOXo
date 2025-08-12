using UnityEngine;

public class SceneCreatureManager : MonoBehaviour
{
    public static SceneCreatureManager Instance { get; private set; }
    
    [Header("自動註冊設定")]
    [SerializeField] private bool autoRegisterOnStart = true;
    [SerializeField] private bool registerOnlyTaggedCreatures = false;
    [SerializeField] private string creatureTag = "Controllable";
    
    [Header("場景特定設定")]
    [SerializeField] private Camera sceneCamera;
    
    /// <summary>
    /// 獲取當前正在控制的生物
    /// </summary>
    public IControllable CurrentControlledCreature
    {
        get
        {
            if (PersistentPlayerControllerInputSystem.Instance != null)
            {
                return PersistentPlayerControllerInputSystem.Instance.GetCurrentControlledCreature();
            }
            return null;
        }
    }
    
    private void Awake()
    {
        // 單例模式初始化
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"場景中已存在 SceneCreatureManager 實例，銷毀重複的物件: {gameObject.name}");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 優先使用 Input System 版本的控制器
        var inputSystemController = PersistentPlayerControllerInputSystem.Instance;
        if (inputSystemController != null)
        {
            // 設定場景特定的攝影機
            if (sceneCamera != null)
            {
                inputSystemController.SetCamera(sceneCamera);
            }
            
            // 自動註冊生物
            if (autoRegisterOnStart)
            {
                RegisterCreaturesInScene();
            }
            
            // 監聽控制器事件
            inputSystemController.OnSceneChanged += OnSceneChanged;
            inputSystemController.OnCreatureSwitched += OnCreatureSwitched;
        }
    }
    
    private void OnDestroy()
    {
        // 取消事件監聽
        if (PersistentPlayerControllerInputSystem.Instance!=null)
        {
           PersistentPlayerControllerInputSystem.Instance.OnSceneChanged -= OnSceneChanged;
           PersistentPlayerControllerInputSystem.Instance.OnCreatureSwitched -= OnCreatureSwitched;
        }
    }
    
    private void RegisterCreaturesInScene()
    {
        var controller = PersistentPlayerControllerInputSystem.Instance;
        if (controller == null)
        {
            Debug.LogError("PersistentPlayerControllerInputSystem 實例不存在，無法註冊生物");
            return;
        }
        
        if (registerOnlyTaggedCreatures)
        {
            // 只註冊有特定標籤的生物
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(creatureTag);
            
            foreach (GameObject obj in taggedObjects)
            {
                IControllable controllable = obj.GetComponent<IControllable>();
                if (controllable != null)
                {
                    controller.RegisterControllableCreature(controllable);
                }
            }
            
            Debug.Log($"註冊了 {taggedObjects.Length} 個標籤為 '{creatureTag}' 的可控制物件");
        }
        else
        {
            // 註冊所有 ControllableCreature 組件
            ControllableCreature[] creatures = FindObjectsByType<ControllableCreature>(FindObjectsSortMode.None);
            
            foreach (ControllableCreature creature in creatures)
            {
                controller.RegisterControllableCreature(creature);
            }
            
            Debug.Log($"註冊了 {creatures.Length} 個 ControllableCreature 組件");
        }
    }
    
    private void OnSceneChanged()
    {
        Debug.Log($"場景管理器收到場景變更通知: {PersistentPlayerControllerInputSystem.Instance.GetCurrentSceneName()}");
        
        // 可以在這裡添加場景特定的初始化邏輯
        // 例如：設定特殊的遊戲規則、UI 元素等
    }
    
    private void OnCreatureSwitched(IControllable newCreature)
    {
        Debug.Log($"場景管理器收到生物切換通知: {newCreature.GetTransform().name}");
        
        // 通知InventoryManager切換到新生物的背包
        if (InventoryManager.Instance != null && newCreature is ControllableCreature creature)
        {
            InventoryManager.Instance.SetCurrentCreature(creature);
        }
        
        // 可以在這裡添加生物切換時的場景特定邏輯
        // 例如：更新 UI、播放音效等
    }
    
    // 手動註冊特定生物
    public void RegisterCreature(IControllable creature)
    {
        var controller = PersistentPlayerControllerInputSystem.Instance;
        if (controller != null && creature != null)
        {
            controller.RegisterControllableCreature(creature);
        }
        else
        {
            Debug.LogError("無法註冊生物：控制器或生物為 null");
        }
    }
    
    // 手動取消註冊特定生物
    public void UnregisterCreature(IControllable creature)
    {
        var controller = PersistentPlayerControllerInputSystem.Instance;
        if (controller != null && creature != null)
        {
            controller.UnregisterControllableCreature(creature);
        }
        else
        {
            Debug.LogError("無法取消註冊生物：控制器或生物為 null");
        }
    }
    
    // 重新註冊所有生物（用於動態生成生物後）
    public void RefreshCreatureRegistration()
    {
        RegisterCreaturesInScene();
    }
    
    // 清除當前場景的所有生物註冊
    public void ClearSceneCreatures()
    {
        var controller = PersistentPlayerControllerInputSystem.Instance;
        if (controller != null)
        {
            controller.ClearAllCreatures();
        }
        else
        {
            Debug.LogError("無法清除場景生物：PersistentPlayerControllerInputSystem 實例不存在");
        }
    }
}
