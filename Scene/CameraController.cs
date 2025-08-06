using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 相機控制器 - 專注於自身路徑移動和目標管理
/// 直接掛載在相機物件上，不需要指定targetCamera
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("相機設定")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 2f;
    
    [Header("相機模式")]
    [SerializeField] private CameraMode currentMode = CameraMode.FollowPlayer;
    
    public enum CameraMode
    {
        Fixed,          // 固定模式：鏡頭角度和位置都固定
        FollowPlayer    // 跟隨模式：鏡頭對準玩家，在路徑上移動
    }
    
    [Header("路徑系統")]
    [SerializeField] private bool useVectorWaypoints = false; // 是否使用Vector3路徑點
    [SerializeField] private List<Vector3> vectorWaypoints = new List<Vector3>(); // Vector3路徑點
    [SerializeField] private List<Transform> waypoints = new List<Transform>(); // Transform路徑點
    [SerializeField] private bool loopPath = true;
    
    [Header("跟隨設定")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float minDistanceToPlayer = 5f;
    [SerializeField] private float maxDistanceToPlayer = 15f;
    [SerializeField] private float heightOffset = 2f;
    
    [Header("視野設定")]
    [SerializeField] private float fieldOfView = 60f;
    [SerializeField] private float viewDistance = 20f;
    
    // 路徑狀態
    private int currentWaypointIndex = 0;
    private Vector3 currentPathPosition;
    
    // 目標狀態
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    private Camera thisCamera;
    
    private void Awake()
    {
        thisCamera = GetComponent<Camera>();
        if (thisCamera == null)
        {
            Debug.LogError("CameraController必須掛載在帶有Camera組件的物件上！");
            return;
        }
        
        // 根據設定初始化路徑
        InitializeWaypoints();
    }

    /// <summary>
    /// 初始化路徑點
    /// </summary>
    private void InitializeWaypoints()
    {
        if (useVectorWaypoints)
        {
            // 使用Vector3路徑點，自動建立Transform
            CreateWaypointsFromVectors();
        }
        
        // 初始化路徑位置
        if (waypoints.Count > 0)
        {
            currentPathPosition = waypoints[0].position;
            transform.position = currentPathPosition;
        }
    }

    /// <summary>
    /// 從Vector3列表建立Transform路徑點
    /// </summary>
    private void CreateWaypointsFromVectors()
    {
        if (vectorWaypoints.Count == 0) return;
        
        // 清除現有的Transform路徑點
        waypoints.Clear();
        
        // 為每個Vector3建立一個GameObject作為路徑點
        for (int i = 0; i < vectorWaypoints.Count; i++)
        {
            GameObject waypointObj = new GameObject($"Waypoint_{i}");
            waypointObj.transform.position = vectorWaypoints[i];
            waypointObj.transform.SetParent(transform);
            waypoints.Add(waypointObj.transform);
        }
    }
    
    private void Update()
    {
        UpdateCameraMode();
        UpdatePathMovement();
        UpdateCameraTransform();
    }
    
    /// <summary>
    /// 初始化相機控制器
    /// </summary>
    public void Initialize(Transform player)
    {
        playerTarget = player;
    }
    
    /// <summary>
    /// 設定相機模式
    /// </summary>
    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
    }
    
    /// <summary>
    /// 設定玩家目標
    /// </summary>
    public void SetPlayerTarget(Transform player)
    {
        playerTarget = player;
    }
    
    /// <summary>
    /// 添加路徑點
    /// </summary>
    public void AddWaypoint(Transform waypoint)
    {
        if (!waypoints.Contains(waypoint))
        {
            waypoints.Add(waypoint);
        }
    }
    
    /// <summary>
    /// 移除路徑點
    /// </summary>
    public void RemoveWaypoint(Transform waypoint)
    {
        waypoints.Remove(waypoint);
    }
    
    /// <summary>
    /// 清除所有路徑點
    /// </summary>
    public void ClearWaypoints()
    {
        waypoints.Clear();
    }
    
    /// <summary>
    /// 更新相機模式邏輯
    /// </summary>
    private void UpdateCameraMode()
    {
        switch (currentMode)
        {
            case CameraMode.Fixed:
                HandleFixedMode();
                break;
                
            case CameraMode.FollowPlayer:
                HandleFollowMode();
                break;
        }
    }
    
    /// <summary>
    /// 處理固定模式
    /// </summary>
    private void HandleFixedMode()
    {
        if (waypoints.Count > 0 && currentWaypointIndex < waypoints.Count)
        {
            targetPosition = waypoints[currentWaypointIndex].position;
            targetRotation = waypoints[currentWaypointIndex].rotation;
        }
    }
    
    /// <summary>
    /// 處理跟隨模式
    /// </summary>
    private void HandleFollowMode()
    {
        if (playerTarget == null || waypoints.Count < 2)
        {
            HandleFixedMode();
            return;
        }
        
        // 計算玩家在路徑上的投影位置
        Vector3 closestPoint = GetClosestPointOnPath(playerTarget.position);
        
        // 確保相機在路徑上移動 - 使用路徑上的最近點
        targetPosition = closestPoint;
        
        // 計算瞄準點，應用 heightOffset
        Vector3 lookAtPoint = playerTarget.position + Vector3.up * heightOffset;
        
        // 讓相機始終對準玩家
        Vector3 lookDirection = lookAtPoint - targetPosition;
        if (lookDirection != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(lookDirection);
        }
        
        // 檢查玩家是否在視野內
        if (!IsPlayerInView())
        {
            AdjustCameraForPlayer();
        }
    }
    
    /// <summary>
    /// 更新路徑移動
    /// </summary>
    private void UpdatePathMovement()
    {
        if (waypoints.Count < 2) return;
        
        // 根據玩家位置更新路徑位置
        if (currentMode == CameraMode.FollowPlayer && playerTarget != null)
        {
            // 直接計算玩家在路徑上的投影位置
            Vector3 closestPoint = GetClosestPointOnPath(playerTarget.position);
            currentPathPosition = closestPoint;
            
            // 找到最近的路徑點索引（用於其他邏輯）
            float closestDistance = float.MaxValue;
            for (int i = 0; i < waypoints.Count; i++)
            {
                float distance = Vector3.Distance(waypoints[i].position, closestPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    currentWaypointIndex = i;
                }
            }
        }
    }
    
    /// <summary>
    /// 更新相機變換
    /// </summary>
    private void UpdateCameraTransform()
    {
        // 直接使用路徑上的最近點作為目標位置
        targetPosition = currentPathPosition;
        
        // 平滑移動到目標位置
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        
        // 平滑旋轉到目標角度
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// 獲取路徑上最接近的點
    /// </summary>
    private Vector3 GetClosestPointOnPath(Vector3 target)
    {
        if (waypoints.Count < 2) return transform.position;
        
        Vector3 closestPoint = Vector3.zero;
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 segmentStart = waypoints[i].position;
            Vector3 segmentEnd = waypoints[i + 1].position;
            
            Vector3 closestOnSegment = GetClosestPointOnSegment(target, segmentStart, segmentEnd);
            float distance = Vector3.Distance(target, closestOnSegment);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = closestOnSegment;
            }
        }
        
        // 處理循環路徑的最後一段
        if (loopPath && waypoints.Count > 2)
        {
            Vector3 segmentStart = waypoints[waypoints.Count - 1].position;
            Vector3 segmentEnd = waypoints[0].position;
            
            Vector3 closestOnSegment = GetClosestPointOnSegment(target, segmentStart, segmentEnd);
            float distance = Vector3.Distance(target, closestOnSegment);
            
            if (distance < minDistance)
            {
                closestPoint = closestOnSegment;
            }
        }
        
        return closestPoint;
    }
    
    /// <summary>
    /// 獲取線段上最接近的點
    /// </summary>
    private Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float segmentLength = segment.magnitude;
        
        if (segmentLength < 0.001f) return start;
        
        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / (segmentLength * segmentLength));
        return start + t * segment;
    }
    
    /// <summary>
    /// 檢查玩家是否在視野內
    /// </summary>
    private bool IsPlayerInView()
    {
        if (playerTarget == null) return true;
        
        Vector3 directionToPlayer = playerTarget.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        
        return angle < fieldOfView * 0.5f && directionToPlayer.magnitude < viewDistance;
    }
    
    /// <summary>
    /// 調整相機位置確保玩家在視野內
    /// </summary>
    private void AdjustCameraForPlayer()
    {
        if (playerTarget == null) return;
        
        // 計算從當前位置到玩家的方向
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        
        // 確保距離在合理範圍內
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer < minDistanceToPlayer)
        {
            // 如果太近，沿著路徑向後移動
            targetPosition = GetAdjustedPositionAwayFromPlayer();
        }
        else if (distanceToPlayer > maxDistanceToPlayer)
        {
            // 如果太遠，沿著路徑向前移動
            targetPosition = GetAdjustedPositionTowardsPlayer();
        }
    }
    
    /// <summary>
    /// 獲取調整後的位置（遠離玩家）
    /// </summary>
    private Vector3 GetAdjustedPositionAwayFromPlayer()
    {
        if (waypoints.Count < 2) return transform.position;
        
        // 找到下一個路徑點
        int nextIndex = (currentWaypointIndex + 1) % waypoints.Count;
        if (!loopPath && nextIndex == 0)
        {
            nextIndex = currentWaypointIndex;
        }
        
        return waypoints[nextIndex].position;
    }
    
    /// <summary>
    /// 獲取調整後的位置（靠近玩家）
    /// </summary>
    private Vector3 GetAdjustedPositionTowardsPlayer()
    {
        if (waypoints.Count < 2) return transform.position;
        
        // 找到前一個路徑點
        int prevIndex = currentWaypointIndex - 1;
        if (prevIndex < 0)
        {
            prevIndex = loopPath ? waypoints.Count - 1 : 0;
        }
        
        return waypoints[prevIndex].position;
    }
    
    /// <summary>
    /// 設置路徑點
    /// </summary>
    public void SetWaypoints(List<Transform> newWaypoints)
    {
        waypoints = new List<Transform>(newWaypoints);
        if (waypoints.Count > 0)
        {
            currentWaypointIndex = 0;
            currentPathPosition = waypoints[0].position;
        }
    }
    
    /// <summary>
    /// 獲取路徑點數量
    /// </summary>
    public int GetWaypointCount()
    {
        return waypoints.Count;
    }
    
    /// <summary>
    /// 獲取當前路徑位置
    /// </summary>
    public Vector3 GetCurrentPathPosition()
    {
        return currentPathPosition;
    }
    
    /// <summary>
    /// 獲取當前路徑進度
    /// </summary>
    public float GetPathProgress()
    {
        if (waypoints.Count == 0) return 0f;
        return (float)currentWaypointIndex / waypoints.Count;
    }
    
    /// <summary>
    /// 在編輯器中顯示路徑
    /// </summary>
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        
        // 繪製路徑線
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
        
        // 繪製循環路徑
        if (loopPath && waypoints.Count > 2 && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
        }
        
        // 繪製路徑點
        Gizmos.color = Color.green;
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawWireSphere(waypoint.position, 0.5f);
            }
        }
        
        // 繪製當前位置
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // 繪製視野範圍
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            
            float halfFOV = fieldOfView * 0.5f * Mathf.Deg2Rad;
            float viewHeight = Mathf.Tan(halfFOV) * viewDistance;
            float viewWidth = viewHeight * cam.aspect;
            
            Vector3 topLeft = transform.position + forward * viewDistance - right * viewWidth + up * viewHeight;
            Vector3 topRight = transform.position + forward * viewDistance + right * viewWidth + up * viewHeight;
            Vector3 bottomLeft = transform.position + forward * viewDistance - right * viewWidth - up * viewHeight;
            Vector3 bottomRight = transform.position + forward * viewDistance + right * viewWidth - up * viewHeight;
            
            Gizmos.DrawLine(transform.position, topLeft);
            Gizmos.DrawLine(transform.position, topRight);
            Gizmos.DrawLine(transform.position, bottomLeft);
            Gizmos.DrawLine(transform.position, bottomRight);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
    }

    /// <summary>
    /// Editor-Only: 刷新相機位置和路徑
    /// </summary>
    [ContextMenu("Refresh Camera")]
    public void RefreshCamera()
    {
        #if UNITY_EDITOR
        // 重新初始化路徑
        InitializeWaypoints();
        
        // 重置相機到第一個路徑點
        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0].position;
            transform.rotation = waypoints[0].rotation;
            currentWaypointIndex = 0;
            currentPathPosition = waypoints[0].position;
            Debug.Log("相機已刷新到第一個路徑點");
        }
        else
        {
            Debug.LogWarning("沒有可用的路徑點，無法刷新相機");
        }
        #else
        Debug.LogWarning("RefreshCamera 只能在編輯模式下使用！");
        #endif
    }
}
