using UnityEngine;
using System.Collections;

/// <summary>
/// 場景內傳送點 - 簡化版本，專注於玩家傳送
/// </summary>
public class InScenePortal : MonoBehaviour
{
    [Header("傳送點設定")]
    [SerializeField] private string portalName = "場景內傳送點";
    [SerializeField] private string description = "踩上去傳送到指定位置";
    
    [Header("傳送位置設定")]
    [SerializeField] private Vector3 targetPosition = Vector3.zero;
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    [SerializeField] private Transform targetTransform; // 可選的目標Transform
    [SerializeField] private bool useTargetTransform = false;
    [SerializeField] private bool maintainPlayerRotation = false; // 是否保持玩家原本的旋轉
    
    [Header("相機控制器")]
    [SerializeField] private CameraController cameraController; // 引用相機控制器
    [SerializeField] private bool changeCameraMode = false; // 是否更改相機模式
    [SerializeField] private CameraController.CameraMode cameraMode = CameraController.CameraMode.FollowPlayer;
    [SerializeField] private Transform cameraTarget; // 可選的相機目標點
    
    [Header("觸發設定")]
    [SerializeField] private PortalTriggerType triggerType = PortalTriggerType.OnTriggerEnter;
    [SerializeField] private float activationDelay = 0.2f;
    [SerializeField] private bool requiresInteraction = false;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float teleportDelay = 0.5f; // 傳送延遲時間
    
    [Header("視覺效果")]
    [SerializeField] private GameObject portalEffect;
    [SerializeField] private ParticleSystem portalParticles;
    [SerializeField] private Light portalLight;
    [SerializeField] private AudioSource portalAudio;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField] private GameObject teleportEffect; // 傳送時的特效
    [SerializeField] private float effectDuration = 1f;
    
    [Header("玩家檢測")]
    [SerializeField] private LayerMask playerLayerMask = 1;
    [SerializeField] private string playerTag = "Player";
    
    [Header("傳送限制")]
    [SerializeField] private float cooldownTime = 2f; // 冷卻時間
    [SerializeField] private bool canTeleportMultipleTimes = true; // 是否可以多次傳送
    [SerializeField] private int maxTeleportCount = -1; // 最大傳送次數（-1為無限制）
    
    [Header("除錯設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;
    
    public enum PortalTriggerType
    {
        OnTriggerEnter,     // 玩家進入觸發器時
        OnTriggerStay,      // 玩家停留在觸發器中時
        OnInteraction,      // 需要按鍵互動
        OnProximity         // 接近時（使用距離檢測）
    }
    
    // 狀態變數
    private bool isPlayerNearby = false;
    private bool isActivated = false;
    private bool isOnCooldown = false;
    private GameObject currentPlayer;
    private Coroutine activationCoroutine;
    private int teleportCount = 0;
    private float lastTeleportTime = 0f;
    
    // UI提示
    private bool showingInteractionPrompt = false;
    
    public string PortalName => portalName;
    public bool IsActivated => isActivated;
    public bool IsPlayerNearby => isPlayerNearby;
    public bool IsOnCooldown => isOnCooldown;
    public int TeleportCount => teleportCount;
    public Vector3 TargetPosition => GetTargetPosition();
    
    private void Start()
    {
        InitializePortal();
    }
    
    private void Update()
    {
        HandleProximityTrigger();
        HandleInteractionInput();
        UpdateVisualEffects();
        UpdateCooldown();
    }
    
    /// <summary>
    /// 初始化傳送點
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
        
        // 驗證設定
        ValidateSettings();
        
        if (debugMode)
            Debug.Log($"{portalName} 初始化完成，目標位置: {GetTargetPosition()}");
    }
    
    /// <summary>
    /// 驗證設定
    /// </summary>
    private void ValidateSettings()
    {
        if (useTargetTransform && targetTransform == null)
        {
            Debug.LogWarning($"{portalName}: 啟用了使用目標Transform但未設置targetTransform！");
        }
        
        if (cameraController == null)
        {
            Debug.LogWarning($"{portalName}: 未設置相機控制器，將使用預設相機行為");
        }
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
        if (!requiresInteraction || !isPlayerNearby || isOnCooldown) return;
        
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
            if (isOnCooldown) targetIntensity *= 0.5f; // 冷卻時降低亮度
            portalLight.intensity = Mathf.Lerp(portalLight.intensity, targetIntensity, Time.deltaTime * 2f);
        }
        
        // 粒子效果調整
        if (portalParticles != null)
        {
            var emission = portalParticles.emission;
            float targetRate = isPlayerNearby ? 50f : 20f;
            if (isOnCooldown) targetRate *= 0.3f; // 冷卻時降低粒子數量
            emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, targetRate, Time.deltaTime * 2f);
        }
    }
    
    /// <summary>
    /// 更新冷卻狀態
    /// </summary>
    private void UpdateCooldown()
    {
        if (isOnCooldown && Time.time - lastTeleportTime >= cooldownTime)
        {
            isOnCooldown = false;
            if (debugMode)
                Debug.Log($"{portalName}: 冷卻時間結束");
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
        
        if (triggerType == PortalTriggerType.OnTriggerEnter && !requiresInteraction && !isOnCooldown)
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
        
        if (triggerType == PortalTriggerType.OnTriggerStay && !requiresInteraction && !isOnCooldown)
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
        if (requiresInteraction && !isOnCooldown)
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
    /// 激活傳送點
    /// </summary>
    public void ActivatePortal()
    {
        if (isActivated || isOnCooldown) return;
        
        // 檢查傳送次數限制
        if (maxTeleportCount > 0 && teleportCount >= maxTeleportCount)
        {
            if (debugMode)
                Debug.Log($"{portalName}: 已達到最大傳送次數限制 ({maxTeleportCount})");
            return;
        }
        
        if (currentPlayer == null)
        {
            Debug.LogError($"{portalName}: 無法激活，找不到玩家！");
            return;
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 開始激活，目標位置: {GetTargetPosition()}");
        
        isActivated = true;
        
        // 隱藏互動提示
        HideInteractionPrompt();
        
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
        
        // 顯示傳送特效
        if (teleportEffect != null)
        {
            teleportEffect.SetActive(true);
        }
        
        // 等待激活延遲
        yield return new WaitForSeconds(activationDelay);
        
        // 執行傳送
        TeleportPlayer();
        
        // 等待傳送延遲
        yield return new WaitForSeconds(teleportDelay);
        
        // 切換相機
        SwitchCamera();
        
        // 隱藏傳送特效
        if (teleportEffect != null)
        {
            yield return new WaitForSeconds(effectDuration);
            teleportEffect.SetActive(false);
        }
        
        // 完成傳送
        CompleteTeleport();
    }
    
    /// <summary>
    /// 傳送玩家
    /// </summary>
    private void TeleportPlayer()
    {
        if (currentPlayer == null) return;
        
        Vector3 targetPos = GetTargetPosition();
        Vector3 targetRot = GetTargetRotation();
        
        // 傳送玩家位置
        currentPlayer.transform.position = targetPos;
        
        // 設定玩家旋轉（如果不保持原本旋轉）
        if (!maintainPlayerRotation)
        {
            currentPlayer.transform.eulerAngles = targetRot;
        }
        
        // 如果玩家有CharacterController，需要特殊處理
        CharacterController characterController = currentPlayer.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            currentPlayer.transform.position = targetPos;
            characterController.enabled = true;
        }
        
        // 如果玩家有Rigidbody，重置速度
        Rigidbody playerRigidbody = currentPlayer.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 玩家已傳送到 {targetPos}，旋轉: {targetRot}");
    }
    
    /// <summary>
    /// 切換相機
    /// </summary>
    private void SwitchCamera()
    {
        if (cameraController == null || !changeCameraMode)
        {
            return;
        }

        // 使用相機控制器切換相機
        cameraController.SetCameraMode(cameraMode);
        if (cameraTarget != null)
        {
            cameraController.SetPlayerTarget(cameraTarget);
        }

        if (debugMode)
            Debug.Log($"{portalName}: 使用相機控制器切換到 {cameraMode} 模式");
    }
    
    /// <summary>
    /// 完成傳送
    /// </summary>
    private void CompleteTeleport()
    {
        isActivated = false;
        teleportCount++;
        lastTeleportTime = Time.time;
        
        // 設定冷卻狀態
        if (cooldownTime > 0)
        {
            isOnCooldown = true;
        }
        
        // 如果不能多次傳送，禁用傳送點
        if (!canTeleportMultipleTimes)
        {
            gameObject.SetActive(false);
        }
        
        if (debugMode)
            Debug.Log($"{portalName}: 傳送完成，傳送次數: {teleportCount}");
    }
    
    /// <summary>
    /// 獲取目標位置
    /// </summary>
    private Vector3 GetTargetPosition()
    {
        if (useTargetTransform && targetTransform != null)
        {
            return targetTransform.position;
        }
        else
        {
            return targetPosition;
        }
    }
    
    /// <summary>
    /// 獲取目標旋轉
    /// </summary>
    private Vector3 GetTargetRotation()
    {
        if (useTargetTransform && targetTransform != null)
        {
            return targetTransform.eulerAngles;
        }
        else
        {
            return targetRotation;
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
            Debug.Log($"{portalName}: 檢測到玩家 {obj.name}, Tag: {hasCorrectTag}, Layer: {hasCorrectLayer}");
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
        
        // 這裡可以顯示UI提示，例如 "按 E 鍵傳送"
        if (debugMode)
            Debug.Log($"{portalName}: 顯示互動提示 - 按 {interactionKey} 鍵傳送到 {GetTargetPosition()}");
    }
    
    /// <summary>
    /// 隱藏互動提示
    /// </summary>
    private void HideInteractionPrompt()
    {
        if (!showingInteractionPrompt) return;
        
        showingInteractionPrompt = false;
        
        if (debugMode)
            Debug.Log($"{portalName}: 隱藏互動提示");
    }
    
    /// <summary>
    /// 設置目標位置
    /// </summary>
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
        
        if (debugMode)
            Debug.Log($"{portalName}: 設置目標位置為 {position}");
    }
    
    /// <summary>
    /// 設置目標旋轉
    /// </summary>
    public void SetTargetRotation(Vector3 rotation)
    {
        targetRotation = rotation;
        
        if (debugMode)
            Debug.Log($"{portalName}: 設置目標旋轉為 {rotation}");
    }
    
    /// <summary>
    /// 設置目標Transform
    /// </summary>
    public void SetTargetTransform(Transform target)
    {
        targetTransform = target;
        useTargetTransform = target != null;
        
        if (debugMode)
            Debug.Log($"{portalName}: 設置目標Transform為 {(target != null ? target.name : "null")}");
    }
    
    /// <summary>
    /// 重置傳送點狀態
    /// </summary>
    public void ResetPortal()
    {
        isActivated = false;
        isPlayerNearby = false;
        isOnCooldown = false;
        currentPlayer = null;
        teleportCount = 0;
        
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
        
        HideInteractionPrompt();
        
        if (debugMode)
            Debug.Log($"{portalName}: 傳送點狀態已重置");
    }
    
    /// <summary>
    /// 在Scene視圖中顯示傳送點範圍
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // 顯示觸發範圍
        Gizmos.color = isPlayerNearby ? Color.green : gizmoColor;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        
        // 顯示接近檢測範圍（如果使用接近觸發）
        if (triggerType == PortalTriggerType.OnProximity)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 3f);
        }
        
        // 顯示目標位置
        Vector3 targetPos = GetTargetPosition();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPos, 0.5f);
        
        // 顯示傳送方向箭頭
        Gizmos.color = Color.magenta;
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, direction * 2f);
        }
        
        // 顯示傳送點名稱和狀態
        if (!string.IsNullOrEmpty(portalName))
        {
            Vector3 labelPosition = transform.position + Vector3.up * 2f;
            string statusText = isOnCooldown ? " (冷卻中)" : (isPlayerNearby ? " (玩家附近)" : "");
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, $"{portalName}\n→ {GetTargetPosition()}{statusText}");
            #endif
        }
    }
    
    /// <summary>
    /// 在Inspector中顯示傳送點詳細資訊
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // 顯示詳細的觸發範圍
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        
        // 顯示目標位置的詳細資訊
        Vector3 targetPos = GetTargetPosition();
        Vector3 targetRot = GetTargetRotation();
        
        // 目標位置
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(targetPos, 0.3f);
        
        // 目標旋轉方向
        Gizmos.color = Color.blue;
        Vector3 forward = Quaternion.Euler(targetRot) * Vector3.forward;
        Gizmos.DrawRay(targetPos, forward * 2f);
        
        // 顯示相機位置（如果設定了相機目標）
        if (cameraTarget != null)
        {
            Vector3 camPos = cameraTarget.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(camPos, Vector3.one * 0.5f);
            
            // 顯示相機到目標的視線
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(camPos, targetPos);
        }
        
        // 顯示傳送點名稱和狀態
        if (!string.IsNullOrEmpty(portalName))
        {
            Vector3 labelPosition = transform.position + Vector3.up * 2f;
            string statusText = isOnCooldown ? " (冷卻中)" : (isPlayerNearby ? " (玩家附近)" : "");
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, $"{portalName}\n→ {GetTargetPosition()}{statusText}");
            #endif
        }
    }
}
