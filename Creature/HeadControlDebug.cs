using UnityEngine;

/// <summary>
/// 極簡化的頭部控制調試組件
/// 使用 LateUpdate 直接操作 Transform rotation
/// </summary>
public class HeadControlDebug : MonoBehaviour
{
    [Header("手動設定")]
    [SerializeField] private Transform headTransform;                   // 手動指定頭部Transform
    [SerializeField] private Transform debugTarget;                     // 手動指定看向目標
    [SerializeField] private bool enableLookAt = true;                  // 啟用看向功能
    [SerializeField, Range(0f, 1f)] private float lookWeight = 1f;      // 看向權重 (0-1)
    [SerializeField] private bool enableDebugLogs = true;               // 啟用調試信息
    
    [Header("旋轉設定")]
    [SerializeField] private float lookSpeed = 2f;                      // 頭部轉動速度
    [SerializeField] private bool smoothRotation = true;                // 使用平滑過渡

    // 私有變數
    private Quaternion originalHeadRotation;                            // 頭部原始旋轉
    private bool isInitialized = false;                                 // 是否已初始化

    private void Start()
    {
        // 保存頭部原始旋轉
        if (headTransform != null)
        {
            originalHeadRotation = headTransform.rotation;
            isInitialized = true;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[HeadControlDebug] {name}: 初始化完成，保存原始旋轉");
            }
        }
        else
        {
            Debug.LogWarning($"[HeadControlDebug] {name}: headTransform 未設定");
        }
    }

    private void OnValidate()
    {
        // 確保參數在有效範圍內
        lookWeight = Mathf.Clamp01(lookWeight);
        lookSpeed = Mathf.Max(0f, lookSpeed);
    }

    /// <summary>
    /// LateUpdate - 在動畫更新後執行頭部看向
    /// </summary>
    private void LateUpdate()
    {
        if (!isInitialized || !enableLookAt || headTransform == null || debugTarget == null)
        {
            // 如果功能停用且有頭部引用，逐漸恢復原始旋轉
            if (isInitialized && headTransform != null && !enableLookAt)
            {
                if (smoothRotation)
                {
                    headTransform.rotation = Quaternion.Slerp(headTransform.rotation, originalHeadRotation, lookSpeed * Time.deltaTime);
                }
                else
                {
                    headTransform.rotation = Quaternion.Slerp(headTransform.rotation, originalHeadRotation, 1f - lookWeight);
                }
            }
            return;
        }

        // 計算看向方向
        Vector3 lookDirection = (debugTarget.position - headTransform.position).normalized;
        
        // 計算目標旋轉
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        
        // 根據設定應用旋轉
        if (smoothRotation)
        {
            // 平滑過渡模式：使用時間和速度
            float actualSpeed = lookSpeed * lookWeight * Time.deltaTime;
            headTransform.rotation = Quaternion.Slerp(headTransform.rotation, targetRotation, actualSpeed);
        }
        else
        {
            // 直接混合模式：使用權重直接插值
            headTransform.rotation = Quaternion.Slerp(originalHeadRotation, targetRotation, lookWeight);
        }

        // 調試信息輸出
        if (enableDebugLogs && Time.frameCount % 60 == 0) // 每秒輸出一次
        {
            float angle = Quaternion.Angle(originalHeadRotation, headTransform.rotation);
            Debug.Log($"[HeadControlDebug] {name}: 看向 {debugTarget.name}, 角度偏移 {angle:F1}°, 權重 {lookWeight:F2}");
        }
    }

    /// <summary>
    /// Scene 視圖調試顯示
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 顯示頭部位置
        if (headTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(headTransform.position, 0.1f);
        }

        // 顯示看向目標和連線
        if (debugTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(debugTarget.position, 0.15f);

            if (headTransform != null)
            {
                // 根據狀態選擇顏色
                Gizmos.color = enableLookAt ? Color.green : Color.red;
                Gizmos.DrawLine(headTransform.position, debugTarget.position);
            }
        }

        // 顯示狀態信息
        #if UNITY_EDITOR
        Vector3 labelPosition = transform.position + Vector3.up * 2f;
        
        string statusText = $"看向狀態: {(enableLookAt ? "啟用" : "停用")}\n";
        statusText += $"權重: {lookWeight:F2}\n";
        statusText += $"速度: {lookSpeed:F1}\n";
        statusText += $"平滑: {(smoothRotation ? "是" : "否")}\n";
        statusText += $"目標: {(debugTarget != null ? debugTarget.name : "無")}";
        
        if (isInitialized && headTransform != null)
        {
            float currentAngle = Quaternion.Angle(originalHeadRotation, headTransform.rotation);
            statusText += $"\n角度偏移: {currentAngle:F1}°";
        }
        
        UnityEditor.Handles.Label(labelPosition, statusText);
        #endif
    }

    /// <summary>
    /// 設定看向目標（可供外部調用）
    /// </summary>
    /// <param name="target">目標 Transform</param>
    public void SetTarget(Transform target)
    {
        debugTarget = target;
        if (enableDebugLogs)
        {
            Debug.Log($"[HeadControlDebug] {name}: 設定目標為 {(target != null ? target.name : "null")}");
        }
    }

    /// <summary>
    /// 設定看向權重（可供外部調用）
    /// </summary>
    /// <param name="weight">權重值 (0-1)</param>
    public void SetLookWeight(float weight)
    {
        lookWeight = Mathf.Clamp01(weight);
        if (enableDebugLogs)
        {
            Debug.Log($"[HeadControlDebug] {name}: 設定權重為 {lookWeight:F2}");
        }
    }

    /// <summary>
    /// 切換看向功能（可供外部調用）
    /// </summary>
    /// <param name="enabled">是否啟用</param>
    public void SetLookAtEnabled(bool enabled)
    {
        enableLookAt = enabled;
        if (enableDebugLogs)
        {
            Debug.Log($"[HeadControlDebug] {name}: 看向功能 {(enabled ? "啟用" : "停用")}");
        }
    }

    /// <summary>
    /// 設定看向速度（可供外部調用）
    /// </summary>
    /// <param name="speed">轉動速度</param>
    public void SetLookSpeed(float speed)
    {
        lookSpeed = Mathf.Max(0f, speed);
        if (enableDebugLogs)
        {
            Debug.Log($"[HeadControlDebug] {name}: 設定轉動速度為 {lookSpeed:F2}");
        }
    }

    /// <summary>
    /// 設定平滑旋轉模式（可供外部調用）
    /// </summary>
    /// <param name="smooth">是否使用平滑過渡</param>
    public void SetSmoothRotation(bool smooth)
    {
        smoothRotation = smooth;
        if (enableDebugLogs)
        {
            Debug.Log($"[HeadControlDebug] {name}: 平滑旋轉 {(smooth ? "啟用" : "停用")}");
        }
    }

    /// <summary>
    /// 重置頭部到原始旋轉（可供外部調用）
    /// </summary>
    public void ResetToOriginal()
    {
        if (isInitialized && headTransform != null)
        {
            headTransform.rotation = originalHeadRotation;
            if (enableDebugLogs)
            {
                Debug.Log($"[HeadControlDebug] {name}: 重置到原始旋轉");
            }
        }
    }
}