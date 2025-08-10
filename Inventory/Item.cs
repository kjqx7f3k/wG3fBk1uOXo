using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("基本信息")]
    [SerializeField] private int itemId = 0;
    [SerializeField] private string itemName;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemType itemType;
    [SerializeField] private ItemRarity rarity;
    
    [Header("堆疊設定")]
    [SerializeField] private int maxStackSize = 1;
    
    [Header("價值")]
    [SerializeField] private int value = 0;
    
    [Header("使用設定")]
    [SerializeField] private bool isUsable = false;
    [SerializeField] private bool isConsumable = false;
    
    [Header("3D 預覽設定")]
    [SerializeField] private GameObject itemPrefab3D;           // 3D 模型 prefab
    [SerializeField] private Vector3 previewScale = Vector3.one; // 預覽縮放
    [SerializeField] private Vector3 previewRotation;           // 初始旋轉角度
    [SerializeField] private Vector3 previewPosition;           // 相對位置偏移
    [SerializeField] private bool enableAutoRotation = true;    // 是否自動旋轉
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 45, 0); // 旋轉速度
    
    public int Id => itemId;
    public string Name => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemType Type => itemType;
    public ItemRarity Rarity => rarity;
    public int MaxStackSize => maxStackSize;
    public int Value => value;
    public bool IsUsable => isUsable;
    public bool IsConsumable => isConsumable;
    
    // 3D 預覽屬性
    public GameObject ItemPrefab3D => itemPrefab3D;
    public Vector3 PreviewScale => previewScale;
    public Vector3 PreviewRotation => previewRotation;
    public Vector3 PreviewPosition => previewPosition;
    public bool EnableAutoRotation => enableAutoRotation;
    public Vector3 RotationSpeed => rotationSpeed;
    
    /// <summary>
    /// 使用物品
    /// </summary>
    /// <param name="user">使用者</param>
    /// <returns>是否成功使用</returns>
    public virtual bool UseItem(GameObject user)
    {
        if (!isUsable)
        {
            Debug.Log($"{itemName} 無法使用");
            return false;
        }
        
        Debug.Log($"{user.name} 使用了 {itemName}");
        
        // 在子類中實作具體的使用效果
        OnUse(user);
        
        return true;
    }
    
    /// <summary>
    /// 物品使用效果（由子類實作）
    /// </summary>
    /// <param name="user">使用者</param>
    protected virtual void OnUse(GameObject user)
    {
        // 基礎物品沒有特殊效果
    }
    
    /// <summary>
    /// 獲取物品的顏色（根據稀有度）
    /// </summary>
    /// <returns>物品顏色</returns>
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Color.white;
            case ItemRarity.Uncommon:
                return Color.green;
            case ItemRarity.Rare:
                return Color.blue;
            case ItemRarity.Epic:
                return Color.magenta;
            case ItemRarity.Legendary:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// 獲取物品的詳細信息
    /// </summary>
    /// <returns>詳細信息字符串</returns>
    public virtual string GetDetailedInfo()
    {
        string info = $"<color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor())}>{itemName}</color>\n";
        info += $"{description}\n";
        info += $"類型: {GetTypeDisplayName()}\n";
        info += $"稀有度: {GetRarityDisplayName()}\n";
        
        if (maxStackSize > 1)
        {
            info += $"最大堆疊: {maxStackSize}\n";
        }
        
        if (value > 0)
        {
            info += $"價值: {value} 金幣\n";
        }
        
        return info;
    }
    
    /// <summary>
    /// 獲取物品類型的顯示名稱
    /// </summary>
    /// <returns>類型顯示名稱</returns>
    private string GetTypeDisplayName()
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                return "武器";
            case ItemType.Armor:
                return "防具";
            case ItemType.Consumable:
                return "消耗品";
            case ItemType.Material:
                return "材料";
            case ItemType.Quest:
                return "任務物品";
            case ItemType.Misc:
                return "雜項";
            default:
                return "未知";
        }
    }
    
    /// <summary>
    /// 獲取稀有度的顯示名稱
    /// </summary>
    /// <returns>稀有度顯示名稱</returns>
    private string GetRarityDisplayName()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return "普通";
            case ItemRarity.Uncommon:
                return "不常見";
            case ItemRarity.Rare:
                return "稀有";
            case ItemRarity.Epic:
                return "史詩";
            case ItemRarity.Legendary:
                return "傳說";
            default:
                return "未知";
        }
    }
}

/// <summary>
/// 物品類型枚舉
/// </summary>
public enum ItemType
{
    Weapon,     // 武器
    Armor,      // 防具
    Consumable, // 消耗品
    Material,   // 材料
    Quest,      // 任務物品
    Misc        // 雜項
}

/// <summary>
/// 物品稀有度枚舉
/// </summary>
public enum ItemRarity
{
    Common,     // 普通（白色）
    Uncommon,   // 不常見（綠色）
    Rare,       // 稀有（藍色）
    Epic,       // 史詩（紫色）
    Legendary   // 傳說（橙色）
}
