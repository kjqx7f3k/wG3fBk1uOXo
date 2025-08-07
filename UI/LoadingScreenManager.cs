using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 載入畫面管理器 - 顯示載入提示和視覺效果（Prefab 掛載模式）
/// </summary>
public class LoadingScreenManager : UIPanel
{
    public static LoadingScreenManager Instance { get; private set; }
    
    [Header("UI 組件引用")]
    [SerializeField] private TextMeshProUGUI loadingTipText;
    [SerializeField] private TextMeshProUGUI sceneNameText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject loadingIcon;
    
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
    
    private string currentSceneName = "";
    private Coroutine tipRotationCoroutine;
    
    protected override void Awake()
    {
        base.Awake(); // 呼叫基底類別的Awake
        
        // 設定UIPanel屬性
        pauseGameWhenOpen = false;      // 載入畫面不暫停遊戲
        blockCharacterMovement = true;  // 阻擋角色移動
        canCloseWithEscape = false;     // 不能用ESC關閉
        
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
        // 確保面板初始為關閉狀態
        if (panelCanvas != null)
        {
            panelCanvas.enabled = false;
        }
        isOpen = false;
        Debug.Log("LoadingScreenManager: 初始化為關閉狀態");
    }
    
    private void Update()
    {
        // 旋轉載入圖標
        if (loadingIcon != null && IsOpen)
        {
            loadingIcon.transform.Rotate(0, 0, -iconRotationSpeed * Time.unscaledDeltaTime);
        }
    }
    
    /// <summary>
    /// 面板開啟時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnOpened()
    {
        base.OnOpened();
        Debug.Log("載入畫面已開啟");
        
        InitializeLoadingScreen();
    }
    
    /// <summary>
    /// 面板關閉時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnClosed()
    {
        base.OnClosed();
        Debug.Log("載入畫面已關閉");
        
        // 停止提示輪播
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
            tipRotationCoroutine = null;
        }
    }
    
    /// <summary>
    /// 初始化載入畫面
    /// </summary>
    private void InitializeLoadingScreen()
    {
        // 設置初始狀態
        if (sceneNameText != null)
        {
            if (!string.IsNullOrEmpty(currentSceneName))
            {
                sceneNameText.text = $"載入場景: {currentSceneName}";
            }
            else
            {
                sceneNameText.text = "載入中...";
            }
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
        Open();
    }
    
    /// <summary>
    /// 隱藏載入UI
    /// </summary>
    public void HideLoadingUI()
    {
        Close();
    }
    
    /// <summary>
    /// 場景載入開始事件
    /// </summary>
    public void OnSceneLoadStarted(string sceneName)
    {
        currentSceneName = sceneName;
        
        // 顯示載入UI
        ShowLoadingUI();
        
        Debug.Log($"載入畫面: 開始載入場景 {sceneName}");
    }
    
    /// <summary>
    /// 場景載入完成事件
    /// </summary>
    public void OnSceneLoadCompleted(string sceneName)
    {
        Debug.Log($"載入畫面: 場景 {sceneName} 載入完成");
        
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
        // 等待一段時間讓用戶看到完成訊息
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
        // 停止協程
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
        }
    }
}