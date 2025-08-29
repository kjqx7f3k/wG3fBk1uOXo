using UnityEngine;

/// <summary>
/// 頭部控制組件 - 讓生物頭部看向當前控制的玩家
/// 可掛載到任何GameObject上，獨立運作
/// 支援 IK 和 LateUpdate 兩種控制模式
/// </summary>
public class HeadControl : MonoBehaviour
{
    /// <summary>
    /// 頭部看向控制模式
    /// </summary>
    public enum LookAtMode
    {
        IK,           // 使用 Unity IK 系統（需要 Animator 和 IK Pass）
        LateUpdate    // 使用 LateUpdate 直接操作 Transform 旋轉（權重基礎插值）
    }

    [Header("控制模式")]
    [SerializeField] private LookAtMode lookAtMode = LookAtMode.IK;      // 看向控制模式
    [Header("頭部看向設定")]
    [SerializeField] private bool enableHeadLook = false;              // 啟用/停用看向功能
    [SerializeField] private bool enableDebugLogs = true;              // 啟用調試日誌
    [SerializeField] private Transform headTransform;                   // 頭部Transform
    [SerializeField] private float lookAtRadius = 10f;                 // 開始看向的半徑範圍
    [SerializeField] private float maxHeadAngle = 90f;                 // 頭部最大轉動角度（與身體朝向的夾角）
    [SerializeField] private float headLookSpeed = 2f;                 // 頭部轉動速度
    [SerializeField] private float lookAtUpdateFrequency = 10f;        // 更新頻率（每秒）
    [SerializeField, Range(0f, 1f)] private float lookWeight = 1f;     // 看向權重（權重模式使用）
    
    [Header("頭部朝向修正")]
    [SerializeField] private Vector3 headForwardOffset = Vector3.zero; // 頭部朝向偏移角度（歐拉角）
    
    [Header("玩家目標設定")]
    [SerializeField] private bool autoDetectPlayerHead = true;          // 自動偵測玩家頭部
    [SerializeField] private float lookPlayerHeightOffset = 1.7f;      // 玩家高度偏移（當未偵測到頭部時使用）

    // 私有變數
    private Quaternion originalHeadRotation;                           // 頭部原始旋轉（本地座標）
    private Vector3 currentLookTarget;                                  // 當前看向目標
    private float lastLookAtUpdate = 0f;                              // 上次更新時間
    private bool isLookingAtPlayer = false;                           // 是否正在看向玩家
    private float lookAtUpdateInterval;                               // 更新間隔時間
    
    // IK 系統相關變數
    private Animator animator;                                         // Animator 組件
    private float currentLookWeight = 0f;                             // 當前 IK 權重
    private Vector3 ikLookTarget;                                     // IK 看向目標位置
    
    // LateUpdate 模式相關變數
    private bool isLateUpdateInitialized = false;                     // LateUpdate 模式初始化狀態
    private Quaternion originalHeadWorldRotation;                     // 頭部原始世界旋轉（用於 LateUpdate）

    private void Awake()
    {
        // 計算更新間隔
        UpdateLookAtInterval();
        
        // 根據模式初始化
        if (lookAtMode == LookAtMode.IK)
        {
            InitializeIKMode();
        }
        
        // 初始化頭部 Transform
        InitializeHeadTransform();
    }
    
    /// <summary>
    /// 初始化 IK 模式
    /// </summary>
    private void InitializeIKMode()
    {
        // 獲取 Animator 組件
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[HeadControl] {name}: IK 模式需要 Animator 組件，請添加或改用 LateUpdate 模式");
        }
        else
        {
            // 檢查 Animator Controller 是否存在
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"[HeadControl] {name}: Animator Controller 未設置，IK 功能將無法工作");
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"[HeadControl] {name}: IK 模式初始化成功");
            }
        }
    }
    
    /// <summary>
    /// 初始化頭部 Transform
    /// </summary>
    private void InitializeHeadTransform()
    {
        // 如果沒有指定頭部Transform，嘗試自動找到
        if (headTransform == null)
        {
            headTransform = FindHeadTransform();
            if (headTransform != null && enableDebugLogs)
            {
                Debug.Log($"[HeadControl] {name}: 自動找到頭部 Transform: {headTransform.name}");
            }
        }
        
        // 記錄頭部原始旋轉
        if (headTransform != null)
        {
            originalHeadRotation = headTransform.localRotation;
            
            if (lookAtMode == LookAtMode.LateUpdate)
            {
                // LateUpdate 模式需要保存世界旋轉
                originalHeadWorldRotation = headTransform.rotation;
                isLateUpdateInitialized = true;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[HeadControl] {name}: 頭部初始化成功 ({lookAtMode} 模式)");
            }
        }
        else
        {
            Debug.LogError($"[HeadControl] {name}: 找不到頭部Transform，HeadControl組件將無法正常工作");
        }
    }

    private void OnValidate()
    {
        // 在Inspector中修改參數時重新計算更新間隔
        UpdateLookAtInterval();
        
        // 驗證參數合理性
        ValidateParameters();
    }

    /// <summary>
    /// 更新看向邏輯的時間間隔
    /// </summary>
    private void UpdateLookAtInterval()
    {
        lookAtUpdateInterval = 1f / Mathf.Max(lookAtUpdateFrequency, 0.1f);
    }

    /// <summary>
    /// 驗證參數合理性並自動修正
    /// </summary>
    private void ValidateParameters()
    {
        // 驗證距離參數
        if (lookAtRadius < 0f)
        {
            Debug.LogWarning($"[HeadControl] {name}: lookAtRadius 不能為負數，已自動修正為 0");
            lookAtRadius = 0f;
        }
        else if (lookAtRadius > 50f)
        {
            Debug.LogWarning($"[HeadControl] {name}: lookAtRadius 過大 ({lookAtRadius})，建議設為 5-10 範圍");
        }
        
        // 驗證角度參數
        if (maxHeadAngle < 0f)
        {
            Debug.LogWarning($"[HeadControl] {name}: maxHeadAngle 不能為負數，已自動修正為 0");
            maxHeadAngle = 0f;
        }
        else if (maxHeadAngle > 180f)
        {
            Debug.LogWarning($"[HeadControl] {name}: maxHeadAngle 過大 ({maxHeadAngle})，已自動修正為 180");
            maxHeadAngle = 180f;
        }
        
        // 驗證速度參數
        if (headLookSpeed < 0f)
        {
            Debug.LogWarning($"[HeadControl] {name}: headLookSpeed 不能為負數，已自動修正為 0");
            headLookSpeed = 0f;
        }
        
        // 驗證更新頻率
        if (lookAtUpdateFrequency <= 0f)
        {
            Debug.LogWarning($"[HeadControl] {name}: lookAtUpdateFrequency 必須大於 0，已自動修正為 1");
            lookAtUpdateFrequency = 1f;
        }
        else if (lookAtUpdateFrequency > 60f)
        {
            Debug.LogWarning($"[HeadControl] {name}: lookAtUpdateFrequency 過高 ({lookAtUpdateFrequency})，可能影響效能");
        }
        
        // 驗證高度偏移
        if (lookPlayerHeightOffset < 0f)
        {
            Debug.LogWarning($"[HeadControl] {name}: lookPlayerHeightOffset 為負數 ({lookPlayerHeightOffset})，可能導致看向地面");
        }
        else if (lookPlayerHeightOffset > 5f)
        {
            Debug.LogWarning($"[HeadControl] {name}: lookPlayerHeightOffset 過高 ({lookPlayerHeightOffset})，建議設為 1.5-2.0 範圍");
        }
        
        // 驗證權重參數
        lookWeight = Mathf.Clamp01(lookWeight);
    }

    private void Update()
    {
        // 只有 IK 模式才在 Update 中處理
        if (lookAtMode != LookAtMode.IK) return;
        
        if (!enableHeadLook)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0) // 每2秒輸出一次
                Debug.Log($"[HeadControl] {name}: enableHeadLook 已停用");
            return;
        }
        
        if (headTransform == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.LogWarning($"[HeadControl] {name}: headTransform 為 null");
            return;
        }
        
        if (animator == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.LogWarning($"[HeadControl] {name}: animator 為 null");
            return;
        }

        // 根據更新頻率控制計算頻率
        if (Time.time - lastLookAtUpdate >= lookAtUpdateInterval)
        {
            UpdateHeadLook();
            lastLookAtUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// LateUpdate - 用於 LateUpdate 模式的頭部控制
    /// </summary>
    private void LateUpdate()
    {
        // 只有 LateUpdate 模式才處理
        if (lookAtMode != LookAtMode.LateUpdate) return;
        
        UpdateHeadLookDirect();
    }

    /// <summary>
    /// 自動尋找頭部Transform
    /// </summary>
    /// <returns>找到的頭部Transform，如果沒找到則返回null</returns>
    private Transform FindHeadTransform()
    {
        if (enableDebugLogs)
            Debug.Log($"[HeadControl] {name}: 正在搜尋頭部 Transform...");
        
        // 首先尋找名為 "Head" 的直接子物件
        Transform head = transform.Find("Head");
        if (head != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HeadControl] {name}: 找到直接子物件 'Head': {head.name}");
            return head;
        }

        // 如果沒找到，遞迴搜尋所有子物件中包含 "head" 的Transform
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        if (enableDebugLogs)
            Debug.Log($"[HeadControl] {name}: 總共找到 {allChildren.Length} 個子 Transform");
            
        foreach (Transform child in allChildren)
        {
            if (child != transform && child.name.ToLower().Contains("head"))
            {
                if (enableDebugLogs)
                    Debug.Log($"[HeadControl] {name}: 找到包含 'head' 的子物件: {child.name}");
                return child;
            }
        }

        // 如果還是沒找到，嘗試找包含 "Head" 的子物件
        foreach (Transform child in allChildren)
        {
            if (child != transform && child.name.Contains("Head"))
            {
                if (enableDebugLogs)
                    Debug.Log($"[HeadControl] {name}: 找到包含 'Head' 的子物件: {child.name}");
                return child;
            }
        }

        // 都沒找到則返回null
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[HeadControl] {name}: 未找到任何頭部 Transform，嘗試這些名稱: {string.Join(", ", System.Array.ConvertAll(allChildren, t => t.name))}");
        }
        return null;
    }

    /// <summary>
    /// 更新頭部看向邏輯（IK 模式）
    /// </summary>
    private void UpdateHeadLook()
    {
        // 獲取當前控制的玩家
        Transform playerTransform = GetCurrentPlayerTransform();
        if (playerTransform == null)
        {
            // 沒有玩家時，停止看向
            if (isLookingAtPlayer)
            {
                isLookingAtPlayer = false;
                if (enableDebugLogs)
                    Debug.Log($"[HeadControl] {name}: 停止看向，沒有找到玩家");
            }
            return;
        }

        // 計算與玩家的距離
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (enableDebugLogs && Time.frameCount % 60 == 0) // 每秒輸出一次
        {
            Debug.Log($"[HeadControl] {name}: 玩家距離 = {distanceToPlayer:F2}, 看向半徑 = {lookAtRadius:F2}");
        }
        
        if (distanceToPlayer <= lookAtRadius)
        {
            // 玩家在範圍內，開始看向玩家（使用新的目標位置計算）
            Vector3 targetLookPosition = GetPlayerLookTarget(playerTransform);
            
            // 檢查角度限制
            Vector3 directionToTarget = (targetLookPosition - headTransform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            if (IsAngleWithinLimit(directionToTarget))
            {
                ikLookTarget = targetLookPosition;
                if (enableDebugLogs && !isLookingAtPlayer)
                    Debug.Log($"[HeadControl] {name}: 開始看向玩家，角度 = {angleToTarget:F1}°");
            }
            else
            {
                // 超出角度限制，計算限制後的方向
                Vector3 limitedDirection = GetLimitedDirection(directionToTarget);
                ikLookTarget = headTransform.position + limitedDirection * Vector3.Distance(headTransform.position, targetLookPosition);
                if (enableDebugLogs && !isLookingAtPlayer)
                    Debug.Log($"[HeadControl] {name}: 角度受限，原始角度 = {angleToTarget:F1}°, 最大角度 = {maxHeadAngle:F1}°");
            }
            
            if (!isLookingAtPlayer)
            {
                isLookingAtPlayer = true;
            }
        }
        else
        {
            // 玩家超出範圍，停止看向
            if (isLookingAtPlayer)
            {
                isLookingAtPlayer = false;
                if (enableDebugLogs)
                    Debug.Log($"[HeadControl] {name}: 停止看向，玩家超出範圍");
            }
        }
        
        // 更新 IK 權重（平滑過渡）
        float targetWeight = isLookingAtPlayer ? 1f : 0f;
        float previousWeight = currentLookWeight;
        currentLookWeight = Mathf.Lerp(currentLookWeight, targetWeight, headLookSpeed * Time.deltaTime);
        
        if (enableDebugLogs && Mathf.Abs(currentLookWeight - previousWeight) > 0.01f && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[HeadControl] {name}: IK權重 = {currentLookWeight:F3}, 目標權重 = {targetWeight:F3}");
        }
    }

    /// <summary>
    /// 獲取當前控制的玩家Transform
    /// </summary>
    /// <returns>玩家Transform，如果沒有則返回null</returns>
    private Transform GetCurrentPlayerTransform()
    {
        // 通過CreatureController獲取當前控制的生物
        if (CreatureController.Instance == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.LogWarning($"[HeadControl] {name}: CreatureController.Instance 為 null");
            return null;
        }
        
        var currentCreature = CreatureController.Instance.GetCurrentControlledCreature();
        if (currentCreature == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.LogWarning($"[HeadControl] {name}: 當前沒有控制的生物");
            return null;
        }
        
        Transform playerTransform = currentCreature.GetTransform();
        if (playerTransform == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.LogWarning($"[HeadControl] {name}: 玩家Transform為null");
        }
        
        return playerTransform;
    }

    /// <summary>
    /// 尋找玩家的頭部Transform
    /// </summary>
    /// <param name="playerTransform">玩家的主Transform</param>
    /// <returns>找到的頭部Transform，如果沒找到則返回null</returns>
    private Transform FindPlayerHead(Transform playerTransform)
    {
        // 首先尋找名為 "Head" 的直接子物件
        Transform head = playerTransform.Find("Head");
        if (head != null)
        {
            return head;
        }

        // 如果沒找到，遞迴搜尋所有子物件中包含 "head" 的Transform（不區分大小寫）
        Transform[] allChildren = playerTransform.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child != playerTransform && child.name.ToLower().Contains("head"))
            {
                return child;
            }
        }

        // 如果還是沒找到，嘗試找包含 "Head" 的子物件（區分大小寫）
        foreach (Transform child in allChildren)
        {
            if (child != playerTransform && child.name.Contains("Head"))
            {
                return child;
            }
        }

        // 都沒找到則返回null
        return null;
    }

    /// <summary>
    /// 獲取玩家的看向目標位置
    /// </summary>
    /// <param name="playerTransform">玩家的主Transform</param>
    /// <returns>應該看向的目標位置</returns>
    private Vector3 GetPlayerLookTarget(Transform playerTransform)
    {
        if (autoDetectPlayerHead)
        {
            // 嘗試自動偵測玩家的頭部
            Transform playerHead = FindPlayerHead(playerTransform);
            if (playerHead != null)
            {
                return playerHead.position;
            }
        }
        
        // 使用高度偏移作為備用方案
        return playerTransform.position + Vector3.up * lookPlayerHeightOffset;
    }

    /// <summary>
    /// 更新頭部看向邏輯（LateUpdate 模式 - 權重基礎插值）
    /// </summary>
    private void UpdateHeadLookDirect()
    {
        if (!isLateUpdateInitialized || !enableHeadLook || headTransform == null)
        {
            // 如果功能停用，逐漸恢復原始旋轉
            if (isLateUpdateInitialized && headTransform != null && !enableHeadLook)
            {
                headTransform.rotation = Quaternion.Slerp(headTransform.rotation, originalHeadWorldRotation, 1f - lookWeight);
            }
            return;
        }

        // 獲取當前控制的玩家
        Transform playerTransform = GetCurrentPlayerTransform();
        if (playerTransform == null)
        {
            // 沒有目標時，恢復原始旋轉
            headTransform.rotation = originalHeadWorldRotation;
            
            if (isLookingAtPlayer)
            {
                isLookingAtPlayer = false;
                if (enableDebugLogs)
                    Debug.Log($"[HeadControl] {name}: LateUpdate 模式停止看向，沒有找到玩家");
            }
            return;
        }

        // 計算與玩家的距離
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (enableDebugLogs && Time.frameCount % 60 == 0) // 每秒輸出一次
        {
            Debug.Log($"[HeadControl] {name}: LateUpdate 模式 - 玩家距離 = {distanceToPlayer:F2}, 看向半徑 = {lookAtRadius:F2}");
        }
        
        if (distanceToPlayer <= lookAtRadius)
        {
            // 玩家在範圍內，計算看向目標
            Vector3 targetLookPosition = GetPlayerLookTarget(playerTransform);
            
            // 檢查角度限制
            Vector3 directionToTarget = (targetLookPosition - headTransform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            Vector3 finalDirection = directionToTarget;
            if (!IsAngleWithinLimit(directionToTarget))
            {
                // 超出角度限制，計算限制後的方向
                finalDirection = GetLimitedDirection(directionToTarget);
                if (enableDebugLogs && !isLookingAtPlayer)
                    Debug.Log($"[HeadControl] {name}: LateUpdate 模式角度受限，原始角度 = {angleToTarget:F1}°, 最大角度 = {maxHeadAngle:F1}°");
            }
            
            // 計算目標旋轉並應用頭部朝向偏移
            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            if (headForwardOffset != Vector3.zero)
            {
                targetRotation *= Quaternion.Euler(headForwardOffset);
            }
            
            // 權重基礎插值（來自 HeadControlDebug 的有效方法）
            headTransform.rotation = Quaternion.Slerp(originalHeadWorldRotation, targetRotation, lookWeight);
                
            if (!isLookingAtPlayer)
            {
                isLookingAtPlayer = true;
                if (enableDebugLogs)
                    Debug.Log($"[HeadControl] {name}: LateUpdate 模式開始看向玩家，角度 = {angleToTarget:F1}°");
            }
        }
        else
        {
            // 玩家超出範圍，恢復原始旋轉
            headTransform.rotation = originalHeadWorldRotation;
            
            if (isLookingAtPlayer)
            {
                isLookingAtPlayer = false;
                if (enableDebugLogs)
                    Debug.Log($"[HeadControl] {name}: LateUpdate 模式停止看向，玩家超出範圍");
            }
        }
    }

    /// <summary>
    /// OnAnimatorIK - Unity IK 系統回調（僅限 IK 模式）
    /// </summary>
    /// <param name="layerIndex">動畫層索引</param>
    void OnAnimatorIK(int layerIndex)
    {
        // 只有 IK 模式才處理
        if (lookAtMode != LookAtMode.IK) return;
        
        if (layerIndex != 0)
        {
            if (enableDebugLogs && Time.frameCount % 300 == 0) // 每5秒輸出一次
                Debug.Log($"[HeadControl] {name}: OnAnimatorIK 只處理層 0，當前層 {layerIndex}");
            return;
        }
        
        if (animator == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.LogWarning($"[HeadControl] {name}: OnAnimatorIK 中 animator 為 null");
            return;
        }
        
        // 檢查當前動畫狀態是否啟用了IK Pass
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (enableDebugLogs && Time.frameCount % 300 == 0)
        {
            Debug.Log($"[HeadControl] {name}: 當前動畫狀態 = {stateInfo.shortNameHash}, IK Pass 應該已啟用");
        }

        if (enableHeadLook && isLookingAtPlayer && currentLookWeight > 0.01f)
        {
            // 應用 IK 看向
            animator.SetLookAtWeight(currentLookWeight);
            animator.SetLookAtPosition(ikLookTarget);
            
            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[HeadControl] {name}: 應用 IK - 權重 = {currentLookWeight:F3}, 目標位置 = {ikLookTarget}");
            }
        }
        else
        {
            // 停用 IK 看向
            animator.SetLookAtWeight(0f);
            
            if (enableDebugLogs && Time.frameCount % 180 == 0)
            {
                string reason = !enableHeadLook ? "功能已停用" : 
                               !isLookingAtPlayer ? "不在看向狀態" : 
                               "權重過低";
                Debug.Log($"[HeadControl] {name}: 停用 IK - 原因: {reason}");
            }
        }
    }

    /// <summary>
    /// 檢查角度是否在限制範圍內
    /// </summary>
    /// <param name="direction">要檢查的方向</param>
    /// <returns>是否在限制範圍內</returns>
    private bool IsAngleWithinLimit(Vector3 direction)
    {
        // 身體的前方向（世界座標）
        Vector3 bodyForward = transform.forward;
        
        // 計算方向與身體前方向的夾角
        float angle = Vector3.Angle(bodyForward, direction);
        
        return angle <= maxHeadAngle;
    }

    /// <summary>
    /// 獲取限制後的方向
    /// </summary>
    /// <param name="originalDirection">原始方向</param>
    /// <returns>限制後的方向</returns>
    private Vector3 GetLimitedDirection(Vector3 originalDirection)
    {
        Vector3 bodyForward = transform.forward;
        
        // 使用 Vector3.RotateTowards 限制角度
        Vector3 limitedDirection = Vector3.RotateTowards(bodyForward, originalDirection, maxHeadAngle * Mathf.Deg2Rad, 0f);
        
        return limitedDirection.normalized;
    }

    /// <summary>
    /// 在Scene視圖中顯示調試資訊
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (headTransform == null) return;

        // 顯示看向半徑
        Gizmos.color = isLookingAtPlayer ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lookAtRadius);

        // 顯示身體朝向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * lookAtRadius * 0.8f);

        // 顯示角度限制範圍（扇形區域）
        if (maxHeadAngle > 0)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // 半透明黃色
            
            // 計算扇形的邊界射線
            Vector3 bodyForward = transform.forward;
            Vector3 bodyRight = transform.right;
            Vector3 bodyUp = transform.up;
            
            // 繪製扇形的多個射線來表示角度限制
            int rayCount = 12;
            for (int i = 0; i <= rayCount; i++)
            {
                float angle = (maxHeadAngle * 2f * i / rayCount) - maxHeadAngle;
                
                // 在水平面上的旋轉
                Vector3 horizontalDirection = Quaternion.AngleAxis(angle, bodyUp) * bodyForward;
                Gizmos.DrawRay(headTransform.position, horizontalDirection * lookAtRadius * 0.6f);
                
                // 在垂直面上的旋轉
                Vector3 verticalDirection = Quaternion.AngleAxis(angle, bodyRight) * bodyForward;
                Gizmos.DrawRay(headTransform.position, verticalDirection * lookAtRadius * 0.6f);
            }
        }

        // 顯示頭部當前看向方向
        if (isLookingAtPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(headTransform.position, headTransform.forward * lookAtRadius * 0.5f);
            
            // 顯示IK目標位置
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ikLookTarget, 0.15f);
            Gizmos.DrawLine(headTransform.position, ikLookTarget);
        }

        // 顯示到當前玩家的連線
        Transform playerTransform = GetCurrentPlayerTransform();
        if (playerTransform != null)
        {
            // 獲取實際的目標位置（可能是頭部或高度偏移）
            Vector3 actualTargetPosition = GetPlayerLookTarget(playerTransform);
            
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= lookAtRadius)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(headTransform.position, actualTargetPosition);
                
                // 顯示目標點
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(actualTargetPosition, 0.1f);
                
                // 顯示距離和角度信息
                Vector3 directionToTarget = (actualTargetPosition - headTransform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                    $"模式: {lookAtMode}" + (lookAtMode == LookAtMode.LateUpdate ? $" (權重: {lookWeight:F2})" : "") + 
                    $"\n距離: {distance:F1}m\n角度: {angleToTarget:F1}°\n" +
                    $"{(lookAtMode == LookAtMode.IK ? $"IK權重: {currentLookWeight:F2}" : "權重控制")}\n狀態: {(isLookingAtPlayer ? "看向中" : "未看向")}");
                #endif
            }
            else
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(headTransform.position, actualTargetPosition);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                    $"模式: {lookAtMode}" + (lookAtMode == LookAtMode.LateUpdate ? $" (權重: {lookWeight:F2})" : "") + 
                    $"\n距離: {distance:F1}m (超出範圍)\n看向半徑: {lookAtRadius:F1}m");
                #endif
            }
        }
        else
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"模式: {lookAtMode}" + (lookAtMode == LookAtMode.LateUpdate ? $" (權重: {lookWeight:F2})" : "") + 
                $"\n沒有找到目標玩家");
            #endif
        }

        // 顯示組件狀態
        if (!enableHeadLook)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
                $"HeadControl 已停用 ({lookAtMode} 模式)");
            #endif
        }
        
        if (lookAtMode == LookAtMode.IK && animator == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3.5f, 
                "IK 模式缺少 Animator 組件");
            #endif
        }
    }
}