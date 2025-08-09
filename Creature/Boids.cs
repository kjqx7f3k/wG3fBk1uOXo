using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Boids群集生物 - 繼承自Creature並實現三維boids算法
/// 實現separation(分離)、alignment(對齊)、cohesion(聚合)三大群集行為
/// </summary>
public class Boids : Creature
{
    // 群集參數快取 (從 BoidsManager 獲取)
    private float detectionRadius;            // 檢測半徑
    private float separationRadius;           // 分離半徑
    private float maxSpeed;                   // 最大速度
    private float maxForce;                   // 最大轉向力
    
    // 行為權重快取 (從 BoidsManager 獲取)
    private float separationWeight;        // 分離權重
    private float alignmentWeight;         // 對齊權重
    private float cohesionWeight;          // 聚合權重
    private float avoidanceWeight;         // 避障權重
    
    // 邊界設定快取 (從 BoidsManager 獲取)
    private Vector3 boundaryCenter; // 邊界中心
    private Vector3 boundarySize; // 邊界大小
    private float boundaryForce; // 邊界拉回力強度
    
    // 物理設定快取 (從 BoidsManager 獲取)
    private float boidsBounciness; // 彈性係數
    private float boidsFriction;   // 摩擦力
    private float boidsMass;       // 質量
    private float boidsDrag;       // 阻力
    
    [Header("避障設定")]
    [SerializeField] private float avoidanceDistance = 3f;         // 避障距離
    [SerializeField] private LayerMask obstacleLayer = 1;          // 障礙物圖層
    
    // 性能優化設定快取 (從 BoidsManager 獲取)
    private bool usePhysicsSystem;        // 使用物理系統 (較重)
    private bool enableDebugLogs;         // 啟用調試日誌
    
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
    private float lastParameterSyncCheck = 0f; // 參數同步檢查計時器
    
    // 性能優化：快取計算結果
    private Vector3 cachedAcceleration = Vector3.zero;
    private bool needsFlockingUpdate = true;
    
    // 距離平方快取 (避免重複計算)
    private float detectionRadiusSquared;
    private float separationRadiusSquared;
    
    // 參數同步標記
    private bool parametersInitialized = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 尋找 BoidsManager
        boidsManager = FindFirstObjectByType<BoidsManager>();
        
        // 初始化池化的鄰居列表
        if (boidsManager != null)
        {
            neighbors = boidsManager.GetNeighborsList();
            // 從 BoidsManager 同步所有參數
            SyncParametersFromManager();
        }
        else
        {
            neighbors = new List<Boids>(); // fallback
            Debug.LogWarning($"{name}: 找不到 BoidsManager，使用預設參數");
            // 設置預設參數值
            SetDefaultParameters();
        }
        
        // 預計算距離平方以避免重複計算
        UpdateDistanceSquaredCache();
        
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
            rb.mass = boidsMass;
            rb.linearDamping = boidsDrag;
            rb.angularDamping = 0.5f;
            
            // 確保有 Collider
            Collider boidCollider = GetComponent<Collider>();
            if (boidCollider == null)
            {
                boidCollider = gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)boidCollider).radius = 0.5f;
                if (enableDebugLogs)
                {
                    Debug.Log($"{name}: 自動添加 SphereCollider");
                }
            }
            
            // 創建並應用彈性材質
            PhysicsMaterial physicsMaterial = new PhysicsMaterial("BoidsMaterial");
            physicsMaterial.bounciness = boidsBounciness;
            
            // 嘗試設定摩擦力（不同Unity版本可能有不同屬性名）
            try
            {
                // Unity 2022+ 使用 staticFriction 和 dynamicFriction
                physicsMaterial.staticFriction = boidsFriction;
                physicsMaterial.dynamicFriction = boidsFriction;
            }
            catch
            {
                // 如果上述屬性不存在，就跳過摩擦力設定
                if (enableDebugLogs)
                {
                    Debug.Log($"{name}: 無法設定摩擦力，跳過此設定");
                }
            }
            
            physicsMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
            physicsMaterial.frictionCombine = PhysicsMaterialCombine.Average;
            boidCollider.material = physicsMaterial;
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
        // 定期檢查參數同步（每秒檢查一次）
        if (Time.time - lastParameterSyncCheck > 1f)
        {
            CheckParameterSync();
            lastParameterSyncCheck = Time.time;
        }
        
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
            Vector3 boundary = StayInBounds();
            
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
            // 使用 AddForce 而不是直接設定 velocity，以保留碰撞反彈
            Vector3 targetVelocity = velocity;
            Vector3 velocityDifference = targetVelocity - rb.linearVelocity;
            
            // 只在速度差異較大時施加力，避免覆蓋碰撞反彈
            if (velocityDifference.magnitude > 0.1f)
            {
                rb.AddForce(velocityDifference * 10f, ForceMode.Force);
            }
            
            // 限制最大速度，但不覆蓋整個速度向量
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
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
    /// 簡單邊界拉回力 - 當 boid 接近或超出邊界時施加拉回力
    /// </summary>
    private Vector3 StayInBounds()
    {
        if (boundaryForce <= 0) return Vector3.zero;
        
        Vector3 currentPos = transform.position;
        Vector3 boundaryMin = boundaryCenter - boundarySize * 0.5f;
        Vector3 boundaryMax = boundaryCenter + boundarySize * 0.5f;
        Vector3 force = Vector3.zero;
        
        // 檢查每個軸向的邊界
        if (currentPos.x < boundaryMin.x)
            force.x = (boundaryMin.x - currentPos.x) * boundaryForce;
        else if (currentPos.x > boundaryMax.x)
            force.x = (boundaryMax.x - currentPos.x) * boundaryForce;
            
        if (currentPos.y < boundaryMin.y)
            force.y = (boundaryMin.y - currentPos.y) * boundaryForce;
        else if (currentPos.y > boundaryMax.y)
            force.y = (boundaryMax.y - currentPos.y) * boundaryForce;
            
        if (currentPos.z < boundaryMin.z)
            force.z = (boundaryMin.z - currentPos.z) * boundaryForce;
        else if (currentPos.z > boundaryMax.z)
            force.z = (boundaryMax.z - currentPos.z) * boundaryForce;
        
        return Vector3.ClampMagnitude(force, maxForce);
    }
    
    
    
    
    /// <summary>
    /// 從 BoidsManager 同步所有參數
    /// </summary>
    public void SyncParametersFromManager()
    {
        if (boidsManager == null) return;
        
        // 獲取群集參數
        detectionRadius = boidsManager.DetectionRadius;
        separationRadius = boidsManager.SeparationRadius;
        maxSpeed = boidsManager.MaxSpeed;
        maxForce = boidsManager.MaxForce;
        
        // 獲取行為權重
        separationWeight = boidsManager.SeparationWeight;
        alignmentWeight = boidsManager.AlignmentWeight;
        cohesionWeight = boidsManager.CohesionWeight;
        avoidanceWeight = boidsManager.AvoidanceWeight;
        
        // 獲取邊界設定
        boundaryCenter = boidsManager.BoundaryCenter;
        boundarySize = boidsManager.BoundarySize;
        boundaryForce = boidsManager.BoundaryForce;
        
        // 獲取物理設定
        boidsBounciness = boidsManager.BoidsBounciness;
        boidsFriction = boidsManager.BoidsFriction;
        boidsMass = boidsManager.BoidsMass;
        boidsDrag = boidsManager.BoidsDrag;
        
        // 獲取性能設定
        usePhysicsSystem = boidsManager.UsePhysicsSystem;
        enableDebugLogs = boidsManager.EnableDebugLogs;
        
        parametersInitialized = true;
        UpdateDistanceSquaredCache();
    }
    
    /// <summary>
    /// 設置預設參數值（當找不到 BoidsManager 時使用）
    /// </summary>
    private void SetDefaultParameters()
    {
        // 預設群集參數
        detectionRadius = 5f;
        separationRadius = 2f;
        maxSpeed = 8f;
        maxForce = 3f;
        
        // 預設行為權重
        separationWeight = 1.5f;
        alignmentWeight = 1.0f;
        cohesionWeight = 1.0f;
        avoidanceWeight = 2.0f;
        
        // 預設邊界設定
        boundaryCenter = Vector3.zero;
        boundarySize = new Vector3(50, 20, 50);
        boundaryForce = 2f;
        
        // 預設物理設定
        boidsBounciness = 0.8f;
        boidsFriction = 0.3f;
        boidsMass = 1f;
        boidsDrag = 0.1f;
        
        // 預設性能設定
        usePhysicsSystem = false;
        enableDebugLogs = false;
        
        parametersInitialized = true;
        UpdateDistanceSquaredCache();
    }
    
    /// <summary>
    /// 更新距離平方快取
    /// </summary>
    private void UpdateDistanceSquaredCache()
    {
        detectionRadiusSquared = detectionRadius * detectionRadius;
        separationRadiusSquared = separationRadius * separationRadius;
    }
    
    /// <summary>
    /// 檢查是否需要重新同步參數
    /// </summary>
    public void CheckParameterSync()
    {
        if (boidsManager != null && parametersInitialized)
        {
            // 簡單的參數一致性檢查
            if (Mathf.Abs(detectionRadius - boidsManager.DetectionRadius) > 0.01f ||
                Mathf.Abs(maxSpeed - boidsManager.MaxSpeed) > 0.01f)
            {
                SyncParametersFromManager();
                needsFlockingUpdate = true; // 參數改變後需要重新計算
            }
        }
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
    /// 強制從 BoidsManager 重新同步參數
    /// </summary>
    public void ForceParameterSync()
    {
        SyncParametersFromManager();
        needsFlockingUpdate = true;
    }
    
    /// <summary>
    /// 獲取參數是否已初始化
    /// </summary>
    public bool IsParametersInitialized => parametersInitialized;
    
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
        
        // 繪製邊界框（當啟用邊界力時）
        if (boundaryForce > 0)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
        }
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
    
    /// <summary>
    /// 碰撞事件處理 - 用於調試和增強碰撞效果
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"{name} 碰撞到 {collision.gameObject.name}，碰撞速度: {collision.relativeVelocity.magnitude:F2}");
        }
    }
}