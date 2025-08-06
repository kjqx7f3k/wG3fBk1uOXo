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
    [SerializeField] private string description = "踩上去進入下一個區域";
    
    [Header("傳送位置設定")]
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
    [SerializeField] private bool useCustomSpawnPosition = false;
    [SerializeField] private Transform spawnPoint; // 可選的生成點Transform
    
    [Header("攝影機設定")]
    [SerializeField] private CameraMode cameraMode = CameraMode.FollowPlayer;
    [SerializeField] private Vector3 cameraPosition = Vector3.zero;
    [SerializeField] private Vector3 cameraRotation = Vector3.zero;
    [SerializeField] private Transform cameraTarget; // 可選的攝影機目標點
    [SerializeField] private string sceneCameraTag = "MainCamera"; // 場景攝影機標籤
    [SerializeField] private string sceneCameraName = ""; // 場景攝影機名稱（可選）
    
    public enum CameraMode
    {
        FollowPlayer,           // 跟隨玩家（預設）
        UseSceneCamera,         // 使用場景中的固定攝影機
        SetFixedPosition,       // 設定固定位置
        UseCameraTarget         // 使用指定的Transform目標
    }
    
    [Header("觸發設定")]
    [SerializeField] private PortalTriggerType triggerType = PortalTriggerType.OnTriggerEnter;
    [SerializeField] private float activationDelay = 0.5f;
    [SerializeField] private bool requiresInteraction = false;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    
    [Header("視覺效果")]
    [SerializeField] private GameObject portalEffect;
    [SerializeField] private ParticleSystem portalParticles;
    [SerializeField] private Light portalLight;
    [SerializeField] private AudioSource portalAudio;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip teleportSound;
    
    [Header("玩家檢測")]
    [SerializeField] private LayerMask playerLayerMask = 1;
    [SerializeField] private string playerTag = "Player";
    
    [Header("除錯設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool showGizmos = true;
    
    // 狀態變數
    private bool isPlayerNearby = false;
    private bool isActivated = false;
    private GameObject currentPlayer;
    private Coroutine activationCoroutine;
    
    // UI提示
    private bool showingInteractionPrompt = false;
    
    public enum PortalTriggerType
    {
        OnTriggerEnter,     // 玩家進入觸發器時
        OnTriggerStay,      // 玩家停留在觸發器中時
        OnInteraction,      // 需要按鍵互動
        OnProximity         // 接近時（使用距離檢測）
    }
    
    public string TargetSceneName => targetSceneName;
    public string PortalName => portalName;
    public bool IsActivated => isActivated;
    public bool IsPlayerNearby => isPlayerNearby;
    
    private void Start()
    {
        InitializePortal();
    }
    
    private void Update()
    {
        HandleProximityTrigger();
        HandleInteractionInput();
        UpdateVisualEffects();
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
        
        // 初始化視覺效果
        if (portalEffect != null)
        {
            portalEffect.SetActive(true);
        }
        
        if (portalParticles != null && !portalParticles.isPlaying)
        {
            portalParticles.Play();
        }
        
        // 驗證目標場景
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"{portalName}: 未設置目標場景名稱！");
        }
        else if (GameSceneManager.Instance != null && !GameSceneManager.Instance.SceneExists(targetSceneName))
        {
            Debug.LogWarning($"{portalName}: 目標場景 '{targetSceneName}' 不存在於場景資料庫中，但仍可嘗試載入");
        }
        
        if (debugMode)
            Debug.Log($"{portalName} 初始化完成，目標場景: {targetSceneName}");
    }
    
    /// <summary>
    /// 處理接近觸發
    /// </summary>
    private void HandleProximityTrigger()
    {
        if (triggerType != PortalTriggerType.OnProximity) return;
        
        // 尋找附近的玩家
        GameObject player = FindNearbyPlayer();
        
        if (player != null && !isPlayerNearby)
        {
            OnPlayerEnterProximity(player);
        }
        else if (player == null && isPlayerNearby)
        {
            OnPlayerExitProximity();
        }
    }
    
    /// <summary>
    /// 處理互動輸入
    /// </summary>
    private void HandleInteractionInput()
    {
        if (!requiresInteraction || !isPlayerNearby) return;
        
        bool interactPressed = false;
        
        // 檢查是否有 Input System Manager 和現有的 Interact Action
        if (PersistentPlayerControllerInputSystem.Instance != null)
        {
            // 使用現有的 Player Input System 中的 Interact Action
            var inputActions = PersistentPlayerControllerInputSystem.Instance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (inputActions != null)
            {
                var interactAction = inputActions.actions["Interact"];
                if (interactAction != null)
                {
                    interactPressed = interactAction.WasPressedThisFrame();
                }
            }
        }
        
        // 如果沒有專用的 Interact，使用 UI Confirm
        if (!interactPressed && InputSystemWrapper.Instance != null)
        {
            interactPressed = InputSystemWrapper.Instance.GetUIConfirmDown();
        }
        
        if (interactPressed)
        {
            ActivatePortal();
        }
    }
    
    /// <summary>
    /// 更新視覺效果
    /// </summary>
    private void UpdateVisualEffects()
    {
        // 根據玩家是否附近調整效果強度
        if (portalLight != null)
        {
            float targetIntensity = isPlayerNearby ? 2f : 1f;
            portalLight.intensity = Mathf.Lerp(portalLight.intensity, targetIntensity, Time.deltaTime * 2f);
        }
        
        // 粒子效果調整
        if (portalParticles != null)
        {
            var emission = portalParticles.emission;
            float targetRate = isPlayerNearby ? 50f : 20f;
            emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, targetRate, Time.deltaTime * 2f);
        }
    }
    
    /// <summary>
    /// 尋找附近的玩家
    /// </summary>
    private GameObject FindNearbyPlayer()
    {
        float detectionRadius = 3f;
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayerMask);
        
        foreach (Collider col in nearbyColliders)
        {
            if (col.CompareTag(playerTag))
            {
                return col.gameObject;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 玩家進入觸發器
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidPlayer(other.gameObject)) return;
        
        OnPlayerEnterProximity(other.gameObject);
        
        if (triggerType == PortalTriggerType.OnTriggerEnter && !requiresInteraction)
        {
            ActivatePortal();
        }
    }
    
    /// <summary>
    /// 玩家停留在觸發器中
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (!IsValidPlayer(other.gameObject)) return;
        
        if (triggerType == PortalTriggerType.OnTriggerStay && !requiresInteraction)
        {
            if (!isActivated)
            {
                ActivatePortal();
            }
        }
    }
    
    /// <summary>
    /// 玩家離開觸發器
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (!IsValidPlayer(other.gameObject)) return;
        
        OnPlayerExitProximity();
    }
    
    /// <summary>
    /// 準備生物傳送
    /// </summary>
    private void PrepareCreatureTransfer()
    {
        // 如果有PersistentPlayerControllerInputSystem，準備生物傳送
        if (PersistentPlayerControllerInputSystem.Instance != null)
        {
            // 決定生成位置
            Vector3 spawnPosition = GetPlayerSpawnPosition();
            
            // 準備生物傳送
            PersistentPlayerControllerInputSystem.Instance.PrepareCreatureTransfer(spawnPosition);
            
            // 根據攝影機模式設定攝影機參數
            SetupCameraTransferSettings();
            
            if (debugMode)
            {
                int creatureCount = PersistentPlayerControllerInputSystem.Instance.GetTransferCreatureCount();
                Debug.Log($"{portalName}: 準備傳送 {creatureCount} 個生物到 {targetSceneName}, 生成位置: {spawnPosition}, 攝影機模式: {cameraMode}");
            }
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"{portalName}: 找不到PersistentPlayerControllerInputSystem，無法傳送生物");
        }
    }
    
    /// <summary>
    /// 設定攝影機傳送設定
    /// </summary>
    private void SetupCameraTransferSettings()
    {
        switch (cameraMode)
        {
            case CameraMode.FollowPlayer:
                // 跟隨玩家模式，不需要特殊設定
                if (debugMode)
                    Debug.Log($"{portalName}: 攝影機將跟隨玩家");
                break;
                
            case CameraMode.UseSceneCamera:
                // 使用場景中的固定攝影機
                PersistentPlayerControllerInputSystem.Instance.SetCameraTransferMode(
                    PersistentPlayerControllerInputSystem.CameraTransferMode.UseSceneCamera,
                    sceneCameraTag,
                    sceneCameraName
                );
                if (debugMode)
                    Debug.Log($"{portalName}: 設定使用場景攝影機 - 標籤: {sceneCameraTag}, 名稱: {sceneCameraName}");
                break;
                
            case CameraMode.SetFixedPosition:
                // 設定固定位置
                Vector3 camPos = GetCameraPosition();
                Vector3 camRot = GetCameraRotation();
                PersistentPlayerControllerInputSystem.Instance.SetCameraTransferSettings(camPos, camRot, false);
                if (debugMode)
                    Debug.Log($"{portalName}: 設定攝影機固定位置: {camPos}, 旋轉: {camRot}");
                break;
                
            case CameraMode.UseCameraTarget:
                // 使用指定的Transform目標
                if (cameraTarget != null)
                {
                    Vector3 targetPos = cameraTarget.position;
                    Vector3 targetRot = cameraTarget.eulerAngles;
                    PersistentPlayerControllerInputSystem.Instance.SetCameraTransferSettings(targetPos, targetRot, false);
                    if (debugMode)
                        Debug.Log($"{portalName}: 設定攝影機目標位置: {targetPos}, 旋轉: {targetRot}");
                }
                else
                {
                    Debug.LogWarning($"{portalName}: 攝影機目標未設定，將使用跟隨玩家模式");
                }
                break;
        }
    }
    
    /// <summary>
    /// 獲取玩家生成位置
    /// </summary>
    private Vector3 GetPlayerSpawnPosition()
    {
        if (useCustomSpawnPosition)
        {
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }
            else
            {
                return playerSpawnPosition;
            }
        }
        else
        {
            // 預設使用傳送門前方2單位的位置
            return transform.position + transform.forward * 2f;
        }
    }
    
    /// <summary>
    /// 獲取攝影機位置
    /// </summary>
    private Vector3 GetCameraPosition()
    {
        if (cameraTarget != null)
        {
            return cameraTarget.position;
        }
        else
        {
            return cameraPosition;
        }
    }
    
    /// <summary>
    /// 獲取攝影機旋轉
    /// </summary>
    private Vector3 GetCameraRotation()
    {
        if (cameraTarget != null)
        {
            return cameraTarget.eulerAngles;
        }
        else
        {
            return cameraRotation;
        }
    }
    
    /// <summary>
    /// 玩家進入附近區域
    /// </summary>
    private void OnPlayerEnterProximity(GameObject player)
    {
        isPlayerNearby = true;
        currentPlayer = player;
        
        // 播放激活音效
        if (portalAudio != null && activationSound != null)
        {
            portalAudio.PlayOneShot(activationSound);
        }
        
        // 顯示互動提示
        if (requiresInteraction)
        {
            ShowInteractionPrompt();
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 玩家 {player.name} 進入附近區域");
    }
    
    /// <summary>
    /// 玩家離開附近區域
    /// </summary>
    private void OnPlayerExitProximity()
    {
        isPlayerNearby = false;
        currentPlayer = null;
        
        // 隱藏互動提示
        if (requiresInteraction)
        {
            HideInteractionPrompt();
        }
        
        // 取消激活協程
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 玩家離開附近區域");
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
        
        // 隱藏互動提示
        HideInteractionPrompt();
        
        // 準備生物傳送
        PrepareCreatureTransfer();
        
        // 開始激活協程
        activationCoroutine = StartCoroutine(ActivationSequence());
    }
    
    /// <summary>
    /// 激活序列
    /// </summary>
    private IEnumerator ActivationSequence()
    {
        // 播放傳送音效
        if (portalAudio != null && teleportSound != null)
        {
            portalAudio.PlayOneShot(teleportSound);
        }
        
        // 增強視覺效果
        if (portalParticles != null)
        {
            var emission = portalParticles.emission;
            emission.rateOverTime = 100f;
        }
        
        if (portalLight != null)
        {
            portalLight.intensity = 3f;
        }
        
        // 等待激活延遲
        yield return new WaitForSeconds(activationDelay);
        
        // 載入目標場景
        if (GameSceneManager.Instance != null)
        {
            // 使用GameSceneManager載入場景，這會觸發載入畫面和進度事件
            GameSceneManager.Instance.LoadScene(targetSceneName, true);
            if (debugMode)
                Debug.Log($"{portalName}: 通過GameSceneManager載入場景 {targetSceneName}");
        }
        else
        {
            // 如果沒有GameSceneManager，使用Unity內建的場景載入
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
            if (debugMode)
                Debug.Log($"{portalName}: 通過Unity SceneManager載入場景 {targetSceneName}（警告：無載入進度顯示）");
        }
    }
    
    /// <summary>
    /// 檢查是否為有效玩家
    /// </summary>
    private bool IsValidPlayer(GameObject obj)
    {
        if (obj == null) return false;
        
        // 檢查標籤
        bool hasCorrectTag = obj.CompareTag(playerTag);
        
        // 檢查Layer（如果playerLayerMask不為0）
        bool hasCorrectLayer = playerLayerMask == 0 || ((1 << obj.layer) & playerLayerMask) != 0;
        
        if (debugMode && hasCorrectTag)
        {
            Debug.Log($"{portalName}: 檢測到玩家 {obj.name}, Tag: {hasCorrectTag}, Layer: {hasCorrectLayer} (Layer: {obj.layer}, Mask: {playerLayerMask})");
        }
        
        return hasCorrectTag && hasCorrectLayer;
    }
    
    /// <summary>
    /// 顯示互動提示
    /// </summary>
    private void ShowInteractionPrompt()
    {
        if (showingInteractionPrompt) return;
        
        showingInteractionPrompt = true;
        
        // 這裡可以顯示UI提示，例如 "按 E 鍵進入"
        // 可以與UI系統整合
        if (debugMode)
            Debug.Log($"{portalName}: 顯示互動提示 - 按 {interactionKey} 鍵進入 {targetSceneName}");
    }
    
    /// <summary>
    /// 隱藏互動提示
    /// </summary>
    private void HideInteractionPrompt()
    {
        if (!showingInteractionPrompt) return;
        
        showingInteractionPrompt = false;
        
        // 隱藏UI提示
        if (debugMode)
            Debug.Log($"{portalName}: 隱藏互動提示");
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
        isPlayerNearby = false;
        currentPlayer = null;
        
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
        
        HideInteractionPrompt();
        
        if (debugMode)
            Debug.Log($"{portalName}: 傳送門狀態已重置");
    }
    
    /// <summary>
    /// 在Scene視圖中顯示傳送門範圍
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // 顯示觸發範圍
        Gizmos.color = isPlayerNearby ? Color.green : Color.blue;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        
        // 顯示接近檢測範圍（如果使用接近觸發）
        if (triggerType == PortalTriggerType.OnProximity)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 3f);
        }
        
        // 顯示傳送門名稱
        if (!string.IsNullOrEmpty(portalName))
        {
            Vector3 labelPosition = transform.position + Vector3.up * 2f;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, $"{portalName}\n→ {targetSceneName}");
            #endif
        }
    }
    
    /// <summary>
    /// 在Inspector中顯示傳送門資訊
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // 顯示詳細資訊
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        
        // 顯示方向箭頭（如果有目標位置的話）
        Gizmos.color = Color.magenta;
        Vector3 arrowStart = transform.position + Vector3.up * 0.5f;
        Vector3 arrowEnd = arrowStart + transform.forward * 2f;
        Gizmos.DrawRay(arrowStart, transform.forward * 2f);
    }
}
