using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System;

/// <summary>
/// 交互式對話管理器 - 阻擋角色移動，處理選項選擇
/// </summary>
public class InteractiveDialogManager : BaseDialogManager
{
    public static InteractiveDialogManager Instance { get; private set; }
    
    protected override void Awake()
    {
        // 標準 Singleton 寫法，避免編輯器中的重複執行問題
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        base.Awake(); // 呼叫基底類別的Awake
        
        // 設定UIPanel屬性 - 交互式對話阻擋角色移動
        blockCharacterMovement = true;  // 阻擋角色移動
        
        Debug.Log("InteractiveDialogManager 初始化完成");
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("InteractiveDialogManager 已清理");
        }
    }
    
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// 處理自定義輸入 - 交互式對話處理完整輸入
    /// </summary>
    protected override void HandleCustomInput()
    {
        // 處理對話的鍵盤輸入
        HandleDialogInput();
    }
    
    /// <summary>
    /// 處理對話輸入邏輯
    /// </summary>
    private void HandleDialogInput()
    {
        // 修改：處理跳過打字動畫的邏輯
        // 當對話正在播放、打字動畫未完成且玩家按下空白鍵時
        if (InputSystemWrapper.Instance == null)
        {
            Debug.LogError("[InteractiveDialogManager] InputSystemWrapper instance not found!");
            return;
        }
        
        bool confirmPressed = InputSystemWrapper.Instance.GetUIConfirmDown();
        
        if (isDisplayingDialog && !isTypingComplete && confirmPressed)
        {
            isSkippingAnimation = true;
        }

        if (currentOptions.Count > 0)
        {
            // 允許在選項顯示過程中或顯示完成後進行導航
            if (!enableTerminalCursor || isDisplayingOptions || isOptionsTypingComplete)
            {
                Vector2 navigation = InputSystemWrapper.Instance.GetUINavigationInput();
                bool confirmInput = InputSystemWrapper.Instance.GetUIConfirmDown();
                
                // 檢查是否有導航輸入並應用冷卻時間
                bool hasNavigationInput = Mathf.Abs(navigation.y) > 0.5f;
                
                if (hasNavigationInput)
                {
                    // 只有當前時間超過了 (上次導航時間 + 冷卻時間) 才執行導航
                    if (Time.unscaledTime > lastNavigationTime + navigationCooldown)
                    {
                        lastNavigationTime = Time.unscaledTime; // 更新上次導航時間
                        
                        // 處理上下導航
                        if (navigation.y > 0.5f)
                        {
                            selectedOptionIndex = (selectedOptionIndex - 1 + currentOptions.Count) % currentOptions.Count;
                            UpdateOptionsDisplay();
                        }
                        else if (navigation.y < -0.5f)
                        {
                            selectedOptionIndex = (selectedOptionIndex + 1) % currentOptions.Count;
                            UpdateOptionsDisplay();
                        }
                    }
                    // 如果在冷卻時間內，忽略導航輸入（不輸出調試訊息以避免日誌洪水）
                }

                // 處理確認鍵（空格或回車）- 只有在選項顯示完成後才接受，確認輸入不受冷卻時間影響
                if (confirmInput && (!enableTerminalCursor || isOptionsTypingComplete))
                {
                    OnOptionSelected(currentOptions[selectedOptionIndex].nextId);
                }
            }
        }
    }
    
    /// <summary>
    /// 顯示選項並帶有完整的交互功能
    /// </summary>
    protected override void ShowOptions(List<DialogOption> options)
    {
        currentOptions = options;
        selectedOptionIndex = 0;
        
        // 不立即啟用選項UI，等待對話文字完成後再啟用
        if (optionsText != null)
        {
            if (enableTerminalCursor) StartCoroutine(DisplayOptionsWithCursor());
            else StartCoroutine(DisplayOptionsAfterDelay());
        }
    }
    
    /// <summary>
    /// 處理沒有選項的對話 - 等待玩家輸入繼續
    /// </summary>
    protected override IEnumerator HandleNoOptionsDialog()
    {
        // 如果沒有選項但有下一個對話ID，等待玩家輸入後繼續
        yield return new WaitUntil(() => {
            if (InputSystemWrapper.Instance == null)
            {
                Debug.LogError("[InteractiveDialogManager] InputSystemWrapper instance not found!");
                return false;
            }
            
            return InputSystemWrapper.Instance.GetUIConfirmDown();
        });
            
        // 使用新的下一個對話ID決定邏輯
        int nextDialogId = DetermineNextDialogId(currentDialogId);
        
        if (nextDialogId > 0)
        {
            currentDialogId = nextDialogId;
            StartCoroutine(DisplayNextLine());
        }
        else HideDialogUI(); // 對話結束
    }
    
    /// <summary>
    /// 處理選項選擇
    /// </summary>
    protected override void OnOptionSelected(int nextId)
    {
        // 使用新的選項條件性下一個對話ID決定邏輯
        int actualNextId = DetermineOptionNextDialogId(selectedOptionIndex, nextId);
        
        optionsText.gameObject.SetActive(false);
        currentOptions.Clear();
        
        // 檢查是否應該結束對話
        if (actualNextId <= 0) HideDialogUI();
        else
        {
            currentDialogId = actualNextId;
            StartCoroutine(DisplayNextLine());
        }
    }
    
    private void UpdateOptionsDisplay()
    {
        if (optionsText != null)
        {
            // 如果啟用終端效果，使用動態前綴更新
            if (enableTerminalCursor)
            {
                // 基於baseOptionsText動態添加選擇前綴
                if (isDisplayingOptions || isOptionsTypingComplete)
                {
                    UpdateOptionsDisplayWithCursor();
                }
            }
            else
            {
                // 普通顯示模式
                string optionsDisplay = "";
                for (int i = 0; i < currentOptions.Count; i++)
                {
                    if (i == selectedOptionIndex)
                    {
                        optionsDisplay += "> " + currentOptions[i].text + "\n";
                    }
                    else
                    {
                        optionsDisplay += "  " + currentOptions[i].text + "\n";
                    }
                }
                optionsText.text = optionsDisplay;
            }
        }
    }
    
    /// <summary>
    /// 顯示選項並帶有終端游標效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator DisplayOptionsWithCursor()
    {
        // 等待對話文字完全顯示完成
        yield return new WaitUntil(() => isTypingComplete);
        
        // 啟用選項UI
        optionsText.gameObject.SetActive(true);
        
        isDisplayingOptions = true;
        isOptionsTypingComplete = false;
        currentOptionsText = "";
        baseOptionsText = ""; // 重置基礎文字
        currentOptionIndex = 0;
        
        // 停止之前的選項游標閃爍
        if (optionsCursorBlinkCoroutine != null) StopCoroutine(optionsCursorBlinkCoroutine);
        
        // 開始選項游標閃爍
        optionsCursorBlinkCoroutine = StartCoroutine(BlinkOptionsCursor());
        
        // 逐個顯示選項（不包含前綴，純文字內容）
        for (int i = 0; i < currentOptions.Count; i++)
        {
            string optionLine = currentOptions[i].text;
            
            // 逐字顯示當前選項
            foreach (char c in optionLine)
            {
                baseOptionsText += c;
                UpdateOptionsDisplayWithCursor();
                yield return new WaitForSeconds(typingSpeed * 0.5f); // 選項顯示稍快一些
            }
            
            // 只在不是最後一個選項時添加換行符
            if (i < currentOptions.Count - 1)
            {
                baseOptionsText += "\n";
                UpdateOptionsDisplayWithCursor();
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        
        // 選項顯示完成，但保持游標閃爍
        isOptionsTypingComplete = true;
        isDisplayingOptions = false;
        
        // 最終更新顯示以確保正確的選擇狀態
        UpdateOptionsDisplayWithCursor();
    }
    
    /// <summary>
    /// 選項游標閃爍協程
    /// </summary>
    /// <returns></returns>
    private IEnumerator BlinkOptionsCursor()
    {
        while (true)
        {
            isOptionsCursorVisible = !isOptionsCursorVisible;
            UpdateOptionsDisplayWithCursor();
            yield return new WaitForSeconds(1f / cursorBlinkSpeed);
        }
    }
    
    /// <summary>
    /// 更新選項顯示（包含游標效果）
    /// </summary>
    private void UpdateOptionsDisplayWithCursor()
    {
        if (optionsText == null) return;
        
        // 基於baseOptionsText動態構建帶前綴的顯示文字
        string[] optionLines = baseOptionsText.Split('\n');
        currentOptionsText = "";
        
        for (int i = 0; i < optionLines.Length && i < currentOptions.Count; i++)
        {
            // 動態添加選擇前綴
            string prefix = (i == selectedOptionIndex) ? "> " : "  ";
            currentOptionsText += prefix + optionLines[i];
            
            // 只在不是最後一行時添加換行符
            if (i < optionLines.Length - 1)
            {
                currentOptionsText += "\n";
            }
        }
        
        // 顯示文字和游標
        string displayText = currentOptionsText;
        
        if (enableTerminalCursor)
        {
            // 添加選項游標效果
            string colorHex = ColorUtility.ToHtmlStringRGB(cursorColor);
            if (isOptionsCursorVisible) displayText += $"<color=#{colorHex}>{cursorCharacter}</color>";
            else displayText += $"<color=#00000000>{cursorCharacter}</color>";
        }
        optionsText.text = displayText;
    }

    /// <summary>
    /// 在延遲後顯示選項（用於非終端效果模式）
    /// </summary>
    /// <returns></returns>
    private IEnumerator DisplayOptionsAfterDelay()
    {
        // 等待對話文字完全顯示完成
        yield return new WaitUntil(() => isTypingComplete);
        
        // 啟用選項UI
        optionsText.gameObject.SetActive(true);
        
        // 標記選項輸入完成，允許玩家操作
        isOptionsTypingComplete = true;
        
        // 更新選項顯示
        UpdateOptionsDisplay();
    }
}

/// <summary>
/// 向後兼容性：保留原有的 DialogManager 引用
/// </summary>
public class DialogManager
{
    public static InteractiveDialogManager Instance => InteractiveDialogManager.Instance;
}