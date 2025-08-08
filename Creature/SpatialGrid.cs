using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 空間格子系統 - 用於高效的3D空間鄰居查詢
/// 將3D空間劃分為均勻格子，每個格子維護物件列表
/// Phase 2 優化：使用固定陣列替代 Dictionary，減少哈希計算
/// </summary>
public class SpatialGrid
{
    private List<Boids>[] grid; // 使用陣列替代 Dictionary
    private float cellSize;
    private Vector3 worldMin;
    private Vector3 worldMax;
    private Vector3Int gridDimensions;
    private int totalCells;
    
    // 性能監控
    private int totalInsertions = 0;
    private int totalQueries = 0;
    private float lastResetTime = 0f;
    
    // 容量管理系統
    private int currentCapacity = 8; // 當前使用的容量
    private bool enableAdaptiveCapacity = false; // 是否啟用自適應容量
    
    // 容量統計數據
    private int[] cellBoidsCounts; // 記錄每個格子的 Boids 數量
    private int totalExpansions = 0; // List 擴展次數
    private int maxBoidsInAnyCell = 0; // 歷史最大格子 Boids 數
    private float lastCapacityStatsUpdate = 0f;
    
    /// <summary>
    /// 初始化空間格子
    /// </summary>
    /// <param name="cellSize">格子大小</param>
    /// <param name="worldBounds">世界邊界</param>
    /// <param name="initialCapacity">初始容量</param>
    /// <param name="enableAdaptiveCapacity">啟用自適應容量</param>
    public SpatialGrid(float cellSize, Bounds worldBounds, int initialCapacity = 8, bool enableAdaptiveCapacity = false)
    {
        this.cellSize = cellSize;
        this.worldMin = worldBounds.min;
        this.worldMax = worldBounds.max;
        this.currentCapacity = initialCapacity;
        this.enableAdaptiveCapacity = enableAdaptiveCapacity;
        
        // 計算格子維度
        Vector3 worldSize = worldMax - worldMin;
        gridDimensions = new Vector3Int(
            Mathf.CeilToInt(worldSize.x / cellSize),
            Mathf.CeilToInt(worldSize.y / cellSize),
            Mathf.CeilToInt(worldSize.z / cellSize)
        );
        
        // 使用固定陣列替代 Dictionary (性能優化)
        totalCells = gridDimensions.x * gridDimensions.y * gridDimensions.z;
        grid = new List<Boids>[totalCells];
        cellBoidsCounts = new int[totalCells]; // 初始化統計陣列
        
        // 預先初始化所有格子列表，使用智能容量
        for (int i = 0; i < totalCells; i++)
        {
            grid[i] = ListPool<Boids>.Get(currentCapacity);
            cellBoidsCounts[i] = 0;
        }
        
        Debug.Log($"SpatialGrid 初始化: 格子大小={cellSize}, 維度={gridDimensions}, 總格子數={totalCells}, " +
                 $"初始容量={currentCapacity}, 自適應容量={(enableAdaptiveCapacity ? "啟用" : "停用")}");
    }
    
    /// <summary>
    /// 將世界座標轉換為格子座標
    /// </summary>
    private Vector3Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - worldMin;
        return new Vector3Int(
            Mathf.FloorToInt(relativePos.x / cellSize),
            Mathf.FloorToInt(relativePos.y / cellSize),
            Mathf.FloorToInt(relativePos.z / cellSize)
        );
    }
    
    /// <summary>
    /// 檢查格子座標是否在有效範圍內
    /// </summary>
    private bool IsValidGridPosition(Vector3Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridDimensions.x &&
               gridPos.y >= 0 && gridPos.y < gridDimensions.y &&
               gridPos.z >= 0 && gridPos.z < gridDimensions.z;
    }
    
    /// <summary>
    /// 將3D格子座標轉換為1D陣列索引 (性能優化 + 安全性檢查)
    /// </summary>
    private int GridToIndex(Vector3Int gridPos)
    {
        // 安全性檢查：確保索引在有效範圍內
        if (!IsValidGridPosition(gridPos))
        {
            return -1; // 返回無效索引
        }
        
        return gridPos.x + gridPos.y * gridDimensions.x + gridPos.z * gridDimensions.x * gridDimensions.y;
    }
    
    /// <summary>
    /// 直接從世界座標計算陣列索引（帶安全檢查）
    /// </summary>
    private int WorldToIndex(Vector3 worldPos)
    {
        Vector3Int gridPos = WorldToGrid(worldPos);
        return GridToIndex(gridPos); // 已包含邊界檢查
    }
    
    /// <summary>
    /// 清空所有格子
    /// </summary>
    public void Clear()
    {
        // 清空所有格子，但不銷毀列表本身 (池化優化)
        for (int i = 0; i < totalCells; i++)
        {
            if (grid[i] != null)
            {
                grid[i].Clear();
                cellBoidsCounts[i] = 0; // 重置統計數據
            }
        }
    }
    
    /// <summary>
    /// 插入物件到格子中
    /// </summary>
    public void Insert(Boids boid)
    {
        if (boid == null) return;
        
        int index = WorldToIndex(boid.transform.position);
        if (index < 0 || index >= totalCells) return; // 邊界外，跳過
        
        List<Boids> cell = grid[index];
        if (!cell.Contains(boid))
        {
            // 檢測是否需要擴展容量
            if (enableAdaptiveCapacity && cell.Count >= cell.Capacity)
            {
                totalExpansions++; // 記錄擴展次數
            }
            
            cell.Add(boid);
            cellBoidsCounts[index] = cell.Count; // 更新統計
            
            // 更新最大值記錄
            if (cell.Count > maxBoidsInAnyCell)
            {
                maxBoidsInAnyCell = cell.Count;
            }
            
            totalInsertions++;
        }
    }
    
    /// <summary>
    /// 查詢指定位置周圍的鄰居
    /// </summary>
    public void QueryNeighbors(Vector3 position, float radius, List<Boids> results)
    {
        results.Clear();
        totalQueries++;
        
        Vector3Int centerGrid = WorldToGrid(position);
        int searchRadius = Mathf.CeilToInt(radius / cellSize);
        float radiusSquared = radius * radius; // 使用距離平方比較
        
        // 搜尋周圍的格子 (優化：使用陣列索引)
        for (int x = centerGrid.x - searchRadius; x <= centerGrid.x + searchRadius; x++)
        {
            for (int y = centerGrid.y - searchRadius; y <= centerGrid.y + searchRadius; y++)
            {
                for (int z = centerGrid.z - searchRadius; z <= centerGrid.z + searchRadius; z++)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    
                    if (!IsValidGridPosition(gridPos)) continue;
                    
                    int index = GridToIndex(gridPos);
                    List<Boids> cellBoids = grid[index];
                    
                    // 使用 for 迴圈替代 foreach 以提升性能
                    for (int i = 0; i < cellBoids.Count; i++)
                    {
                        Boids boid = cellBoids[i];
                        if (boid == null || boid.IsDead) continue;
                        
                        // 使用距離平方比較，避免開方運算
                        float distanceSquared = (position - boid.transform.position).sqrMagnitude;
                        if (distanceSquared <= radiusSquared)
                        {
                            results.Add(boid);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 查詢指定 Boid 的鄰居（排除自己）
    /// </summary>
    public void QueryNeighbors(Boids queryBoid, float radius, List<Boids> results)
    {
        results.Clear();
        if (queryBoid == null) return;
        
        totalQueries++;
        Vector3 position = queryBoid.transform.position;
        Vector3Int centerGrid = WorldToGrid(position);
        int searchRadius = Mathf.CeilToInt(radius / cellSize);
        float radiusSquared = radius * radius; // 使用距離平方比較
        
        // 搜尋周圍的格子 (優化：使用陣列索引)
        for (int x = centerGrid.x - searchRadius; x <= centerGrid.x + searchRadius; x++)
        {
            for (int y = centerGrid.y - searchRadius; y <= centerGrid.y + searchRadius; y++)
            {
                for (int z = centerGrid.z - searchRadius; z <= centerGrid.z + searchRadius; z++)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    
                    if (!IsValidGridPosition(gridPos)) continue;
                    
                    int index = GridToIndex(gridPos);
                    List<Boids> cellBoids = grid[index];
                    
                    // 使用 for 迴圈替代 foreach 以提升性能
                    for (int i = 0; i < cellBoids.Count; i++)
                    {
                        Boids boid = cellBoids[i];
                        if (boid == null || boid.IsDead || boid == queryBoid) continue;
                        
                        // 使用距離平方比較，避免開方運算
                        float distanceSquared = (position - boid.transform.position).sqrMagnitude;
                        if (distanceSquared <= radiusSquared)
                        {
                            results.Add(boid);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 獲取指定格子位置的物件列表
    /// </summary>
    public List<Boids> GetCellContents(Vector3Int gridPos)
    {
        if (!IsValidGridPosition(gridPos)) return new List<Boids>();
        
        int index = GridToIndex(gridPos);
        return grid[index];
    }
    
    /// <summary>
    /// 獲取所有非空格子的位置 (優化：減少不必要的遍歷)
    /// </summary>
    public List<Vector3Int> GetOccupiedCells()
    {
        List<Vector3Int> occupiedCells = ListPool<Vector3Int>.Get();
        
        // 只遍歷有效範圍，避免檢查無效索引
        for (int i = 0; i < totalCells; i++)
        {
            if (grid[i].Count > 0)
            {
                // 從1D索引反推3D座標
                int z = i / (gridDimensions.x * gridDimensions.y);
                int remainder = i % (gridDimensions.x * gridDimensions.y);
                int y = remainder / gridDimensions.x;
                int x = remainder % gridDimensions.x;
                
                occupiedCells.Add(new Vector3Int(x, y, z));
            }
        }
        
        return occupiedCells;
    }
    
    /// <summary>
    /// 歸還佔用格子列表到池中（重要：避免記憶體洩漏）
    /// </summary>
    public void ReturnOccupiedCellsList(List<Vector3Int> list)
    {
        if (list != null)
        {
            ListPool<Vector3Int>.Return(list);
        }
    }
    
    /// <summary>
    /// 將格子座標轉換為世界座標（格子中心）
    /// </summary>
    public Vector3 GridToWorld(Vector3Int gridPos)
    {
        return worldMin + new Vector3(
            (gridPos.x + 0.5f) * cellSize,
            (gridPos.y + 0.5f) * cellSize,
            (gridPos.z + 0.5f) * cellSize
        );
    }
    
    /// <summary>
    /// 獲取格子邊界
    /// </summary>
    public Bounds GetCellBounds(Vector3Int gridPos)
    {
        Vector3 center = GridToWorld(gridPos);
        return new Bounds(center, Vector3.one * cellSize);
    }
    
    /// <summary>
    /// 獲取性能統計資訊
    /// </summary>
    public string GetPerformanceStats()
    {
        float currentTime = Time.time;
        if (currentTime - lastResetTime > 1f) // 每秒重置一次
        {
            // 計算活躍格子數
            int activeCells = 0;
            for (int i = 0; i < totalCells; i++)
            {
                if (grid[i].Count > 0)
                {
                    activeCells++;
                }
            }
            
            string stats = $"空間格子性能統計:\n" +
                          $"插入次數: {totalInsertions}/秒\n" +
                          $"查詢次數: {totalQueries}/秒\n" +
                          $"活躍格子數: {activeCells}\n" +
                          $"格子大小: {cellSize}\n" +
                          $"總格子數: {totalCells}";
            
            totalInsertions = 0;
            totalQueries = 0;
            lastResetTime = currentTime;
            
            return stats;
        }
        
        return "";
    }
    
    /// <summary>
    /// 獲取格子系統資訊
    /// </summary>
    public void LogGridInfo()
    {
        int totalBoids = 0;
        int occupiedCells = 0;
        
        // 遍歷所有格子統計資訊
        for (int i = 0; i < totalCells; i++)
        {
            if (grid[i].Count > 0)
            {
                occupiedCells++;
                totalBoids += grid[i].Count;
            }
        }
        
        Debug.Log($"格子系統資訊 (Phase 2 優化):\n" +
                 $"格子大小: {cellSize}\n" +
                 $"格子維度: {gridDimensions}\n" +
                 $"總格子數: {totalCells}\n" +
                 $"已佔用格子: {occupiedCells}\n" +
                 $"總物件數: {totalBoids}\n" +
                 $"當前容量: {currentCapacity}\n" +
                 $"使用陣列索引替代字典查詢");
    }
    
    /// <summary>
    /// 容量統計結構
    /// </summary>
    public struct CapacityStats
    {
        public float averageBoidsPerCell;
        public float standardDeviation;
        public int maxBoidsInCell;
        public int totalExpansions;
        public float capacityUtilization;
        public int activeCells;
        public int totalCells;
    }
    
    /// <summary>
    /// 獲取容量統計資訊
    /// </summary>
    public CapacityStats GetCapacityStats()
    {
        int activeCells = 0;
        int totalBoids = 0;
        int maxBoids = 0;
        float sumSquaredDiff = 0;
        
        // 計算基本統計
        for (int i = 0; i < totalCells; i++)
        {
            int count = grid[i].Count;
            if (count > 0)
            {
                activeCells++;
                totalBoids += count;
                if (count > maxBoids) maxBoids = count;
            }
        }
        
        float average = activeCells > 0 ? (float)totalBoids / activeCells : 0;
        
        // 計算標準差
        for (int i = 0; i < totalCells; i++)
        {
            if (grid[i].Count > 0)
            {
                float diff = grid[i].Count - average;
                sumSquaredDiff += diff * diff;
            }
        }
        
        float standardDev = activeCells > 0 ? Mathf.Sqrt(sumSquaredDiff / activeCells) : 0;
        
        // 計算容量利用率
        int totalCapacity = activeCells * currentCapacity;
        float utilization = totalCapacity > 0 ? (float)totalBoids / totalCapacity : 0;
        
        return new CapacityStats
        {
            averageBoidsPerCell = average,
            standardDeviation = standardDev,
            maxBoidsInCell = Mathf.Max(maxBoids, maxBoidsInAnyCell),
            totalExpansions = totalExpansions,
            capacityUtilization = utilization,
            activeCells = activeCells,
            totalCells = totalCells
        };
    }
    
    /// <summary>
    /// 更新最佳容量
    /// </summary>
    public void UpdateOptimalCapacity(int newCapacity)
    {
        if (newCapacity != currentCapacity && newCapacity > 0)
        {
            int oldCapacity = currentCapacity;
            currentCapacity = newCapacity;
            
            // 重新創建所有格子列表以使用新容量
            for (int i = 0; i < totalCells; i++)
            {
                if (grid[i] != null)
                {
                    var oldList = grid[i];
                    var newList = ListPool<Boids>.Get(newCapacity);
                    
                    // 複製現有數據
                    foreach (var boid in oldList)
                    {
                        newList.Add(boid);
                    }
                    
                    // 歸還舊列表到池中
                    ListPool<Boids>.Return(oldList);
                    grid[i] = newList;
                }
            }
            
            Debug.Log($"空間格子容量已更新: {oldCapacity} → {newCapacity}");
        }
    }
    
    /// <summary>
    /// 重置容量統計
    /// </summary>
    public void ResetCapacityStats()
    {
        totalExpansions = 0;
        maxBoidsInAnyCell = 0;
        lastCapacityStatsUpdate = Time.time;
        
        // 重置統計陣列
        for (int i = 0; i < totalCells; i++)
        {
            cellBoidsCounts[i] = grid[i].Count;
        }
    }
    
    /// <summary>
    /// 獲取詳細容量報告
    /// </summary>
    public string GetDetailedCapacityReport()
    {
        var stats = GetCapacityStats();
        
        // 計算容量分布
        int[] capacityDistribution = new int[5]; // 0-20%, 20-40%, 40-60%, 60-80%, 80-100%
        int[] boidsDistribution = new int[6]; // 0, 1-2, 3-5, 6-10, 11-20, 21+
        
        for (int i = 0; i < totalCells; i++)
        {
            int count = grid[i].Count;
            if (count > 0)
            {
                // 容量利用率分布
                float utilization = (float)count / currentCapacity;
                int utilizationBucket = Mathf.Clamp((int)(utilization * 5), 0, 4);
                capacityDistribution[utilizationBucket]++;
                
                // Boids 數量分布
                if (count == 1) boidsDistribution[1]++;
                else if (count <= 2) boidsDistribution[1]++;
                else if (count <= 5) boidsDistribution[2]++;
                else if (count <= 10) boidsDistribution[3]++;
                else if (count <= 20) boidsDistribution[4]++;
                else boidsDistribution[5]++;
            }
            else
            {
                boidsDistribution[0]++;
            }
        }
        
        string report = $"智能容量管理詳細報告:\n" +
                       $"=== 基本統計 ===\n" +
                       $"當前容量設定: {currentCapacity}\n" +
                       $"活躍格子數: {stats.activeCells}/{stats.totalCells}\n" +
                       $"平均每格 Boids: {stats.averageBoidsPerCell:F2}\n" +
                       $"標準差: {stats.standardDeviation:F2}\n" +
                       $"最大格子 Boids: {stats.maxBoidsInCell}\n" +
                       $"總擴展次數: {stats.totalExpansions}\n" +
                       $"容量利用率: {stats.capacityUtilization:P1}\n" +
                       $"\n=== 容量利用率分布 ===\n" +
                       $"0-20%: {capacityDistribution[0]} 格子\n" +
                       $"20-40%: {capacityDistribution[1]} 格子\n" +
                       $"40-60%: {capacityDistribution[2]} 格子\n" +
                       $"60-80%: {capacityDistribution[3]} 格子\n" +
                       $"80-100%: {capacityDistribution[4]} 格子\n" +
                       $"\n=== Boids 數量分布 ===\n" +
                       $"空格子: {boidsDistribution[0]}\n" +
                       $"1-2 個: {boidsDistribution[1]}\n" +
                       $"3-5 個: {boidsDistribution[2]}\n" +
                       $"6-10 個: {boidsDistribution[3]}\n" +
                       $"11-20 個: {boidsDistribution[4]}\n" +
                       $"21+ 個: {boidsDistribution[5]}";
        
        return report;
    }
}