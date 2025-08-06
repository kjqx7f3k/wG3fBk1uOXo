using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Addressable資源管理器 - 處理Addressable資源的載入和管理
/// </summary>
public class AddressableResourceManager : MonoBehaviour
{
    public static AddressableResourceManager Instance { get; private set; }
    
    [Header("Addressable設定")]
    [SerializeField] private List<string> preloadLabels = new List<string>();
    [SerializeField] private List<string> preloadKeys = new List<string>();
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool debugMode = false;
    
    // 資源快取
    private Dictionary<string, UnityEngine.Object> loadedAssets = new Dictionary<string, UnityEngine.Object>();
    
    // 載入操作追蹤
    private List<AsyncOperationHandle> activeOperations = new List<AsyncOperationHandle>();
    private Dictionary<string, AsyncOperationHandle> keyToHandle = new Dictionary<string, AsyncOperationHandle>();
    
    // 載入狀態
    private bool isPreloading = false;
    private float preloadProgress = 0f;
    private int totalPreloadItems = 0;
    private int loadedPreloadItems = 0;
    
    // 事件
    public event Action OnPreloadStarted;
    public event Action<float> OnPreloadProgress; // 0.0 to 1.0
    public event Action OnPreloadCompleted;
    public event Action<string> OnAssetLoaded;
    public event Action<string, string> OnAssetLoadFailed; // key, error
    
    public bool IsPreloading => isPreloading;
    public float PreloadProgress => preloadProgress;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (debugMode)
                Debug.Log("AddressableResourceManager 初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (preloadOnStart)
        {
            StartCoroutine(PreloadResourcesCoroutine());
        }
    }
    
    /// <summary>
    /// 開始預載入資源
    /// </summary>
    public void StartPreload()
    {
        if (!isPreloading)
        {
            StartCoroutine(PreloadResourcesCoroutine());
        }
    }
    
    /// <summary>
    /// 預載入資源協程
    /// </summary>
    private IEnumerator PreloadResourcesCoroutine()
    {
        if (isPreloading)
        {
            Debug.LogWarning("預載入已在進行中");
            yield break;
        }
        
        isPreloading = true;
        preloadProgress = 0f;
        loadedPreloadItems = 0;
        
        OnPreloadStarted?.Invoke();
        
        if (debugMode)
            Debug.Log("開始Addressable資源預載入");
        
        // 計算總預載入項目數
        totalPreloadItems = preloadLabels.Count + preloadKeys.Count;
        
        if (totalPreloadItems == 0)
        {
            CompletePreload();
            yield break;
        }
        
        // 預載入標籤資源
        foreach (string label in preloadLabels)
        {
            yield return StartCoroutine(PreloadByLabel(label));
        }
        
        // 預載入指定鍵值資源
        foreach (string key in preloadKeys)
        {
            yield return StartCoroutine(PreloadByKey(key));
        }
        
        CompletePreload();
    }
    
    /// <summary>
    /// 根據標籤預載入資源
    /// </summary>
    private IEnumerator PreloadByLabel(string label)
    {
        if (debugMode)
            Debug.Log($"開始預載入標籤: {label}");
        
        var handle = Addressables.LoadResourceLocationsAsync(label);
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var locations = handle.Result;
            
            foreach (var location in locations)
            {
                yield return StartCoroutine(LoadAssetByLocation(location));
            }
        }
        else
        {
            Debug.LogError($"載入標籤 '{label}' 的資源位置失敗: {handle.OperationException}");
            OnAssetLoadFailed?.Invoke(label, handle.OperationException?.Message ?? "Unknown error");
        }
        
        Addressables.Release(handle);
        UpdatePreloadProgress();
    }
    
    /// <summary>
    /// 根據鍵值預載入資源
    /// </summary>
    private IEnumerator PreloadByKey(string key)
    {
        if (debugMode)
            Debug.Log($"開始預載入鍵值: {key}");
        
        yield return StartCoroutine(LoadAssetByKey(key));
        UpdatePreloadProgress();
    }
    
    /// <summary>
    /// 根據資源位置載入資產
    /// </summary>
    private IEnumerator LoadAssetByLocation(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation location)
    {
        var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(location);
        activeOperations.Add(handle);
        
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var asset = handle.Result;
            string key = location.PrimaryKey;
            
            loadedAssets[key] = asset;
            OnAssetLoaded?.Invoke(key);
            
            if (debugMode)
                Debug.Log($"成功載入資產: {key} ({asset.GetType().Name})");
        }
        else
        {
            Debug.LogError($"載入資產失敗: {location.PrimaryKey} - {handle.OperationException}");
            OnAssetLoadFailed?.Invoke(location.PrimaryKey, handle.OperationException?.Message ?? "Unknown error");
        }
        
        keyToHandle[location.PrimaryKey] = handle;
    }
    
    /// <summary>
    /// 根據鍵值載入資產
    /// </summary>
    private IEnumerator LoadAssetByKey(string key)
    {
        var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
        activeOperations.Add(handle);
        
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var asset = handle.Result;
            
            loadedAssets[key] = asset;
            OnAssetLoaded?.Invoke(key);
            
            if (debugMode)
                Debug.Log($"成功載入資產: {key} ({asset.GetType().Name})");
        }
        else
        {
            Debug.LogError($"載入資產失敗: {key} - {handle.OperationException}");
            OnAssetLoadFailed?.Invoke(key, handle.OperationException?.Message ?? "Unknown error");
        }
        
        keyToHandle[key] = handle;
    }
    
    /// <summary>
    /// 更新預載入進度
    /// </summary>
    private void UpdatePreloadProgress()
    {
        loadedPreloadItems++;
        preloadProgress = totalPreloadItems > 0 ? (float)loadedPreloadItems / totalPreloadItems : 1f;
        
        OnPreloadProgress?.Invoke(preloadProgress);
        
        if (debugMode)
            Debug.Log($"預載入進度: {loadedPreloadItems}/{totalPreloadItems} ({preloadProgress * 100:F1}%)");
    }
    
    /// <summary>
    /// 完成預載入
    /// </summary>
    private void CompletePreload()
    {
        isPreloading = false;
        preloadProgress = 1f;
        
        OnPreloadCompleted?.Invoke();
        
        if (debugMode)
            Debug.Log($"Addressable資源預載入完成，共載入 {loadedPreloadItems} 個資源");
    }
    
    /// <summary>
    /// 異步載入單個資產
    /// </summary>
    public void LoadAssetAsync<T>(string key, Action<T> onComplete, Action<string> onFailed = null) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAssetAsyncCoroutine(key, onComplete, onFailed));
    }
    
    /// <summary>
    /// 異步載入資產協程
    /// </summary>
    private IEnumerator LoadAssetAsyncCoroutine<T>(string key, Action<T> onComplete, Action<string> onFailed) where T : UnityEngine.Object
    {
        // 檢查是否已經載入
        if (HasAsset(key))
        {
            T cachedAsset = GetAsset<T>(key);
            if (cachedAsset != null)
            {
                onComplete?.Invoke(cachedAsset);
                yield break;
            }
        }
        
        var handle = Addressables.LoadAssetAsync<T>(key);
        activeOperations.Add(handle);
        
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var asset = handle.Result;
            loadedAssets[key] = asset;
            keyToHandle[key] = handle;
            
            onComplete?.Invoke(asset);
            OnAssetLoaded?.Invoke(key);
            
            if (debugMode)
                Debug.Log($"異步載入資產成功: {key}");
        }
        else
        {
            string error = handle.OperationException?.Message ?? "Unknown error";
            onFailed?.Invoke(error);
            OnAssetLoadFailed?.Invoke(key, error);
            
            Debug.LogError($"異步載入資產失敗: {key} - {error}");
        }
    }
    
    /// <summary>
    /// 獲取已載入的資產
    /// </summary>
    public T GetAsset<T>(string key) where T : UnityEngine.Object
    {
        return loadedAssets.TryGetValue(key, out UnityEngine.Object asset) ? asset as T : null;
    }
    
    /// <summary>
    /// 檢查資產是否已載入
    /// </summary>
    public bool HasAsset(string key)
    {
        return loadedAssets.ContainsKey(key);
    }
    
    /// <summary>
    /// 釋放指定資產
    /// </summary>
    public void ReleaseAsset(string key)
    {
        if (keyToHandle.TryGetValue(key, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            keyToHandle.Remove(key);
            activeOperations.Remove(handle);
        }
        
        // 從快取中移除
        loadedAssets.Remove(key);
        
        if (debugMode)
            Debug.Log($"釋放資產: {key}");
    }
    
    /// <summary>
    /// 釋放所有資產
    /// </summary>
    public void ReleaseAllAssets()
    {
        // 釋放所有操作句柄
        foreach (var handle in activeOperations)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        
        activeOperations.Clear();
        keyToHandle.Clear();
        
        // 清空快取
        loadedAssets.Clear();
        
        if (debugMode)
            Debug.Log("釋放所有Addressable資產");
    }
    
    /// <summary>
    /// 獲取載入統計資訊
    /// </summary>
    public string GetLoadingStats()
    {
        return $"已載入Addressable資產: {loadedAssets.Count}";
    }
    
    /// <summary>
    /// 添加預載入標籤
    /// </summary>
    public void AddPreloadLabel(string label)
    {
        if (!preloadLabels.Contains(label))
        {
            preloadLabels.Add(label);
            
            if (debugMode)
                Debug.Log($"添加預載入標籤: {label}");
        }
    }
    
    /// <summary>
    /// 添加預載入鍵值
    /// </summary>
    public void AddPreloadKey(string key)
    {
        if (!preloadKeys.Contains(key))
        {
            preloadKeys.Add(key);
            
            if (debugMode)
                Debug.Log($"添加預載入鍵值: {key}");
        }
    }
    
    /// <summary>
    /// 異步載入場景
    /// </summary>
    public void LoadSceneAsync(string sceneKey, LoadSceneMode loadMode, Action<float> onProgress = null, Action onComplete = null, Action<string> onFailed = null)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneKey, loadMode, onProgress, onComplete, onFailed));
    }
    
    /// <summary>
    /// 預載入場景（不激活）
    /// </summary>
    public void PreloadSceneAsync(string sceneKey, Action<float> onProgress = null, Action onComplete = null, Action<string> onFailed = null)
    {
        StartCoroutine(PreloadSceneAsyncCoroutine(sceneKey, onProgress, onComplete, onFailed));
    }
    
    /// <summary>
    /// 激活已預載入的場景
    /// </summary>
    public void ActivatePreloadedScene(string sceneKey, Action onComplete = null, Action<string> onFailed = null)
    {
        StartCoroutine(ActivatePreloadedSceneCoroutine(sceneKey, onComplete, onFailed));
    }
    
    /// <summary>
    /// 檢查場景是否已預載入
    /// </summary>
    public bool IsScenePreloaded(string sceneKey)
    {
        return keyToHandle.ContainsKey(sceneKey + "_preload");
    }
    
    /// <summary>
    /// 異步載入場景協程
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneKey, LoadSceneMode loadMode, Action<float> onProgress, Action onComplete, Action<string> onFailed)
    {
        if (debugMode)
            Debug.Log($"開始載入Addressable場景: {sceneKey}, 模式: {loadMode}");
        
        var handle = Addressables.LoadSceneAsync(sceneKey, loadMode);
        activeOperations.Add(handle);
        
        // 追蹤進度
        while (!handle.IsDone)
        {
            float progress = handle.PercentComplete;
            onProgress?.Invoke(progress);
            
            if (debugMode && Time.frameCount % 30 == 0) // 每30幀記錄一次進度
                Debug.Log($"Addressable場景載入進度: {progress * 100:F1}%");
            
            yield return null;
        }
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            onProgress?.Invoke(1f); // 確保進度為100%
            onComplete?.Invoke();
            
            if (debugMode)
                Debug.Log($"Addressable場景載入成功: {sceneKey}");
        }
        else
        {
            string error = handle.OperationException?.Message ?? "Unknown error";
            onFailed?.Invoke(error);
            
            Debug.LogError($"Addressable場景載入失敗: {sceneKey} - {error}");
        }
        
        keyToHandle[sceneKey] = handle;
    }
    
    /// <summary>
    /// 卸載場景
    /// </summary>
    public void UnloadSceneAsync(string sceneKey, Action onComplete = null, Action<string> onFailed = null)
    {
        StartCoroutine(UnloadSceneAsyncCoroutine(sceneKey, onComplete, onFailed));
    }
    
    /// <summary>
    /// 預載入場景協程（不激活）
    /// </summary>
    private IEnumerator PreloadSceneAsyncCoroutine(string sceneKey, Action<float> onProgress, Action onComplete, Action<string> onFailed)
    {
        if (debugMode)
            Debug.Log($"開始預載入Addressable場景: {sceneKey}（不激活）");
        
        string preloadKey = sceneKey + "_preload";
        
        // 檢查是否已經預載入
        if (keyToHandle.ContainsKey(preloadKey))
        {
            if (debugMode)
                Debug.Log($"場景 {sceneKey} 已經預載入，跳過");
            onProgress?.Invoke(1f);
            onComplete?.Invoke();
            yield break;
        }
        
        // 使用Additive模式預載入場景（不會自動激活）
        var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive);
        activeOperations.Add(handle);
        
        // 追蹤預載入進度
        while (!handle.IsDone)
        {
            float progress = handle.PercentComplete;
            onProgress?.Invoke(progress);
            
            if (debugMode && Time.frameCount % 30 == 0)
                Debug.Log($"Addressable場景預載入進度: {sceneKey} - {progress * 100:F1}%");
            
            yield return null;
        }
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 場景已載入但未激活，立即將其設為非活動狀態
            SceneInstance sceneInstance = (SceneInstance)handle.Result;
            Scene preloadedScene = sceneInstance.Scene;
            
            // 禁用場景中的所有根物件（實現"不激活"效果）
            GameObject[] rootObjects = preloadedScene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                rootObj.SetActive(false);
            }
            
            keyToHandle[preloadKey] = handle;
            
            onProgress?.Invoke(1f);
            onComplete?.Invoke();
            
            if (debugMode)
                Debug.Log($"Addressable場景預載入成功: {sceneKey}，場景已載入但未激活");
        }
        else
        {
            string error = handle.OperationException?.Message ?? "Unknown error";
            onFailed?.Invoke(error);
            
            Debug.LogError($"Addressable場景預載入失敗: {sceneKey} - {error}");
        }
    }
    
    /// <summary>
    /// 激活已預載入的場景協程
    /// </summary>
    private IEnumerator ActivatePreloadedSceneCoroutine(string sceneKey, Action onComplete, Action<string> onFailed)
    {
        if (debugMode)
            Debug.Log($"開始激活預載入的場景: {sceneKey}");
        
        string preloadKey = sceneKey + "_preload";
        
        // 檢查場景是否已預載入
        if (!keyToHandle.ContainsKey(preloadKey))
        {
            string error = $"場景 {sceneKey} 尚未預載入，無法激活";
            onFailed?.Invoke(error);
            Debug.LogError(error);
            yield break;
        }
        
        var handle = keyToHandle[preloadKey];
        SceneInstance sceneInstance = (SceneInstance)handle.Result;
        Scene preloadedScene = sceneInstance.Scene;
        
        // 獲取當前活動場景，準備卸載
        Scene currentScene = SceneManager.GetActiveScene();
        string currentSceneName = currentScene.name;
        bool needUnloadCurrentScene = currentScene.name != preloadedScene.name;
        
        if (debugMode)
            Debug.Log($"目前場景: {currentSceneName}, 預載入場景: {preloadedScene.name}, 需要卸載當前場景: {needUnloadCurrentScene}");
        
        // 1. 基本驗證
        if (!preloadedScene.IsValid())
        {
            string error = "預載入場景無效，無法激活";
            onFailed?.Invoke(error);
            Debug.LogError(error);
            yield break;
        }
        
        if (!preloadedScene.isLoaded)
        {
            string error = "預載入場景未載入，無法激活";
            onFailed?.Invoke(error);
            Debug.LogError(error);
            yield break;
        }

        // 2. 激活預載入的場景
        bool setActiveResult = SceneManager.SetActiveScene(preloadedScene);
        if (!setActiveResult)
        {
            string error = $"無法設置場景 {sceneKey} 為活動場景";
            onFailed?.Invoke(error);
            Debug.LogError(error);
            yield break;
        }
        
        // 3. 啟用場景中的所有根物件
        GameObject[] rootObjects = preloadedScene.GetRootGameObjects();
        foreach (GameObject rootObj in rootObjects)
        {
            rootObj.SetActive(true);
        }
        
        if (debugMode)
            Debug.Log($"場景激活成功: {sceneKey}，根物件數量: {rootObjects.Length}");
        
        // 4. 卸載原先的場景（在新場景激活後進行）
        if (needUnloadCurrentScene)
        {
            if (debugMode)
                Debug.Log($"開始卸載原先場景: {currentSceneName}");
            
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentScene);
            yield return unloadOp;
            
            if (unloadOp.isDone)
            {
                if (debugMode)
                    Debug.Log($"原先場景卸載完成: {currentSceneName}");
            }
            else
            {
                Debug.LogWarning($"原先場景卸載可能未完全完成: {currentSceneName}");
            }
        }
        
        // 5. 將預載入的handle轉移為正常的場景handle
        keyToHandle[sceneKey] = handle;
        keyToHandle.Remove(preloadKey);
        
        onComplete?.Invoke();
        
        if (debugMode)
            Debug.Log($"場景激活和清理完成: {sceneKey}");
    }
    
    /// <summary>
    /// 卸載場景協程
    /// </summary>
    private IEnumerator UnloadSceneAsyncCoroutine(string sceneKey, Action onComplete, Action<string> onFailed)
    {
        if (debugMode)
            Debug.Log($"開始卸載Addressable場景: {sceneKey}");
        
        // 檢查場景是否已載入
        if (!keyToHandle.ContainsKey(sceneKey))
        {
            string error = $"場景 {sceneKey} 未找到或未通過Addressable載入";
            onFailed?.Invoke(error);
            Debug.LogWarning(error);
            yield break;
        }
        
        var handle = Addressables.UnloadSceneAsync(keyToHandle[sceneKey]);
        
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            keyToHandle.Remove(sceneKey);
            onComplete?.Invoke();
            
            if (debugMode)
                Debug.Log($"Addressable場景卸載成功: {sceneKey}");
        }
        else
        {
            string error = handle.OperationException?.Message ?? "Unknown error";
            onFailed?.Invoke(error);
            
            Debug.LogError($"Addressable場景卸載失敗: {sceneKey} - {error}");
        }
        
        Addressables.Release(handle);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            ReleaseAllAssets();
            Instance = null;
        }
    }
    
    private void OnApplicationQuit()
    {
        if (Instance == this)
        {
            ReleaseAllAssets();
            Instance = null;
        }
    }
}
