using UnityEngine;
using System.Collections.Generic;

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
    
    private List<Boids> spawnedBoids = new List<Boids>();
    
    private void Start()
    {
        if (autoSpawn)
        {
            SpawnBoids();
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
                DestroyImmediate(child.gameObject);
            }
        }
        
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
    
    private void OnDrawGizmos()
    {
        // 繪製生成區域
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnCenter, spawnArea);
        
        // 繪製邊界
        if (showBoundary)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
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
        if (Application.isPlaying && spawnedBoids.Count > 0)
        {
            UpdateBoidsParameters();
        }
    }
}