using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// 簡潔的選項切換器組件
/// 提供 < 當前選項 > 格式的UI，支援左右切換
/// </summary>
public class OptionSwitcher : MonoBehaviour
{
    [Header("UI 組件")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private TextMeshProUGUI optionText;
    
    [Header("選項設定")]
    [SerializeField] private List<string> options = new List<string>();
    [SerializeField] private int currentIndex = 0;
    
    [Header("樣式設定")]
    [SerializeField] private string leftArrow = "<";
    [SerializeField] private string rightArrow = ">";
    [SerializeField] private bool showArrows = true;
    
    // 選中狀態
    private bool isSelected = false;
    
    // 事件
    public UnityEvent<int> OnValueChanged = new UnityEvent<int>();
    
    // 屬性
    public int CurrentIndex 
    { 
        get => currentIndex; 
        set => SetCurrentIndex(value); 
    }
    
    public string CurrentOption 
    { 
        get => (currentIndex >= 0 && currentIndex < options.Count) ? options[currentIndex] : ""; 
    }
    
    public List<string> Options 
    { 
        get => new List<string>(options); 
        set => SetOptions(value); 
    }
    
    public bool IsSelected 
    { 
        get => isSelected; 
        set => SetSelected(value); 
    }

    private void Awake()
    {
        // 自動尋找子組件（如果未在Inspector中設置）
        if (leftButton == null)
            leftButton = transform.Find("LeftButton")?.GetComponent<Button>();
        if (rightButton == null)
            rightButton = transform.Find("RightButton")?.GetComponent<Button>();
        if (optionText == null)
        {
            optionText = GetComponentInChildren<TextMeshProUGUI>();
            // 如果找不到 TextMeshProUGUI，嘗試在所有子物件中找
            if (optionText == null)
            {
                var tmpComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmpComponents.Length > 0)
                {
                    optionText = tmpComponents[0];
                    // Debug.Log($"[OptionSwitcher] {gameObject.name}: 在子物件中找到 TextMeshProUGUI: {optionText.gameObject.name}");
                }
            }
        }
            
        ValidateComponents();
    }

    private void Start()
    {
        SetupEventListeners();
        UpdateDisplay();
        UpdateButtonTexts(); // 確保初始化時按鈕文字正確
    }
    
    /// <summary>
    /// 驗證必要組件是否存在
    /// </summary>
    private void ValidateComponents()
    {
        if (leftButton == null)
            Debug.LogError($"[OptionSwitcher] {gameObject.name}: leftButton 未設置！");
        if (rightButton == null)
            Debug.LogError($"[OptionSwitcher] {gameObject.name}: rightButton 未設置！");
        if (optionText == null)
            Debug.LogError($"[OptionSwitcher] {gameObject.name}: optionText 未設置！");
        
        // 警告按鈕缺少文字組件
        if (leftButton != null)
        {
            var leftTextTMP = leftButton.GetComponentInChildren<TextMeshProUGUI>();
            var leftTextUI = leftButton.GetComponentInChildren<Text>();
            if (leftTextTMP == null && leftTextUI == null)
                Debug.LogWarning($"[OptionSwitcher] {gameObject.name}: leftButton 缺少 Text 或 TextMeshProUGUI 組件！");
        }
        
        if (rightButton != null)
        {
            var rightTextTMP = rightButton.GetComponentInChildren<TextMeshProUGUI>();
            var rightTextUI = rightButton.GetComponentInChildren<Text>();
            if (rightTextTMP == null && rightTextUI == null)
                Debug.LogWarning($"[OptionSwitcher] {gameObject.name}: rightButton 缺少 Text 或 TextMeshProUGUI 組件！");
        }
    }
    
    /// <summary>
    /// 設置事件監聽器
    /// </summary>
    private void SetupEventListeners()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(MovePrevious);
        if (rightButton != null)
            rightButton.onClick.AddListener(MoveNext);
    }
    
    /// <summary>
    /// 設置選項列表
    /// </summary>
    public void SetOptions(List<string> newOptions)
    {
        if (newOptions == null) newOptions = new List<string>();
        
        options = new List<string>(newOptions);
        
        // 確保當前索引在有效範圍內
        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, options.Count - 1));
        
        // Debug.Log($"[OptionSwitcher] {gameObject.name}: SetOptions 被調用，選項數量: {options.Count}");
        if (options.Count > 0)
        {
            // Debug.Log($"[OptionSwitcher] {gameObject.name}: 第一個選項: {options[0]}, 當前索引: {currentIndex}");
        }
        
        // 立即嘗試更新顯示
        if (optionText != null)
        {
            UpdateDisplay();
            UpdateButtonTexts();
        }
        else
        {
            Debug.LogWarning($"[OptionSwitcher] {gameObject.name}: optionText 為 null，延遲更新顯示");
            // 延遲到下一幀更新，確保組件已初始化
            StartCoroutine(DelayedUpdateDisplay());
        }
    }
    
    /// <summary>
    /// 延遲更新顯示（當組件引用未就緒時）
    /// </summary>
    private System.Collections.IEnumerator DelayedUpdateDisplay()
    {
        yield return null; // 等待一幀
        
        // 重新嘗試找到 optionText 組件
        if (optionText == null)
        {
            optionText = GetComponentInChildren<TextMeshProUGUI>();
            if (optionText == null)
            {
                var tmpComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmpComponents.Length > 0)
                {
                    optionText = tmpComponents[0];
                }
            }
        }
        
        if (optionText != null)
        {
            // Debug.Log($"[OptionSwitcher] {gameObject.name}: 延遲更新成功，optionText 已找到");
            UpdateDisplay();
            UpdateButtonTexts();
        }
        else
        {
            Debug.LogError($"[OptionSwitcher] {gameObject.name}: 延遲更新失敗，仍找不到 optionText 組件");
        }
    }
    
    /// <summary>
    /// 添加選項
    /// </summary>
    public void AddOption(string option)
    {
        options.Add(option);
        UpdateDisplay();
    }
    
    /// <summary>
    /// 清空選項
    /// </summary>
    public void ClearOptions()
    {
        options.Clear();
        currentIndex = 0;
        UpdateDisplay();
    }
    
    /// <summary>
    /// 設置當前索引
    /// </summary>
    public void SetCurrentIndex(int index)
    {
        if (options.Count == 0) return;
        
        int newIndex = Mathf.Clamp(index, 0, options.Count - 1);
        if (newIndex != currentIndex)
        {
            currentIndex = newIndex;
            UpdateDisplay();
            OnValueChanged?.Invoke(currentIndex);
        }
    }
    
    /// <summary>
    /// 移動到前一個選項
    /// </summary>
    public void MovePrevious()
    {
        if (options.Count <= 1) return;
        
        int newIndex = currentIndex - 1;
        if (newIndex < 0) newIndex = options.Count - 1; // 循環到最後一個
        
        SetCurrentIndex(newIndex);
    }
    
    /// <summary>
    /// 移動到下一個選項
    /// </summary>
    public void MoveNext()
    {
        if (options.Count <= 1) return;
        
        int newIndex = currentIndex + 1;
        if (newIndex >= options.Count) newIndex = 0; // 循環到第一個
        
        SetCurrentIndex(newIndex);
    }
    
    /// <summary>
    /// 更新顯示文字
    /// </summary>
    private void UpdateDisplay()
    {
        if (optionText == null) 
        {
            Debug.LogWarning($"[OptionSwitcher] {gameObject.name}: UpdateDisplay 被調用但 optionText 為 null");
            return;
        }
        
        if (options.Count == 0)
        {
            optionText.text = "無選項";
            SetButtonsInteractable(false);
            UpdateButtonTexts();
            // Debug.Log($"[OptionSwitcher] {gameObject.name}: 顯示「無選項」");
            return;
        }
        
        // 中間只顯示純選項文字，不包含箭頭
        string currentOption = CurrentOption;
        optionText.text = currentOption;
        
        // Debug.Log($"[OptionSwitcher] {gameObject.name}: 更新顯示文字為「{currentOption}」(索引: {currentIndex}/{options.Count})");
        
        // 更新按鈕文字和可互動狀態
        SetButtonsInteractable(options.Count > 1);
        UpdateButtonTexts();
    }
    
    /// <summary>
    /// 更新按鈕文字
    /// </summary>
    private void UpdateButtonTexts()
    {
        // 只有在選中狀態且顯示箭頭的設定下才顯示箭頭
        bool shouldShowArrows = isSelected && showArrows && options.Count > 1;
        
        // 設置左按鈕文字
        if (leftButton != null)
        {
            string buttonText = shouldShowArrows ? leftArrow : "";
            
            var leftButtonTextTMP = leftButton.GetComponentInChildren<TextMeshProUGUI>();
            if (leftButtonTextTMP != null)
            {
                leftButtonTextTMP.text = buttonText;
            }
            else
            {
                var leftButtonTextUI = leftButton.GetComponentInChildren<Text>();
                if (leftButtonTextUI != null)
                {
                    leftButtonTextUI.text = buttonText;
                }
            }
        }
        
        // 設置右按鈕文字
        if (rightButton != null)
        {
            string buttonText = shouldShowArrows ? rightArrow : "";
            
            var rightButtonTextTMP = rightButton.GetComponentInChildren<TextMeshProUGUI>();
            if (rightButtonTextTMP != null)
            {
                rightButtonTextTMP.text = buttonText;
            }
            else
            {
                var rightButtonTextUI = rightButton.GetComponentInChildren<Text>();
                if (rightButtonTextUI != null)
                {
                    rightButtonTextUI.text = buttonText;
                }
            }
        }
    }
    
    /// <summary>
    /// 設置按鈕可互動狀態
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        if (leftButton != null) leftButton.interactable = interactable;
        if (rightButton != null) rightButton.interactable = interactable;
    }
    
    /// <summary>
    /// 設置組件可互動狀態
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (leftButton != null) leftButton.interactable = interactable && options.Count > 1;
        if (rightButton != null) rightButton.interactable = interactable && options.Count > 1;
    }
    
    /// <summary>
    /// 處理鍵盤輸入（由外部調用）
    /// </summary>
    public void HandleKeyboardInput(Vector2 input)
    {
        // 只有在選中狀態下才響應鍵盤輸入
        if (!isSelected || options.Count <= 1) return;
        
        if (input.x > 0.5f) // 右鍵
        {
            MoveNext();
        }
        else if (input.x < -0.5f) // 左鍵
        {
            MovePrevious();
        }
    }
    
    /// <summary>
    /// 調整數值（用於外部鍵盤導航）
    /// </summary>
    /// <param name="direction">-1 為左/減, 1 為右/增</param>
    public void AdjustValue(int direction)
    {
        if (options.Count <= 1) return;
        
        if (direction > 0)
        {
            MoveNext();
        }
        else if (direction < 0)
        {
            MovePrevious();
        }
    }
    
    /// <summary>
    /// 設置選中狀態
    /// </summary>
    /// <param name="selected">是否選中</param>
    public void SetSelected(bool selected)
    {
        if (isSelected != selected)
        {
            isSelected = selected;
            UpdateButtonTexts(); // 更新按鈕文字顯示狀態
            
            // Debug.Log($"[OptionSwitcher] {gameObject.name}: 選中狀態改變為 {selected}");
        }
    }
    
    /// <summary>
    /// 根據選項文字設置當前選項
    /// </summary>
    public bool SetCurrentOption(string optionText)
    {
        int index = options.IndexOf(optionText);
        if (index >= 0)
        {
            SetCurrentIndex(index);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 獲取所有選項數量
    /// </summary>
    public int GetOptionCount()
    {
        return options.Count;
    }

    private void OnDestroy()
    {
        // 清理事件監聽器
        if (leftButton != null)
            leftButton.onClick.RemoveListener(MovePrevious);
        if (rightButton != null)
            rightButton.onClick.RemoveListener(MoveNext);
    }
    
    // 編輯器工具方法
    #if UNITY_EDITOR
    /// <summary>
    /// 編輯器中驗證設定
    /// </summary>
    private void OnValidate()
    {
        // 確保索引在有效範圍內
        if (options != null && options.Count > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, options.Count - 1);
        }
        
        // 如果在播放模式中，更新顯示
        if (Application.isPlaying)
        {
            UpdateDisplay();
            UpdateButtonTexts();
        }
    }
    #endif
}