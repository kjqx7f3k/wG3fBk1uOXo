using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 遊戲場景管理器 - 處理Addressable場景載入、切換和資源預載入
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    
    [Header("場景設定")]
    [SerializeField] private List<SceneData> sceneDatabase = new List<SceneData>();
    
    [Header("Addressable設定")]
    [SerializeField] private List<string> addressablePreloadLabels = new List<string>();
    [SerializeField] private List<string> addressablePreloadKeys = new List<string>();
    [SerializeField] private List<string> addressablePreloadScenes = new List<string>();
    [SerializeField] private bool preloadOnStart = true;
    
    [Header("除錯設定")]
    [SerializeField] private bool debugMode = false;
    
    // 當前場景資訊
    private string currentSceneName;
    private bool isLoading = false;
    private float loadingProgress = 0f;
    
    // 事件
    public event Action<string> OnSceneLoadStarted;
    public event Action<string, float> OnSceneLoadProgress;
    public event Action<string> OnSceneLoadCompleted;
    public event Action OnPreloadCompleted;
    
    public string CurrentSceneName => currentSceneName;
    public bool IsLoading => isLoading;
    public float LoadingProgress => loadingProgress;
    
    [System.Serializable]
    public class SceneData
    {
        public string sceneName;
        public string displayName;
        public string description;
        public Sprite sceneIcon;
        public string addressableKey = "";
        public string addressableLabel = "";
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 獲取當前場景名稱
            currentSceneName = SceneManager.GetActiveScene().name;
            
            if (debugMode)
                Debug.Log($"GameSceneManager 初始化，當前場景: {currentSceneName}");
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
            StartCoroutine(PreloadAddressableResources());
        }
    }
    
    /// <summary>
    /// 載入Addressable場景
    /// </summary>
    /// <param name="sceneName">場景名稱</param>
    /// <param name="useLoadingScreen">是否使用載入畫面</param>
    public void LoadScene(string sceneName, bool useLoadingScreen = true)
    {
        if (isLoading)
        {
            Debug.LogWarning("場景正在載入中，請稍後再試");
            return;
        }
        
        if (AddressableResourceManager.Instance == null)
        {
            Debug.LogError("AddressableResourceManager 不存在，無法載入場景");
            return;
        }
        
        SceneData sceneData = GetSceneData(sceneName);
        if (sceneData == null)
        {
            // 如果場景不在資料庫中，創建一個臨時的場景資料
            sceneData = new SceneData
            {
                sceneName = sceneName,
                displayName = sceneName,
                description = "自動生成的場景資料",
                addressableKey = sceneName
            };
            
            if (debugMode)
                Debug.LogWarning($"場景 '{sceneName}' 不在資料庫中，創建臨時場景資料");
        }
        
        if (debugMode)
            Debug.Log($"開始載入Addressable場景: {sceneName}");
        
        if (useLoadingScreen)
        {
            StartCoroutine(LoadSceneWithLoadingScreen(sceneName, sceneData));
        }
        else
        {
            StartCoroutine(LoadSceneDirectly(sceneName, sceneData));
        }
    }
    
    /// <summary>
    /// 使用載入畫面載入Addressable場景
    /// </summary>
    private IEnumerator LoadSceneWithLoadingScreen(string targetSceneName, SceneData sceneData)
    {
        isLoading = true;
        // OnSceneLoadStarted?.Invoke(targetSceneName);
        
        // 等待LoadingScreenManager顯示載入UI
        // yield return new WaitForSeconds(0.1f);
        
        yield return StartCoroutine(LoadAddressableScene(targetSceneName, sceneData));
        
        // 完成載入
        currentSceneName = targetSceneName;
        isLoading = false;
        loadingProgress = 0f;
        
        if (debugMode)
            Debug.Log($"場景載入完成: {targetSceneName}");
    }
    
    /// <summary>
    /// 載入Addressable場景（支援快速激活預載入場景）
    /// </summary>
    private IEnumerator LoadAddressableScene(string targetSceneName, SceneData sceneData)
    {
        if (debugMode)
            Debug.Log($"使用Addressable載入場景: {targetSceneName}, Key: {sceneData.addressableKey}");
        
        string sceneKey = !string.IsNullOrEmpty(sceneData.addressableKey) ? sceneData.addressableKey : targetSceneName;
        
        // 🎯 檢查場景是否已預載入
        if (AddressableResourceManager.Instance.IsScenePreloaded(sceneKey))
        {
            if (debugMode)
                Debug.Log($"場景 {sceneKey} 已預載入，使用瞬間激活（無載入畫面）");
            
            yield return StartCoroutine(ActivatePreloadedAddressableSceneInstantly(targetSceneName, sceneKey));
        }
        else
        {
            if (debugMode)
                Debug.Log($"場景 {sceneKey} 未預載入，使用直接載入");
            
            yield return StartCoroutine(LoadAddressableSceneDirectly(targetSceneName, sceneKey));
        }
    }
    
    /// <summary>
    /// 瞬間激活已預載入的Addressable場景（無載入畫面）
    /// </summary>
    private IEnumerator ActivatePreloadedAddressableSceneInstantly(string targetSceneName, string sceneKey)
    {
        if (debugMode)
            Debug.Log($"瞬間激活預載入場景: {sceneKey}（無載入畫面）");
        
        bool activationCompleted = false;
        bool activationFailed = false;
        string errorMessage = "";
        
        // 直接激活預載入的場景，不顯示任何載入進度
        AddressableResourceManager.Instance.ActivatePreloadedScene(
            sceneKey,
            () => {
                activationCompleted = true;
                if (debugMode)
                    Debug.Log($"GameSceneManager: 預載入場景瞬間激活完成");
            },
            (error) => {
                activationFailed = true;
                errorMessage = error;
                Debug.LogError($"GameSceneManager: 預載入場景激活失敗: {error}");
            }
        );
        
        // 等待激活完成或失敗
        while (!activationCompleted && !activationFailed)
        {
            yield return null;
        }
        
        if (activationFailed)
        {
            Debug.LogWarning($"預載入場景激活失敗，嘗試直接載入: {errorMessage}");
            yield return StartCoroutine(LoadAddressableSceneDirectly(targetSceneName, sceneKey));
        }
        else
        {
            // 瞬間完成，不需要任何載入畫面或進度模擬
            if (debugMode)
                Debug.Log($"場景瞬間切換完成: {targetSceneName}");
        }
    }
    
    /// <summary>
    /// 激活已預載入的Addressable場景（帶載入畫面）
    /// </summary>
    private IEnumerator ActivatePreloadedAddressableScene(string targetSceneName, string sceneKey)
    {
        if (debugMode)
            Debug.Log($"快速激活預載入場景: {sceneKey}");
        
        bool activationCompleted = false;
        bool activationFailed = false;
        string errorMessage = "";
        
        // 模擬載入進度（因為場景已預載入，這裡只是UI效果）
        float simulatedProgress = 0f;
        while (simulatedProgress < 0.9f)
        {
            simulatedProgress += 0.1f; // 快速進度
            loadingProgress = simulatedProgress;
            OnSceneLoadProgress?.Invoke(targetSceneName, loadingProgress);
            
            if (debugMode)
                Debug.Log($"GameSceneManager: 模擬激活進度 {loadingProgress * 100:F1}%");
            
            yield return new WaitForSeconds(0.05f); // 很短的等待時間
        }
        
        // 激活預載入的場景
        AddressableResourceManager.Instance.ActivatePreloadedScene(
            sceneKey,
            () => {
                activationCompleted = true;
                if (debugMode)
                    Debug.Log($"GameSceneManager: 預載入場景激活完成");
            },
            (error) => {
                activationFailed = true;
                errorMessage = error;
                Debug.LogError($"GameSceneManager: 預載入場景激活失敗: {error}");
            }
        );
        
        // 等待激活完成或失敗
        while (!activationCompleted && !activationFailed)
        {
            yield return null;
        }
        
        if (activationFailed)
        {
            Debug.LogWarning($"預載入場景激活失敗，嘗試直接載入: {errorMessage}");
            yield return StartCoroutine(LoadAddressableSceneDirectly(targetSceneName, sceneKey));
        }
        else
        {
            // 模擬最終進度到100%
            yield return StartCoroutine(SimulateFinalProgress(targetSceneName));
        }
    }
    
    /// <summary>
    /// 直接載入Addressable場景
    /// </summary>
    private IEnumerator LoadAddressableSceneDirectly(string targetSceneName, string sceneKey)
    {
        if (debugMode)
            Debug.Log($"直接載入Addressable場景: {sceneKey}");
        
        bool sceneLoadCompleted = false;
        bool sceneLoadFailed = false;
        string errorMessage = "";
        
        AddressableResourceManager.Instance.LoadSceneAsync(
            sceneKey,
            LoadSceneMode.Single,
            (progress) => {
                loadingProgress = progress;
                OnSceneLoadProgress?.Invoke(targetSceneName, loadingProgress);
                if (debugMode)
                    Debug.Log($"GameSceneManager: Addressable載入進度 {loadingProgress * 100:F1}%");
            },
            () => {
                sceneLoadCompleted = true;
                if (debugMode)
                    Debug.Log($"GameSceneManager: Addressable場景載入完成");
            },
            (error) => {
                sceneLoadFailed = true;
                errorMessage = error;
                Debug.LogError($"GameSceneManager: Addressable場景載入失敗: {error}");
            }
        );
        
        // 等待載入完成或失敗
        while (!sceneLoadCompleted && !sceneLoadFailed)
        {
            yield return null;
        }
        
        if (sceneLoadFailed)
        {
            Debug.LogError($"Addressable場景載入失敗: {errorMessage}");
            yield break;
        }
        
        // 模擬最終進度到100%
        yield return StartCoroutine(SimulateFinalProgress(targetSceneName));
    }
    
    /// <summary>
    /// 模擬最終進度到100%
    /// </summary>
    private IEnumerator SimulateFinalProgress(string targetSceneName)
    {
        // 模擬更平滑的進度過渡到100%
        // float finalProgress = 0.9f;
        // while (finalProgress < 1f)
        // {
        //     finalProgress += 0.05f; // 每次增加5%
        //     finalProgress = Mathf.Clamp01(finalProgress);
        //     loadingProgress = finalProgress;
        //     OnSceneLoadProgress?.Invoke(targetSceneName, loadingProgress);
            
        //     if (debugMode)
        //         Debug.Log($"GameSceneManager: 模擬最終進度 {loadingProgress * 100:F1}%");
            
        //     yield return new WaitForSeconds(0.1f); // 每0.1秒更新一次
        // }
        
        // 確保顯示100%
        // float loadingProgress = 1f;
        // OnSceneLoadProgress?.Invoke(targetSceneName, loadingProgress);
        // if (debugMode)
        //     Debug.Log($"GameSceneManager: 載入完成，等待1秒讓用戶看到100%");
        
        // 等待1秒讓用戶看到100%的進度
        yield return new WaitForSeconds(0f);
        
        // 觸發載入完成事件（這會讓LoadingScreenManager隱藏UI）
        // OnSceneLoadCompleted?.Invoke(targetSceneName);
        
        // 再等待1秒讓LoadingScreenManager隱藏UI
        // yield return new WaitForSeconds(0f);
    }
    
    /// <summary>
    /// 直接載入場景（無載入畫面）
    /// </summary>
    private IEnumerator LoadSceneDirectly(string targetSceneName, SceneData sceneData)
    {
        isLoading = true;
        OnSceneLoadStarted?.Invoke(targetSceneName);
        
        yield return StartCoroutine(LoadAddressableSceneDirectly(targetSceneName, 
            !string.IsNullOrEmpty(sceneData.addressableKey) ? sceneData.addressableKey : targetSceneName));
        
        currentSceneName = targetSceneName;
        isLoading = false;
        loadingProgress = 0f;
        
        OnSceneLoadCompleted?.Invoke(targetSceneName);
        
        if (debugMode)
            Debug.Log($"場景直接載入完成: {targetSceneName}");
    }
    
    /// <summary>
    /// 預載入Addressable資源
    /// </summary>
    private IEnumerator PreloadAddressableResources()
    {
        if (AddressableResourceManager.Instance == null)
        {
            Debug.LogError("AddressableResourceManager 不存在，無法預載入資源");
            yield break;
        }
        
        var addressableManager = AddressableResourceManager.Instance;
        
        // 添加預載入標籤和鍵值到AddressableResourceManager
        foreach (string label in addressablePreloadLabels)
        {
            addressableManager.AddPreloadLabel(label);
        }
        
        foreach (string key in addressablePreloadKeys)
        {
            addressableManager.AddPreloadKey(key);
        }
        
        // 監聽Addressable預載入事件
        bool addressablePreloadCompleted = false;
        addressableManager.OnPreloadCompleted += () => addressablePreloadCompleted = true;
        
        // 開始Addressable預載入
        addressableManager.StartPreload();
        
        // 等待Addressable預載入完成
        while (!addressablePreloadCompleted)
        {
            yield return null;
        }
        
        // 🎯 預載入Addressable場景（不激活）
        yield return StartCoroutine(PreloadAddressableScenes());
        
        OnPreloadCompleted?.Invoke();
        
        if (debugMode)
            Debug.Log("Addressable資源預載入完成");
    }
    
    /// <summary>
    /// 預載入Addressable場景（不激活）
    /// </summary>
    private IEnumerator PreloadAddressableScenes()
    {
        if (debugMode)
            Debug.Log($"開始預載入 {addressablePreloadScenes.Count} 個Addressable場景");
        
        var addressableManager = AddressableResourceManager.Instance;
        int completedScenes = 0;
        int totalScenes = addressablePreloadScenes.Count;
        
        foreach (string sceneKey in addressablePreloadScenes)
        {
            if (debugMode)
                Debug.Log($"預載入場景: {sceneKey} ({completedScenes + 1}/{totalScenes})");
            
            bool scenePreloadCompleted = false;
            bool scenePreloadFailed = false;
            
            addressableManager.PreloadSceneAsync(
                sceneKey,
                (progress) => {
                    if (debugMode && Time.frameCount % 60 == 0) // 每60幀記錄一次
                        Debug.Log($"場景 {sceneKey} 預載入進度: {progress * 100:F1}%");
                },
                () => {
                    scenePreloadCompleted = true;
                    if (debugMode)
                        Debug.Log($"場景 {sceneKey} 預載入完成");
                },
                (error) => {
                    scenePreloadFailed = true;
                    Debug.LogError($"場景 {sceneKey} 預載入失敗: {error}");
                }
            );
            
            // 等待當前場景預載入完成
            while (!scenePreloadCompleted && !scenePreloadFailed)
            {
                yield return null;
            }
            
            completedScenes++;
        }
        
        if (debugMode)
            Debug.Log($"所有Addressable場景預載入完成: {completedScenes}/{totalScenes}");
    }
    
    /// <summary>
    /// 獲取場景資料
    /// </summary>
    private SceneData GetSceneData(string sceneName)
    {
        return sceneDatabase.Find(data => data.sceneName == sceneName);
    }
    
    /// <summary>
    /// 獲取Addressable資產
    /// </summary>
    public T GetAddressableAsset<T>(string key) where T : UnityEngine.Object
    {
        if (AddressableResourceManager.Instance != null)
        {
            return AddressableResourceManager.Instance.GetAsset<T>(key);
        }
        return null;
    }
    
    /// <summary>
    /// 異步載入Addressable資產
    /// </summary>
    public void LoadAddressableAssetAsync<T>(string key, System.Action<T> onComplete, System.Action<string> onFailed = null) where T : UnityEngine.Object
    {
        if (AddressableResourceManager.Instance != null)
        {
            AddressableResourceManager.Instance.LoadAssetAsync(key, onComplete, onFailed);
        }
        else
        {
            onFailed?.Invoke("AddressableResourceManager不存在");
        }
    }
    
    /// <summary>
    /// 檢查Addressable資產是否已載入
    /// </summary>
    public bool HasAddressableAsset(string key)
    {
        if (AddressableResourceManager.Instance != null)
        {
            return AddressableResourceManager.Instance.HasAsset(key);
        }
        return false;
    }
    
    /// <summary>
    /// 釋放Addressable資產
    /// </summary>
    public void ReleaseAddressableAsset(string key)
    {
        if (AddressableResourceManager.Instance != null)
        {
            AddressableResourceManager.Instance.ReleaseAsset(key);
        }
    }
    
    /// <summary>
    /// 獲取資源載入統計資訊
    /// </summary>
    public string GetResourceLoadingStats()
    {
        if (AddressableResourceManager.Instance != null)
        {
            return AddressableResourceManager.Instance.GetLoadingStats();
        }
        return "AddressableResourceManager不存在";
    }
    
    /// <summary>
    /// 重新載入當前場景
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// 檢查場景是否存在
    /// </summary>
    public bool SceneExists(string sceneName)
    {
        return GetSceneData(sceneName) != null;
    }
    
    /// <summary>
    /// 獲取所有可用場景
    /// </summary>
    public List<SceneData> GetAllScenes()
    {
        return new List<SceneData>(sceneDatabase);
    }
    
    /// <summary>
    /// 添加場景到資料庫
    /// </summary>
    public void AddSceneToDatabase(SceneData sceneData)
    {
        if (GetSceneData(sceneData.sceneName) == null)
        {
            sceneDatabase.Add(sceneData);
            
            if (debugMode)
                Debug.Log($"添加場景到資料庫: {sceneData.sceneName}");
        }
    }
    
    /// <summary>
    /// 從資料庫移除場景
    /// </summary>
    public void RemoveSceneFromDatabase(string sceneName)
    {
        SceneData sceneData = GetSceneData(sceneName);
        if (sceneData != null)
        {
            sceneDatabase.Remove(sceneData);
            
            if (debugMode)
                Debug.Log($"從資料庫移除場景: {sceneName}");
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnApplicationQuit()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
