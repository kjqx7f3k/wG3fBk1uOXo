// Path: Assets/Scripts/GameMenuManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems; // 新增: 處理 UI 選中狀態

/// <summary>
/// 遊戲選單管理器 - 獨立管理遊戲選單UI和功能
/// </summary>
public class GameMenuManager : UIPanel
{
    public static GameMenuManager Instance { get; private set; }
    
    [Header("遊戲選單按鈕引用")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitGameButton;
    
    // 遊戲選單導航
    private int selectedMenuIndex = 0;
    private Button[] menuButtons;

    [Header("導航設定")]
    [SerializeField] private float navigationCooldown = 0.2f; // 導航冷卻時間（秒）
    private float lastNavigationTime = 0f; // 上次導航的時間
    
    public bool IsGameMenuOpen => IsOpen;
    
    protected override void Awake()
    {
        base.Awake(); // 呼叫基底類別的Awake
        
        // 設定UIPanel屬性
        pauseGameWhenOpen = true;   // 遊戲選單暫停遊戲
        blockCharacterMovement = true;  // 阻擋角色移動
        canCloseWithEscape = true;  // 可用ESC關閉
        
        // 檢查是否已經有Instance且不是當前物件
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameMenuManager已存在，銷毀重複實例");
            Destroy(gameObject);
            return;
        }
        
        // 如果Instance為null或是當前物件，則設置為Instance
        if (Instance == null)
        {
            Instance = this;
            
            // 安全地調用DontDestroyOnLoad
            try
            {
                DontDestroyOnLoad(gameObject);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"GameMenuManager DontDestroyOnLoad失敗: {e.Message}");
            }
            
            Debug.Log("GameMenuManager初始化完成");
        }
    }
    
    private void Start()
    {
        SetupButtonEvents();
        InitializeMenuButtons();
        
        // 確保面板初始為關閉狀態
        if (panelCanvas != null)
        {
            panelCanvas.enabled = false;
        }
        isOpen = false;
    }
    
    protected override void Update()
    {
        // 檢查ESC鍵輸入（只檢查一次）
        bool escPressed = InputSystemWrapper.Instance != null && InputSystemWrapper.Instance.GetUICancelDown();
        
        if (escPressed)
        {
            Debug.Log($"[GameMenuManager] ESC鍵被按下，當前選單狀態: {isOpen}");
        }
        
        // 根據當前狀態決定如何處理ESC鍵
        if (escPressed)
        {
            if (isOpen)
            {
                // 選單已開啟，ESC鍵用於關閉選單
                Debug.Log("[GameMenuManager] 選單已開啟，ESC鍵關閉選單");
                CloseGameMenu();
            }
            else
            {
                // 選單未開啟，檢查是否可以開啟選單
                HandleEscToOpenMenu();
            }
        }
        
        // 處理選單內部的非ESC輸入（當選單開啟時）
        if (isOpen)
        {
            HandleMenuNavigation();
        }
    }
    
    /// <summary>
    /// 處理ESC鍵開啟選單的邏輯
    /// </summary>
    private void HandleEscToOpenMenu()
    {
        bool anyUIOpen = IsAnyUIOpen();
        Debug.Log($"[GameMenuManager] 檢查是否可開啟選單，其他UI開啟狀態: {anyUIOpen}");
        
        if (!anyUIOpen)
        {
            Debug.Log("[GameMenuManager] 沒有其他UI開啟，開啟遊戲選單");
            OpenGameMenu();
        }
        else
        {
            Debug.Log("[GameMenuManager] 有其他UI開啟，讓那些UI自己處理ESC鍵");
        }
    }
    
    /// <summary>
    /// 處理選單的導航輸入（不包括ESC鍵）
    /// </summary>
    private void HandleMenuNavigation()
    {
        // 檢查是否有其他UI覆蓋
        bool anotherUIActive = IsAnotherUIActive();
        
        if (anotherUIActive)
        {
            Debug.Log("[GameMenuManager] 其他UI處於活躍狀態，跳過選單導航");
            return;
        }
        
        // 處理選單導航輸入
        HandleGameMenuInput();
    }
    
    
    /// <summary>
    /// 檢查是否有其他UI開啟（不包含自己）
    /// </summary>
    /// <returns>如果有其他UI開啟返回true</returns>
    private bool IsAnyUIOpen()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen)
            return true;
            
        if (DialogManager.Instance != null && DialogManager.Instance.IsInDialog)
            return true;
            
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen)
            return true;
            
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen)
            return true;
        
        return false;
    }

    
    /// <summary>
    /// 檢查是否有其他UI處於活躍狀態，以避免輸入衝突。
    /// </summary>
    private bool IsAnotherUIActive()
    {
        // 檢查存檔UI或設定UI是否已開啟
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen)
        {
            return true;
        }
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen)
        {
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 處理遊戲選單的鍵盤輸入
    /// </summary>
    private void HandleGameMenuInput()
    {
        if (menuButtons == null || menuButtons.Length == 0) 
        {
            Debug.LogWarning("[GameMenuManager] menuButtons 為空或長度為0");
            return;
        }
        
        if (InputSystemWrapper.Instance == null)
        {
            Debug.LogError("[GameMenuManager] InputSystemWrapper instance not found!");
            return;
        }
        
        // 檢查輸入系統狀態
        if (!InputSystemWrapper.Instance.IsUIInputEnabled())
        {
            Debug.LogWarning($"[GameMenuManager] UI輸入未啟用！狀態: {InputSystemWrapper.Instance.GetInputSystemStatus()}");
            // 嘗試重新啟用
            InputSystemWrapper.Instance.EnableUIInput();
        }
        
        Vector2 navigation = InputSystemWrapper.Instance.GetUINavigationInput();
        bool confirmInput = InputSystemWrapper.Instance.GetUIConfirmDown();
        
        // 調試：顯示導航輸入
        if (navigation.sqrMagnitude > 0.1f)
        {
            Debug.Log($"[GameMenuManager] 檢測到導航輸入: {navigation}");
        }
        
        // --- 新增的冷卻時間判斷 ---
        if (navigation.sqrMagnitude > 0.1f) // 檢查是否有導航輸入
        {
            // 只有當前時間超過了 (上次導航時間 + 冷卻時間) 才執行
            if (Time.unscaledTime > lastNavigationTime + navigationCooldown)
            {
                lastNavigationTime = Time.unscaledTime; // 更新上次導航時間
                Debug.Log($"[GameMenuManager] 處理導航輸入，當前選中索引: {selectedMenuIndex}");

                // 上下箭頭鍵或W/S鍵選擇按鈕
                if (navigation.y > 0.5f)
                {
                    Debug.Log("[GameMenuManager] 向上導航");
                    SelectPreviousMenuButton();
                }
                else if (navigation.y < -0.5f)
                {
                    Debug.Log("[GameMenuManager] 向下導航");
                    SelectNextMenuButton();
                }
            }
            else
            {
                Debug.Log("[GameMenuManager] 導航輸入被冷卻時間阻止");
            }
        }
        
        // Enter鍵或Space鍵執行選中的按鈕
        if (confirmInput)
        {
            Debug.Log($"[GameMenuManager] 確認輸入，執行選中按鈕: {selectedMenuIndex}");
            ExecuteSelectedMenuButton();
        }
    }
    
    private void InitializeUI()
    {
        // 腳本直接掛載在prefab上，不需要動態實例化
        Debug.Log("GameMenuManager 已直接掛載在prefab上");
    }
    
    private void SetupButtonEvents()
    {
        if (saveGameButton != null)
            saveGameButton.onClick.AddListener(SaveGame);
            
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(LoadGame);
        }
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
            
        if (exitGameButton != null)
            exitGameButton.onClick.AddListener(ExitGame);
            
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
    }
    
    public void ToggleGameMenu()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }
    
    /// <summary>
    /// 檢查是否有其他暫停遊戲的UI開啟
    /// </summary>
    /// <returns>如果有其他暫停UI返回true</returns>
    private bool HasOtherPausingUI()
    {
        // 檢查常見的暫停UI
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen && SaveUIController.Instance.PausesGame)
            return true;
            
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen && PlayerGameSettingsUI.Instance.PausesGame)
            return true;
        
        return false;
    }
    
    public void OpenGameMenu()
    {
        Debug.Log($"[GameMenuManager] OpenGameMenu 被調用，當前狀態: isOpen={isOpen}");
        Open();
    }
    
    public void CloseGameMenu()
    {
        Debug.Log($"[GameMenuManager] CloseGameMenu 被調用，當前狀態: isOpen={isOpen}");
        Close();
    }
    
    /// <summary>
    /// 面板開啟時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnOpened()
    {
        base.OnOpened();
        Debug.Log("遊戲選單已開啟");
        
        // 重置所有按鈕狀態
        ResetAllButtonStates();
        
        ResetMenuSelection();
    }
    
    /// <summary>
    /// 面板關閉時調用 - 重寫UIPanel方法
    /// </summary>
    protected override void OnClosed()
    {
        base.OnClosed();
        Debug.Log("遊戲選單已關閉");
    }
    
    private void ResumeGame()
    {
        CloseGameMenu();
    }
    
    private void SaveGame()
    {
        if (SaveManager.Instance != null)
        {
            // 生成當前時間的檔名
            string timeStamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            bool saveSuccess = SaveManager.Instance.SaveGameWithCustomFileName(timeStamp);
            
            if (saveSuccess)
            {
                Debug.Log($"遊戲已儲存: {timeStamp}.eqg");
                
                // 直接設置按鈕為已儲存狀態
                if (saveGameButton != null)
                {
                    TextMeshProUGUI buttonText = saveGameButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = "已儲存";
                    }
                    saveGameButton.interactable = false;
                }
            }
            else
            {
                Debug.LogError("儲存遊戲失敗");
                ShowMessage("儲存失敗！請檢查磁碟空間");
            }
        }
        else
        {
            Debug.LogError("SaveManager Instance 未找到！");
            ShowMessage("儲存系統未初始化");
        }
    }
    
// Path: Assets/Scripts/GameMenuManager.cs
// (程式碼其餘部分保持不變)

    /// <summary>
    /// 載入遊戲功能 - 現在會開啟存檔 UI
    /// </summary>
    private void LoadGame()
    {
        if (SaveUIController.Instance != null)
        {
            // 關閉遊戲選單
            CloseGameMenu();
            
            // // === 修正: 每次開啟存檔UI時，都添加監聽器。關閉時會移除，避免重複呼叫 ===
            // SaveUIController.Instance.OnCloseUI.AddListener(OpenGameMenu);
            
            // 開啟存檔 UI
            SaveUIController.Instance.OpenSaveUI();
        }
        else
        {
            Debug.LogError("SaveUIController Instance not found! Please make sure it exists in the scene.");
        }
    }

    // (程式碼其餘部分保持不變)

    private void OpenSettings()
    {
        if (PlayerGameSettingsUI.Instance != null)
        {
            // 關閉遊戲選單
            CloseGameMenu();
            
            // 開啟設定UI
            PlayerGameSettingsUI.Instance.OpenSettings();
        }
        else
        {
            Debug.LogError("PlayerGameSettingsUI Instance not found! Please make sure it exists in the scene.");
            ShowMessage("設定系統未初始化");
        }
    }
    
    private void ExitGame()
    {
        Debug.Log("離開遊戲");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void ShowMessage(string message)
    {
        Debug.Log($"遊戲選單訊息: {message}");
    }
    
    public bool ShouldBlockGameInput()
    {
        return IsOpen;
    }
    
    // ==================== 遊戲選單導航功能 ====================
    
    private void InitializeMenuButtons()
    {
        // 將所有引用的按鈕加入陣列（Inspector中需要手動設置）
        menuButtons = new Button[]
        {
            resumeButton,
            saveGameButton,
            loadGameButton,
            settingsButton,
            exitGameButton
        };
        
        // 過濾掉null的按鈕
        System.Collections.Generic.List<Button> validButtons = new System.Collections.Generic.List<Button>();
        foreach (Button btn in menuButtons)
        {
            if (btn != null)
            {
                validButtons.Add(btn);
            }
            else
            {
                Debug.LogWarning("發現未設置的按鈕引用！請在Inspector中設置所有按鈕引用。");
            }
        }
        menuButtons = validButtons.ToArray();
        
        selectedMenuIndex = 0;
        
        Debug.Log($"初始化選單按鈕完成，共 {menuButtons.Length} 個按鈕");
    }
    
    private void SelectPreviousMenuButton()
    {
        if (menuButtons == null || menuButtons.Length == 0) return;
        
        int startIndex = selectedMenuIndex;
        do
        {
            selectedMenuIndex = selectedMenuIndex <= 0 ? menuButtons.Length - 1 : selectedMenuIndex - 1;
        } 
        while (menuButtons[selectedMenuIndex] != null && !menuButtons[selectedMenuIndex].interactable && selectedMenuIndex != startIndex);
        
        UpdateMenuButtonSelection();
    }
    
    private void SelectNextMenuButton()
    {
        if (menuButtons == null || menuButtons.Length == 0) return;
        
        int startIndex = selectedMenuIndex;
        do
        {
            selectedMenuIndex = selectedMenuIndex >= menuButtons.Length - 1 ? 0 : selectedMenuIndex + 1;
        } 
        while (menuButtons[selectedMenuIndex] != null && !menuButtons[selectedMenuIndex].interactable && selectedMenuIndex != startIndex);
        
        UpdateMenuButtonSelection();
    }
    
    private void ExecuteSelectedMenuButton()
    {
        if (menuButtons == null || selectedMenuIndex < 0 || selectedMenuIndex >= menuButtons.Length) return;
        
        Button selectedButton = menuButtons[selectedMenuIndex];
        if (selectedButton != null && selectedButton.interactable)
        {
            selectedButton.onClick.Invoke();
        }
    }
    
    private void UpdateMenuButtonSelection()
    {
        Debug.Log($"[GameMenuManager] UpdateMenuButtonSelection 被調用，selectedMenuIndex: {selectedMenuIndex}");
        
        if (menuButtons == null) 
        {
            Debug.LogWarning("[GameMenuManager] menuButtons 為 null");
            return;
        }
        
        Debug.Log($"[GameMenuManager] menuButtons 長度: {menuButtons.Length}");
        
        for (int i = 0; i < menuButtons.Length; i++)
        {
            Button button = menuButtons[i];
            if (button == null) 
            {
                Debug.LogWarning($"[GameMenuManager] menuButtons[{i}] 為 null");
                continue;
            }
            
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                string currentText = buttonText.text;
                
                // 只有可互動的按鈕才能顯示選中狀態
                if (i == selectedMenuIndex && button.interactable)
                {
                    // 如果當前選中但文字沒有前綴，添加前綴
                    if (!currentText.StartsWith("> "))
                    {
                        string newText = "> " + currentText;
                        buttonText.text = newText;
                        Debug.Log($"[GameMenuManager] 為按鈕 {i} 添加前綴: '{currentText}' -> '{newText}'");
                    }
                    else
                    {
                        Debug.Log($"[GameMenuManager] 按鈕 {i} 已有前綴: '{currentText}'");
                    }
                }
                else
                {
                    // 如果不是選中或不可互動但文字有前綴，移除前綴
                    if (currentText.StartsWith("> "))
                    {
                        string newText = currentText.Substring(2);
                        buttonText.text = newText;
                        Debug.Log($"[GameMenuManager] 從按鈕 {i} 移除前綴: '{currentText}' -> '{newText}'");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[GameMenuManager] 按鈕 {i} 沒有 TextMeshProUGUI 組件");
            }
        }
    }
    
    private string GetOriginalButtonText(Button button)
    {
        if (button == resumeButton) return "繼續遊戲";
        if (button == saveGameButton) return "儲存遊戲";
        if (button == loadGameButton) return "載入遊戲";
        if (button == settingsButton) return "設定";
        if (button == exitGameButton) return "離開遊戲";
        
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            string text = buttonText.text;
            if (text.StartsWith("> "))
            {
                return text.Substring(2);
            }
            return text;
        }
        
        return "未知按鈕";
    }
    
    /// <summary>
    /// 重置所有按鈕狀態為原始狀態
    /// </summary>
    private void ResetAllButtonStates()
    {
        // 重置儲存按鈕
        if (saveGameButton != null)
        {
            TextMeshProUGUI buttonText = saveGameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "儲存遊戲";
            }
            saveGameButton.interactable = true;
        }
        
        // 可以在這裡重置其他按鈕狀態（如果需要的話）
    }
    
    private void ResetMenuSelection()
    {
        selectedMenuIndex = 0;
        UpdateMenuButtonSelection();

        // 在遊戲選單開啟時，讓 EventSystem 選中第一個按鈕
        if (menuButtons != null && menuButtons.Length > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
        }
    }
}
