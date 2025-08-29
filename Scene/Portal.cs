using UnityEngine;
using System.Collections;

/// <summary>
/// 傳送門類別 - 玩家接近或踩上時切換場景
/// </summary>
public class Portal : MonoBehaviour
{
    [Header("傳送門設定")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string portalName = "傳送門";
    
    [Header("傳送位置設定")]
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
    
    [Header("相機設定")]
    [SerializeField] private string targetCameraName = "";
    
    [Header("場景載入設定")]
    [SerializeField] private bool showLoadingScreen = true;
    
    [Header("觸發設定")]
    [SerializeField] private float activationDelay = 0.5f;
    [SerializeField] private bool instantMode = false;
    
    [Header("除錯設定")]
    [SerializeField] private bool debugMode = false;
    
    // 狀態變數
    private bool isActivated = false;
    private Coroutine activationCoroutine;
    
    public string TargetSceneName => targetSceneName;
    public string PortalName => portalName;
    public bool IsActivated => isActivated;
    
    private void Start()
    {
        InitializePortal();
    }
    
    private void Update()
    {
        // 簡化版本不需要 Update 處理
    }
    
    /// <summary>
    /// 初始化傳送門
    /// </summary>
    private void InitializePortal()
    {
        // 確保有Collider組件且設為Trigger
        Collider portalCollider = GetComponent<Collider>();
        if (portalCollider == null)
        {
            portalCollider = gameObject.AddComponent<BoxCollider>();
            if (debugMode)
                Debug.Log($"{portalName}: 自動添加BoxCollider組件");
        }
        portalCollider.isTrigger = true;
        
        // 驗證目標場景
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"{portalName}: 未設置目標場景名稱！");
        }
        
        if (debugMode)
            Debug.Log($"{portalName} 初始化完成，目標場景: {targetSceneName}");
    }
    
    /// <summary>
    /// 玩家進入觸發器
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsCurrentlyControlledCreature(other.gameObject)) return;
        
        if (debugMode)
            Debug.Log($"{portalName}: 當前控制生物 {other.gameObject.name} 進入，開始傳送");
        
        ActivatePortal();
    }
    
    /// <summary>
    /// 檢查是否為當前控制的生物
    /// </summary>
    private bool IsCurrentlyControlledCreature(GameObject obj)
    {
        if (obj == null) return false;

        var controller = CreatureController.Instance;
        if (controller?.CurrentControlledCreature == null)
        {
            if (debugMode)
                Debug.Log($"{portalName}: 沒有當前控制的生物");
            return false;
        }

        // 檢查這個GameObject是否屬於當前控制的生物
        var controllable = obj.GetComponent<IControllable>();
        bool isControlled = controllable != null && controllable == controller.CurrentControlledCreature;

        if (debugMode && controllable != null)
        {
            Debug.Log($"{portalName}: 檢測到生物 {obj.name}, 是否為當前控制: {isControlled}");
        }

        return isControlled;
    }
    
    /// <summary>
    /// 激活傳送門
    /// </summary>
    public void ActivatePortal()
    {
        if (isActivated) return;
        
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"{portalName}: 無法激活，目標場景名稱為空！");
            return;
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 開始激活，目標場景: {targetSceneName}");
        
        isActivated = true;
        
        // 開始激活協程
        activationCoroutine = StartCoroutine(ActivationSequence());
    }
    
    /// <summary>
    /// 激活序列 - 整合場景載入功能
    /// </summary>
    private IEnumerator ActivationSequence()
    {
        // 即時模式或無延遲時直接載入
        if (instantMode || activationDelay <= 0)
        {
            LoadSceneWithSettings();
            yield break;
        }
        
        // 等待激活延遲
        yield return new WaitForSeconds(activationDelay);
        
        // 開始場景載入
        LoadSceneWithSettings();
    }
    
    /// <summary>
    /// 使用設定載入場景
    /// </summary>
    private void LoadSceneWithSettings()
    {
        if (debugMode)
            Debug.Log($"{portalName}: 開始載入場景 {targetSceneName}");
        
        // 只在非即時模式時才顯示載入畫面
        if (showLoadingScreen && !instantMode && LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.OnSceneLoadStarted(targetSceneName);
        }
        
        // 使用Unity內建的場景載入
        StartCoroutine(LoadSceneAsync());
    }
    
    /// <summary>
    /// 異步載入場景
    /// </summary>
    private IEnumerator LoadSceneAsync()
    {
        var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetSceneName);
        
        // 即時模式或無延遲時直接允許場景激活
        if (instantMode || activationDelay <= 0)
        {
            asyncOperation.allowSceneActivation = true;
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
        }
        else
        {
            asyncOperation.allowSceneActivation = false;
            
            // 模擬載入進度
            while (!asyncOperation.isDone)
            {
                float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                
                if (debugMode)
                    Debug.Log($"{portalName}: 場景載入進度: {progress * 100:F1}%");
                
                // 當進度達到90%時，允許場景激活
                if (asyncOperation.progress >= 0.9f)
                {
                    // 只在有延遲設定時才等待
                    if (activationDelay > 0)
                    {
                        yield return new WaitForSeconds(0.1f);
                    }
                    asyncOperation.allowSceneActivation = true;
                }
                
                yield return null;
            }
        }
        
        // 場景載入完成後處理
        if (!instantMode && activationDelay > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        SetupNewScene();
        
        // 隱藏載入畫面
        if (showLoadingScreen && LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.OnSceneLoadCompleted(targetSceneName);
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 場景載入完成: {targetSceneName}");
    }
    
    /// <summary>
    /// 設置新場景
    /// </summary>
    private void SetupNewScene()
    {
        // 設定玩家位置
        SetPlayerPosition();
        
        // 設定相機
        SetupTargetCamera();
    }
    
    /// <summary>
    /// 設定玩家位置
    /// </summary>
    private void SetPlayerPosition()
    {
        var controller = CreatureController.Instance;
        if (controller?.CurrentControlledCreature != null)
        {
            var playerTransform = controller.CurrentControlledCreature.GetTransform();
            if (playerTransform != null)
            {
                playerTransform.position = playerSpawnPosition;
                
                // 如果玩家有CharacterController，需要特殊處理
                var characterController = playerTransform.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                    playerTransform.position = playerSpawnPosition;
                    characterController.enabled = true;
                }
                
                if (debugMode)
                    Debug.Log($"{portalName}: 玩家位置設定為 {playerSpawnPosition}");
            }
        }
    }
    
    /// <summary>
    /// 設置目標相機
    /// </summary>
    private void SetupTargetCamera()
    {
        if (string.IsNullOrEmpty(targetCameraName)) return;
        
        // 通過名稱查找相機
        GameObject cameraObject = GameObject.Find(targetCameraName);
        if (cameraObject != null)
        {
            Camera targetCamera = cameraObject.GetComponent<Camera>();
            if (targetCamera != null)
            {
                // 關閉當前主相機
                Camera currentMainCamera = Camera.main;
                if (currentMainCamera != null && currentMainCamera != targetCamera)
                {
                    currentMainCamera.gameObject.SetActive(false);
                }
                
                // 激活目標相機
                targetCamera.gameObject.SetActive(true);
                
                if (debugMode)
                    Debug.Log($"{portalName}: 切換到相機 {targetCameraName}");
            }
            else
            {
                Debug.LogWarning($"{portalName}: 找到物件 {targetCameraName} 但沒有Camera組件");
            }
        }
        else
        {
            Debug.LogWarning($"{portalName}: 找不到名稱為 {targetCameraName} 的相機物件");
        }
    }
    
    /// <summary>
    /// 設置目標場景
    /// </summary>
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        
        if (debugMode)
            Debug.Log($"{portalName}: 設置目標場景為 {sceneName}");
    }
    
    /// <summary>
    /// 重置傳送門狀態
    /// </summary>
    public void ResetPortal()
    {
        isActivated = false;
        
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 傳送門狀態已重置");
    }
    
    /// <summary>
    /// 在Scene視圖中顯示傳送門範圍
    /// </summary>
    private void OnDrawGizmos()
    {
        // 顯示觸發範圍
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        
        // 顯示目標位置
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerSpawnPosition, 0.5f);
        
        // 顯示傳送門名稱
        if (!string.IsNullOrEmpty(portalName))
        {
            Vector3 labelPosition = transform.position + Vector3.up * 2f;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, $"{portalName}\n→ {targetSceneName}");
#endif
        }
    }
}
