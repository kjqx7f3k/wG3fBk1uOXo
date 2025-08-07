using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System;

/// <summary>
/// 旁白對話管理器 - 不阻擋角色移動，自動播放，不處理輸入
/// </summary>
public class NarrationDialogManager : BaseDialogManager
{
    public static NarrationDialogManager Instance { get; private set; }
    
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
        
        // 設定UIPanel屬性 - 旁白對話不阻擋角色移動
        blockCharacterMovement = false;  // 不阻擋角色移動
        
        Debug.Log("NarrationDialogManager 初始化完成");
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("NarrationDialogManager 已清理");
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
    /// 處理自定義輸入 - 旁白對話不處理任何輸入
    /// </summary>
    protected override void HandleCustomInput()
    {
        // 旁白對話完全不處理輸入
        // 空實現
    }
    
    /// <summary>
    /// 顯示選項 - 旁白對話跳過選項
    /// </summary>
    protected override void ShowOptions(List<DialogOption> options)
    {
        // 旁白對話跳過選項，直接選擇第一個選項繼續
        if (options != null && options.Count > 0)
        {
            Debug.Log($"[NarrationDialogManager] 旁白跳過選項，自動選擇第一個選項");
            OnOptionSelected(options[0].nextId);
        }
        else
        {
            // 沒有選項，結束對話
            HideDialogUI();
        }
    }
    
    /// <summary>
    /// 處理沒有選項的對話 - 自動繼續
    /// </summary>
    protected override IEnumerator HandleNoOptionsDialog()
    {
        // 旁白對話不等待輸入，自動繼續
        yield return new WaitForSeconds(0.5f); // 短暫延遲以確保文字顯示完成
        
        // 使用新的下一個對話ID決定邏輯
        int nextDialogId = DetermineNextDialogId(currentDialogId);
        
        if (nextDialogId > 0)
        {
            currentDialogId = nextDialogId;
            StartCoroutine(DisplayNextLine());
        }
        else 
        {
            HideDialogUI(); // 對話結束
        }
    }
    
    /// <summary>
    /// 處理選項選擇 - 自動選擇
    /// </summary>
    protected override void OnOptionSelected(int nextId)
    {
        // 旁白對話自動處理選項選擇
        int actualNextId = DetermineOptionNextDialogId(0, nextId); // 始終使用第一個選項
        
        currentOptions.Clear();
        
        // 檢查是否應該結束對話
        if (actualNextId <= 0) 
        {
            HideDialogUI();
        }
        else
        {
            currentDialogId = actualNextId;
            StartCoroutine(DisplayNextLine());
        }
    }
    
    /// <summary>
    /// 顯示旁白文字（便利方法）
    /// </summary>
    /// <param name="text">旁白文字</param>
    /// <param name="autoHideDelay">自動隱藏延遲（秒），-1 表示不自動隱藏</param>
    public void ShowNarration(string text, float autoHideDelay = -1)
    {
        if (dialogText == null || dialogBackground == null)
        {
            Debug.LogError("NarrationDialogManager: UI組件未設置！");
            return;
        }
        
        // 創建簡單的對話數據
        var dialogData = new DialogData();
        dialogData.dialogName = "Narration";
        dialogData.defaultInitialDialogId = 1;
        dialogData.dialogs = new DialogEntry[1];
        dialogData.dialogs[0] = new DialogEntry
        {
            id = 1,
            nextId = -1, // 結束對話
            text = text,
            events = new GameEvent[0],
            options = new DialogOptionEntry[0]
        };
        
        // 創建對話行
        var dialogLines = new Dictionary<int, DialogLine>();
        dialogLines.Add(1, new DialogLine(1, -1, 0, text, new GameEvent[0]));
        
        // 設置對話數據
        SetDialogData(dialogLines, dialogData);
        
        // 如果設定了自動隱藏延遲，啟動自動隱藏
        if (autoHideDelay > 0)
        {
            StartCoroutine(AutoHideNarration(autoHideDelay));
        }
    }
    
    /// <summary>
    /// 自動隱藏旁白
    /// </summary>
    /// <param name="delay">延遲時間</param>
    /// <returns></returns>
    private IEnumerator AutoHideNarration(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (IsInDialog)
        {
            HideDialogUI();
            Debug.Log($"[NarrationDialogManager] 自動隱藏旁白");
        }
    }
    
    /// <summary>
    /// 手動隱藏旁白
    /// </summary>
    public void HideNarration()
    {
        if (IsInDialog)
        {
            HideDialogUI();
            Debug.Log($"[NarrationDialogManager] 手動隱藏旁白");
        }
    }
    
    /// <summary>
    /// 檢查旁白是否正在顯示
    /// </summary>
    /// <returns>是否正在顯示旁白</returns>
    public bool IsShowingNarration()
    {
        return IsInDialog;
    }
}