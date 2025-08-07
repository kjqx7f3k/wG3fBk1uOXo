using UnityEngine;

/// <summary>
/// 統一的遊戲條件結構
/// 用於各種系統的條件檢查（對話、事件觸發等）
/// </summary>
[System.Serializable]
public class GameCondition
{
    public string type; // 條件類型：TAG_CHECK, ITEM_OWNED, QUEST_STATUS, PLAYER_LEVEL 等
    public string param; // 參數：tagId, itemId, questId 等
    public string value; // 目標值：可以是數字或字串
    public string @operator; // 比較運算子：EQUAL, GREATER_EQUAL, LESS_THAN, NOT_EQUAL 等
}

/// <summary>
/// 統一的遊戲事件結構
/// 用於各種系統的事件處理（對話、觸發器等）
/// </summary>
[System.Serializable]
public class GameEvent
{
    [Header("事件基本設定")]
    public string event_type; // 事件類型：update_tag, give_item, take_item, play_narration, play_audio, load_scene 等
    public string param1; // 第一個參數：tagId, itemId, dialogId, audioClipName, sceneName 等
    public string param2; // 第二個參數：tag值, item數量, 音量, showLoading 等
    
    [Header("條件設定")]
    [Tooltip("勾選此選項以啟用條件檢查。如果不勾選，事件將無條件執行。")]
    public bool useCondition = false; // 是否使用條件檢查
    
    [Tooltip("當 useCondition 為 true 時，事件只有在此條件滿足時才會執行")]
    public GameCondition condition; // 事件觸發條件（只有當 useCondition 為 true 時才使用）
}

/// <summary>
/// 初始條件結構
/// 用於對話系統的起始對話條件
/// </summary>
[System.Serializable]
public class InitialCondition
{
    public GameCondition condition;
    public int dialogId;
}

/// <summary>
/// 下一步條件結構
/// 用於對話系統的下一個對話條件
/// </summary>
[System.Serializable]
public class NextCondition
{
    public GameCondition condition;
    public int nextId;
}

/// <summary>
/// 選項條件結構
/// 用於對話系統的選項顯示條件
/// </summary>
[System.Serializable]
public class OptionCondition
{
    public GameCondition condition;
    public string text;
    public int nextId;
    public GameEvent[] events; // 選擇此選項時觸發的事件
}