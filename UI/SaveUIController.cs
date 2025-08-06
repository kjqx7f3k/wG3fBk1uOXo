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

        [Header("Pagination")]
        [SerializeField] private int _itemsPerPage = 5;
        [SerializeField] private TextMeshProUGUI _pageInfoText;

        [Header("Settings")]
        [SerializeField] private string _uiTitle = "";
        [SerializeField] private string[] _saveFileExtensions = { ".eqg", ".sav" };
        [SerializeField] private string _selectedPrefix = "> ";
        [SerializeField] private string _deleteButtonNormalText = "刪除";
        [SerializeField] private string _deleteButtonConfirmText = "確認?";
        [SerializeField] private string _backButtonOriginalText = "返回";
        [SerializeField] private string _loadButtonOriginalText = "載入";

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
            
            _uiTitleText.text = _uiTitle;
            _loadButton.onClick.AddListener(OnLoadButtonClicked);
            _deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            
            // 返回按鈕的行為會根據當前狀態改變
            _backButton.onClick.AddListener(OnBackButtonClicked);

            // 為了Debug，讓動作按鈕面板總是可見，但按鈕預設不可互動
            _actionButtonsPanel.SetActive(true);
            _loadButton.interactable = false;
            _deleteButton.interactable = false;
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
            Debug.Log("存檔UI已開啟");

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
            Debug.Log("存檔UI已關閉");
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

            // 顯示當前頁的內容
            DisplayCurrentPage();
            
            // 刷新後，重置為未選擇任何存檔的初始狀態
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

            // 預設選擇 "載入" 按鈕
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

            // 重建導航列表，只包含存檔項目和返回按鈕
            BuildNavigableButtons(false);
            SetDefaultSelection();
        }

        private void OnLoadButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedFileName)) return;
            
            // 使用真正的SaveManager載入遊戲
            if (SaveManager.Instance != null)
            {
                Debug.Log($"[SaveUI] 嘗試載入存檔: {_selectedFileName}");
                
                bool loadSuccess = SaveManager.Instance.LoadGameWithCustomFileName(_selectedFileName);
                if (loadSuccess)
                {
                    Debug.Log($"[SaveUI] 存檔載入開始: {_selectedFileName}");
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
                
                // 直接使用File.Delete而不是假的SaveManager
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[SaveUI] 已刪除存檔文件: {filePath}");
                }
                else
                {
                    Debug.LogWarning($"[SaveUI] 嘗試刪除不存在的文件: {filePath}");
                }
                
                RefreshSaveList(); // 刪除後刷新整個列表並重置狀態
            }
            else
            {
                _isDeleteConfirming = true;
                UpdateSelectionVisuals(); // 更新視覺，讓刪除按鈕顯示確認文字
            }
        }

        /// <summary>
        /// 返回按鈕的統一處理函式
        /// </summary>
        private void OnBackButtonClicked()
        {
            GameMenuManager.Instance.OpenGameMenu(); // 呼叫遊戲選單管理器的開啟方法
            
            // 返回按鈕的功能就是關閉UI，回到GameMenu
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
                // 只有當前時間超過了 (上次導航時間 + 冷卻時間) 才執行導航
                if (Time.unscaledTime > lastNavigationTime + navigationCooldown)
                {
                    lastNavigationTime = Time.unscaledTime; // 更新上次導航時間
                    
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
                // 如果在冷卻時間內，忽略導航輸入（不輸出調試訊息以避免日誌洪水）
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
                // 狀態1: 預設狀態，可導航按鈕只包含存檔項目（不包含返回按鈕）
                foreach (var button in _buttonTextMapping.Keys)
                {
                    _navigableButtons.Add(button);
                }
                // 不添加返回按鈕到導航列表中
            }
        }
        
        /// <summary>
        /// 設定預設選中的項目
        /// </summary>
        private void SetDefaultSelection()
        {
            if (_navigableButtons.Count > 0)
            {
                // 如果有存檔文件，預設選中第一個；否則，唯一的選項就是返回按鈕
                _currentNavIndex = _buttonTextMapping.Count > 0 ? 0 : 0;
            }
            else
            {
                _currentNavIndex = -1; // 沒有任何可選項目
            }
            UpdateSelectionVisuals();
        }

        /// <summary>
        /// 導航到上一個或下一個按鈕
        /// </summary>
        /// <param name="direction">-1 為上, 1 為下</param>
        private void Navigate(int direction)
        {
            // 如果已選擇存檔，使用操作按鈕的導航邏輯
            if (!string.IsNullOrEmpty(_selectedFileName))
            {
                if (_navigableButtons.Count <= 1) return;

                _currentNavIndex += direction;

                // 循環導航
                if (_currentNavIndex < 0) { _currentNavIndex = _navigableButtons.Count - 1; }
                if (_currentNavIndex >= _navigableButtons.Count) { _currentNavIndex = 0; }

                UpdateSelectionVisuals();
                return;
            }

            // 存檔列表導航邏輯（支持自動換頁）
            if (_navigableButtons.Count == 0) return; // 如果沒有任何可導航的按鈕，直接返回

            // 如果總頁數只有一頁，則在頁內循環
            if (_totalPages <= 1)
            {
                if (_navigableButtons.Count <= 1) return; // 如果只有一個項目且只有一頁，不導航
                
                _currentNavIndex += direction;
                if (_currentNavIndex < 0) { _currentNavIndex = _navigableButtons.Count - 1; }
                if (_currentNavIndex >= _navigableButtons.Count) { _currentNavIndex = 0; }
                
                UpdateSelectionVisuals();
                return;
            }

            // --- 多頁情況下的換頁邏輯 ---
            int newIndex = _currentNavIndex + direction;
            
            if (newIndex < 0) // 向上超出當前頁範圍
            {
                _currentPage = (_currentPage - 1 + _totalPages) % _totalPages;
                DisplayCurrentPage();
                BuildNavigableButtons(false);
                _currentNavIndex = _buttonTextMapping.Count - 1; // 選中上一頁的最後一個存檔項目
            }
            else if (newIndex >= _navigableButtons.Count) // 向下超出當前頁範圍
            {
                _currentPage = (_currentPage + 1) % _totalPages;
                DisplayCurrentPage();
                BuildNavigableButtons(false);
                _currentNavIndex = 0; // 選中下一頁的第一個存檔項目
            }
            else // 正常在頁內導航
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
            // 如果已經在存檔列表導航模式，或者沒有存檔文件，則不執行
            if (string.IsNullOrEmpty(_selectedFileName) || _buttonTextMapping.Count == 0) return;

            Debug.Log("[Navigation] 切換到存檔列表導航模式");
            
            // 記住當前選中的存檔文件名
            string rememberedFileName = _selectedFileName;
            
            // 取消存檔選擇，回到存檔列表導航
            DeselectSaveFile();
            
            // 嘗試導航到之前選中的存檔項目
            NavigateToSaveFile(rememberedFileName);
        }

        /// <summary>
        /// 切換到操作按鈕導航模式
        /// </summary>
        private void SwitchToActionButtonsNavigation()
        {
            // 如果已經在操作按鈕導航模式，或者沒有存檔文件，則不執行
            if (!string.IsNullOrEmpty(_selectedFileName) || _buttonTextMapping.Count == 0) return;

            Debug.Log("[Navigation] 切換到操作按鈕導航模式");
            
            // 選擇當前選中的存檔項目
            if (_currentNavIndex >= 0 && _currentNavIndex < _navigableButtons.Count)
            {
                Button selectedButton = _navigableButtons[_currentNavIndex];
                if (_buttonTextMapping.ContainsKey(selectedButton))
                {
                    // 獲取選中的存檔文件名
                    string fileName = _buttonTextMapping[selectedButton].text.TrimStart(_selectedPrefix.ToCharArray());
                    SelectSaveFile(fileName, selectedButton);
                }
            }
        }
        
        /// <summary>
        /// 更新所有按鈕的視覺表現，確保只有選中的按鈕有 ">" 前綴
        /// </summary>
        private void UpdateSelectionVisuals()
        {
            // 1. 重置所有按鈕的文字為原始狀態
            foreach (var pair in _buttonTextMapping)
            {
                string originalText = pair.Value.text.TrimStart(_selectedPrefix.ToCharArray());
                pair.Value.text = originalText;
            }
            _backButtonText.text = _backButtonOriginalText;
            _loadButton.GetComponentInChildren<TextMeshProUGUI>().text = _loadButtonOriginalText;
            _deleteButtonText.text = _isDeleteConfirming ? _deleteButtonConfirmText : _deleteButtonNormalText;

            // 2. 如果有選中的存檔文件，為該存檔項目加上前綴（即使不在當前導航列表中）
            if (!string.IsNullOrEmpty(_selectedFileName))
            {
                foreach (var pair in _buttonTextMapping)
                {
                    string buttonFileName = pair.Value.text.TrimStart(_selectedPrefix.ToCharArray());
                    if (buttonFileName == _selectedFileName)
                    {
                        pair.Value.text = _selectedPrefix + buttonFileName;
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
                    string currentText = textComponent.text.TrimStart(_selectedPrefix.ToCharArray());
                    textComponent.text = _selectedPrefix + currentText;
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
                    _pageInfoText.text = "無存檔文件";
                }
                else
                {
                    _pageInfoText.text = $"第 {_currentPage + 1} 頁 / 共 {_totalPages} 頁";
                }
            }

            Debug.Log($"[SaveUI] 當前頁: {_currentPage + 1}/{_totalPages}, 總存檔數: {_allSaveFiles.Count}");
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
            
            Debug.Log($"[NavigateToSaveFile] 導航到存檔: {fileName}, 索引: {targetIndex}, 目標頁面: {targetPage + 1}");

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
            foreach (var pair in _buttonTextMapping)
            {
                string buttonFileName = pair.Value.text.TrimStart(_selectedPrefix.ToCharArray());
                if (buttonFileName == fileName)
                {
                    _currentNavIndex = buttonIndex;
                    UpdateSelectionVisuals();
                    Debug.Log($"[NavigateToSaveFile] 成功導航到存檔: {fileName}, 按鈕索引: {buttonIndex}");
                    return;
                }
                buttonIndex++;
            }

            Debug.LogWarning($"[NavigateToSaveFile] 在當前頁面中找不到存檔按鈕: {fileName}");
        }
    }
