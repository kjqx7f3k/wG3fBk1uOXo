using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 載入畫面管理器 - 顯示載入進度和提示（持久化單例）
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }
    
    [Header("載入UI Prefab")]
    [SerializeField] private GameObject loadingUIPrefab;
    

    private Slider progressBar;
    private TextMeshProUGUI progressText;
    private TextMeshProUGUI loadingTipText;
    private TextMeshProUGUI sceneNameText;
    private Image backgroundImage;
    private GameObject loadingIcon;
    
    // 當前載入UI實例
    private GameObject currentLoadingUI;
    private Canvas loadingCanvas;
    
    [Header("載入提示")]
    [SerializeField] private string[] loadingTips = {
        "探索每個角落，你可能會發現隱藏的寶藏...",
        "與NPC對話可以獲得有用的資訊和任務...",
        "合理管理你的背包空間，丟棄不需要的物品...",
        "某些區域可能需要特定的物品才能進入...",
        "注意觀察環境，線索往往隱藏在細節中...",
        "不同的選擇會導致不同的結果...",
        "保存遊戲進度，避免意外丟失...",
        "嘗試不同的互動方式，可能會有驚喜..."
    };
    
    [Header("動畫設定")]
    [SerializeField] private float tipChangeInterval = 3f;
    [SerializeField] private float iconRotationSpeed = 90f;
    [SerializeField] private AnimationCurve progressAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private string currentSceneName = "";
    private Coroutine tipRotationCoroutine;
    private Coroutine progressAnimationCoroutine;
    private bool isLoadingUIActive = false;
    
    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("LoadingScreenManager: 初始化為持久化單例");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 延遲綁定事件，確保GameSceneManager已經初始化
        //RefreshUIReferences();
        StartCoroutine(DelayedEventBinding());
        
    }
    
    /// <summary>
    /// 延遲綁定事件
    /// </summary>
    private IEnumerator DelayedEventBinding()
    {
        // 等待幾幀確保GameSceneManager初始化
        yield return new WaitForSeconds(0.1f);
        
        // // 嘗試綁定事件
        // if (GameSceneManager.Instance != null)
        // {
        //     GameSceneManager.Instance.OnSceneLoadStarted += OnSceneLoadStarted;
        //     GameSceneManager.Instance.OnSceneLoadProgress += OnSceneLoadProgress;
        //     GameSceneManager.Instance.OnSceneLoadCompleted += OnSceneLoadCompleted;
        //     Debug.Log("LoadingScreenManager: 成功綁定GameSceneManager事件");
        // }
        // else
        // {
        //     Debug.LogError("LoadingScreenManager: 找不到GameSceneManager實例！啟動備用進度模擬");
        //     // 如果找不到GameSceneManager，啟動備用進度模擬
        //     StartCoroutine(FallbackProgressSimulation());
        // }
    }
    
    /// <summary>
    /// 備用進度模擬（當GameSceneManager不可用時）
    /// </summary>
    private IEnumerator FallbackProgressSimulation()
    {
        Debug.Log("LoadingScreenManager: 啟動備用進度模擬");
        
        float simulatedProgress = 0f;
        float progressSpeed = 0.3f; // 每秒增加30%
        
        while (simulatedProgress < 1f)
        {
            simulatedProgress += progressSpeed * Time.unscaledDeltaTime;
            simulatedProgress = Mathf.Clamp01(simulatedProgress);
            
            targetProgress = simulatedProgress;
            
            // 啟動進度條動畫
            if (progressAnimationCoroutine != null)
            {
                StopCoroutine(progressAnimationCoroutine);
            }
            progressAnimationCoroutine = StartCoroutine(AnimateProgress());
            
            Debug.Log($"LoadingScreenManager: 模擬進度 {simulatedProgress * 100:F1}%");
            
            yield return null;
        }
        
        // 確保顯示100%
        SetProgress(1f);
        Debug.Log("LoadingScreenManager: 備用進度模擬完成");
    }
    
    private void Update()
    {
        // 旋轉載入圖標
        if (loadingIcon != null)
        {
            loadingIcon.transform.Rotate(0, 0, -iconRotationSpeed * Time.unscaledDeltaTime);
        }
    }
    
    /// <summary>
    /// 初始化載入畫面
    /// </summary>
    private void InitializeLoadingScreen()
    {
        // 設置初始狀態
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        
        if (progressText != null)
        {
            progressText.text = "0%";
        }
        
        if (sceneNameText != null)
        {
            sceneNameText.text = "載入中...";
        }
        
        // 開始提示輪播
        if (loadingTips.Length > 0)
        {
            tipRotationCoroutine = StartCoroutine(RotateLoadingTips());
        }
        
        Debug.Log("載入畫面初始化完成");
    }
    
    /// <summary>
    /// 顯示載入UI
    /// </summary>
    public void ShowLoadingUI()
    {
        if (isLoadingUIActive) return;
        
        if (loadingUIPrefab != null)
        {
            // 實例化載入UI Prefab
            currentLoadingUI = Instantiate(loadingUIPrefab);
            
            // 確保載入UI在最上層
            loadingCanvas = currentLoadingUI.GetComponent<Canvas>();
            if (loadingCanvas == null)
            {
                loadingCanvas = currentLoadingUI.AddComponent<Canvas>();
            }
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 1000; // 確保在最上層
            
            // 添加GraphicRaycaster以支援UI互動
            if (currentLoadingUI.GetComponent<GraphicRaycaster>() == null)
            {
                currentLoadingUI.AddComponent<GraphicRaycaster>();
            }
            
            // 重新獲取UI組件引用
            RefreshUIReferences();
            
            // 初始化載入畫面
            InitializeLoadingScreen();
            
            isLoadingUIActive = true;
            Debug.Log("LoadingScreenManager: 顯示載入UI");
        }
        else
        {
            Debug.LogWarning("LoadingScreenManager: 載入UI Prefab未設置！");
        }
    }
    
    /// <summary>
    /// 隱藏載入UI
    /// </summary>
    public void HideLoadingUI()
    {
        if (!isLoadingUIActive) return;
        
        // 停止所有協程
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
            tipRotationCoroutine = null;
        }
        
        if (progressAnimationCoroutine != null)
        {
            StopCoroutine(progressAnimationCoroutine);
            progressAnimationCoroutine = null;
        }
        
        // 銷毀載入UI
        if (currentLoadingUI != null)
        {
            Destroy(currentLoadingUI);
            currentLoadingUI = null;
            loadingCanvas = null;
        }
        
        // 清空UI組件引用
        ClearUIReferences();
        
        isLoadingUIActive = false;
        Debug.Log("LoadingScreenManager: 隱藏載入UI");
    }
    
    /// <summary>
    /// 重新獲取UI組件引用
    /// </summary>
    private void RefreshUIReferences()
    {
        if (currentLoadingUI == null) return;
        
        // 在載入UI中尋找組件
        progressBar = loadingCanvas.GetComponentInChildren<Slider>();
        progressText = FindUIComponent<TextMeshProUGUI>(currentLoadingUI, "ProgressText");
        loadingTipText = FindUIComponent<TextMeshProUGUI>(currentLoadingUI, "LoadingTipText");
        sceneNameText = FindUIComponent<TextMeshProUGUI>(currentLoadingUI, "SceneNameText");
        backgroundImage = FindUIComponent<Image>(currentLoadingUI, "BackgroundImage");
        loadingIcon = FindUIGameObject(currentLoadingUI, "LoadingIcon");
        
        Debug.Log($"LoadingScreenManager: UI組件引用更新完成 - ProgressBar: {progressBar != null}, ProgressText: {progressText != null}");
    }
    
    /// <summary>
    /// 清空UI組件引用
    /// </summary>
    private void ClearUIReferences()
    {
        progressBar = null;
        progressText = null;
        loadingTipText = null;
        sceneNameText = null;
        backgroundImage = null;
        loadingIcon = null;
    }
    
    /// <summary>
    /// 尋找UI組件
    /// </summary>
    private T FindUIComponent<T>(GameObject parent, string name) where T : Component
    {
        Transform found = parent.transform.Find(name);
        if (found != null)
        {
            return found.GetComponent<T>();
        }
        
        // 如果直接尋找失敗，遞歸搜尋子物件
        T[] components = parent.GetComponentsInChildren<T>();
        foreach (T component in components)
        {
            if (component.name == name)
            {
                return component;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 尋找UI GameObject
    /// </summary>
    private GameObject FindUIGameObject(GameObject parent, string name)
    {
        Transform found = parent.transform.Find(name);
        if (found != null)
        {
            return found.gameObject;
        }
        
        // 遞歸搜尋子物件
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 場景載入開始事件
    /// </summary>
    private void OnSceneLoadStarted(string sceneName)
    {
        currentSceneName = sceneName;
        
        // 顯示載入UI
        ShowLoadingUI();
        
        if (sceneNameText != null)
        {
            sceneNameText.text = $"載入場景: {sceneName}";
        }
        
        Debug.Log($"載入畫面: 開始載入場景 {sceneName}");
    }
    
    /// <summary>
    /// 場景載入進度事件
    /// </summary>
    private void OnSceneLoadProgress(string sceneName, float progress)
    {
        Debug.Log($"LoadingScreenManager: 收到進度事件 - 場景: {sceneName}, 進度: {progress * 100:F1}%");
        
        targetProgress = progress;
        
        // 啟動進度條動畫
        if (progressAnimationCoroutine != null)
        {
            StopCoroutine(progressAnimationCoroutine);
        }
        progressAnimationCoroutine = StartCoroutine(AnimateProgress());
    }
    
    /// <summary>
    /// 場景載入完成事件
    /// </summary>
    private void OnSceneLoadCompleted(string sceneName)
    {
        Debug.Log($"載入畫面: 場景 {sceneName} 載入完成");
        
        // 停止提示輪播
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
            tipRotationCoroutine = null;
        }
        
        // 確保進度條顯示100%
        SetProgress(1f);
        
        // 顯示載入完成訊息
        if (loadingTipText != null)
        {
            loadingTipText.text = "載入完成！正在進入場景...";
        }
        
        // 延遲隱藏載入UI，讓用戶看到完成狀態
        StartCoroutine(DelayedHideLoadingUI());
    }
    
    /// <summary>
    /// 延遲隱藏載入UI
    /// </summary>
    private IEnumerator DelayedHideLoadingUI()
    {
        // 等待一段時間讓用戶看到100%和完成訊息
        yield return new WaitForSeconds(1f);
        
        // 隱藏載入UI
        HideLoadingUI();
    }
    
    /// <summary>
    /// 輪播載入提示
    /// </summary>
    private IEnumerator RotateLoadingTips()
    {
        int currentTipIndex = 0;
        
        while (true)
        {
            if (loadingTipText != null && loadingTips.Length > 0)
            {
                loadingTipText.text = loadingTips[currentTipIndex];
                currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
            }
            
            yield return new WaitForSecondsRealtime(tipChangeInterval);
        }
    }
    
    /// <summary>
    /// 進度條動畫
    /// </summary>
    private IEnumerator AnimateProgress()
    {
        float startProgress = currentProgress;
        float animationTime = 0f;
        float animationDuration = 0.5f;
        
        while (animationTime < animationDuration)
        {
            animationTime += Time.unscaledDeltaTime;
            float normalizedTime = animationTime / animationDuration;
            float curveValue = progressAnimationCurve.Evaluate(normalizedTime);
            
            currentProgress = Mathf.Lerp(startProgress, targetProgress, curveValue);
            SetProgress(currentProgress);
            
            yield return null;
        }
        
        currentProgress = targetProgress;
        SetProgress(currentProgress);
    }
    
    /// <summary>
    /// 設置進度顯示
    /// </summary>
    private void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        
        if (progressText != null)
        {
            progressText.text = $"{progress * 100:F0}%";
        }
    }
    
    /// <summary>
    /// 設置背景圖片
    /// </summary>
    public void SetBackgroundImage(Sprite backgroundSprite)
    {
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
        }
    }
    
    /// <summary>
    /// 設置場景名稱顯示
    /// </summary>
    public void SetSceneName(string sceneName)
    {
        currentSceneName = sceneName;
        
        if (sceneNameText != null)
        {
            sceneNameText.text = $"載入場景: {sceneName}";
        }
    }
    
    /// <summary>
    /// 添加自定義載入提示
    /// </summary>
    public void AddLoadingTip(string tip)
    {
        if (!string.IsNullOrEmpty(tip))
        {
            System.Array.Resize(ref loadingTips, loadingTips.Length + 1);
            loadingTips[loadingTips.Length - 1] = tip;
        }
    }
    
    /// <summary>
    /// 設置載入提示
    /// </summary>
    public void SetLoadingTips(string[] tips)
    {
        if (tips != null && tips.Length > 0)
        {
            loadingTips = tips;
        }
    }
    
    /// <summary>
    /// 顯示特定訊息
    /// </summary>
    public void ShowMessage(string message)
    {
        if (loadingTipText != null)
        {
            loadingTipText.text = message;
        }
    }
    
    /// <summary>
    /// 設置載入圖標可見性
    /// </summary>
    public void SetLoadingIconVisible(bool visible)
    {
        if (loadingIcon != null)
        {
            loadingIcon.SetActive(visible);
        }
    }
    
    private void OnDestroy()
    {
        // // 取消事件監聽
        // if (GameSceneManager.Instance != null)
        // {
        //     GameSceneManager.Instance.OnSceneLoadStarted -= OnSceneLoadStarted;
        //     GameSceneManager.Instance.OnSceneLoadProgress -= OnSceneLoadProgress;
        //     GameSceneManager.Instance.OnSceneLoadCompleted -= OnSceneLoadCompleted;
        // }
        
        // 停止協程
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
        }
        
        if (progressAnimationCoroutine != null)
        {
            StopCoroutine(progressAnimationCoroutine);
        }
    }
}
