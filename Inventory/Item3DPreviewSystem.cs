using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 3D 道具預覽系統
/// 負責管理所有道具的 3D 模型預覽，使用全域預載入策略
/// </summary>
public class Item3DPreviewSystem : MonoBehaviour
{
    [Header("預覽相機設定")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private RawImage previewDisplay;
    
    [Header("預覽場景設定")]
    [SerializeField] private Transform previewArea;        // 顯示區域
    [SerializeField] private Light previewLight;           // 預覽光源
    
    [Header("載入設定")]
    [SerializeField] private string itemResourcePath = "Items/";  // Item ScriptableObject 資源路徑
    
    // 全域模型池
    private Dictionary<int, GameObject> globalModelPool = new Dictionary<int, GameObject>();
    private GameObject currentActiveModel;
    private Transform poolContainer;
    
    // 旋轉控制
    private bool isRotationEnabled = true;
    private Vector3 currentRotationSpeed = Vector3.zero;
    
    public static Item3DPreviewSystem Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        InitializePreviewSystem();
        LoadAllItemModels();
    }
    
    private void Update()
    {
        // 處理自動旋轉
        if (isRotationEnabled && currentActiveModel != null && currentRotationSpeed != Vector3.zero)
        {
            currentActiveModel.transform.Rotate(currentRotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// 初始化預覽系統
    /// </summary>
    private void InitializePreviewSystem()
    {
        // 創建全域模型容器
        GameObject poolObject = new GameObject("GlobalItemModelPool");
        poolContainer = poolObject.transform;
        poolContainer.SetParent(transform);
        poolContainer.gameObject.SetActive(false); // 隱藏容器
        
        // 初始化相機和 RenderTexture
        if (previewCamera == null)
        {
            previewCamera = GetComponentInChildren<Camera>();
        }
        
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(512, 512, 16);
            renderTexture.name = "ItemPreviewRT";
        }
        
        if (previewCamera != null)
        {
            previewCamera.targetTexture = renderTexture;
            previewCamera.enabled = true;
        }
        
        // 設置 RawImage 顯示 RenderTexture
        if (previewDisplay != null)
        {
            previewDisplay.texture = renderTexture;
        }
        
        Debug.Log("[Item3DPreviewSystem] 預覽系統初始化完成");
    }
    
    /// <summary>
    /// 載入所有道具的 3D 模型
    /// </summary>
    private void LoadAllItemModels()
    {
        // 載入所有 Item ScriptableObject 資源
        Item[] allItems = Resources.LoadAll<Item>(itemResourcePath);
        
        Debug.Log($"[Item3DPreviewSystem] 開始載入 {allItems.Length} 個道具的 3D 模型");
        
        int loadedCount = 0;
        foreach (Item item in allItems)
        {
            if (item.ItemPrefab3D != null)
            {
                // 實例化模型
                GameObject model = Instantiate(item.ItemPrefab3D, poolContainer);
                model.name = $"Preview_{item.Name}_{item.Id}";
                model.SetActive(false);
                
                // 應用預設的預覽設定
                ApplyItemPreviewSettings(model, item);
                
                // 移除物理組件（預覽不需要物理）
                RemovePhysicsComponents(model);
                
                // 加入全域池
                globalModelPool[item.Id] = model;
                loadedCount++;
                
                Debug.Log($"[Item3DPreviewSystem] 載入模型: {item.Name} (ID: {item.Id})");
            }
        }
        
        Debug.Log($"[Item3DPreviewSystem] 模型載入完成，共載入 {loadedCount} 個模型");
    }
    
    /// <summary>
    /// 應用道具的預覽設定
    /// </summary>
    private void ApplyItemPreviewSettings(GameObject model, Item item)
    {
        model.transform.localScale = item.PreviewScale;
        model.transform.localRotation = Quaternion.Euler(item.PreviewRotation);
        model.transform.localPosition = item.PreviewPosition;
    }
    
    /// <summary>
    /// 移除預覽模型上不需要的物理組件
    /// </summary>
    private void RemovePhysicsComponents(GameObject model)
    {
        // 禁用所有碰撞器
        Collider[] colliders = model.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // 移除或設定剛體為 kinematic
        Rigidbody[] rigidbodies = model.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }
    
    /// <summary>
    /// 顯示指定道具的 3D 預覽
    /// </summary>
    public void ShowItemPreview(Item item)
    {
        if (item == null)
        {
            HidePreview();
            return;
        }
        
        // 隱藏當前模型
        HideCurrentModel();
        
        // 從全域池中獲取模型
        if (globalModelPool.TryGetValue(item.Id, out GameObject model))
        {
            // 移動到預覽區域並激活
            if (previewArea != null)
            {
                model.transform.SetParent(previewArea);
            }
            model.SetActive(true);
            currentActiveModel = model;
            
            // 設定旋轉
            if (item.EnableAutoRotation)
            {
                currentRotationSpeed = item.RotationSpeed;
                isRotationEnabled = true;
            }
            else
            {
                currentRotationSpeed = Vector3.zero;
                isRotationEnabled = false;
            }
            
            Debug.Log($"[Item3DPreviewSystem] 顯示預覽: {item.Name}");
        }
        else
        {
            Debug.LogWarning($"[Item3DPreviewSystem] 找不到道具模型: {item.Name} (ID: {item.Id})");
        }
    }
    
    /// <summary>
    /// 隱藏當前的預覽
    /// </summary>
    public void HidePreview()
    {
        HideCurrentModel();
        currentRotationSpeed = Vector3.zero;
    }
    
    /// <summary>
    /// 隱藏當前激活的模型
    /// </summary>
    private void HideCurrentModel()
    {
        if (currentActiveModel != null)
        {
            currentActiveModel.SetActive(false);
            currentActiveModel.transform.SetParent(poolContainer); // 移回池容器
            currentActiveModel = null;
        }
    }
    
    /// <summary>
    /// 設定旋轉啟用狀態
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        isRotationEnabled = enabled;
    }
    
    /// <summary>
    /// 清理模型池（可選，用於釋放內存）
    /// </summary>
    public void ClearModelPool()
    {
        HidePreview();
        
        foreach (var kvp in globalModelPool)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
            }
        }
        
        globalModelPool.Clear();
        Debug.Log("[Item3DPreviewSystem] 模型池已清理");
    }
    
    /// <summary>
    /// 獲取預覽系統狀態信息
    /// </summary>
    public string GetSystemInfo()
    {
        return $"載入模型數量: {globalModelPool.Count}, 當前預覽: {(currentActiveModel?.name ?? "無")}";
    }
    
    private void OnDestroy()
    {
        ClearModelPool();
        
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}