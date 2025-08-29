// Path: Assets/Scripts/UI/SaveUIController.cs
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public class SaveUIController : UIPanel
{
    // === Singleton 實作 ===
    public static SaveUIController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _uiTitleText;
        [SerializeField] private GameObject _saveGameButtonPrefab;
        [SerializeField] private Transform _contentPanel;
        [SerializeField] private GameObject _noSaveFilesText;
        [SerializeField] private GameObject _actionButtonsPanel;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private TextMeshProUGUI _deleteButtonText;
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _backButtonText;

        [Header("本地化組件")]
        [SerializeField] private TextMeshProUGUI _noSaveFilesLabel;

        [Header("Pagination")]
        [SerializeField] private int _itemsPerPage = 5;
        [SerializeField] private TextMeshProUGUI _pageInfoText;

        [Header("Settings")]
        [SerializeField] private string[] _saveFileExtensions = { ".eqg", ".sav" };

        public UnityEvent OnCloseUI = new UnityEvent();

        private string _selectedFileName;
        private Dictionary<Button, TextMeshProUGUI> _buttonTextMapping = new Dictionary<Button, TextMeshProUGUI>();
        private bool _isDeleteConfirming = false;

        // === 分頁相關變數 ===
        private List<string> _allSaveFiles = new List<string>();
        private int _currentPage = 0;
        private int _totalPages = 0;

        // === 重構後的鍵盤導航相關 ===
        private List<Button> _navigableButtons = new List<Button>();
        private int _currentNavIndex = -1;
        
        [Header("導航設定")]
        [SerializeField] private float navigationCooldown = 0.2f; // 導航冷卻時間（秒）
        private float lastNavigationTime = 0f; // 上次導航的時間

        protected override void Awake()
        {
            base.Awake(); // 呼叫基底類別的Awake
            
            // 設定UIPanel屬性
            pauseGameWhenOpen = true;   // 存檔UI暫停遊戲
            blockCharacterMovement = true;  // 阻擋角色移動
            canCloseWithEscape = true;  // 可用ESC關閉
            
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
            // 確保面板初始為關閉狀態
            if (panelCanvas != null)
            {
                panelCanvas.enabled = false;
            }
            isOpen = false;
            
            _loadButton.onClick.AddListener(OnLoadButtonClicked);
            _deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            
            // 返回按鈕的行為會根據當前狀態改變
            _backButton.onClick.AddListener(OnBackButtonClicked);

            // 為了Debug，讓動作按鈕面板總是可見，但按鈕預設不可互動
            _actionButtonsPanel.SetActive(true);
            _loadButton.interactable = false;
            _deleteButton.interactable = false;
            
            // 延遲初始化以確保組件順序正確
            StartCoroutine(DelayedInitialization());
        }

        /// <summary>
        /// 處理自定義輸入 - 重寫UIPanel方法
        /// </summary>
        protected override void HandleCustomInput()
        {
            HandleKeyboardInput();
        }

        public void OpenSaveUI()
        {
            Open(); // 使用UIPanel的Open方法
        }

        private IEnumerator SetDefaultSelectionNextFrame()
        {
            yield return null; // 等待一幀，確保UI佈局更新完畢
            SetDefaultSelection();
        }

        public void CloseSaveUI()
        {
            Close(); // 使用UIPanel的Close方法
        }
        
        /// <summary>
        /// 面板開啟時調用 - 重寫UIPanel方法
        /// </summary>
        protected override void OnOpened()
        {
            RefreshSaveList();

            // 確保在下一幀設置預設選中
            StartCoroutine(SetDefaultSelectionNextFrame());
        }
        
        /// <summary>
        /// 面板關閉時調用 - 重寫UIPanel方法
        /// </summary>
        protected override void OnClosed()
        {
            OnCloseUI?.Invoke();
            // 每次關閉 UI 後，從事件中移除監聽器，避免重複呼叫
            OnCloseUI.RemoveAllListeners();
        }
        
        /// <summary>
        /// 處理ESC鍵邏輯 - 重寫UIPanel方法
        /// </summary>
        protected override void HandleEscapeKey()
        {
            // ESC鍵關閉存檔UI並返回GameMenu
            CloseSaveUI();
        }

        public void RefreshSaveList()
        {
            // 獲取所有存檔文件
            string savePath = Application.persistentDataPath;
            var saveFiles = Directory.GetFiles(savePath)
                .Where(file => _saveFileExtensions.Contains(Path.GetExtension(file).ToLower()))
                .Select(filePath => new FileInfo(filePath))
                .OrderByDescending(fileInfo => fileInfo.LastWriteTime) // 按修改時間降序排序（最新的在前）
                .Select(fileInfo => Path.GetFileNameWithoutExtension(fileInfo.FullName))
                .ToArray();

            // 更新存檔文件列表
            _allSaveFiles.Clear();
            _allSaveFiles.AddRange(saveFiles);

            // 計算總頁數
            _totalPages = _allSaveFiles.Count > 0 ? Mathf.CeilToInt((float)_allSaveFiles.Count / _itemsPerPage) : 1;
            
            // 確保當前頁數在有效範圍內
            _currentPage = Mathf.Clamp(_currentPage, 0, _totalPages - 1);

            DisplayCurrentPage();
            DeselectSaveFile();
        }
        
        /// <summary>
        /// 當玩家透過點擊或鍵盤選擇了一個存檔文件時呼叫
        /// </summary>
        private void SelectSaveFile(string fileName, Button clickedButton)
        {
            _selectedFileName = fileName;
            _isDeleteConfirming = false;
            
            // 讓載入和刪除按鈕可以互動
            _loadButton.interactable = true;
            _deleteButton.interactable = true;
            
            // 重建導航列表以包含 "載入", "刪除", "返回"
            BuildNavigableButtons(true);

_currentNavIndex = _navigableButtons.IndexOf(_loadButton);
            UpdateSelectionVisuals();
        }

        /// <summary>
        /// 取消存檔選擇，返回存檔列表導航
        /// </summary>
        private void DeselectSaveFile()
        {
            _selectedFileName = null;
            _isDeleteConfirming = false;
            
            // 讓載入和刪除按鈕不可互動
            _loadButton.interactable = false;
            _deleteButton.interactable = false;

BuildNavigableButtons(false);
            SetDefaultSelection();
        }

        private void OnLoadButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedFileName)) return;
            
            // 使用真正的SaveManager載入遊戲
            if (SaveManager.Instance != null)
            {
                bool loadSuccess = SaveManager.Instance.LoadGameWithCustomFileName(_selectedFileName);
                if (loadSuccess)
                {
                    CloseSaveUI();
                }
                else
                {
                    Debug.LogError($"[SaveUI] 存檔載入失敗: {_selectedFileName}");
                }
            }
            else
            {
                Debug.LogError("[SaveUI] SaveManager Instance 未找到！");
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedFileName)) return;

            if (_isDeleteConfirming)
            {
                string filePath = Path.Combine(Application.persistentDataPath, _selectedFileName + GetExtensionForFile(_selectedFileName));
                
                // 直接使用File.Delete
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else
                {
                    Debug.LogWarning($"[SaveUI] 嘗試刪除不存在的文件: {filePath}");
                }
                
RefreshSaveList();
            }
            else
            {
                _isDeleteConfirming = true;
                UpdateSelectionVisuals();
            }
        }

        /// <summary>
        /// 返回按鈕的統一處理函式
        /// </summary>
        private void OnBackButtonClicked()
        {
            GameMenuManager.Instance.OpenGameMenu();
            CloseSaveUI();
        }

        private string GetExtensionForFile(string fileName)
        {
            string fullPathWithoutExtension = Path.Combine(Application.persistentDataPath, fileName);
            foreach (string ext in _saveFileExtensions)
            {
                if (File.Exists(fullPathWithoutExtension + ext)) { return ext; }
            }
            return "";
        }

        // ==================== 重構後的鍵盤導航功能 ====================

        private void HandleKeyboardInput()
        {
            if (InputSystemWrapper.Instance == null)
            {
                Debug.LogError("[SaveUIController] InputSystemWrapper instance not found!");
                return;
            }
            
            Vector2 navigation = InputSystemWrapper.Instance.GetUINavigationInput();
            bool confirmInput = InputSystemWrapper.Instance.GetUIConfirmDown();
            bool cancelInput = InputSystemWrapper.Instance.GetUICancelDown();
            
            // 檢查是否有導航輸入並應用冷卻時間
            bool hasNavigationInput = Mathf.Abs(navigation.y) > 0.5f || Mathf.Abs(navigation.x) > 0.5f;
            
            if (hasNavigationInput)
            {
                if (Time.unscaledTime > lastNavigationTime + navigationCooldown)
                {
                    lastNavigationTime = Time.unscaledTime;
                    
                    if (navigation.y > 0.5f)
                    {
                        Navigate(-1);
                    }
                    else if (navigation.y < -0.5f)
                    {
                        Navigate(1);
                    }
                    else if (navigation.x < -0.5f)
                    {
                        SwitchToSaveListNavigation();
                    }
                    else if (navigation.x > 0.5f)
                    {
                        SwitchToActionButtonsNavigation();
                    }
                }
                // 如果在冷卻時間內，忽略導航輸入
            }
            
            // 確認和取消輸入不受冷卻時間影響
            if (confirmInput)
            {
                ExecuteSelectedButton();
            }
            else if (cancelInput)
            {
                // ESC 鍵的功能和返回按鈕完全一樣
                OnBackButtonClicked();
            }
        }

        /// <summary>
        /// 根據當前狀態建立可導航的按鈕列表
        /// </summary>
        /// <param name="isSaveSelected">是否已經選擇了一個存檔</param>
        private void BuildNavigableButtons(bool isSaveSelected)
        {
            _navigableButtons.Clear();

            if (isSaveSelected)
            {
                // 狀態2: 玩家已選擇存檔，可導航按鈕為 載入、刪除、返回
                _navigableButtons.Add(_loadButton);
                _navigableButtons.Add(_deleteButton);
                _navigableButtons.Add(_backButton);
            }
            else
            {
                // 狀態1: 預設狀態，可導航按鈕只包含存檔項目
                foreach (var button in _buttonTextMapping.Keys)
                {
                    _navigableButtons.Add(button);
                }
            }
        }
        
        /// <summary>
        /// 設定預設選中的項目
        /// </summary>
        private void SetDefaultSelection()
        {
            if (_navigableButtons.Count > 0)
            {
                _currentNavIndex = _buttonTextMapping.Count > 0 ? 0 : 0;
            }
            else
            {
                _currentNavIndex = -1;
            }
            UpdateSelectionVisuals();
        }

        /// <summary>
        /// 導航到上一個或下一個按鈕
        /// </summary>
        /// <param name="direction">-1 為上, 1 為下</param>
        private void Navigate(int direction)
        {
            if (!string.IsNullOrEmpty(_selectedFileName))
            {
                if (_navigableButtons.Count <= 1) return;

                _currentNavIndex += direction;

                if (_currentNavIndex < 0) { _currentNavIndex = _navigableButtons.Count - 1; }
                if (_currentNavIndex >= _navigableButtons.Count) { _currentNavIndex = 0; }

                UpdateSelectionVisuals();
                return;
            }

            // 存檔列表導航邏輯（支持自動換頁）
            if (_navigableButtons.Count == 0) return;

            // 如果總頁數只有一頁，則在頁內循環
            if (_totalPages <= 1)
            {
                if (_navigableButtons.Count <= 1) return;
                
                _currentNavIndex += direction;
                if (_currentNavIndex < 0) { _currentNavIndex = _navigableButtons.Count - 1; }
                if (_currentNavIndex >= _navigableButtons.Count) { _currentNavIndex = 0; }
                
                UpdateSelectionVisuals();
                return;
            }

            // 多頁情況下的換頁邏輯
            int newIndex = _currentNavIndex + direction;
            
            if (newIndex < 0)
            {
                _currentPage = (_currentPage - 1 + _totalPages) % _totalPages;
                DisplayCurrentPage();
                BuildNavigableButtons(false);
                _currentNavIndex = _buttonTextMapping.Count - 1;
            }
            else if (newIndex >= _navigableButtons.Count)
            {
                _currentPage = (_currentPage + 1) % _totalPages;
                DisplayCurrentPage();
                BuildNavigableButtons(false);
                _currentNavIndex = 0;
            }
            else
            {
                _currentNavIndex = newIndex;
            }

            UpdateSelectionVisuals();
        }
        
        /// <summary>
        /// 執行當前用鍵盤選中的按鈕的點擊事件
        /// </summary>
        private void ExecuteSelectedButton()
        {
            if (_currentNavIndex < 0 || _currentNavIndex >= _navigableButtons.Count) return;

            Button selectedButton = _navigableButtons[_currentNavIndex];
            if (selectedButton != null && selectedButton.interactable)
            {
                selectedButton.onClick.Invoke();
            }
        }

        /// <summary>
        /// 切換到存檔列表導航模式
        /// </summary>
        private void SwitchToSaveListNavigation()
        {
            if (string.IsNullOrEmpty(_selectedFileName) || _buttonTextMapping.Count == 0) return;
            
            // 記住當前選中的存檔文件名
            string rememberedFileName = _selectedFileName;
            
            DeselectSaveFile();
            
            NavigateToSaveFile(rememberedFileName);
        }

        /// <summary>
        /// 切換到操作按鈕導航模式
        /// </summary>
        private void SwitchToActionButtonsNavigation()
        {
            if (!string.IsNullOrEmpty(_selectedFileName) || _buttonTextMapping.Count == 0) return;
            
            if (_currentNavIndex >= 0 && _currentNavIndex < _navigableButtons.Count)
            {
                Button selectedButton = _navigableButtons[_currentNavIndex];
                if (_buttonTextMapping.ContainsKey(selectedButton))
                {
                    string selectedPrefix = GetLocalizedPrefix();
                    string fileName = _buttonTextMapping[selectedButton].text.TrimStart(selectedPrefix.ToCharArray());
                    SelectSaveFile(fileName, selectedButton);
                }
            }
        }
        
        /// <summary>
        /// 更新所有按鈕的視覺表現，確保只有選中的按鈕有 ">" 前綴
        /// </summary>
        private void UpdateSelectionVisuals()
        {
            string selectedPrefix = GetLocalizedPrefix();
            
            // 1. 重置所有按鈕的文字為原始狀態
            foreach (var pair in _buttonTextMapping)
            {
                string originalText = pair.Value.text.TrimStart(selectedPrefix.ToCharArray());
                pair.Value.text = originalText;
            }
            
            // 更新按鈕文字（從本地化系統獲取）
            UpdateButtonTexts();

            // 2. 如果有選中的存檔文件，為該存檔項目加上前綴（即使不在當前導航列表中）
            if (!string.IsNullOrEmpty(_selectedFileName))
            {
                foreach (var pair in _buttonTextMapping)
                {
                    string buttonFileName = pair.Value.text.TrimStart(selectedPrefix.ToCharArray());
                    if (buttonFileName == _selectedFileName)
                    {
                        pair.Value.text = selectedPrefix + buttonFileName;
                        break;
                    }
                }
            }

            // 3. 為當前導航選中的按鈕加上前綴
            if (_currentNavIndex >= 0 && _currentNavIndex < _navigableButtons.Count)
            {
                Button selectedButton = _navigableButtons[_currentNavIndex];
                TextMeshProUGUI textComponent = selectedButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    string currentText = textComponent.text.TrimStart(selectedPrefix.ToCharArray());
                    textComponent.text = selectedPrefix + currentText;
                }
            }
        }
        
        /// <summary>
        /// 更新按鈕文字（從本地化系統獲取或使用回退值）
        /// </summary>
        private void UpdateButtonTexts()
        {
            // 更新載入按鈕文字
            if (_loadButton != null)
            {
                TextMeshProUGUI loadButtonText = _loadButton.GetComponentInChildren<TextMeshProUGUI>();
                if (loadButtonText != null)
                {
                    if (GameSettings.Instance != null && GameSettings.Instance.IsLocalizationInitialized())
                    {
                        loadButtonText.text = GameSettings.Instance.GetLocalizedString("UI_Tables", "saveui.button.load");
                    }
                    else
                    {
                        loadButtonText.text = "載入"; // 回退值
                    }
                }
            }
            
            // 更新刪除按鈕文字（根據狀態）
            if (_deleteButtonText != null)
            {
                string deleteKey = _isDeleteConfirming ? "saveui.button.delete_confirm" : "saveui.button.delete";
                if (GameSettings.Instance != null && GameSettings.Instance.IsLocalizationInitialized())
                {
                    _deleteButtonText.text = GameSettings.Instance.GetLocalizedString("UI_Tables", deleteKey);
                }
                else
                {
                    _deleteButtonText.text = _isDeleteConfirming ? "確認?" : "刪除"; // 回退值
                }
            }
            
            // 更新返回按鈕文字
            if (_backButtonText != null)
            {
                if (GameSettings.Instance != null && GameSettings.Instance.IsLocalizationInitialized())
                {
                    _backButtonText.text = GameSettings.Instance.GetLocalizedString("UI_Tables", "saveui.button.back");
                }
                else
                {
                    _backButtonText.text = "返回"; // 回退值
                }
            }
        }
        
        // ==================== 分頁功能 ====================
        
        /// <summary>
        /// 顯示當前頁的內容
        /// </summary>
        private void DisplayCurrentPage()
        {
            // 清除現有的按鈕
            foreach (Transform child in _contentPanel)
            {
                Destroy(child.gameObject);
            }
            _buttonTextMapping.Clear();

            // 顯示無存檔文件提示
            _noSaveFilesText.SetActive(_allSaveFiles.Count == 0);
            _contentPanel.gameObject.SetActive(_allSaveFiles.Count > 0);

            if (_allSaveFiles.Count == 0)
            {
                UpdatePaginationUI();
                return;
            }

            // 計算當前頁要顯示的項目範圍
            int startIndex = _currentPage * _itemsPerPage;
            int endIndex = Mathf.Min(startIndex + _itemsPerPage, _allSaveFiles.Count);

            // 創建當前頁的按鈕
            for (int i = startIndex; i < endIndex; i++)
            {
                string fileName = _allSaveFiles[i];
                GameObject buttonGO = Instantiate(_saveGameButtonPrefab, _contentPanel);
                Button buttonComponent = buttonGO.GetComponentInChildren<Button>();
                TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                buttonGO.transform.localScale = Vector3.one;

                if (buttonComponent != null && buttonText != null)
                {
                    buttonText.text = fileName;
                    _buttonTextMapping.Add(buttonComponent, buttonText);
                    
                    // 將點擊事件綁定到選擇存檔的函式
                    buttonComponent.onClick.AddListener(() => SelectSaveFile(fileName, buttonComponent));
                }
            }

            UpdatePaginationUI();
        }

        /// <summary>
        /// 更新分頁UI狀態
        /// </summary>
        private void UpdatePaginationUI()
        {
            // 更新頁面信息文字
            if (_pageInfoText != null)
            {
                if (_allSaveFiles.Count == 0)
                {
                    if (GameSettings.Instance != null && GameSettings.Instance.IsLocalizationInitialized())
                    {
                        _pageInfoText.text = GameSettings.Instance.GetLocalizedString("UI_Tables", "saveui.message.no_save_files");
                    }
                    else
                    {
                        _pageInfoText.text = "無存檔文件"; // 回退值
                    }
                }
                else
                {
                    UpdateLocalizedPaginationInfo();
                }
            }
        }
        
        /// <summary>
        /// 更新本地化的分頁信息
        /// </summary>
        private void UpdateLocalizedPaginationInfo()
        {
            if (_pageInfoText == null) return;
            
            if (GameSettings.Instance != null && GameSettings.Instance.IsLocalizationInitialized())
            {
                string formatString = GameSettings.Instance.GetLocalizedString("UI_Tables", "saveui.pagination.page_info");
                _pageInfoText.text = string.Format(formatString, _currentPage + 1, _totalPages);
            }
            else
            {
                // 回退機制
                UpdatePaginationInfoWithFallback();
            }
        }
        
        /// <summary>
        /// 使用回退機制更新分頁信息
        /// </summary>
        private void UpdatePaginationInfoWithFallback()
        {
            if (_pageInfoText == null) return;
            
            if (_allSaveFiles.Count == 0)
            {
                _pageInfoText.text = "無存檔文件";
            }
            else
            {
                _pageInfoText.text = $"第 {_currentPage + 1} 頁 / 共 {_totalPages} 頁";
            }
        }

        /// <summary>
        /// 導航到指定的存檔文件
        /// </summary>
        /// <param name="fileName">要導航到的存檔文件名</param>
        private void NavigateToSaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || _allSaveFiles.Count == 0) return;

            // 在所有存檔文件中查找目標文件的索引
            int targetIndex = _allSaveFiles.IndexOf(fileName);
            if (targetIndex == -1)
            {
                Debug.LogWarning($"[NavigateToSaveFile] 找不到存檔文件: {fileName}");
                return;
            }

            // 計算目標文件所在的頁面
            int targetPage = targetIndex / _itemsPerPage;

            // 如果目標頁面不是當前頁面，需要切換頁面
            if (targetPage != _currentPage)
            {
                _currentPage = targetPage;
                DisplayCurrentPage();
                BuildNavigableButtons(false);
            }

            // 計算目標文件在當前頁面中的索引
            int indexInCurrentPage = targetIndex % _itemsPerPage;
            
            // 在當前頁面的按鈕中查找對應的按鈕
            int buttonIndex = 0;
            string selectedPrefix = GetLocalizedPrefix();
            foreach (var pair in _buttonTextMapping)
            {
                string buttonFileName = pair.Value.text.TrimStart(selectedPrefix.ToCharArray());
                if (buttonFileName == fileName)
                {
                    _currentNavIndex = buttonIndex;
                    UpdateSelectionVisuals();
                    return;
                }
                buttonIndex++;
            }

            Debug.LogWarning($"[NavigateToSaveFile] 在當前頁面中找不到存檔按鈕: {fileName}");
        }
        
        #region 本地化系統
        
        /// <summary>
        /// 延遲初始化以確保組件順序正確
        /// </summary>
        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return null;
            
            InitializeLocalization();
        }
        
        /// <summary>
        /// 初始化本地化系統
        /// </summary>
        private void InitializeLocalization()
        {
            try
            {
                if (UnityEngine.Localization.Settings.LocalizationSettings.Instance != null)
                {
                    var selectedLocale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
                }
                else
                {
                    Debug.LogWarning("[SaveUIController] Unity LocalizationSettings 實例為 null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveUIController] 檢查 Unity LocalizationSettings 時發生異常: {e.Message}");
            }
            
            CheckLocalizationComponentReferences();
            
            StartCoroutine(WaitForLocalizationAndUpdateUI());
            
            if (GameSettings.Instance != null)
            {
                try
                {
                    GameSettings.Instance.OnLanguageChanged -= OnLocalizationLanguageChanged;
                    GameSettings.Instance.OnLanguageChanged += OnLocalizationLanguageChanged;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveUIController] 註冊語言變更事件時發生錯誤: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("[SaveUIController] 無法註冊語言變更事件 - GameSettings.Instance 為 null");
                Debug.LogError("[SaveUIController] 這可能表示:");
                Debug.LogError("1. GameSettings 物件在場景中不存在");
                Debug.LogError("2. GameSettings 的 Awake() 方法尚未執行");
                Debug.LogError("3. GameSettings 腳本有編譯錯誤");
            }
        }
        
        /// <summary>
        /// 檢查所有本地化組件引用
        /// </summary>
        private void CheckLocalizationComponentReferences()
        {
            int nullCount = 0;
            if (_uiTitleText == null) nullCount++;
            if (_loadButton == null || _loadButton.GetComponentInChildren<TextMeshProUGUI>() == null) nullCount++;
            if (_deleteButtonText == null) nullCount++;
            if (_backButtonText == null) nullCount++;
            if (_noSaveFilesLabel == null) nullCount++;
            if (_pageInfoText == null) nullCount++;
            
            if (nullCount > 0)
            {
                Debug.LogError($"[SaveUIController] 發現 {nullCount} 個本地化組件引用為 null！請在 Inspector 中設置這些引用。");
            }
        }
        
        /// <summary>
        /// 等待本地化系統初始化並更新 UI
        /// </summary>
        private System.Collections.IEnumerator WaitForLocalizationAndUpdateUI()
        {
            float startTime = Time.unscaledTime;
            float timeout = 10f;
            int checkCount = 0;
            
            // 等待 GameSettings 初始化完成
            while (GameSettings.Instance == null || !GameSettings.Instance.IsLocalizationInitialized())
            {
                checkCount++;
                float elapsedTime = Time.unscaledTime - startTime;
                
                if (elapsedTime > timeout)
                {
                    Debug.LogError($"[SaveUIController] 本地化系統初始化超時 - 等待時間: {elapsedTime:F2}s, 檢查次數: {checkCount}");
                    Debug.LogError($"[SaveUIController] GameSettings.Instance: {(GameSettings.Instance != null ? "存在" : "null")}");
                    
                    if (GameSettings.Instance != null)
                    {
                        Debug.LogError($"[SaveUIController] GameSettings 初始化狀態: {GameSettings.Instance.IsLocalizationInitialized()}");
                    }
                    else
                    {
                        Debug.LogError("[SaveUIController] 請檢查:");
                        Debug.LogError("1. 場景中是否存在 GameSettings 物件");
                        Debug.LogError("2. GameSettings 腳本是否正確掛載");
                        Debug.LogError("3. Unity Localization Package 是否正確安裝和配置");
                    }
                    
                    Debug.LogWarning("[SaveUIController] 啟用回退機制：使用硬編碼文字");
                    UpdateAllLocalizedTextsWithFallback();
                    yield break;
                }
                
                // 每秒報告一次狀態
                if (elapsedTime > 1f && checkCount % 10 == 0)
                {
                    Debug.LogWarning($"[SaveUIController] 仍在等待本地化系統... 時間: {elapsedTime:F1}s, 檢查: {checkCount}");
                    
                    // 嘗試手動觸發 GameSettings 初始化（如果存在但未初始化）
                    if (GameSettings.Instance != null && !GameSettings.Instance.IsLocalizationInitialized())
                    {
                        Debug.LogWarning("[SaveUIController] 嘗試手動觸發 GameSettings 初始化...");
                        StartCoroutine(GameSettings.Instance.InitializeLocalization());
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // 更新所有 UI 文字
            UpdateAllLocalizedTexts();
        }
        
        /// <summary>
        /// 語言變更事件處理
        /// </summary>
        private void OnLocalizationLanguageChanged(string languageCode)
        {
            if (GameSettings.Instance == null)
            {
                Debug.LogError("[SaveUIController] 語言變更時 GameSettings.Instance 為 null！");
                return;
            }
            
            if (!GameSettings.Instance.IsLocalizationInitialized())
            {
                Debug.LogError("[SaveUIController] 語言變更時本地化系統尚未初始化！");
                return;
            }
            
            UpdateAllLocalizedTexts();
        }
        
        /// <summary>
        /// 更新所有 UI 元素的本地化文字
        /// </summary>
        private void UpdateAllLocalizedTexts()
        {
            if (GameSettings.Instance == null || !GameSettings.Instance.IsLocalizationInitialized())
            {
                Debug.LogWarning("[SaveUIController] UpdateAllLocalizedTexts: GameSettings 尚未初始化");
                return;
            }
            
            // 更新標題
            if (_uiTitleText != null)
            {
                GameSettings.Instance.UpdateLocalizedText(_uiTitleText, "UI_Tables", "saveui.title");
            }
            else
            {
                Debug.LogWarning("[SaveUIController] _uiTitleText 為 null，跳過");
            }
            
            // 更新載入按鈕文字
            if (_loadButton != null)
            {
                TextMeshProUGUI loadButtonText = _loadButton.GetComponentInChildren<TextMeshProUGUI>();
                if (loadButtonText != null)
                {
                    GameSettings.Instance.UpdateLocalizedText(loadButtonText, "UI_Tables", "saveui.button.load");
                }
                else
                {
                    Debug.LogWarning("[SaveUIController] _loadButton 缺少 TextMeshProUGUI 組件，跳過");
                }
            }
            else
            {
                Debug.LogWarning("[SaveUIController] _loadButton 為 null，跳過");
            }
            
            if (_deleteButtonText != null)
            {
                string deleteKey = _isDeleteConfirming ? "saveui.button.delete_confirm" : "saveui.button.delete";
                GameSettings.Instance.UpdateLocalizedText(_deleteButtonText, "UI_Tables", deleteKey);
            }
            else
            {
                Debug.LogWarning("[SaveUIController] _deleteButtonText 為 null，跳過");
            }
            
            if (_backButtonText != null)
            {
                GameSettings.Instance.UpdateLocalizedText(_backButtonText, "UI_Tables", "saveui.button.back");
            }
            else
            {
                Debug.LogWarning("[SaveUIController] _backButtonText 為 null，跳過");
            }
            
            // 更新無存檔文件標籤
            if (_noSaveFilesLabel != null)
            {
                GameSettings.Instance.UpdateLocalizedText(_noSaveFilesLabel, "UI_Tables", "saveui.message.no_save_files");
            }
            else
            {
                Debug.LogWarning("[SaveUIController] _noSaveFilesLabel 為 null，跳過");
            }
            
            // 更新分頁信息（需要格式化處理）
            UpdateLocalizedPaginationInfo();
        }
        
        /// <summary>
        /// 使用硬編碼文字作為回退機制更新 UI
        /// </summary>
        private void UpdateAllLocalizedTextsWithFallback()
        {
            // 使用硬編碼的繁體中文文字作為回退
            if (_uiTitleText != null)
            {
                _uiTitleText.text = "存檔管理";
            }
            
            if (_loadButton != null)
            {
                TextMeshProUGUI loadButtonText = _loadButton.GetComponentInChildren<TextMeshProUGUI>();
                if (loadButtonText != null)
                {
                    loadButtonText.text = "載入";
                }
            }
            
            if (_deleteButtonText != null)
            {
                _deleteButtonText.text = _isDeleteConfirming ? "確認?" : "刪除";
            }
            
            if (_backButtonText != null)
            {
                _backButtonText.text = "返回";
            }
            
            if (_noSaveFilesLabel != null)
            {
                _noSaveFilesLabel.text = "無存檔文件";
            }
            
            // 更新分頁信息
            UpdatePaginationInfoWithFallback();
        }
        
        /// <summary>
        /// 獲取本地化的選中前綴
        /// </summary>
        private string GetLocalizedPrefix()
        {
            if (GameSettings.Instance != null && GameSettings.Instance.IsLocalizationInitialized())
            {
                return GameSettings.Instance.GetLocalizedString("UI_Tables", "saveui.navigation.prefix");
            }
            return "> "; // 回退值
        }
        
        /// <summary>
        /// 手動觸發本地化文字更新（用於除錯）
        /// </summary>
        [ContextMenu("手動更新本地化文字")]
        public void ForceUpdateLocalization()
        {
            Debug.Log("[SaveUIController] ===== 手動觸發本地化更新 =====");
            
            if (GameSettings.Instance == null)
            {
                Debug.LogError("[SaveUIController] GameSettings.Instance 為 null！請確保場景中存在 GameSettings。");
                return;
            }
            
            if (!GameSettings.Instance.IsLocalizationInitialized())
            {
                Debug.LogError("[SaveUIController] GameSettings 尚未初始化！請等待初始化完成。");
                return;
            }
            
            Debug.Log($"[SaveUIController] 當前語言: {GameSettings.Instance.GetCurrentLanguageCode()}");
            
            // 檢查組件引用
            CheckLocalizationComponentReferences();
            
            // 強制更新所有本地化文字
            UpdateAllLocalizedTexts();
            
            Debug.Log("[SaveUIController] ===== 手動本地化更新完成 =====");
        }
        
        /// <summary>
        /// 檢查本地化系統狀態（用於除錯）
        /// </summary>
        [ContextMenu("檢查本地化系統狀態")]
        public void CheckLocalizationSystemStatus()
        {
            Debug.Log("[SaveUIController] ===== 本地化系統狀態檢查 =====");
            Debug.Log($"GameSettings.Instance 存在: {GameSettings.Instance != null}");
            
            if (GameSettings.Instance != null)
            {
                Debug.Log($"GameSettings 已初始化: {GameSettings.Instance.IsLocalizationInitialized()}");
                Debug.Log($"當前語言: {GameSettings.Instance.GetCurrentLanguageCode()}");
                var availableLanguages = GameSettings.Instance.GetAvailableLanguages();
                Debug.Log($"可用語言: {string.Join(", ", availableLanguages)}");
                
                // 測試獲取本地化文字
                string testText = GameSettings.Instance.GetLocalizedString("UI_Tables", "saveui.title");
                Debug.Log($"測試獲取本地化文字 'saveui.title': '{testText}'");
            }
            
            CheckLocalizationComponentReferences();
            Debug.Log("[SaveUIController] ===== 狀態檢查完成 =====");
        }
        
        #endregion
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                // 取消註冊本地化事件
                if (GameSettings.Instance != null)
                {
                    GameSettings.Instance.OnLanguageChanged -= OnLocalizationLanguageChanged;
                }
                
                Instance = null;
            }
        }
    }