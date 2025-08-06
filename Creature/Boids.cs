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
    
    // 私有變數
    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 acceleration;
    private FlockingState flockingState;
    
    // 靜態群集列表
    private static List<Boids> allBoids = new List<Boids>();
    
    // 鄰居快取
    private List<Boids> neighbors = new List<Boids>();
    private float lastNeighborUpdate = 0f;
    private const float neighborUpdateInterval = 0.1f; // 每0.1秒更新一次鄰居
    
    protected override void Awake()
    {
        base.Awake();
        
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
        
        // 獲取或添加Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 設置Rigidbody屬性
        rb.useGravity = false;  // Boids通常不受重力影響
        rb.linearDamping = 1f;           // 添加一些阻力
        
        // 初始化速度
        velocity = transform.forward * maxSpeed * 0.5f;
        rb.linearVelocity = velocity;
    }
    
    /// <summary>
    /// 更新群集行為 - 在FlockingState的FrameUpdate中調用
    /// </summary>
    public void UpdateFlocking()
    {
        if (Time.time - lastNeighborUpdate > neighborUpdateInterval)
        {
            UpdateNeighbors();
            lastNeighborUpdate = Time.time;
        }
        
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
    }
    
    /// <summary>
    /// 應用移動 - 在FlockingState的PhysicsUpdate中調用
    /// </summary>
    public void ApplyMovement()
    {
        // 更新速度
        velocity += acceleration * Time.fixedDeltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        
        // 應用速度到Rigidbody
        rb.linearVelocity = velocity;
        
        // 更新朝向
        if (velocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
    
    /// <summary>
    /// 更新鄰居列表
    /// </summary>
    private void UpdateNeighbors()
    {
        neighbors.Clear();
        
        foreach (Boids other in allBoids)
        {
            if (other != this && other != null && !other.IsDead)
            {
                float distance = Vector3.Distance(transform.position, other.transform.position);
                if (distance < detectionRadius)
                {
                    neighbors.Add(other);
                }
            }
        }
    }
    
    /// <summary>
    /// 分離行為 - 避免與鄰近個體碰撞
    /// </summary>
    private Vector3 Separate()
    {
        Vector3 steer = Vector3.zero;
        int count = 0;
        
        foreach (Boids other in neighbors)
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);
            
            if (distance < separationRadius && distance > 0)
            {
                Vector3 diff = (transform.position - other.transform.position).normalized;
                diff /= distance; // 距離越近，權重越大
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
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (Boids other in neighbors)
        {
            sum += other.velocity;
            count++;
        }
        
        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;
            Vector3 steer = sum - velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);
            return steer;
        }
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// 聚合行為 - 計算鄰近個體的中心位置
    /// </summary>
    private Vector3 Cohesion()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (Boids other in neighbors)
        {
            sum += other.transform.position;
            count++;
        }
        
        if (count > 0)
        {
            sum /= count;
            return sum;
        }
        
        return transform.position;
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
    
    private void OnDestroy()
    {
        // 從全局列表中移除
        if (allBoids.Contains(this))
        {
            allBoids.Remove(this);
        }
    }
    
    protected override void OnDrawGizmosSelected()
    {
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