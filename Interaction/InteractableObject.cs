using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("互動設定")]
    [SerializeField] private string dialogFileName;
    [SerializeField] private string interactionPrompt = "按 E 互動";
    [SerializeField] private GameObject dialogModel;
    
    [Header("視覺效果")]
    [SerializeField] private bool showInteractionUI = true;
    [SerializeField] private GameObject interactionIndicator;
    
    /// <summary>
    /// 對話檔案名稱
    /// </summary>
    public string DialogFileName => dialogFileName;
    
    /// <summary>
    /// 互動提示文字
    /// </summary>
    public string InteractionPrompt => interactionPrompt;
    
    /// <summary>
    /// 對話模型
    /// </summary>
    public GameObject DialogModel => dialogModel;
    
    /// <summary>
    /// 是否顯示互動UI
    /// </summary>
    public bool ShowInteractionUI => showInteractionUI;
    
    /// <summary>
    /// 執行互動
    /// </summary>
    public void Interact()
    {
        if (!string.IsNullOrEmpty(dialogFileName))
        {
            // 檢查DialogManager是否存在
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.LoadDialog(dialogFileName, dialogModel);
            }
            else
            {
                Debug.LogWarning($"DialogManager not found! Cannot load dialog: {dialogFileName}");
            }
        }
        else
        {
            Debug.LogWarning($"InteractableObject {name} has no dialog file assigned!");
        }
        
        // 觸發互動事件
        OnInteracted();
    }
    
    /// <summary>
    /// 當物件被互動時調用
    /// </summary>
    protected virtual void OnInteracted()
    {
        Debug.Log($"Interacted with {name}");
    }
    
    /// <summary>
    /// 顯示互動指示器
    /// </summary>
    public void ShowInteractionIndicator()
    {
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隱藏互動指示器
    /// </summary>
    public void HideInteractionIndicator()
    {
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
    }
    
    private void Start()
    {
        // 初始時隱藏互動指示器
        HideInteractionIndicator();
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}
