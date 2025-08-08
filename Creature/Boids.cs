using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Boids群集生物 - 繼承自Creature並實現三維boids算法
/// 實現separation(分離)、alignment(對齊)、cohesion(聚合)三大群集行為
/// </summary>
public class Boids : Creature
{
    [Header("Boids 群集參數")]
    [SerializeField] private float detectionRadius = 5f;            // 檢測半徑
    [SerializeField] private float separationRadius = 2f;           // 分離半徑
    [SerializeField] private float maxSpeed = 8f;                   // 最大速度
    [SerializeField] private float maxForce = 3f;                   // 最大轉向力
    
    [Header("行為權重")]
    [SerializeField] private float separationWeight = 1.5f;        // 分離權重
    [SerializeField] private float alignmentWeight = 1.0f;         // 對齊權重
    [SerializeField] private float cohesionWeight = 1.0f;          // 聚合權重
    [SerializeField] private float avoidanceWeight = 2.0f;         // 避障權重
    
    [Header("邊界設定")]
    [SerializeField] private Vector3 boundaryCenter = Vector3.zero; // 邊界中心
    [SerializeField] private Vector3 boundarySize = new Vector3(50, 20, 50); // 邊界大小
    [SerializeField] private float boundaryForce = 5f;             // 邊界回彈力
    
    [Header("避障設定")]
    [SerializeField] private float avoidanceDistance = 3f;         // 避障距離
    [SerializeField] private LayerMask obstacleLayer = 1;          // 障礙物圖層
    
    [Header("性能優化設定")]
    [SerializeField] private bool usePhysicsSystem = false;        // 使用物理系統 (較重)
    [SerializeField] private bool enableDebugLogs = false;         // 啟用調試日誌
    
    // 私有變數
    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 acceleration;
    private FlockingState flockingState;
    
    // 靜態群集列表
    private static List<Boids> allBoids = new List<Boids>();
    
    // 鄰居快取 (使用池化系統)
    private List<Boids> neighbors;
    private float lastNeighborUpdate = 0f;
    private float neighborUpdateInterval = 0.1f; // 動態更新間隔，由 BoidsManager 控制
    
    // 搜尋方法控制
    private NeighborSearchMethod currentSearchMethod = NeighborSearchMethod.Physics;
    private BoidsManager boidsManager;
    
    // 頻率統計
    private int neighborUpdateCount = 0;
    private float lastFrequencyLog = 0f;
    
    // 性能優化：快取計算結果
    private Vector3 cachedAcceleration = Vector3.zero;
    private bool needsFlockingUpdate = true;
    
    // 距離平方快取 (避免重複計算)
    private float detectionRadiusSquared;
    private float separationRadiusSquared;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 尋找 BoidsManager
        boidsManager = FindFirstObjectByType<BoidsManager>();
        
        // 初始化池化的鄰居列表
        if (boidsManager != null)
        {
            neighbors = boidsManager.GetNeighborsList();
        }
        else
        {
            neighbors = new List<Boids>(); // fallback
        }
        
        // 預計算距離平方以避免重複計算
        detectionRadiusSquared = detectionRadius * detectionRadius;
        separationRadiusSquared = separationRadius * separationRadius;
        
        // 創建專用的群集狀態
        flockingState = new FlockingState(this, stateMachine);
        
        // 註冊到全局boids列表
        if (!allBoids.Contains(this))
        {
            allBoids.Add(this);
        }
    }
    
    protected override void Start()
    {
        // 初始化為群集狀態而不是Idle狀態
        stateMachine.Initialize(flockingState);
    }
    
    protected override void GetRequiredComponents()
    {
        base.GetRequiredComponents();
        
        if (usePhysicsSystem)
        {
            // 獲取或添加Rigidbody (重量級物理系統)
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            // 設置Rigidbody屬性
            rb.useGravity = false;  // Boids通常不受重力影響
            rb.linearDamping = 1f;  // 添加一些阻力
        }
        else
        {
            // 輕量級運動系統：移除 Rigidbody 以提升性能
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(rb);
                }
                else
                {
                    DestroyImmediate(rb);
                }
                rb = null;
            }
        }
        
        // 初始化速度
        velocity = transform.forward * maxSpeed * 0.5f;
        
        // 如果使用物理系統，同步到 Rigidbody
        if (usePhysicsSystem && rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }
    
    /// <summary>
    /// 更新群集行為 - 在FlockingState的FrameUpdate中調用
    /// </summary>
    public void UpdateFlocking()
    {
        // 檢查是否需要更新鄰居列表
        if (Time.time - lastNeighborUpdate > neighborUpdateInterval)
        {
            UpdateNeighbors();
            lastNeighborUpdate = Time.time;
            neighborUpdateCount++;
            needsFlockingUpdate = true; // 鄰居更新後需要重新計算群集行為
        }
        
        // 只有在需要時才重新計算群集行為（性能優化）
        if (needsFlockingUpdate)
        {
            // 重置加速度
            acceleration = Vector3.zero;
            
            // 計算群集行為
            Vector3 separation = Separate() * separationWeight;
            Vector3 alignment = Align() * alignmentWeight;
            Vector3 cohesion = Seek(Cohesion()) * cohesionWeight;
            Vector3 avoidance = Avoid() * avoidanceWeight;
            Vector3 boundary = StayInBounds() * boundaryForce;
            
            // 應用所有力
            acceleration += separation;
            acceleration += alignment;
            acceleration += cohesion;
            acceleration += avoidance;
            acceleration += boundary;
            
            // 限制加速度
            acceleration = Vector3.ClampMagnitude(acceleration, maxForce);
            
            // 快取計算結果
            cachedAcceleration = acceleration;
            needsFlockingUpdate = false;
        }
        else
        {
            // 使用快取的加速度
            acceleration = cachedAcceleration;
        }
    }
    
    /// <summary>
    /// 應用移動 - 在FlockingState的PhysicsUpdate中調用
    /// </summary>
    public void ApplyMovement()
    {
        // 更新速度
        velocity += acceleration * Time.fixedDeltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        
        if (usePhysicsSystem && rb != null)
        {
            // 使用物理系統 (較重但支援碰撞)
            rb.linearVelocity = velocity;
        }
        else
        {
            // 使用輕量級運動系統 (較輕但無碰撞)
            transform.position += velocity * Time.fixedDeltaTime;
        }
        
        // 更新朝向
        if (velocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    private Collider[] neighborsBuffer = new Collider[600]; // 預先準備好一個陣列
    private void UpdateNeighbors()
    {
        switch (currentSearchMethod)
        {
            case NeighborSearchMethod.Physics:
                UpdateNeighborsPhysics();
                break;
            case NeighborSearchMethod.SpatialGrid:
                UpdateNeighborsSpatialGrid();
                break;
        }
    }
    
    /// <summary>
    /// 使用物理引擎進行鄰居搜尋 (原有方法)
    /// </summary>
    private void UpdateNeighborsPhysics()
    {
        System.Diagnostics.Stopwatch stopwatch = null;
        if (enableDebugLogs)
        {
            // 只在啟用調試模式時記錄時間
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
        }

        // 使用 OverlapSphereNonAlloc
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, neighborsBuffer);
        neighbors.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            Boids other = neighborsBuffer[i].GetComponent<Boids>();
            if (other != null && other != this && !other.IsDead) neighbors.Add(other);
        }
        
        if (enableDebugLogs && stopwatch != null)
        {
            stopwatch.Stop();
            UnityEngine.Debug.Log($"UpdateNeighbors (Physics) 執行時間: {stopwatch.Elapsed.TotalMilliseconds:F3} ms - 找到 {neighbors.Count} 個鄰居 @ {GetCurrentUpdateFrequency():F1} Hz");
        }
    }
    
    /// <summary>
    /// 使用空間格子進行鄰居搜尋 (新方法)
    /// </summary>
    private void UpdateNeighborsSpatialGrid()
    {
        System.Diagnostics.Stopwatch stopwatch = null;
        if (enableDebugLogs)
        {
            // 只在啟用調試模式時記錄時間
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
        }

        if (boidsManager != null)
        {
            boidsManager.QueryNeighborsUsingSpatialGrid(this, detectionRadius, neighbors);
        }
        else
        {
            neighbors.Clear();
        }
        
        if (enableDebugLogs && stopwatch != null)
        {
            stopwatch.Stop();
            UnityEngine.Debug.Log($"UpdateNeighbors (SpatialGrid) 執行時間: {stopwatch.Elapsed.TotalMilliseconds:F3} ms - 找到 {neighbors.Count} 個鄰居 @ {GetCurrentUpdateFrequency():F1} Hz");
        }
    }    
    /// <summary>
    /// 分離行為 - 避免與鄰近個體碰撞
    /// </summary>
    private Vector3 Separate()
    {
        Vector3 steer = Vector3.zero;
        int count = 0;
        Vector3 myPosition = transform.position; // 快取位置
        
        foreach (Boids other in neighbors)
        {
            Vector3 diff = myPosition - other.transform.position;
            float distanceSquared = diff.sqrMagnitude; // 使用距離平方
            
            if (distanceSquared < separationRadiusSquared && distanceSquared > 0)
            {
                // 使用距離平方的倒數作為權重 (避免開方運算)
                float invDistance = 1f / Mathf.Sqrt(distanceSquared);
                diff.Normalize();
                diff *= invDistance;
                steer += diff;
                count++;
            }
        }
        
        if (count > 0)
        {
            steer /= count;
            steer = steer.normalized * maxSpeed;
            steer -= velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);
        }
        
        return steer;
    }
    
    /// <summary>
    /// 對齊行為 - 與鄰近個體保持相同方向
    /// </summary>
    private Vector3 Align()
    {
        if (neighbors.Count == 0) return Vector3.zero; // 提前退出優化
        
        Vector3 sum = Vector3.zero;
        int count = neighbors.Count;
        
        // 直接累加，不使用 foreach 以減少迭代器開銷
        for (int i = 0; i < count; i++)
        {
            sum += neighbors[i].velocity;
        }
        
        sum /= count;
        sum = sum.normalized * maxSpeed;
        Vector3 steer = sum - velocity;
        return Vector3.ClampMagnitude(steer, maxForce);
    }
    
    /// <summary>
    /// 聚合行為 - 計算鄰近個體的中心位置
    /// </summary>
    private Vector3 Cohesion()
    {
        if (neighbors.Count == 0) return transform.position; // 提前退出優化
        
        Vector3 sum = Vector3.zero;
        int count = neighbors.Count;
        
        // 直接累加位置，避免重複計算
        for (int i = 0; i < count; i++)
        {
            sum += neighbors[i].transform.position;
        }
        
        return sum / count;
    }
    
    /// <summary>
    /// 尋找目標 - 用於聚合行為
    /// </summary>
    private Vector3 Seek(Vector3 target)
    {
        Vector3 desired = (target - transform.position).normalized * maxSpeed;
        Vector3 steer = desired - velocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
        return steer;
    }
    
    /// <summary>
    /// 避障行為 - 使用射線檢測避開障礙物
    /// </summary>
    private Vector3 Avoid()
    {
        Vector3 avoidance = Vector3.zero;
        
        // 前方檢測
        if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit, avoidanceDistance, obstacleLayer))
        {
            Vector3 avoidDirection = Vector3.Reflect(velocity.normalized, hit.normal);
            avoidance = avoidDirection * maxSpeed - velocity;
        }
        
        // 多方向檢測
        Vector3[] directions = {
            transform.forward,
            transform.forward + transform.right * 0.5f,
            transform.forward - transform.right * 0.5f,
            transform.forward + transform.up * 0.5f,
            transform.forward - transform.up * 0.5f
        };
        
        foreach (Vector3 direction in directions)
        {
            if (Physics.Raycast(transform.position, direction, avoidanceDistance, obstacleLayer))
            {
                avoidance += -direction * maxSpeed;
            }
        }
        
        return Vector3.ClampMagnitude(avoidance, maxForce);
    }
    
    /// <summary>
    /// 邊界約束 - 保持在指定區域內
    /// </summary>
    private Vector3 StayInBounds()
    {
        Vector3 force = Vector3.zero;
        Vector3 position = transform.position;
        Vector3 min = boundaryCenter - boundarySize * 0.5f;
        Vector3 max = boundaryCenter + boundarySize * 0.5f;
        
        if (position.x < min.x) force.x = maxSpeed;
        if (position.x > max.x) force.x = -maxSpeed;
        if (position.y < min.y) force.y = maxSpeed;
        if (position.y > max.y) force.y = -maxSpeed;
        if (position.z < min.z) force.z = maxSpeed;
        if (position.z > max.z) force.z = -maxSpeed;
        
        return force;
    }
    
    /// <summary>
    /// 設置群集參數
    /// </summary>
    public void SetFlockingParameters(float detection, float separation, float speed, float force)
    {
        detectionRadius = detection;
        separationRadius = separation;
        maxSpeed = speed;
        maxForce = force;
        
        // 重新計算距離平方快取
        detectionRadiusSquared = detectionRadius * detectionRadius;
        separationRadiusSquared = separationRadius * separationRadius;
    }
    
    /// <summary>
    /// 設置行為權重
    /// </summary>
    public void SetBehaviorWeights(float sep, float align, float coh, float avoid)
    {
        separationWeight = sep;
        alignmentWeight = align;
        cohesionWeight = coh;
        avoidanceWeight = avoid;
    }
    
    /// <summary>
    /// 設置邊界
    /// </summary>
    public void SetBoundary(Vector3 center, Vector3 size)
    {
        boundaryCenter = center;
        boundarySize = size;
    }
    
    /// <summary>
    /// 獲取當前鄰居數量
    /// </summary>
    public int GetNeighborCount()
    {
        return neighbors.Count;
    }
    
    /// <summary>
    /// 獲取當前速度
    /// </summary>
    public Vector3 GetVelocity()
    {
        return velocity;
    }
    
    /// <summary>
    /// 設置鄰居搜尋方法 (由 BoidsManager 調用)
    /// </summary>
    public void SetNeighborSearchMethod(NeighborSearchMethod method)
    {
        currentSearchMethod = method;
    }
    
    /// <summary>
    /// 獲取當前搜尋方法
    /// </summary>
    public NeighborSearchMethod GetCurrentSearchMethod()
    {
        return currentSearchMethod;
    }
    
    /// <summary>
    /// 設置鄰居更新頻率 (由 BoidsManager 調用)
    /// </summary>
    public void SetNeighborUpdateFrequency(float frequency)
    {
        neighborUpdateInterval = 1f / Mathf.Max(frequency, 0.1f);
    }
    
    /// <summary>
    /// 獲取當前更新頻率
    /// </summary>
    public float GetCurrentUpdateFrequency()
    {
        return 1f / neighborUpdateInterval;
    }
    
    /// <summary>
    /// 獲取頻率統計資訊
    /// </summary>
    public string GetFrequencyStats()
    {
        float currentTime = Time.time;
        if (currentTime - lastFrequencyLog > 1f)
        {
            float actualFreq = neighborUpdateCount / (currentTime - lastFrequencyLog);
            neighborUpdateCount = 0;
            lastFrequencyLog = currentTime;
            return $"實際更新頻率: {actualFreq:F1} Hz, 設定頻率: {GetCurrentUpdateFrequency():F1} Hz";
        }
        return "";
    }
    
    /// <summary>
    /// 設置性能相關參數 (由 BoidsManager 調用)
    /// </summary>
    public void SetPerformanceSettings(bool usePhysics, bool enableDebug)
    {
        if (usePhysicsSystem != usePhysics)
        {
            usePhysicsSystem = usePhysics;
            // 重新初始化運動系統
            GetRequiredComponents();
        }
        enableDebugLogs = enableDebug;
    }
    
    private void OnDestroy()
    {
        // 歸還池化的鄰居列表
        if (neighbors != null && boidsManager != null)
        {
            boidsManager.ReturnNeighborsList(neighbors);
            neighbors = null;
        }
        
        // 從全局列表中移除
        if (allBoids.Contains(this))
        {
            allBoids.Remove(this);
        }
    }
    
    protected override void OnDrawGizmosSelected()
    {
        if (!enableDebugLogs) return; // 性能模式下不繪製 Gizmos
        
        base.OnDrawGizmosSelected();
        
        // 繪製檢測半徑
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // 繪製分離半徑
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        
        // 繪製速度方向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, velocity);
        
        // 繪製避障檢測射線
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * avoidanceDistance);
        
        // 繪製邊界
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(boundaryCenter, boundarySize);
    }
    
    private void OnDrawGizmos()
    {
        if (!enableDebugLogs) return; // 性能模式下不繪製 Gizmos
        
        // 繪製與鄰居的連線
        if (neighbors != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Boids neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
                }
            }
        }
    }
}