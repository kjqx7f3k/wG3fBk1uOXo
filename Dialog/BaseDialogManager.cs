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
    [SerializeField] protected string dialogBasePath = "StreamingAssets/Dialogs"; // 對話文件基礎路徑
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
        public DialogCondition condition;
        public int dialogId;
    }
    
    [System.Serializable]
    public class DialogEntry
    {
        public int id;
        public int nextId;
        public int expressionId;
        public string text;
        public DialogEvent[] events;
        public NextDialogCondition[] nextDialogConditions; // 新增：下一個對話條件
        public DialogOptionEntry[] options;
    }
    
    [System.Serializable]
    public class NextDialogCondition
    {
        public DialogCondition condition;
        public int nextId;
    }
    
    [System.Serializable]
    public class DialogOptionEntry
    {
        public string text;
        public int nextId;
        public DialogCondition condition; // 新增：選項顯示條件
        public string failText; // 新增：條件不滿足時的提示文本
        public ConditionalNextDialog[] conditionalNextDialogs; // 新增：條件性下一個對話
    }
    
    [System.Serializable]
    public class ConditionalNextDialog
    {
        public DialogCondition condition;
        public int nextId;
    }
    
    [System.Serializable]
    public class DialogCondition
    {
        public string type; // 條件類型：TAG_CHECK, ITEM_OWNED, QUEST_STATUS, PLAYER_LEVEL 等
        public string param; // 參數：tagId, itemId, questId 等
        public string value; // 目標值：可以是數字或字串
        public string @operator; // 比較運算子：EQUAL, GREATER_EQUAL, LESS_THAN, NOT_EQUAL 等
    }
    
    [System.Serializable]
    public class DialogEvent
    {
        public string event_type; // 事件類型：update_tag, give_item, take_item 等
        public string param1; // 第一個參數：tagId, itemId 等
        public string param2; // 第二個參數：tag值, item數量 等
        public DialogCondition condition; // 事件觸發條件
    }
    
    public class DialogLine
    {
        public int id;
        public int nextId;
        public int expressionId;
        public string text;
        public string eventName;
        public DialogEvent[] events;
        public List<DialogOption> options;
        
        public DialogLine(int id, int nextId, int expressionId, string text, DialogEvent[] events)
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
        Debug.Log($"[{GetType().Name}] 開始初始化多語言系統，基礎路徑: {dialogBasePath}");
        
        int loadedLanguageCount = DialogDataLoader.LoadAllLanguageDialogs(dialogBasePath);
        
        if (loadedLanguageCount > 0)
        {
            // 設定預設語言
            if (!DialogDataLoader.SetCurrentLanguage(defaultLanguage))
            {
                // 如果預設語言不存在，使用第一個可用語言
                string[] supportedLanguages = DialogDataLoader.GetSupportedLanguages();
                if (supportedLanguages.Length > 0)
                {
                    DialogDataLoader.SetCurrentLanguage(supportedLanguages[0]);
                    Debug.LogWarning($"[{GetType().Name}] 預設語言 '{defaultLanguage}' 不存在，改用 '{supportedLanguages[0]}'");
                }
            }
            
            Debug.Log($"[{GetType().Name}] 多語言系統初始化完成，載入 {loadedLanguageCount} 種語言");
            Debug.Log($"[{GetType().Name}] 當前語言: {DialogDataLoader.GetCurrentLanguage()}");
            Debug.Log($"[{GetType().Name}] 支援語言: {string.Join(", ", DialogDataLoader.GetSupportedLanguages())}");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 多語言系統初始化失敗，無法載入任何語言文件，路徑: {dialogBasePath}");
        }
    }
    
    protected virtual void OnDestroy()
    {
        // 清理引用
        dialogLines.Clear();
        currentOptions.Clear();
        currentModel = null;
        currentDialogData = null;
        
        Debug.Log($"{GetType().Name} 已清理");
    }
    
    protected virtual void OnApplicationQuit()
    {
        // 應用程式退出時清理
    }
    
    protected void HideDialogUI()
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
        Debug.Log($"隱藏{GetType().Name}");
        if (currentModel != null)
        {
            currentModel.SetActive(false);
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
        
        Debug.Log($"{GetType().Name}對話結束");
    }
    
    protected void ShowDialogUI(GameObject model)
    {
        // 首先確保DialogPanel本身是啟用的，使用重寫的Open方法
        Open();
        EnsureOptionsUIInitialState(); // 新增：在顯示UI時，確保選項被重置和隱藏
        Debug.Log($"啟用{GetType().Name}");
        if (model != null)
        {
            currentModel = model;
            currentModel.SetActive(true);
            Debug.Log("啟用對話模型");
        }
        
        Debug.Log($"{GetType().Name}對話開始");
    }
    
    /// <summary>
    /// 面板開啟時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnOpened()
    {
        base.OnOpened();
        Debug.Log($"{GetType().Name}UI已開啟");
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
            currentModel.SetActive(false);
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
        
        Debug.Log($"{GetType().Name}對話結束");
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
            Debug.Log($"[{GetType().Name}] 強制關閉對話");
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
        
        Debug.Log($"[{GetType().Name}] 成功載入本地化對話: {dialogId} [{DialogDataLoader.GetCurrentLanguage()}]");
        
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
        // TODO: 根據expressionId更新模型表情
        
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
                DialogEventProcessor.ProcessDialogEvents(currentLine.events);
            }
        }
        else
        {
            // 沒有文字時，隱藏對話文字UI
            isTypingComplete = true;
            
            // 即使沒有文字也要處理事件
            if (currentLine.events != null && currentLine.events.Length > 0)
            {
                DialogEventProcessor.ProcessDialogEvents(currentLine.events);
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
        Debug.Log($"文字控制: 刪除字串 '{deleteString}'，刪除速度 {deleteTime}，替換為 '{replaceString}'");
        
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
        Debug.Log(DialogCacheManager.GetCacheInfo());
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
                if (DialogConditionChecker.CheckCondition(condition.condition))
                {
                    Debug.Log($"起始對話條件滿足，使用對話ID: {condition.dialogId}");
                    return condition.dialogId;
                }
            }
        }
        
        // 如果沒有條件滿足，使用預設起始對話ID
        int defaultId = currentDialogData.defaultInitialDialogId > 0 ? currentDialogData.defaultInitialDialogId : 1;
        Debug.Log($"使用預設起始對話ID: {defaultId}");
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
                if (DialogConditionChecker.CheckCondition(condition.condition))
                {
                    Debug.Log($"下一個對話條件滿足，跳轉到對話ID: {condition.nextId}");
                    return condition.nextId;
                }
            }
        }
        
        // 如果沒有條件滿足，使用預設的nextId
        Debug.Log($"使用預設下一個對話ID: {currentEntry.nextId}");
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
            if (optionEntry.condition == null || DialogConditionChecker.CheckCondition(optionEntry.condition))
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
                if (DialogConditionChecker.CheckCondition(conditionalNext.condition))
                {
                    Debug.Log($"選項條件性下一個對話條件滿足，跳轉到對話ID: {conditionalNext.nextId}");
                    return conditionalNext.nextId;
                }
            }
        }
        
        // 如果沒有條件滿足，使用預設的nextId
        Debug.Log($"使用選項預設下一個對話ID: {selectedOption.nextId}");
        return selectedOption.nextId;
    }
}