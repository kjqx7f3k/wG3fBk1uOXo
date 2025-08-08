using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 鄰居搜尋方法枚舉
/// </summary>
public enum NeighborSearchMethod
{
    Physics,        // 使用 Unity Physics.OverlapSphereNonAlloc
    SpatialGrid     // 使用自定義空間格子系統
}

/// <summary>
/// Boids群集管理器 - 用於批量創建和管理Boids群體
/// </summary>
public class BoidsManager : MonoBehaviour
{
    [Header("群體生成設定")]
    [SerializeField] private GameObject boidsPrefab;                // Boids預製體
    [SerializeField] private int boidsCount = 20;                  // 群體數量
    [SerializeField] private Vector3 spawnArea = new Vector3(10, 5, 10); // 生成區域
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;   // 生成中心
    
    [Header("群體參數設定")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float maxForce = 3f;
    
    [Header("行為權重設定")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1.0f;
    [SerializeField] private float cohesionWeight = 1.0f;
    [SerializeField] private float avoidanceWeight = 2.0f;
    
    [Header("邊界設定")]
    [SerializeField] private Vector3 boundaryCenter = Vector3.zero;
    [SerializeField] private Vector3 boundarySize = new Vector3(50, 20, 50);
    
    [Header("運行時控制")]
    [SerializeField] private bool autoSpawn = true;                // 自動生成
    [SerializeField] private bool showBoundary = true;             // 顯示邊界
    
    [Header("鄰居搜尋設定")]
    [SerializeField] private NeighborSearchMethod searchMethod = NeighborSearchMethod.Physics;  // 搜尋方法
    [SerializeField] private float gridCellSize = 5f;              // 格子大小 (僅用於SpatialGrid)
    [SerializeField] private bool showSpatialGrid = false;         // 顯示空間格子
    [SerializeField] private bool showPerformanceStats = true;     // 顯示性能統計
    
    [Header("鄰居更新頻率設定")]
    [SerializeField, Range(1f, 60f)] private float neighborUpdateFrequency = 10f;  // 鄰居更新頻率 (Hz)
    [SerializeField] private bool useAdaptiveFrequency = false;    // 使用自適應頻率
    [SerializeField, Range(1f, 30f)] private float minFrequency = 5f;  // 最小頻率 (自適應模式)
    [SerializeField, Range(10f, 60f)] private float maxFrequency = 30f; // 最大頻率 (自適應模式)
    [SerializeField] private bool showFrequencyStats = true;       // 顯示頻率統計
    
    [Header("性能優化設定")]
    [SerializeField] private bool usePhysicsSystem = false;        // 使用物理系統 (較重但支援碰撞)
    [SerializeField] private bool enableDebugLogs = false;         // 啟用調試日誌
    [SerializeField] private bool enableGizmoDrawing = true;       // 啟用Gizmo繪製
    
    [Header("智能容量管理")]
    [SerializeField] private bool enableAdaptiveCapacity = true;   // 啟用自適應容量管理
    [SerializeField, Range(2, 16)] private int minCapacity = 4;    // 最小初始容量
    [SerializeField, Range(8, 64)] private int maxCapacity = 32;   // 最大初始容量
    [SerializeField, Range(30f, 300f)] private float capacityUpdateInterval = 60f; // 容量更新間隔 (秒)
    [SerializeField] private bool showCapacityStats = false;       // 顯示容量統計
    [SerializeField] private bool autoOptimizeCapacity = true;     // 自動優化容量
    
    private List<Boids> spawnedBoids = new List<Boids>();
    private SpatialGrid spatialGrid;
    private NeighborSearchMethod lastSearchMethod;
    
    // Transform 緩存系統
    private TransformCache transformCache = new TransformCache();
    
    // 頻率控制相關變數
    private float lastFrequencyUpdate = 0f;
    private float lastNeighborUpdateFrequency = 0f;
    private int totalNeighborUpdates = 0;
    private int savedComputations = 0;
    private float frequencyUpdateInterval = 1f; // 每秒檢查一次自適應頻率
    
    // 容量管理相關變數
    private float lastCapacityUpdate = 0f;
    private int currentOptimalCapacity = 8; // 當前計算的最佳容量
    private bool capacityStatsCollected = false; // 是否已收集統計數據
    private float capacityCollectionStartTime = 0f; // 開始收集統計數據的時間
    
    private void Start()
    {
        // 初始化空間格子系統
        InitializeSpatialGrid();
        
        if (autoSpawn)
        {
            SpawnBoids();
        }
        
        // 設置初始搜尋方法和頻率
        lastSearchMethod = searchMethod;
        lastNeighborUpdateFrequency = neighborUpdateFrequency;
        UpdateSearchMethod();
        UpdateNeighborFrequency();
    }
    
    private void Update()
    {
        // 檢查搜尋方法是否改變
        if (searchMethod != lastSearchMethod)
        {
            lastSearchMethod = searchMethod;
            UpdateSearchMethod();
            Debug.Log($"鄰居搜尋方法切換為: {searchMethod}");
        }
        
        // 檢查頻率設定是否改變
        if (Mathf.Abs(neighborUpdateFrequency - lastNeighborUpdateFrequency) > 0.01f)
        {
            lastNeighborUpdateFrequency = neighborUpdateFrequency;
            UpdateNeighborFrequency();
            Debug.Log($"鄰居更新頻率設定為: {neighborUpdateFrequency} Hz");
        }
        
        // 自適應頻率調整
        if (useAdaptiveFrequency && Time.time - lastFrequencyUpdate > frequencyUpdateInterval)
        {
            UpdateAdaptiveFrequency();
            lastFrequencyUpdate = Time.time;
        }
        
        // 智能容量管理更新
        if (enableAdaptiveCapacity && spatialGrid != null)
        {
            UpdateAdaptiveCapacity();
        }
        
        // 更新 Transform 緩存 (性能優化)
        transformCache.UpdateDirtyCache();
        
        // 更新空間格子
        if (searchMethod == NeighborSearchMethod.SpatialGrid && spatialGrid != null)
        {
            UpdateSpatialGrid();
        }
        
        // 顯示性能統計
        if (showPerformanceStats && spatialGrid != null)
        {
            string stats = spatialGrid.GetPerformanceStats();
            if (!string.IsNullOrEmpty(stats))
            {
                Debug.Log(stats);
            }
        }
        
        // 顯示頻率統計
        if (showFrequencyStats)
        {
            ShowFrequencyStats();
        }
        
        // 顯示容量統計
        if (showCapacityStats && enableAdaptiveCapacity && spatialGrid != null)
        {
            ShowCapacityStats();
        }
    }
    
    /// <summary>
    /// 生成Boids群體
    /// </summary>
    [ContextMenu("生成Boids群體")]
    public void SpawnBoids()
    {
        if (boidsPrefab == null)
        {
            Debug.LogError("BoidsManager: boidsPrefab 未設置！");
            return;
        }
        
        // 清除現有的Boids
        ClearBoids();
        
        for (int i = 0; i < boidsCount; i++)
        {
            // 隨機生成位置
            Vector3 randomPosition = spawnCenter + new Vector3(
                Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
                Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f),
                Random.Range(-spawnArea.z * 0.5f, spawnArea.z * 0.5f)
            );
            
            // 隨機生成旋轉
            Quaternion randomRotation = Quaternion.Euler(
                Random.Range(0, 360),
                Random.Range(0, 360),
                Random.Range(0, 360)
            );
            
            // 創建Boids實例
            GameObject boidsObj = Instantiate(boidsPrefab, randomPosition, randomRotation, transform);
            boidsObj.name = $"Boids_{i:D3}";
            
            // 獲取Boids組件並設置參數
            Boids boids = boidsObj.GetComponent<Boids>();
            if (boids != null)
            {
                boids.SetFlockingParameters(detectionRadius, separationRadius, maxSpeed, maxForce);
                boids.SetBehaviorWeights(separationWeight, alignmentWeight, cohesionWeight, avoidanceWeight);
                boids.SetBoundary(boundaryCenter, boundarySize);
                
                // 註冊到 Transform 緩存系統
                transformCache.Register(boids.transform);
                
                spawnedBoids.Add(boids);
            }
            else
            {
                Debug.LogWarning($"BoidsManager: 預製體 {boidsPrefab.name} 沒有Boids組件！");
            }
        }
        
        Debug.Log($"BoidsManager: 成功生成 {spawnedBoids.Count} 個Boids");
    }
    
    /// <summary>
    /// 清除所有Boids
    /// </summary>
    [ContextMenu("清除所有Boids")]
    public void ClearBoids()
    {
        foreach (Boids boids in spawnedBoids)
        {
            if (boids != null)
            {
                // 從緩存中移除
                transformCache.Unregister(boids.transform);
                DestroyImmediate(boids.gameObject);
            }
        }
        
        spawnedBoids.Clear();
        
        // 清除子物件中剩餘的Boids
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<Boids>() != null)
            {
                transformCache.Unregister(child);
                DestroyImmediate(child.gameObject);
            }
        }
        
        // 清理緩存中的無效引用
        transformCache.CleanupNullReferences();
        
        Debug.Log("BoidsManager: 已清除所有Boids");
    }
    
    /// <summary>
    /// 更新所有Boids的參數
    /// </summary>
    [ContextMenu("更新Boids參數")]
    public void UpdateBoidsParameters()
    {
        foreach (Boids boids in spawnedBoids)
        {
            if (boids != null)
            {
                boids.SetFlockingParameters(detectionRadius, separationRadius, maxSpeed, maxForce);
                boids.SetBehaviorWeights(separationWeight, alignmentWeight, cohesionWeight, avoidanceWeight);
                boids.SetBoundary(boundaryCenter, boundarySize);
            }
        }
        
        Debug.Log("BoidsManager: 已更新所有Boids參數");
    }
    
    /// <summary>
    /// 添加單個Boids到現有群體
    /// </summary>
    public void AddBoids(Vector3 position)
    {
        if (boidsPrefab == null) return;
        
        GameObject boidsObj = Instantiate(boidsPrefab, position, Random.rotation, transform);
        boidsObj.name = $"Boids_{spawnedBoids.Count:D3}";
        
        Boids boids = boidsObj.GetComponent<Boids>();
        if (boids != null)
        {
            boids.SetFlockingParameters(detectionRadius, separationRadius, maxSpeed, maxForce);
            boids.SetBehaviorWeights(separationWeight, alignmentWeight, cohesionWeight, avoidanceWeight);
            boids.SetBoundary(boundaryCenter, boundarySize);
            
            spawnedBoids.Add(boids);
        }
    }
    
    /// <summary>
    /// 移除指定的Boids
    /// </summary>
    public void RemoveBoids(Boids boids)
    {
        if (spawnedBoids.Contains(boids))
        {
            spawnedBoids.Remove(boids);
            if (boids != null)
            {
                Destroy(boids.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 獲取群體統計信息
    /// </summary>
    public void LogFlockStatistics()
    {
        if (spawnedBoids.Count == 0)
        {
            Debug.Log("BoidsManager: 沒有活躍的Boids");
            return;
        }
        
        int aliveCount = 0;
        float avgSpeed = 0f;
        int totalNeighbors = 0;
        
        foreach (Boids boids in spawnedBoids)
        {
            if (boids != null && !boids.IsDead)
            {
                aliveCount++;
                avgSpeed += boids.GetVelocity().magnitude;
                totalNeighbors += boids.GetNeighborCount();
            }
        }
        
        if (aliveCount > 0)
        {
            avgSpeed /= aliveCount;
            float avgNeighbors = (float)totalNeighbors / aliveCount;
            
            Debug.Log($"群體統計:\n" +
                     $"總數: {spawnedBoids.Count}\n" +
                     $"存活: {aliveCount}\n" +
                     $"平均速度: {avgSpeed:F2}\n" +
                     $"平均鄰居數: {avgNeighbors:F1}");
        }
    }
    
    /// <summary>
    /// 設置群體目標位置（可用於引導群體移動）
    /// </summary>
    public void SetFlockTarget(Vector3 targetPosition)
    {
        // 這個功能可以在未來擴展，讓群體跟隨特定目標
        foreach (Boids boids in spawnedBoids)
        {
            if (boids != null)
            {
                // 可以在這裡添加目標追蹤邏輯
            }
        }
    }
    
    /// <summary>
    /// 初始化空間格子系統
    /// </summary>
    private void InitializeSpatialGrid()
    {
        Bounds worldBounds = new Bounds(boundaryCenter, boundarySize);
        
        // 根據設定決定初始容量
        int initialCapacity = enableAdaptiveCapacity ? 
            Mathf.Clamp(8, minCapacity, maxCapacity) : 8; // 預設仍為 8
        
        spatialGrid = new SpatialGrid(gridCellSize, worldBounds, initialCapacity, enableAdaptiveCapacity);
        
        // 如果啟用自適應容量，開始收集統計數據
        if (enableAdaptiveCapacity)
        {
            capacityCollectionStartTime = Time.time;
            capacityStatsCollected = false;
            Debug.Log($"智能容量管理已啟用 - 初始容量: {initialCapacity}, 範圍: {minCapacity}-{maxCapacity}");
        }
    }
    
    /// <summary>
    /// 更新搜尋方法
    /// </summary>
    private void UpdateSearchMethod()
    {
        foreach (Boids boid in spawnedBoids)
        {
            if (boid != null)
            {
                boid.SetNeighborSearchMethod(searchMethod);
                // 同步性能設定
                boid.SetPerformanceSettings(usePhysicsSystem, enableDebugLogs);
            }
        }
    }
    
    /// <summary>
    /// 更新空間格子
    /// </summary>
    private void UpdateSpatialGrid()
    {
        if (spatialGrid == null) return;
        
        // 清空格子
        spatialGrid.Clear();
        
        // 插入所有活躍的 boids
        foreach (Boids boid in spawnedBoids)
        {
            if (boid != null && !boid.IsDead)
            {
                spatialGrid.Insert(boid);
            }
        }
    }
    
    /// <summary>
    /// 為指定 Boid 查詢鄰居 (供 Boids 類使用)
    /// </summary>
    public void QueryNeighborsUsingSpatialGrid(Boids queryBoid, float radius, List<Boids> results)
    {
        if (spatialGrid != null)
        {
            spatialGrid.QueryNeighbors(queryBoid, radius, results);
        }
        else
        {
            results.Clear();
        }
    }
    
    /// <summary>
    /// 從池中獲取鄰居列表 (池化優化)
    /// </summary>
    public List<Boids> GetNeighborsList()
    {
        return ListPool<Boids>.Get(50); // 預設容量 50
    }
    
    /// <summary>
    /// 歸還鄰居列表到池中 (池化優化)
    /// </summary>
    public void ReturnNeighborsList(List<Boids> list)
    {
        ListPool<Boids>.Return(list);
    }
    
    /// <summary>
    /// 獲取緩存的 Transform 位置 (性能優化)
    /// </summary>
    public Vector3 GetCachedPosition(Transform transform)
    {
        return transformCache.GetPosition(transform);
    }
    
    /// <summary>
    /// 標記 Transform 為需要更新
    /// </summary>
    public void MarkTransformDirty(Transform transform)
    {
        transformCache.MarkDirty(transform);
    }
    
    /// <summary>
    /// 設置格子大小並重新初始化格子系統
    /// </summary>
    public void SetGridCellSize(float newCellSize)
    {
        if (newCellSize != gridCellSize)
        {
            gridCellSize = newCellSize;
            InitializeSpatialGrid();
            Debug.Log($"格子大小已更新為: {gridCellSize}");
        }
    }
    
    /// <summary>
    /// 切換搜尋方法
    /// </summary>
    [ContextMenu("切換至物理引擎搜尋")]
    public void SwitchToPhysicsSearch()
    {
        searchMethod = NeighborSearchMethod.Physics;
    }
    
    [ContextMenu("切換至格子搜尋")]
    public void SwitchToSpatialGridSearch()
    {
        searchMethod = NeighborSearchMethod.SpatialGrid;
    }
    
    /// <summary>
    /// 顯示當前搜尋方法性能報告
    /// </summary>
    [ContextMenu("顯示性能報告")]
    public void ShowPerformanceReport()
    {
        if (spatialGrid != null)
        {
            spatialGrid.LogGridInfo();
        }
        
        LogFlockStatistics();
        
        Debug.Log($"當前搜尋方法: {searchMethod}");
        Debug.Log($"格子大小: {gridCellSize}");
        Debug.Log($"鄰居更新頻率: {neighborUpdateFrequency} Hz");
        Debug.Log($"自適應頻率: {(useAdaptiveFrequency ? "啟用" : "停用")}");
    }
    
    /// <summary>
    /// 更新所有 Boids 的鄰居搜尋頻率
    /// </summary>
    private void UpdateNeighborFrequency()
    {
        float currentFreq = useAdaptiveFrequency ? CalculateAdaptiveFrequency() : neighborUpdateFrequency;
        
        foreach (Boids boid in spawnedBoids)
        {
            if (boid != null)
            {
                boid.SetNeighborUpdateFrequency(currentFreq);
            }
        }
    }
    
    /// <summary>
    /// 自適應頻率更新
    /// </summary>
    private void UpdateAdaptiveFrequency()
    {
        float newFrequency = CalculateAdaptiveFrequency();
        
        foreach (Boids boid in spawnedBoids)
        {
            if (boid != null)
            {
                boid.SetNeighborUpdateFrequency(newFrequency);
            }
        }
    }
    
    /// <summary>
    /// 計算自適應頻率
    /// </summary>
    private float CalculateAdaptiveFrequency()
    {
        if (spawnedBoids.Count == 0) return neighborUpdateFrequency;
        
        // 基於 Boids 數量的自適應頻率
        float densityFactor = Mathf.InverseLerp(10, 200, spawnedBoids.Count);
        float adaptiveFreq = Mathf.Lerp(maxFrequency, minFrequency, densityFactor);
        
        // 基於平均鄰居數量的調整
        float avgNeighbors = GetAverageNeighborCount();
        if (avgNeighbors > 10)
        {
            adaptiveFreq *= 0.8f; // 高密度時降低頻率
        }
        else if (avgNeighbors < 3)
        {
            adaptiveFreq *= 1.2f; // 低密度時提高頻率
        }
        
        return Mathf.Clamp(adaptiveFreq, minFrequency, maxFrequency);
    }
    
    /// <summary>
    /// 獲取平均鄰居數量
    /// </summary>
    private float GetAverageNeighborCount()
    {
        if (spawnedBoids.Count == 0) return 0;
        
        int totalNeighbors = 0;
        int validBoids = 0;
        
        foreach (Boids boid in spawnedBoids)
        {
            if (boid != null && !boid.IsDead)
            {
                totalNeighbors += boid.GetNeighborCount();
                validBoids++;
            }
        }
        
        return validBoids > 0 ? (float)totalNeighbors / validBoids : 0;
    }
    
    /// <summary>
    /// 顯示頻率統計
    /// </summary>
    private void ShowFrequencyStats()
    {
        float currentTime = Time.time;
        if (currentTime - lastFrequencyUpdate > frequencyUpdateInterval)
        {
            float effectiveFreq = useAdaptiveFrequency ? CalculateAdaptiveFrequency() : neighborUpdateFrequency;
            float maxPossibleUpdates = spawnedBoids.Count * 60f; // 假設60FPS
            float actualUpdates = spawnedBoids.Count * effectiveFreq;
            float savedRatio = (maxPossibleUpdates - actualUpdates) / maxPossibleUpdates * 100f;
            
            Debug.Log($"頻率統計 - 當前: {effectiveFreq:F1} Hz, 節省計算: {savedRatio:F1}%, 平均鄰居數: {GetAverageNeighborCount():F1}");
        }
    }
    
    /// <summary>
    /// 設置頻率的快速選項
    /// </summary>
    [ContextMenu("設為低頻率 (5Hz)")]
    public void SetLowFrequency() => neighborUpdateFrequency = 5f;
    
    [ContextMenu("設為中頻率 (15Hz)")]
    public void SetMediumFrequency() => neighborUpdateFrequency = 15f;
    
    [ContextMenu("設為高頻率 (30Hz)")]
    public void SetHighFrequency() => neighborUpdateFrequency = 30f;
    
    [ContextMenu("切換自適應頻率")]
    public void ToggleAdaptiveFrequency() => useAdaptiveFrequency = !useAdaptiveFrequency;
    
    /// <summary>
    /// 智能容量管理更新邏輯
    /// </summary>
    private void UpdateAdaptiveCapacity()
    {
        float currentTime = Time.time;
        
        // 檢查是否需要更新容量
        if (currentTime - lastCapacityUpdate > capacityUpdateInterval)
        {
            if (autoOptimizeCapacity)
            {
                int newOptimalCapacity = CalculateOptimalCapacity();
                if (newOptimalCapacity != currentOptimalCapacity)
                {
                    currentOptimalCapacity = newOptimalCapacity;
                    ApplyCapacityUpdate(newOptimalCapacity);
                    Debug.Log($"智能容量已更新為: {newOptimalCapacity}");
                }
            }
            lastCapacityUpdate = currentTime;
        }
        
        // 標記統計數據收集完成 (30秒後)
        if (!capacityStatsCollected && currentTime - capacityCollectionStartTime > 30f)
        {
            capacityStatsCollected = true;
            Debug.Log("容量統計數據收集完成，開始智能優化");
        }
    }
    
    /// <summary>
    /// 計算最佳容量
    /// </summary>
    private int CalculateOptimalCapacity()
    {
        if (spatialGrid == null || !capacityStatsCollected) 
            return currentOptimalCapacity;
        
        // 獲取空間格子的容量統計
        var capacityStats = spatialGrid.GetCapacityStats();
        if (capacityStats.averageBoidsPerCell <= 0) 
            return currentOptimalCapacity;
        
        // 計算理想容量: 平均數量 * 1.5 + 標準差 + 緩衝區
        float idealCapacity = capacityStats.averageBoidsPerCell * 1.5f + 
                             capacityStats.standardDeviation + 2f;
        
        // 應用範圍限制
        int optimalCapacity = Mathf.RoundToInt(idealCapacity);
        return Mathf.Clamp(optimalCapacity, minCapacity, maxCapacity);
    }
    
    /// <summary>
    /// 應用容量更新
    /// </summary>
    private void ApplyCapacityUpdate(int newCapacity)
    {
        if (spatialGrid != null)
        {
            spatialGrid.UpdateOptimalCapacity(newCapacity);
        }
    }
    
    /// <summary>
    /// 顯示容量統計
    /// </summary>
    private void ShowCapacityStats()
    {
        if (spatialGrid == null) return;
        
        float currentTime = Time.time;
        if (currentTime - lastCapacityUpdate > 2f) // 每2秒顯示一次
        {
            var stats = spatialGrid.GetCapacityStats();
            string statusText = capacityStatsCollected ? "已收集" : "收集中";
            
            Debug.Log($"容量統計 ({statusText}):\n" +
                     $"當前最佳容量: {currentOptimalCapacity}\n" +
                     $"平均每格 Boids: {stats.averageBoidsPerCell:F2}\n" +
                     $"最大格子 Boids: {stats.maxBoidsInCell}\n" +
                     $"容量利用率: {stats.capacityUtilization:P1}\n" +
                     $"擴展次數: {stats.totalExpansions}");
        }
    }
    
    /// <summary>
    /// 容量管理選單
    /// </summary>
    [ContextMenu("重置容量統計")]
    public void ResetCapacityStats()
    {
        capacityStatsCollected = false;
        capacityCollectionStartTime = Time.time;
        currentOptimalCapacity = 8;
        if (spatialGrid != null)
        {
            spatialGrid.ResetCapacityStats();
        }
        Debug.Log("容量統計已重置");
    }
    
    [ContextMenu("顯示當前容量報告")]
    public void ShowCapacityReport()
    {
        if (spatialGrid != null)
        {
            var stats = spatialGrid.GetCapacityStats();
            Debug.Log($"詳細容量報告:\n{spatialGrid.GetDetailedCapacityReport()}");
        }
    }
    
    /// <summary>
    /// 性能優化選項
    /// </summary>
    [ContextMenu("啟用性能模式 (關閉物理系統和調試)")]
    public void EnablePerformanceMode()
    {
        usePhysicsSystem = false;
        enableDebugLogs = false;
        enableGizmoDrawing = false;
        showFrequencyStats = false;
        showPerformanceStats = false;
        showCapacityStats = false;
        UpdateSearchMethod();
        Debug.Log("性能模式已啟用 - 關閉物理系統、調試日誌和Gizmo繪製");
    }
    
    [ContextMenu("啟用調試模式 (開啟所有功能)")]
    public void EnableDebugMode()
    {
        usePhysicsSystem = true;
        enableDebugLogs = true;
        enableGizmoDrawing = true;
        showFrequencyStats = true;
        showPerformanceStats = true;
        showCapacityStats = true;
        UpdateSearchMethod();
        Debug.Log("調試模式已啟用 - 開啟所有功能以便分析");
    }
    
    [ContextMenu("平衡模式 (中等性能設定)")]
    public void EnableBalanceMode()
    {
        usePhysicsSystem = false;
        enableDebugLogs = false;
        enableGizmoDrawing = true;
        showFrequencyStats = true;
        showPerformanceStats = false;
        showCapacityStats = false;
        UpdateSearchMethod();
        Debug.Log("平衡模式已啟用 - 輕量級運動但保持視覺調試");
    }
    
    private void OnDrawGizmos()
    {
        if (!enableGizmoDrawing) return; // 性能優化：可關閉 Gizmo 繪製
        
        // 繪製生成區域
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnCenter, spawnArea);
        
        // 繪製邊界
        if (showBoundary)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
        }
        
        // 繪製空間格子
        if (showSpatialGrid && spatialGrid != null && Application.isPlaying)
        {
            DrawSpatialGrid();
        }
    }
    
    /// <summary>
    /// 繪製空間格子可視化
    /// </summary>
    private void DrawSpatialGrid()
    {
        // 繪製佔用的格子
        Gizmos.color = new Color(0, 1, 1, 0.3f); // 半透明青色
        List<Vector3Int> occupiedCells = spatialGrid.GetOccupiedCells();
        bool hasOccupiedCells = occupiedCells.Count > 0;
        
        try
        {
            foreach (Vector3Int gridPos in occupiedCells)
            {
                List<Boids> cellBoids = spatialGrid.GetCellContents(gridPos);
                if (cellBoids.Count > 0)
                {
                    Bounds cellBounds = spatialGrid.GetCellBounds(gridPos);
                    Gizmos.DrawCube(cellBounds.center, cellBounds.size);
                    
                    // 繪製格子邊框
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(cellBounds.center, cellBounds.size);
                    Gizmos.color = new Color(0, 1, 1, 0.3f);
                }
            }
        }
        finally
        {
            // 重要：歸還池化列表避免記憶體洩漏
            spatialGrid.ReturnOccupiedCellsList(occupiedCells);
        }
        
        // 繪製格子邊界 (總邊界的格子劃分)
        if (hasOccupiedCells)
        {
            Gizmos.color = new Color(1, 1, 1, 0.1f); // 很淡的白色
            Vector3 worldMin = boundaryCenter - boundarySize * 0.5f;
            Vector3 worldMax = boundaryCenter + boundarySize * 0.5f;
            
            // 繪製主要的格子線 (每隔幾條線繪製一次以避免過度雜亂)
            int stepX = Mathf.Max(1, (int)(boundarySize.x / gridCellSize) / 10);
            int stepY = Mathf.Max(1, (int)(boundarySize.y / gridCellSize) / 10);
            int stepZ = Mathf.Max(1, (int)(boundarySize.z / gridCellSize) / 10);
            
            // X 軸格子線
            for (int i = 0; i <= (int)(boundarySize.x / gridCellSize); i += stepX)
            {
                float x = worldMin.x + i * gridCellSize;
                Gizmos.DrawLine(new Vector3(x, worldMin.y, worldMin.z), 
                               new Vector3(x, worldMax.y, worldMax.z));
            }
            
            // Y 軸格子線
            for (int i = 0; i <= (int)(boundarySize.y / gridCellSize); i += stepY)
            {
                float y = worldMin.y + i * gridCellSize;
                Gizmos.DrawLine(new Vector3(worldMin.x, y, worldMin.z), 
                               new Vector3(worldMax.x, y, worldMax.z));
            }
            
            // Z 軸格子線
            for (int i = 0; i <= (int)(boundarySize.z / gridCellSize); i += stepZ)
            {
                float z = worldMin.z + i * gridCellSize;
                Gizmos.DrawLine(new Vector3(worldMin.x, worldMin.y, z), 
                               new Vector3(worldMax.x, worldMax.y, z));
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 繪製生成區域（選中時）
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawCube(spawnCenter, spawnArea);
        
        // 繪製邊界（選中時）
        Gizmos.color = new Color(1, 1, 1, 0.1f);
        Gizmos.DrawCube(boundaryCenter, boundarySize);
    }
    
    // 編輯器中的即時參數更新
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            if (spawnedBoids.Count > 0)
            {
                UpdateBoidsParameters();
            }
            
            // 如果格子大小改變，重新初始化格子系統
            if (spatialGrid != null)
            {
                Bounds currentBounds = new Bounds(boundaryCenter, boundarySize);
                int initialCapacity = enableAdaptiveCapacity ? 
                    Mathf.Clamp(currentOptimalCapacity, minCapacity, maxCapacity) : 8;
                spatialGrid = new SpatialGrid(gridCellSize, currentBounds, initialCapacity, enableAdaptiveCapacity);
            }
        }
    }
}