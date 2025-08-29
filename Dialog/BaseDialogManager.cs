using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System;

/// <summary>
/// 對話管理器基礎類，包含所有共同功能
/// </summary>
public abstract class BaseDialogManager : UIPanel
{
    [Header("UI 組件引用")]
    [SerializeField] protected TextMeshProUGUI dialogText;
    [SerializeField] protected TextMeshProUGUI optionsText; // 用於顯示選項的TextMeshProUGUI
    [SerializeField] protected Image dialogBackground;
    
    public bool IsInDialog => IsOpen;
    
    [SerializeField] protected float typingSpeed = 0.02f;
    
    [Header("終端效果設定")]
    [SerializeField] protected bool enableTerminalCursor = true;
    [SerializeField] protected string cursorCharacter = "█";
    [SerializeField] protected float cursorBlinkSpeed = 1.0f;
    [SerializeField] protected Color cursorColor = Color.white;
    
    [Header("文字控制設定")]
    [SerializeField] protected bool enableTextControl = true;
    
    [Header("導航設定")]
    [SerializeField] protected float navigationCooldown = 0.2f; // 導航冷卻時間（秒）
    protected float lastNavigationTime = 0f; // 上次導航的時間
    
    [Header("多語言設定")]
    [SerializeField] protected string dialogBasePath = "Dialogs"; // 對話文件基礎路徑
    [SerializeField] protected string defaultLanguage = "en"; // 預設語言代碼
    
    // 動態文字控制變數
    protected float currentTypingSpeed;
    protected float currentCursorBlinkSpeed;
    
    protected bool isDisplayingDialog = false;
    protected Dictionary<int, DialogLine> dialogLines = new Dictionary<int, DialogLine>();
    protected GameObject currentModel;
    protected int currentDialogId = -1;
    protected List<DialogOption> currentOptions = new List<DialogOption>();
    protected int selectedOptionIndex = 0;
    
    // 對話終端游標效果變數
    protected bool isDialogCursorVisible = true;
    protected Coroutine dialogCursorBlinkCoroutine;
    protected string currentDisplayText = "";
    protected bool isTypingComplete = false;
    
    // 選項終端游標效果變數
    protected bool isDisplayingOptions = false;
    protected bool isOptionsCursorVisible = true;
    protected Coroutine optionsCursorBlinkCoroutine;
    protected string currentOptionsText = "";
    protected string baseOptionsText = ""; // 新增：存儲純文字內容，不包含選擇前綴
    protected int currentOptionIndex = 0;
    protected bool isOptionsTypingComplete = false;
    
    protected string currentDialogFile = "";
    
    // 新增：跳過打字動畫旗標
    protected bool isSkippingAnimation = false;
    
    // 當前載入的對話數據（用於條件檢查）
    protected DialogData currentDialogData = null;
    
    // JSON數據結構
    [System.Serializable]
    public class DialogData
    {
        public string dialogName;
        public string version;
        public string description;
        public InitialDialogCondition[] initialDialogConditions; // 新增：起始對話條件
        public int defaultInitialDialogId; // 新增：預設起始對話ID
        public DialogEntry[] dialogs;
    }
    
    [System.Serializable]
    public class InitialDialogCondition
    {
        public GameCondition condition;
        public int dialogId;
    }
    
    [System.Serializable]
    public class DialogEntry
    {
        public int id;
        public int nextId;
        public int expressionId;
        public string text;
        public GameEvent[] events;
        public NextDialogCondition[] nextDialogConditions; // 新增：下一個對話條件
        public DialogOptionEntry[] options;
    }
    
    [System.Serializable]
    public class NextDialogCondition
    {
        public GameCondition condition;
        public int nextId;
    }
    
    [System.Serializable]
    public class DialogOptionEntry
    {
        public string text;
        public int nextId;
        public GameCondition condition; // 新增：選項顯示條件
        public string failText; // 新增：條件不滿足時的提示文本
        public ConditionalNextDialog[] conditionalNextDialogs; // 新增：條件性下一個對話
    }
    
    [System.Serializable]
    public class ConditionalNextDialog
    {
        public GameCondition condition;
        public int nextId;
    }
    
    
    
    public class DialogLine
    {
        public int id;
        public int nextId;
        public int expressionId;
        public string text;
        public string eventName;
        public GameEvent[] events;
        public List<DialogOption> options;
        
        public DialogLine(int id, int nextId, int expressionId, string text, GameEvent[] events)
        {
            this.id = id;
            this.nextId = nextId;
            this.expressionId = expressionId;
            this.text = text;
            this.eventName = "";
            this.events = events;
            this.options = new List<DialogOption>();
        }
        
        /// <summary>
        /// 深拷貝構造函數
        /// </summary>
        public DialogLine(DialogLine original)
        {
            this.id = original.id;
            this.nextId = original.nextId;
            this.expressionId = original.expressionId;
            this.text = original.text;
            this.eventName = original.eventName;
            this.events = original.events;
            this.options = new List<DialogOption>();
            
            // 深拷貝選項列表
            foreach (DialogOption option in original.options)
            {
                this.options.Add(new DialogOption(option.text, option.nextId));
            }
        }
    }
    
    public class DialogOption
    {
        public string text;
        public int nextId;
        
        public DialogOption(string text, int nextId)
        {
            this.text = text;
            this.nextId = nextId;
        }
    }
    
    protected override void Awake()
    {
        base.Awake(); // 呼叫基底類別的Awake
        
        // 設定UIPanel屬性（子類會覆寫）
        pauseGameWhenOpen = false;  // 對話不暫停遊戲
        canCloseWithEscape = false;  // 對話不能用ESC關閉
        
        // 確保面板初始為關閉狀態
        if (panelCanvas != null)
        {
            panelCanvas.enabled = false;
        }
        isOpen = false;
        
        // 初始化多語言系統
        InitializeLocalizationSystem();
    }
    
    /// <summary>
    /// 初始化多語言系統
    /// </summary>
    private void InitializeLocalizationSystem()
    {
        // Debug.log($"[{GetType().Name}] ===== 開始初始化多語言系統 =====");
        // Debug.log($"[{GetType().Name}] 對話文件基礎路徑: {dialogBasePath}");
        
        // 構建完整路徑用於驗證
        string fullBasePath = System.IO.Path.Combine(Application.dataPath, dialogBasePath);
        // Debug.log($"[{GetType().Name}] 完整對話文件路徑: {fullBasePath}");
        
        // 檢查路徑是否存在
        if (!System.IO.Directory.Exists(fullBasePath))
        {
            Debug.LogError($"[{GetType().Name}] 對話文件基礎路徑不存在: {fullBasePath}");
            Debug.LogError($"[{GetType().Name}] 請檢查 dialogBasePath 設定或確保對話文件存在");
            return;
        }
        
        int loadedLanguageCount = DialogDataLoader.LoadAllLanguageDialogs(dialogBasePath);
        // Debug.log($"[{GetType().Name}] DialogDataLoader 載入結果: {loadedLanguageCount} 種語言");
        
        if (loadedLanguageCount > 0)
        {
            // 優先使用 GameSettings 的語言設定
            string initialLanguage = defaultLanguage;
            
            if (GameSettings.Instance != null)
            {
                string gameSettingsLanguage = GameSettings.Instance.GetCurrentLanguageCode();
                initialLanguage = gameSettingsLanguage;
                // Debug.log($"[{GetType().Name}] 使用 GameSettings 語言設定: '{gameSettingsLanguage}'");
            }
            else
            {
                // 如果 GameSettings 不可用，使用預設語言
                Debug.LogWarning($"[{GetType().Name}] GameSettings 不可用，使用預設語言: '{defaultLanguage}'");
                initialLanguage = defaultLanguage;
            }
            
            // 顯示支援的語言
            string[] supportedLanguages = DialogDataLoader.GetSupportedLanguages();
            // Debug.log($"[{GetType().Name}] DialogDataLoader 支援的語言: [{string.Join(", ", supportedLanguages)}]");
            
            // 設定初始語言，加強驗證邏輯
            // Debug.log($"[{GetType().Name}] 嘗試設定初始語言: '{initialLanguage}'");
            if (!DialogDataLoader.SetCurrentLanguage(initialLanguage))
            {
                Debug.LogWarning($"[{GetType().Name}] 無法設定初始語言 '{initialLanguage}'，開始 fallback 處理");
                
                // 如果設定的語言不存在，使用 fallback 策略
                if (supportedLanguages.Length > 0)
                {
                    string fallbackLanguage = FindBestFallbackLanguage(initialLanguage, supportedLanguages);
                    if (!string.IsNullOrEmpty(fallbackLanguage))
                    {
                        // Debug.log($"[{GetType().Name}] 嘗試設定最佳 fallback 語言: '{fallbackLanguage}'");
                        if (DialogDataLoader.SetCurrentLanguage(fallbackLanguage))
                        {
                            Debug.LogWarning($"[{GetType().Name}] 初始語言 '{initialLanguage}' 不存在，使用最佳 fallback 語言: '{fallbackLanguage}'");
                        }
                        else
                        {
                            Debug.LogError($"[{GetType().Name}] 無法設定 fallback 語言: '{fallbackLanguage}'");
                        }
                    }
                    else
                    {
                        // 使用第一個可用語言作為最後的 fallback
                        // Debug.log($"[{GetType().Name}] 嘗試設定第一個可用語言: '{supportedLanguages[0]}'");
                        if (DialogDataLoader.SetCurrentLanguage(supportedLanguages[0]))
                        {
                            Debug.LogWarning($"[{GetType().Name}] 使用第一個可用語言作為最後 fallback: '{supportedLanguages[0]}'");
                        }
                        else
                        {
                            Debug.LogError($"[{GetType().Name}] 連第一個可用語言都無法設定: '{supportedLanguages[0]}'");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] 沒有任何支援的語言可用！");
                }
            }
            else
            {
                // Debug.log($"[{GetType().Name}] 成功設定初始語言: '{initialLanguage}'");
            }
            
            // 語言變更事件訂閱將在 Start() 中進行延遲處理
            // Debug.log($"[{GetType().Name}] 語言變更事件訂閱將在 Start() 中進行延遲處理");
            
            // 最終狀態報告
            string finalLanguage = DialogDataLoader.GetCurrentLanguage();
            // Debug.log($"[{GetType().Name}] ===== 多語言系統初始化完成 =====");
            // Debug.log($"[{GetType().Name}] 載入語言數量: {loadedLanguageCount}");
            // Debug.log($"[{GetType().Name}] 最終設定語言: '{finalLanguage}'");
            // Debug.log($"[{GetType().Name}] 支援語言列表: [{string.Join(", ", DialogDataLoader.GetSupportedLanguages())}]");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] ===== 多語言系統初始化失敗 =====");
            Debug.LogError($"[{GetType().Name}] 無法載入任何語言文件，基礎路徑: {dialogBasePath}");
            Debug.LogError($"[{GetType().Name}] 完整路徑: {fullBasePath}");
            Debug.LogError($"[{GetType().Name}] 請檢查對話文件是否存在於正確位置");
        }
    }
    
    /// <summary>
    /// Unity Start 方法 - 初始化延遲處理
    /// </summary>
    protected virtual void Start()
    {
        // 啟動延遲事件訂閱協程
        StartCoroutine(DelayedEventSubscription());
    }
    
    /// <summary>
    /// 延遲事件訂閱協程 - 等待 GameSettings 初始化完成
    /// </summary>
    private IEnumerator DelayedEventSubscription()
    {
        // Debug.log($"[{GetType().Name}] 開始延遲事件訂閱，等待 GameSettings 初始化...");
        
        float waitTime = 0f;
        const float maxWaitTime = 10f; // 最大等待時間（秒）
        const float checkInterval = 0.1f; // 檢查間隔
        
        // 等待 GameSettings.Instance 可用
        while (GameSettings.Instance == null && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(checkInterval);
            waitTime += checkInterval;
        }
        
        if (GameSettings.Instance == null)
        {
            Debug.LogError($"[{GetType().Name}] 等待 {maxWaitTime} 秒後 GameSettings.Instance 仍為 null，無法訂閱語言變更事件");
            yield break;
        }
        
        // 訂閱語言變更事件
        // Debug.log($"[{GetType().Name}] GameSettings 已就緒，開始訂閱語言變更事件 (等待時間: {waitTime:F2} 秒)");
        SubscribeToLanguageEvents();
        
        // Debug.log($"[{GetType().Name}] 延遲事件訂閱完成");
    }
    
    protected virtual void OnDestroy()
    {
        // 取消訂閱語言變更事件
        UnsubscribeFromLanguageEvents();
        
        // 清理引用
        dialogLines.Clear();
        currentOptions.Clear();
        currentModel = null;
        currentDialogData = null;
        
        // Debug.log($"{GetType().Name} 已清理");
    }
    
    protected virtual void OnApplicationQuit()
    {
        // 應用程式退出時清理
    }
    
    protected virtual void HideDialogUI()
    {
        // 停止對話游標閃爍協程
        if (dialogCursorBlinkCoroutine != null)
        {
            StopCoroutine(dialogCursorBlinkCoroutine);
            dialogCursorBlinkCoroutine = null;
        }
        
        // 停止選項游標閃爍協程
        if (optionsCursorBlinkCoroutine != null)
        {
            StopCoroutine(optionsCursorBlinkCoroutine);
            optionsCursorBlinkCoroutine = null;
        }
        
        // 確保選項UI被隱藏
        if (optionsText != null)
        {
            optionsText.gameObject.SetActive(false);
        }
        
        // 隱藏整個DialogPanel，使用重寫的Close方法
        Close(); 
        // Debug.log($"隱藏{GetType().Name}");
        if (currentModel != null)
        {
            // 只清理引用，不改變模型的 active 狀態
            currentModel = null;
        }
        currentOptions.Clear();
        selectedOptionIndex = 0;
        currentDialogId = -1;
        
        // 清理對話終端效果變數
        currentDisplayText = "";
        isTypingComplete = false;
        isDialogCursorVisible = true;
        
        // 清理選項終端效果變數
        currentOptionsText = "";
        baseOptionsText = ""; // 清理基礎文字
        isOptionsTypingComplete = false;
        isDisplayingOptions = false;
        isOptionsCursorVisible = true;
        currentOptionIndex = 0;
        
        // Debug.log($"{GetType().Name}對話結束");
    }
    
    protected virtual void ShowDialogUI(GameObject model)
    {
        // 首先確保DialogPanel本身是啟用的，使用重寫的Open方法
        Open();
        EnsureOptionsUIInitialState(); // 新增：在顯示UI時，確保選項被重置和隱藏
        // Debug.log($"啟用{GetType().Name}");
        if (model != null)
        {
            currentModel = model;
            currentModel.SetActive(true);
            // Debug.log("啟用對話模型");
        }
        
        // Debug.log($"{GetType().Name}對話開始");
    }
    
    /// <summary>
    /// 面板開啟時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnOpened()
    {
        base.OnOpened();
        // Debug.log($"{GetType().Name}UI已開啟");
    }
    
    /// <summary>
    /// 面板關閉時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnClosed()
    {
        base.OnClosed();
        // 停止所有協程
        if (dialogCursorBlinkCoroutine != null)
        {
            StopCoroutine(dialogCursorBlinkCoroutine);
            dialogCursorBlinkCoroutine = null;
        }
        
        if (optionsCursorBlinkCoroutine != null)
        {
            StopCoroutine(optionsCursorBlinkCoroutine);
            optionsCursorBlinkCoroutine = null;
        }
        
        if (currentModel != null)
        {
            // 只清理引用，不改變模型的 active 狀態
            currentModel = null;
        }
        currentOptions.Clear();
        selectedOptionIndex = 0;
        currentDialogId = -1;
        
        // 清理對話終端效果變數
        currentDisplayText = "";
        isTypingComplete = false;
        isDialogCursorVisible = true;
        
        // 清理選項終端效果變數
        currentOptionsText = "";
        baseOptionsText = ""; // 清理基礎文字
        isOptionsTypingComplete = false;
        isDisplayingOptions = false;
        isOptionsCursorVisible = true;
        
        // Debug.log($"{GetType().Name}對話結束");
    }
    
    /// <summary>
    /// 確保選項UI的初始狀態正確（隱藏且清空內容）
    /// </summary>
    protected void EnsureOptionsUIInitialState()
    {
        if (optionsText != null)
        {
            optionsText.gameObject.SetActive(false);
            optionsText.text = ""; // 清空任何殘留文字
        }
        
        // 重置選項相關狀態標誌
        isDisplayingOptions = false;
        isOptionsTypingComplete = false;
        currentOptionsText = "";
        baseOptionsText = "";
    }
    
    /// <summary>
    /// 檢查是否有其他暫停遊戲的UI開啟
    /// </summary>
    /// <returns>如果有其他暫停UI返回true</returns>
    protected bool HasOtherPausingUI()
    {
        // 檢查常見的暫停UI
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.IsOpen && GameMenuManager.Instance.PausesGame)
            return true;
            
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen && SaveUIController.Instance.PausesGame)
            return true;
            
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen && PlayerGameSettingsUI.Instance.PausesGame)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// 處理自定義輸入 - 抽象方法，由子類實現
    /// </summary>
    protected abstract override void HandleCustomInput();
    
    /// <summary>
    /// 強制關閉對話（用於特殊情況，如存檔加載）
    /// </summary>
    public virtual void ForceCloseDialog()
    {
        if (IsInDialog)
        {
            // Debug.log($"[{GetType().Name}] 強制關閉對話");
            HideDialogUI();
        }
    }
    
    /// <summary>
    /// 檢查是否應該阻止遊戲輸入
    /// </summary>
    /// <returns>是否應該阻止遊戲輸入</returns>
    public virtual bool ShouldBlockGameInput()
    {
        return IsInDialog;
    }
    
    /// <summary>
    /// 設定對話數據（供 LocalizedDialogManager 使用）
    /// </summary>
    /// <param name="dialogLines">對話行字典</param>
    /// <param name="dialogData">對話數據</param>
    /// <param name="model">對話模型</param>
    public virtual void SetDialogData(Dictionary<int, DialogLine> dialogLines, DialogData dialogData, GameObject model = null)
    {
        this.dialogLines = dialogLines;
        this.currentDialogData = dialogData;
        
        if (dialogLines.Count > 0)
        {
            ShowDialogUI(model);
            currentDialogId = DetermineInitialDialogId("");
            StartCoroutine(DisplayNextLine());
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 設定的對話數據為空");
        }
    }
    
    /// <summary>
    /// 載入本地化對話（支援多語言）
    /// </summary>
    /// <param name="dialogId">對話ID</param>
    /// <param name="model">對話模型</param>
    public virtual void LoadDialog(string dialogId, GameObject model = null)
    {
        // 檢查UI組件是否設置
        if (dialogText == null || optionsText == null || dialogBackground == null)
        {
            Debug.LogError($"{GetType().Name}: UI組件未設置！請在Inspector中指定所有必要的UI組件。");
            Debug.LogError("對話系統無法正常工作，請檢查設置。");
            return;
        }
        
        // 檢查多語言系統是否已初始化
        if (!DialogDataLoader.IsLocalizationInitialized())
        {
            Debug.LogError($"[{GetType().Name}] 多語言系統尚未初始化，無法載入對話: {dialogId}");
            return;
        }
        
        currentDialogFile = dialogId; // 現在存儲對話ID而非文件名
        
        // 使用多語言載入系統
        var loadResult = DialogDataLoader.LoadLocalizedDialogData(dialogId);
        
        if (!loadResult.success || loadResult.dialogData == null)
        {
            Debug.LogError($"[{GetType().Name}] 無法載入本地化對話: {dialogId}，當前語言: {DialogDataLoader.GetCurrentLanguage()}");
            
            // 列出當前語言支援的對話
            Debug.LogError($"[{GetType().Name}] 可用語言: {string.Join(", ", DialogDataLoader.GetSupportedLanguages())}");
            return;
        }
        
        dialogLines = loadResult.dialogLines;
        currentDialogData = loadResult.dialogData;
        
        // Debug.log($"[{GetType().Name}] 成功載入本地化對話: {dialogId} [{DialogDataLoader.GetCurrentLanguage()}]");
        
        // 開始顯示對話
        if (dialogLines.Count > 0)
        {
            ShowDialogUI(model);
            // 使用新的起始對話ID決定邏輯
            currentDialogId = DetermineInitialDialogId(dialogId);
            StartCoroutine(DisplayNextLine());
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 載入的對話數據為空: {dialogId}");
        }
    }
    
    /// <summary>
    /// 顯示選項 - 抽象方法，由子類實現
    /// </summary>
    /// <param name="options">選項列表</param>
    protected abstract void ShowOptions(List<DialogOption> options);
    
    protected virtual IEnumerator DisplayNextLine()
    {
        if (isDisplayingDialog || currentDialogId == -1 || !dialogLines.ContainsKey(currentDialogId))
            yield break;
            
        isDisplayingDialog = true;
        isSkippingAnimation = false; // 修改：為每一行對話重置跳過旗標
        DialogLine currentLine = dialogLines[currentDialogId];
        
        // 停止之前的對話游標閃爍
        if (dialogCursorBlinkCoroutine != null)
        {
            StopCoroutine(dialogCursorBlinkCoroutine);
            dialogCursorBlinkCoroutine = null;
        }
        
        // 處理表情變化
        if (currentLine.expressionId > 0 && currentModel != null)
        {
            var animator = currentModel.GetComponent<Animator>();
            if (animator != null)
            {
                // 將 expressionId 轉換為觸發器名稱，可以根據需要自定義映射
                string triggerName = GetExpressionTriggerName(currentLine.expressionId);
                if (!string.IsNullOrEmpty(triggerName))
                {
                    animator.SetTrigger(triggerName);
                }
            }
        }
        
        // 重置對話終端效果變數
        currentDisplayText = "";
        isTypingComplete = false;
        
        // 檢查是否有文字需要顯示
        if (!string.IsNullOrEmpty(currentLine.text))
        {
            // 開始對話游標閃爍效果
            if (enableTerminalCursor) dialogCursorBlinkCoroutine = StartCoroutine(BlinkDialogCursor());
            
            // 使用文字控制系統顯示文字
            yield return StartCoroutine(ProcessTextWithControls(currentLine.text));
            
            // 文字輸入完成，但保持游標閃爍
            isTypingComplete = true;
            
            // 處理事件（在文字顯示完成後執行）
            if (currentLine.events != null && currentLine.events.Length > 0)
            {
                EventProcessor.ProcessEvents(currentLine.events);
            }
        }
        else
        {
            // 沒有文字時，隱藏對話文字UI
            isTypingComplete = true;
            
            // 即使沒有文字也要處理事件
            if (currentLine.events != null && currentLine.events.Length > 0)
            {
                EventProcessor.ProcessEvents(currentLine.events);
            }
        }
        
        if(isSkippingAnimation) yield return new WaitForSeconds(0.5f); // Wait to prevent operation too fast.

        isDisplayingDialog = false;
    
        // 如果有選項，顯示選項
        if (currentLine.options.Count > 0) 
        {
            ShowOptions(currentLine.options);
        }
        else
        {
            // 沒有選項時的處理由子類決定
            yield return StartCoroutine(HandleNoOptionsDialog());
        }
    }
    
    /// <summary>
    /// 處理沒有選項的對話 - 抽象方法，由子類實現
    /// </summary>
    protected abstract IEnumerator HandleNoOptionsDialog();
    
    /// <summary>
    /// 處理選項選擇 - 抽象方法，由子類實現
    /// </summary>
    /// <param name="nextId">選中選項的下一個對話ID</param>
    protected abstract void OnOptionSelected(int nextId);
    
    /// <summary>
    /// 解析文字控制指令並處理文字顯示
    /// </summary>
    /// <param name="text">包含控制指令的文字</param>
    /// <returns></returns>
    protected IEnumerator ProcessTextWithControls(string text)
    {
        if (!enableTextControl)
        {
            // 如果未啟用文字控制，使用原有的逐字顯示
            foreach (char c in text)
            {
                currentDisplayText += c;
                if (enableTerminalCursor)  UpdateDialogDisplay();
                else dialogText.text = currentDisplayText;
                if (!isSkippingAnimation)  yield return new WaitForSeconds(currentTypingSpeed);
            }
            yield break;
        }
        
        // 初始化動態控制變數
        currentTypingSpeed = typingSpeed;
        currentCursorBlinkSpeed = cursorBlinkSpeed;
        
        int i = 0;
        while (i < text.Length)
        {
            // 檢查是否遇到控制指令
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                // 解析控制指令
                var controlResult = ParseControlCommand(text, i);
                if (controlResult.success)
                {
                    // 特殊處理del指令
                    if (controlResult.command == "del")
                    {
                        yield return StartCoroutine(ExecuteNewDelCommand(controlResult.parameter, controlResult.stringParam1, controlResult.stringParam2));
                        i = controlResult.nextIndex;
                        continue;
                    }
                    else
                    {
                        // 執行其他控制指令
                        yield return StartCoroutine(ExecuteControlCommand(controlResult.command, controlResult.parameter));
                        i = controlResult.nextIndex;
                        continue;
                    }
                }
            }
            
            // 普通字符，添加到顯示文字
            currentDisplayText += text[i];

            if (enableTerminalCursor)  UpdateDialogDisplay();
            else dialogText.text = currentDisplayText;

            if (!isSkippingAnimation)  yield return new WaitForSeconds(currentTypingSpeed);

            i++;
        }
    }
    
    /// <summary>
    /// 解析控制指令
    /// </summary>
    /// <param name="text">文字</param>
    /// <param name="startIndex">開始索引</param>
    /// <returns>解析結果</returns>
    protected (bool success, string command, float parameter, string stringParam1, string stringParam2, int nextIndex) ParseControlCommand(string text, int startIndex)
    {
        if (startIndex >= text.Length || text[startIndex] != '\\')
            return (false, "", 0f, "", "", startIndex);
        
        // 尋找指令名稱的結束位置
        int commandStart = startIndex + 1;
        int braceStart = text.IndexOf('{', commandStart);
        if (braceStart == -1)
            return (false, "", 0f, "", "", startIndex);
        
        string command = text.Substring(commandStart, braceStart - commandStart).ToLower();
        
        if (command == "del")  return ParseDelCommand(text, startIndex, braceStart);
        else
        {
            // 處理其他指令的單一參數格式
            int braceEnd = text.IndexOf('}', braceStart);
            if (braceEnd == -1)
                return (false, "", 0f, "", "", startIndex);
            
            string parameterStr = text.Substring(braceStart + 1, braceEnd - braceStart - 1);
            
            if (float.TryParse(parameterStr.Trim(), out float parameter)) return (true, command, parameter, "", "", braceEnd + 1);
        }
        
        return (false, "", 0f, "", "", startIndex);
    }
    
    /// <summary>
    /// 解析del指令的特殊格式
    /// </summary>
    /// <param name="text">完整文字</param>
    /// <param name="startIndex">指令開始位置</param>
    /// <param name="firstBraceStart">第一個大括號位置</param>
    /// <returns>解析結果</returns>
    protected (bool success, string command, float parameter, string stringParam1, string stringParam2, int nextIndex) ParseDelCommand(string text, int startIndex, int firstBraceStart)
    {
        // 解析第一個參數（時間）
        int firstBraceEnd = text.IndexOf('}', firstBraceStart);
        if (firstBraceEnd == -1)
            return (false, "", 0f, "", "", startIndex);
        
        string timeStr = text.Substring(firstBraceStart + 1, firstBraceEnd - firstBraceStart - 1);
        if (!float.TryParse(timeStr.Trim(), out float deleteTime))
            return (false, "", 0f, "", "", startIndex);
        
        // 解析第二個參數（要刪除的字串）
        int secondBraceStart = text.IndexOf('{', firstBraceEnd);
        if (secondBraceStart == -1)
            return (false, "", 0f, "", "", startIndex);
        
        int secondBraceEnd = text.IndexOf('}', secondBraceStart);
        if (secondBraceEnd == -1)
            return (false, "", 0f, "", "", startIndex);
        
        string deleteString = text.Substring(secondBraceStart + 1, secondBraceEnd - secondBraceStart - 1);
        
        // 解析第三個參數（之後要顯示的字串）
        int thirdBraceStart = text.IndexOf('{', secondBraceEnd);
        if (thirdBraceStart == -1)
            return (false, "", 0f, "", "", startIndex);
        
        int thirdBraceEnd = text.IndexOf('}', thirdBraceStart);
        if (thirdBraceEnd == -1)
            return (false, "", 0f, "", "", startIndex);
        
        string replaceString = text.Substring(thirdBraceStart + 1, thirdBraceEnd - thirdBraceStart - 1);
        
        return (true, "del", deleteTime, deleteString, replaceString, thirdBraceEnd + 1);
    }
    
    /// <summary>
    /// 執行控制指令
    /// </summary>
    /// <param name="command">指令名稱</param>
    /// <param name="parameter">參數</param>
    /// <returns></returns>
    protected IEnumerator ExecuteControlCommand(string command, float parameter)
    {
        switch (command)
        {
            case "stop":
                if (!isSkippingAnimation)  yield return new WaitForSeconds(parameter);
                break;
                
            case "speed":
                currentTypingSpeed = parameter;
                break;
                
            case "blink":
                currentCursorBlinkSpeed = parameter;
                
                // 重新啟動游標閃爍協程以應用新速度
                if (enableTerminalCursor && dialogCursorBlinkCoroutine != null)
                {
                    StopCoroutine(dialogCursorBlinkCoroutine);
                    dialogCursorBlinkCoroutine = StartCoroutine(BlinkDialogCursor());
                }
                break;
            default:
                Debug.LogWarning($"未知的文字控制指令: {command}");
                break;
        }
    }
    
    /// <summary>
    /// 執行新的刪除指令 \del{時間}{要刪除的字串}{之後要顯示的字串}
    /// </summary>
    /// <param name="deleteTime">刪除每個字元的間隔時間</param>
    /// <param name="deleteString">要刪除的字串</param>
    /// <param name="replaceString">之後要顯示的字串</param>
    /// <returns></returns>
    protected IEnumerator ExecuteNewDelCommand(float deleteTime, string deleteString, string replaceString)
    {
        // Debug.log($"文字控制: 刪除字串 '{deleteString}'，刪除速度 {deleteTime}，替換為 '{replaceString}'");
        
        // 先逐字顯示要被刪除的字串
        foreach (char c in deleteString)
        {
            currentDisplayText += c;
            if (enableTerminalCursor) UpdateDialogDisplay();
            else dialogText.text = currentDisplayText;
            if (!isSkippingAnimation) yield return new WaitForSeconds(currentTypingSpeed);
        }
        
        // 現在開始刪除字串中的每個字元
        for (int i = 0; i < deleteString.Length && currentDisplayText.Length > 0; i++)
        {
            // 移除最後一個字元
            currentDisplayText = currentDisplayText.Substring(0, currentDisplayText.Length - 1);
            if (enableTerminalCursor) UpdateDialogDisplay();
            else dialogText.text = currentDisplayText;
            if (!isSkippingAnimation) yield return new WaitForSeconds(deleteTime);
        }
        
        // 最後顯示替換字串
        foreach (char c in replaceString)
        {
            currentDisplayText += c;
            if (enableTerminalCursor) UpdateDialogDisplay();
            else dialogText.text = currentDisplayText;
            if (!isSkippingAnimation) yield return new WaitForSeconds(currentTypingSpeed);
        }
    }
    
    /// <summary>
    /// 對話游標閃爍協程
    /// </summary>
    /// <returns></returns>
    protected IEnumerator BlinkDialogCursor()
    {
        while (true)
        {
            isDialogCursorVisible = !isDialogCursorVisible;
            UpdateDialogDisplay();
            yield return new WaitForSeconds(1f / currentCursorBlinkSpeed);
        }
    }
    
    /// <summary>
    /// 更新對話顯示（包含游標效果）
    /// </summary>
    protected void UpdateDialogDisplay()
    {
        if (dialogText == null) return;
        
        string displayText = currentDisplayText;
        
        if (enableTerminalCursor)
        {
            // 添加對話游標效果，使用固定寬度避免抖動
            string colorHex = ColorUtility.ToHtmlStringRGB(cursorColor);
            if (isDialogCursorVisible) displayText += $"<color=#{colorHex}>{cursorCharacter}</color>";
            else displayText += $"<color=#00000000>{cursorCharacter}</color>";
        }
        
        dialogText.text = displayText;
    }
    
    /// <summary>
    /// 清理緩存(editor)
    /// </summary>
    [ContextMenu("Clear All Dialog Cache")]
    public void Editor_ClearAllDialogCache()
    {
    #if UNITY_EDITOR
        DialogCacheManager.ClearAllDialogCache();
    #else
        Debug.LogWarning("ClearAllDialogCache 只能在編輯器中使用！");
    #endif
    }
    
    /// <summary>
    /// 清除所有對話緩存
    /// </summary>
    public static void ClearAllDialogCache()
    {
        DialogCacheManager.ClearAllDialogCache();
    }

    /// <summary>
    /// 獲取緩存統計信息
    /// </summary>
    [ContextMenu("Get Dialog Cache Info")]
    public void GetCacheInfo()
    {
    #if UNITY_EDITOR
        // Debug.log(DialogCacheManager.GetCacheInfo());
    #else
        Debug.LogWarning("GetCacheInfo 只能在編輯器中使用！");
    #endif
    }
    
    // ==================== 條件檢查系統 ====================
    
    /// <summary>
    /// 決定起始對話ID
    /// </summary>
    /// <param name="fileName">對話文件名</param>
    /// <returns>起始對話ID</returns>
    protected int DetermineInitialDialogId(string fileName)
    {
        if (currentDialogData == null)
        {
            Debug.LogWarning($"找不到對話數據: {fileName}，使用預設起始ID 1");
            return 1;
        }
        
        // 檢查是否有起始對話條件
        if (currentDialogData.initialDialogConditions != null && currentDialogData.initialDialogConditions.Length > 0)
        {
            foreach (InitialDialogCondition condition in currentDialogData.initialDialogConditions)
            {
                if (ConditionChecker.CheckCondition(condition.condition))
                {
                    // Debug.log($"起始對話條件滿足，使用對話ID: {condition.dialogId}");
                    return condition.dialogId;
                }
            }
        }
        
        // 如果沒有條件滿足，使用預設起始對話ID
        int defaultId = currentDialogData.defaultInitialDialogId > 0 ? currentDialogData.defaultInitialDialogId : 1;
        // Debug.log($"使用預設起始對話ID: {defaultId}");
        return defaultId;
    }
    
    /// <summary>
    /// 決定下一個對話ID（用於沒有選項的對話）
    /// </summary>
    /// <param name="currentDialogId">當前對話ID</param>
    /// <returns>下一個對話ID</returns>
    protected int DetermineNextDialogId(int currentDialogId)
    {
        if (currentDialogData == null)
        {
            Debug.LogWarning($"找不到對話數據: {currentDialogFile}，使用預設nextId");
            return dialogLines.ContainsKey(currentDialogId) ? dialogLines[currentDialogId].nextId : -1;
        }
        
        // 找到當前對話的DialogEntry
        DialogEntry currentEntry = null;
        foreach (DialogEntry entry in currentDialogData.dialogs)
        {
            if (entry.id == currentDialogId)
            {
                currentEntry = entry;
                break;
            }
        }
        
        if (currentEntry == null)
        {
            Debug.LogWarning($"找不到對話ID {currentDialogId} 的DialogEntry");
            return -1;
        }
        
        // 檢查是否有下一個對話條件
        if (currentEntry.nextDialogConditions != null && currentEntry.nextDialogConditions.Length > 0)
        {
            foreach (NextDialogCondition condition in currentEntry.nextDialogConditions)
            {
                if (ConditionChecker.CheckCondition(condition.condition))
                {
                    // Debug.log($"下一個對話條件滿足，跳轉到對話ID: {condition.nextId}");
                    return condition.nextId;
                }
            }
        }
        
        // 如果沒有條件滿足，使用預設的nextId
        // Debug.log($"使用預設下一個對話ID: {currentEntry.nextId}");
        return currentEntry.nextId;
    }
    
    /// <summary>
    /// 決定選項的下一個對話ID（用於選項被選擇後）
    /// </summary>
    /// <param name="optionIndex">選項索引</param>
    /// <param name="defaultNextId">預設的下一個對話ID</param>
    /// <returns>實際的下一個對話ID</returns>
    protected int DetermineOptionNextDialogId(int optionIndex, int defaultNextId)
    {
        if (currentDialogData == null)
        {
            Debug.LogWarning($"找不到對話數據: {currentDialogFile}，使用預設nextId");
            return defaultNextId;
        }
        
        // 找到當前對話的DialogEntry
        DialogEntry currentEntry = null;
        foreach (DialogEntry entry in currentDialogData.dialogs)
        {
            if (entry.id == currentDialogId)
            {
                currentEntry = entry;
                break;
            }
        }
        
        if (currentEntry == null || currentEntry.options == null)
        {
            Debug.LogWarning($"找不到對話ID {currentDialogId} 的DialogEntry或選項");
            return defaultNextId;
        }
        
        // 找到對應的選項（需要考慮條件過濾後的索引映射）
        int actualOptionIndex = 0;
        DialogOptionEntry selectedOption = null;
        
        foreach (DialogOptionEntry optionEntry in currentEntry.options)
        {
            // 檢查選項是否應該顯示（與載入時的邏輯一致）
            if (optionEntry.condition == null || ConditionChecker.CheckCondition(optionEntry.condition))
            {
                if (actualOptionIndex == optionIndex)
                {
                    selectedOption = optionEntry;
                    break;
                }
                actualOptionIndex++;
            }
            else if (!string.IsNullOrEmpty(optionEntry.failText))
            {
                // 如果有failText，也會顯示為選項
                if (actualOptionIndex == optionIndex)
                {
                    // 但是failText選項不能被選擇，返回-1表示無效
                    Debug.LogWarning($"選擇了不可用的選項: {optionEntry.failText}");
                    return -1;
                }
                actualOptionIndex++;
            }
        }
        
        if (selectedOption == null)
        {
            Debug.LogWarning($"找不到選項索引 {optionIndex} 對應的DialogOptionEntry");
            return defaultNextId;
        }
        
        // 檢查是否有條件性下一個對話
        if (selectedOption.conditionalNextDialogs != null && selectedOption.conditionalNextDialogs.Length > 0)
        {
            foreach (ConditionalNextDialog conditionalNext in selectedOption.conditionalNextDialogs)
            {
                if (ConditionChecker.CheckCondition(conditionalNext.condition))
                {
                    // Debug.log($"選項條件性下一個對話條件滿足，跳轉到對話ID: {conditionalNext.nextId}");
                    return conditionalNext.nextId;
                }
            }
        }
        
        // 如果沒有條件滿足，使用預設的nextId
        // Debug.log($"使用選項預設下一個對話ID: {selectedOption.nextId}");
        return selectedOption.nextId;
    }
    
    /// <summary>
    /// 將 expressionId 轉換為 Animator 觸發器名稱
    /// </summary>
    protected virtual string GetExpressionTriggerName(int expressionId)
    {
        // 可以根據專案需求自定義 ID 與觸發器名稱的對應關係
        switch (expressionId)
        {
            case 1: return "Happy";
            case 2: return "Sad"; 
            case 3: return "Angry";
            case 4: return "Surprised";
            case 5: return "Neutral";
            default: return null;
        }
    }
    
    // ==================== 語言變更事件系統 ====================
    
    /// <summary>
    /// 訂閱語言變更事件
    /// </summary>
    private void SubscribeToLanguageEvents()
    {
        // 訂閱 GameSettings 語言變更事件
        if (GameSettings.Instance != null)
        {
            try
            {
                // 先嘗試取消註冊（防止重複）
                GameSettings.Instance.OnLanguageChanged -= OnGameSettingsLanguageChanged;
                // 然後重新註冊
                GameSettings.Instance.OnLanguageChanged += OnGameSettingsLanguageChanged;
                // Debug.log($"[{GetType().Name}] 已訂閱 GameSettings 語言變更事件");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{GetType().Name}] 訂閱 GameSettings 語言變更事件時發生錯誤: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] GameSettings.Instance 為 null，無法訂閱語言變更事件");
        }
    }
    
    /// <summary>
    /// 取消訂閱語言變更事件
    /// </summary>
    private void UnsubscribeFromLanguageEvents()
    {
        // 取消訂閱 GameSettings 語言變更事件
        if (GameSettings.Instance != null)
        {
            try
            {
                GameSettings.Instance.OnLanguageChanged -= OnGameSettingsLanguageChanged;
                // Debug.log($"[{GetType().Name}] 已取消訂閱 GameSettings 語言變更事件");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{GetType().Name}] 取消訂閱 GameSettings 語言變更事件時發生錯誤: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// GameSettings 語言變更事件處理
    /// </summary>
    /// <param name="newLanguageCode">新的語言代碼</param>
    private void OnGameSettingsLanguageChanged(string newLanguageCode)
    {
        // Debug.log($"[{GetType().Name}] ===== GameSettings 語言變更事件觸發 =====");
        // Debug.log($"[{GetType().Name}] 接收到新語言代碼: '{newLanguageCode}'");
        // Debug.log($"[{GetType().Name}] 當前 DialogDataLoader 語言: '{DialogDataLoader.GetCurrentLanguage()}'");
        
        // 驗證和設定對話系統語言
        string dialogLanguage = newLanguageCode;
        // Debug.log($"[{GetType().Name}] 目標對話語言: '{dialogLanguage}'");
        
        // 檢查 DialogDataLoader 是否已初始化
        if (!DialogDataLoader.IsLocalizationInitialized())
        {
            Debug.LogWarning($"[{GetType().Name}] DialogDataLoader 尚未初始化，跳過語言同步");
            return;
        }
        
        // 顯示支援的語言列表
        string[] supportedLanguages = DialogDataLoader.GetSupportedLanguages();
        // Debug.log($"[{GetType().Name}] DialogDataLoader 支援的語言: [{string.Join(", ", supportedLanguages)}]");
        
        // 嘗試設定對話系統語言
        // Debug.log($"[{GetType().Name}] 嘗試將 DialogDataLoader 語言切換到: '{dialogLanguage}'");
        if (DialogDataLoader.SetCurrentLanguage(dialogLanguage))
        {
            // Debug.log($"[{GetType().Name}] ✅ 對話系統語言成功切換到: '{dialogLanguage}'");
            
            // 觸發對話內容更新（即使沒有對話進行中也要更新）
            OnLanguageChangedRefresh();
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] ⚠️ 無法將對話系統語言切換到: '{dialogLanguage}'");
            Debug.LogWarning($"[{GetType().Name}] 可用語言: [{string.Join(", ", supportedLanguages)}]");
            
            // 嘗試使用支援的語言中最相近的語言
            string fallbackLanguage = FindBestFallbackLanguage(dialogLanguage, supportedLanguages);
            if (!string.IsNullOrEmpty(fallbackLanguage))
            {
                Debug.LogWarning($"[{GetType().Name}] 嘗試使用最佳 fallback 語言: '{fallbackLanguage}'");
                if (DialogDataLoader.SetCurrentLanguage(fallbackLanguage))
                {
                    Debug.LogWarning($"[{GetType().Name}] ✅ 成功使用 fallback 語言: '{fallbackLanguage}'");
                    OnLanguageChangedRefresh();
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] ❌ 連 fallback 語言都無法設定: '{fallbackLanguage}'");
                }
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] ❌ 找不到合適的 fallback 語言");
            }
        }
        
        string finalLanguage = DialogDataLoader.GetCurrentLanguage();
        // Debug.log($"[{GetType().Name}] ===== GameSettings 語言變更處理完成 =====");
        // Debug.log($"[{GetType().Name}] 最終 DialogDataLoader 語言: '{finalLanguage}'");
    }
    
    /// <summary>
    /// 語言變更後的刷新處理
    /// </summary>
    private void OnLanguageChangedRefresh()
    {
        // Debug.log($"[{GetType().Name}] ===== 開始語言變更刷新處理 =====");
        // Debug.log($"[{GetType().Name}] 當前對話狀態 - IsInDialog: {IsInDialog}, currentDialogFile: '{currentDialogFile}'");
        
        // 如果有對話正在進行，刷新對話內容
        if (IsInDialog && !string.IsNullOrEmpty(currentDialogFile))
        {
            // Debug.log($"[{GetType().Name}] 檢測到對話進行中，開始刷新對話內容: '{currentDialogFile}'");
            RefreshDialogContentWithNewLanguage();
        }
        else
        {
            // Debug.log($"[{GetType().Name}] 沒有正在進行的對話，跳過內容刷新");
        }
        
        // 這裡可以添加其他需要在語言變更時執行的邏輯
        string currentLanguage = DialogDataLoader.GetCurrentLanguage();
        // Debug.log($"[{GetType().Name}] ===== 語言變更刷新完成 =====");
        // Debug.log($"[{GetType().Name}] 最終確認 DialogDataLoader 語言: '{currentLanguage}'");
    }
    
    /// <summary>
    /// 尋找最佳 fallback 語言
    /// </summary>
    /// <param name="targetLanguage">目標語言</param>
    /// <param name="supportedLanguages">支援的語言列表</param>
    /// <returns>最佳 fallback 語言，如果找不到返回null</returns>
    private string FindBestFallbackLanguage(string targetLanguage, string[] supportedLanguages)
    {
        if (string.IsNullOrEmpty(targetLanguage) || supportedLanguages == null || supportedLanguages.Length == 0)
        {
            return null;
        }
        
        // 1. 直接匹配
        foreach (string lang in supportedLanguages)
        {
            if (string.Equals(lang, targetLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return lang;
            }
        }
        
        // 2. 語言家族匹配（例如 zh-TW 和 zh-CN）
        string targetBase = targetLanguage.Split('-')[0].ToLower();
        foreach (string lang in supportedLanguages)
        {
            string langBase = lang.Split('-')[0].ToLower();
            if (targetBase == langBase)
            {
                // Debug.log($"[{GetType().Name}] 找到語言家族匹配: {targetLanguage} -> {lang}");
                return lang;
            }
        }
        
        // 3. 常見語言映射
        var languageMapping = new Dictionary<string, string[]>
        {
            {"en", new[]{"en-US", "english"}},
            {"zh", new[]{"zh-TW", "zh-CN", "chinese"}},
            {"ja", new[]{"ja-JP", "japanese"}},
            {"ko", new[]{"ko-KR", "korean"}},
            {"fr", new[]{"fr-FR", "french"}},
            {"de", new[]{"de-DE", "german"}},
            {"es", new[]{"es-ES", "spanish"}},
            {"pt", new[]{"pt-BR", "pt-PT", "portuguese"}},
            {"ru", new[]{"ru-RU", "russian"}},
            {"it", new[]{"it-IT", "italian"}}
        };
        
        foreach (var mapping in languageMapping)
        {
            if (targetBase == mapping.Key)
            {
                foreach (string candidate in mapping.Value)
                {
                    foreach (string lang in supportedLanguages)
                    {
                        if (string.Equals(lang, candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            // Debug.log($"[{GetType().Name}] 找到語言映射匹配: {targetLanguage} -> {lang}");
                            return lang;
                        }
                    }
                }
            }
        }
        
        // 4. 如果都找不到，返回第一個支援的語言作為最後的 fallback
        if (supportedLanguages.Length > 0)
        {
            Debug.LogWarning($"[{GetType().Name}] 找不到相似語言，使用第一個可用語言: {supportedLanguages[0]}");
            return supportedLanguages[0];
        }
        
        return null;
    }
    
    /// <summary>
    /// 刷新對話內容為新語言（即時更新機制）
    /// </summary>
    private void RefreshDialogContentWithNewLanguage()
    {
        if (string.IsNullOrEmpty(currentDialogFile))
        {
            Debug.LogWarning($"[{GetType().Name}] 當前對話文件為空，無法刷新內容");
            return;
        }
        
        // Debug.log($"[{GetType().Name}] 開始重載對話內容: {currentDialogFile}");
        
        // 重載對話數據
        var loadResult = DialogDataLoader.LoadLocalizedDialogData(currentDialogFile);
        if (!loadResult.success || loadResult.dialogData == null)
        {
            Debug.LogError($"[{GetType().Name}] 重載對話失敗: {currentDialogFile}");
            return;
        }
        
        // 更新對話數據和對話行
        dialogLines = loadResult.dialogLines;
        currentDialogData = loadResult.dialogData;
        // Debug.log($"[{GetType().Name}] 對話數據已更新，共 {dialogLines.Count} 行對話");
        
        // 如果當前有顯示的對話，更新其內容
        if (dialogLines.ContainsKey(currentDialogId))
        {
            var currentLine = dialogLines[currentDialogId];
            
            // 如果打字動畫已完成且有對話文字，即時更新文字內容
            if (isTypingComplete && dialogText != null && !string.IsNullOrEmpty(currentLine.text))
            {
                currentDisplayText = currentLine.text;
                UpdateDialogDisplay();
                // Debug.log($"[{GetType().Name}] 已更新對話文字內容");
            }
            
            // 如果正在顯示選項，重新顯示選項
            if (isDisplayingOptions && currentLine.options.Count > 0)
            {
                ShowOptions(currentLine.options);
                // Debug.log($"[{GetType().Name}] 已更新選項內容，共 {currentLine.options.Count} 個選項");
            }
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] 在新語言數據中找不到當前對話ID: {currentDialogId}");
        }
        
        // Debug.log($"[{GetType().Name}] 對話內容刷新完成");
    }
    
}