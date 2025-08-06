using UnityEngine;

/// <summary>
/// 場景內傳送點 - 極簡版本，玩家進入就傳送
/// </summary>
public class InScenePortal : MonoBehaviour
{
    [Header("傳送點設定")]
    [SerializeField] private string portalName = "場景內傳送點";
    
    [Header("傳送位置設定")]
    [SerializeField] private Vector3 targetPosition = Vector3.zero;
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    [SerializeField] private Transform targetTransform; // 可選的目標Transform
    [SerializeField] private bool useTargetTransform = false;
    [SerializeField] private bool maintainPlayerRotation = false; // 是否保持玩家原本的旋轉
    
    [Header("相機設定")]
    [SerializeField] private Camera targetCamera; // 傳送後要切換的相機
    
    [Header("玩家檢測")]
    [SerializeField] private LayerMask playerLayerMask = 1;
    [SerializeField] private string playerTag = "Player";
    
    [Header("除錯設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    public string PortalName => portalName;
    public Vector3 TargetPosition => GetTargetPosition();

    private void Start()
    {
        InitializePortal();
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

        // 驗證設定
        if (useTargetTransform && targetTransform == null)
        {
            Debug.LogWarning($"{portalName}: 啟用了使用目標Transform但未設置targetTransform！");
        }

        if (debugMode)
            Debug.Log($"{portalName} 初始化完成，目標位置: {GetTargetPosition()}");
    }

    /// <summary>
    /// 玩家進入觸發器
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidPlayer(other.gameObject)) return;

        if (debugMode)
            Debug.Log($"{portalName}: 玩家 {other.gameObject.name} 進入，開始傳送");

        TeleportPlayer(other.gameObject);
    }

    /// <summary>
    /// 傳送玩家
    /// </summary>
    private void TeleportPlayer(GameObject player)
    {
        Vector3 targetPos = GetTargetPosition();
        Vector3 targetRot = GetTargetRotation();

        // 傳送玩家位置
        player.transform.position = targetPos;

        // 設定玩家旋轉（如果不保持原本旋轉）
        if (!maintainPlayerRotation)
        {
            player.transform.eulerAngles = targetRot;
        }

        // 如果玩家有CharacterController，需要特殊處理
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            player.transform.position = targetPos;
            characterController.enabled = true;
        }

        // 如果玩家有Rigidbody，重置速度
        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // 切換相機
        SwitchCamera();

        if (debugMode)
            Debug.Log($"{portalName}: 玩家已傳送到 {targetPos}，旋轉: {targetRot}");
    }

    /// <summary>
    /// 切換相機
    /// </summary>
    private void SwitchCamera()
    {
        if (targetCamera == null) return;

        // 關閉當前主相機
        Camera currentMainCamera = Camera.main;
        if (currentMainCamera != null && currentMainCamera != targetCamera)
        {
            currentMainCamera.gameObject.SetActive(false);
        }

        // 激活目標相機
        targetCamera.gameObject.SetActive(true);

        if (debugMode)
            Debug.Log($"{portalName}: 切換到相機 {targetCamera.name}");
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
    /// 在Scene視圖中顯示傳送點範圍
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 顯示觸發範圍
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);

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

        // 顯示傳送點名稱
        if (!string.IsNullOrEmpty(portalName))
        {
            Vector3 labelPosition = transform.position + Vector3.up * 2f;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, $"{portalName}\n→ {GetTargetPosition()}");
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
    }
}