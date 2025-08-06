# Unity Localization 系統使用指南

這是一個完整的多語言化解決方案，整合 Unity Localization Package 與現有遊戲架構，支援對話、UI 文字和字體的動態語言切換。

## 📋 目錄

- [系統概述](#系統概述)
- [快速開始](#快速開始)
- [字體系統設定](#字體系統設定)
- [Dialog 系統整合](#dialog-系統整合)
- [UI 本地化](#ui-本地化)
- [Editor 工具使用](#editor-工具使用)
- [實際使用範例](#實際使用範例)
- [API 參考](#api-參考)
- [故障排除](#故障排除)

## 🏗️ 系統概述

### 核心組件

| 組件 | 功能 | 位置 |
|------|------|------|
| `LocalizedUIHelper` | 本地化核心管理器 | `Core/LocalizedUIHelper.cs` |
| `FontManager` | 字體管理系統 | `Core/FontManager.cs` |
| `LocalizedFontUpdater` | 個別組件字體更新 | `UI/LocalizedFontUpdater.cs` |
| `LocalizedDialogManager` | 本地化對話管理 | `Dialog/LocalizedDialogManager.cs` |
| `LocalizationEditorTools` | 開發工具 | `Tools/LocalizationEditorTools.cs` |

### 支援語言

- 🇹🇼 繁體中文 (zh-TW)
- 🇨🇳 簡體中文 (zh-CN)
- 🇺🇸 English (en)
- 🇯🇵 日本語 (ja)

### 整合現有系統

- ✅ **GameSettings**: 語言偏好保存/載入
- ✅ **DialogManager**: 完全相容現有對話系統
- ✅ **PlayerGameSettingsUI**: 語言切換 UI
- ✅ **字體系統**: 自動/手動字體切換

## 🚀 快速開始

### 1. 安裝 Unity Localization Package

```bash
# 在 Unity Package Manager 中
Window → Package Manager → Unity Registry → Localization → Install
```

### 2. 建立 Localization Settings

```bash
# 建立本地化設定
Window → Asset Management → Localization Settings
```

配置支援的語言：
- Add Locale → Chinese (Traditional) - zh-TW
- Add Locale → Chinese (Simplified) - zh-CN  
- Add Locale → English - en
- Add Locale → Japanese - ja

### 3. 建立 String Tables

建立以下 String Table：

| Table Name | 用途 |
|------------|------|
| `UI_Tables` | UI 介面文字 |
| `Dialog_Tables` | 對話文本 |
| `System_Tables` | 系統訊息 |
| `Items_Tables` | 物品名稱描述 |

### 4. 設定場景管理器

在場景中創建必要的管理器：

```csharp
// 使用 Editor 工具創建
Window → Localization Tools
→ Create FontManager
→ Create LocalizedDialogManager
```

## 🎨 字體系統設定

### 字體資源準備

#### 1. 準備字體文件

```
Assets/Fonts/
├── zh-TW/
│   ├── NotoSansTC-Regular.ttf
│   └── NotoSansTC-Bold.ttf
├── zh-CN/
│   ├── NotoSansSC-Regular.ttf
│   └── NotoSansSC-Bold.ttf
├── en/
│   ├── Roboto-Regular.ttf
│   └── Roboto-Bold.ttf
└── ja/
    ├── NotoSansJP-Regular.ttf
    └── NotoSansJP-Bold.ttf
```

#### 2. 創建 SDF 字體資產

```bash
Window → TextMeshPro → Font Asset Creator

設定：
- Source Font File: 選擇字體文件
- Sampling Point Size: 90
- Padding: 5
- Atlas Resolution: 1024x1024
- Character Set: Unicode Range (Hex)
- Character Sequence: 0x20-0x7E, 0x4E00-0x9FFF
```

#### 3. 配置 FontManager

在 FontManager 的 Inspector 中：

```
Language Font Configs:
[0] 繁體中文
    Language Code: zh-TW
    Font Asset: NotoSansTC-Regular SDF
    Font Size Multiplier: 1.0
    Line Spacing Adjustment: 0

[1] 簡體中文  
    Language Code: zh-CN
    Font Asset: NotoSansSC-Regular SDF
    Font Size Multiplier: 1.0
    Line Spacing Adjustment: 0

[2] English
    Language Code: en
    Font Asset: Roboto-Regular SDF
    Font Size Multiplier: 0.9
    Line Spacing Adjustment: -2

[3] 日本語
    Language Code: ja  
    Font Asset: NotoSansJP-Regular SDF
    Font Size Multiplier: 1.1
    Line Spacing Adjustment: 1

Fallback Font: unifont-16.0 SDF
```

### 字體應用方式

#### 方式 1: 全域自動管理 (推薦)
```csharp
// FontManager 自動管理所有 TextMeshProUGUI
// 無需額外設定，語言切換時自動應用
```

#### 方式 2: 個別組件控制
```csharp
// 為特定組件添加 LocalizedFontUpdater
// 使用 Editor 工具批量添加
Window → Localization Tools → Add LocalizedFontUpdater to All
```

#### 方式 3: 自定義字體配置
```csharp
// 在 LocalizedFontUpdater 中設定 Custom Font Configs
// 適用於特殊字體需求的組件
```

## 💬 Dialog 系統整合

### 本地化對話文件格式

在 Unity Localization 的 `Dialog_Tables` String Table 中：

**Key**: `questDialog`

**繁體中文值**:
```json
{
  "dialogName": "任務對話",
  "version": "1.0",
  "description": "主線任務對話",
  "defaultInitialDialogId": 1,
  "dialogs": [
    {
      "id": 1,
      "nextId": 2,
      "expressionId": 0,
      "text": "你好，勇敢的冒險者！",
      "events": [
        {
          "event_type": "update_tag",
          "param1": "MetNPC",
          "param2": "1"
        }
      ]
    },
    {
      "id": 2,
      "nextId": -1,
      "text": "願你的旅程充滿光明！"
    }
  ]
}
```

**英文值**:
```json
{
  "dialogName": "Quest Dialog",
  "version": "1.0", 
  "description": "Main quest dialog",
  "defaultInitialDialogId": 1,
  "dialogs": [
    {
      "id": 1,
      "nextId": 2,
      "expressionId": 0,
      "text": "Hello, brave adventurer!",
      "events": [
        {
          "event_type": "update_tag",
          "param1": "MetNPC",
          "param2": "1"
        }
      ]
    },
    {
      "id": 2,
      "nextId": -1,
      "text": "May your journey be filled with light!"
    }
  ]
}
```

### 使用本地化對話

```csharp
// 載入本地化對話
LocalizedDialogManager.Instance.LoadLocalizedDialog("questDialog", npcModel);

// 回退到原有系統（如果本地化失敗）
DialogManager.Instance.LoadDialog("questDialog.json", npcModel);
```

### Fallback 機制

1. **Unity Localization** → 嘗試載入對應語言的對話
2. **原有 JSON 文件** → 如果本地化失敗，載入原有格式
3. **錯誤提示** → 如果都失敗，顯示錯誤訊息

## 🎯 UI 本地化

### String Table 設定

在 `UI_Tables` String Table 中加入 UI 文字：

| Key | 繁體中文 | English | 日本語 |
|-----|----------|---------|--------|
| `menu.start` | 開始遊戲 | Start Game | ゲーム開始 |
| `menu.settings` | 設定 | Settings | 設定 |
| `menu.exit` | 離開遊戲 | Exit Game | ゲーム終了 |
| `inventory.title` | 物品欄 | Inventory | インベントリ |

### 程式碼中使用本地化文字

#### 同步獲取
```csharp
string startText = LocalizedUIHelper.Instance.GetLocalizedString("UI_Tables", "menu.start");
buttonText.text = startText;
```

#### 異步獲取 (推薦)
```csharp
LocalizedUIHelper.Instance.GetLocalizedStringAsync("UI_Tables", "menu.start", (localizedText) =>
{
    buttonText.text = localizedText;
});
```

#### 直接更新組件
```csharp
LocalizedUIHelper.Instance.UpdateLocalizedText(buttonText, "UI_Tables", "menu.start");
```

### 語言切換

```csharp
// 在 PlayerGameSettingsUI 中已整合
// 或手動切換
LocalizedUIHelper.Instance.ChangeLanguage("ja");  // 切換到日文

// 獲取當前語言
string currentLang = LocalizedUIHelper.Instance.GetCurrentLanguageCode();

// 獲取可用語言列表
List<string> languages = LocalizedUIHelper.Instance.GetAvailableLanguages();
```

## 🛠️ Editor 工具使用

### 開啟 Localization Tools

```bash
Window → Localization Tools
```

### 功能介紹

#### 1. Text Component Scanner
- **掃描場景**: 找到所有 TextMeshProUGUI 組件
- **搜尋過濾**: 根據物件名稱或文字內容過濾
- **狀態顯示**: ✓ 已有 LocalizedFontUpdater，✗ 尚未添加

#### 2. 批量管理
```csharp
// 批量添加 LocalizedFontUpdater
Add LocalizedFontUpdater to All

// 批量移除 LocalizedFontUpdater  
Remove LocalizedFontUpdater from All

// 個別操作
Select  // 在 Hierarchy 中選中
Add     // 添加組件
Remove  // 移除組件
```

#### 3. 管理器工具
```csharp
// 尋找或創建 FontManager
Find FontManager in Scene
Create FontManager

// 尋找或創建 LocalizedDialogManager
Find DialogManager in Scene  
Create LocalizedDialogManager
```

### 開發工作流程

1. **建立場景** → 添加 UI 元素
2. **掃描組件** → `Localization Tools → Scan Scene`
3. **批量設定** → `Add LocalizedFontUpdater to All`
4. **配置字體** → 在 FontManager 中設定語言字體
5. **測試切換** → 在 Play Mode 中測試語言切換
6. **調整優化** → 針對特殊需求進行個別調整

## 📚 實際使用範例

### 完整設定範例

#### 1. 場景設定
```csharp
// 場景中的管理器
GameObject managers = new GameObject("Managers");
├── GameSettings              // 已存在
├── LocalizedUIHelper        // 新增
├── FontManager              // 新增  
├── DialogManager            // 已存在
└── LocalizedDialogManager   // 新增
```

#### 2. UI 設定範例
```csharp
// Menu 場景
Canvas
├── MenuPanel
│   ├── StartButton
│   │   └── Text (TextMeshProUGUI + LocalizedFontUpdater)
│   ├── SettingsButton  
│   │   └── Text (TextMeshProUGUI + LocalizedFontUpdater)
│   └── ExitButton
│       └── Text (TextMeshProUGUI + LocalizedFontUpdater)
```

#### 3. 程式碼整合範例
```csharp
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI settingsButtonText;
    [SerializeField] private TextMeshProUGUI exitButtonText;
    
    private void Start()
    {
        // 等待本地化系統初始化
        StartCoroutine(InitializeLocalizedUI());
    }
    
    private IEnumerator InitializeLocalizedUI()
    {
        // 等待 LocalizedUIHelper 初始化完成
        yield return new WaitUntil(() => LocalizedUIHelper.Instance.IsInitialized());
        
        // 更新 UI 文字
        UpdateUITexts();
        
        // 註冊語言變更事件
        LocalizedUIHelper.Instance.OnLanguageChanged += OnLanguageChanged;
    }
    
    private void UpdateUITexts()
    {
        LocalizedUIHelper.Instance.UpdateLocalizedText(startButtonText, "UI_Tables", "menu.start");
        LocalizedUIHelper.Instance.UpdateLocalizedText(settingsButtonText, "UI_Tables", "menu.settings");
        LocalizedUIHelper.Instance.UpdateLocalizedText(exitButtonText, "UI_Tables", "menu.exit");
    }
    
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        // 語言變更時重新更新文字
        UpdateUITexts();
    }
    
    private void OnDestroy()
    {
        if (LocalizedUIHelper.Instance != null)
        {
            LocalizedUIHelper.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }
}
```

### 對話系統使用範例

```csharp
public class NPCInteraction : MonoBehaviour
{
    [SerializeField] private string dialogKey = "npcGreeting";
    
    public void StartDialog()
    {
        // 優先使用本地化對話
        if (LocalizedDialogManager.Instance != null)
        {
            LocalizedDialogManager.Instance.LoadLocalizedDialog(dialogKey, gameObject);
        }
        else
        {
            // 回退到原有系統
            DialogManager.Instance.LoadDialog($"{dialogKey}.json", gameObject);
        }
    }
}
```

## 🔧 API 參考

### LocalizedUIHelper

#### 核心方法
```csharp
// 初始化
IEnumerator InitializeLocalization()

// 同步獲取文字
string GetLocalizedString(string tableName, string entryKey)

// 異步獲取文字
void GetLocalizedStringAsync(string tableName, string entryKey, Action<string> callback)

// 更新組件文字
void UpdateLocalizedText(TextMeshProUGUI textComponent, string tableName, string entryKey)

// 語言切換
bool ChangeLanguage(string localeCode)
string GetCurrentLanguageCode()
List<string> GetAvailableLanguages()

// 初始化狀態
bool IsInitialized()
```

#### 事件
```csharp
System.Action OnLocalizationInitialized;
System.Action<Locale> OnLanguageChanged;
```

### FontManager

#### 核心方法
```csharp
// 字體應用
void ApplyFontForLanguage(string languageCode)
LanguageFontConfig GetFontConfigForLanguage(string languageCode)

// 組件管理
void RegisterTextComponent(TextMeshProUGUI textComponent)
void UnregisterTextComponent(TextMeshProUGUI textComponent)
void UpdateTextComponentFont(TextMeshProUGUI textComponent, string languageCode = "")

// 狀態查詢
string GetCurrentLanguageCode()
List<string> GetAvailableLanguages()
```

#### 事件
```csharp
System.Action<string> OnFontChanged;
```

### LocalizedDialogManager

#### 核心方法
```csharp
// 對話載入
void LoadLocalizedDialog(string dialogKey, GameObject model = null)

// 設定
void SetDialogTableName(string tableName)
void SetUseLocalizedStrings(bool useLocalized)
void SetFallbackToOriginalDialog(bool useFallback)

// 狀態查詢
bool IsUsingLocalizedStrings()
```

### GameSettings 整合

#### 新增屬性
```csharp
string CurrentLanguage { get; set; }
```

#### 新增事件
```csharp
System.Action<string> OnLanguageChanged;
```

## 🛠️ 故障排除

### 常見問題

#### 1. 字體沒有切換
**問題**: 語言切換了但字體沒有變化
**解決**:
- 檢查 FontManager 是否正確配置
- 確認 TextMeshProUGUI 組件已註冊或添加 LocalizedFontUpdater
- 檢查字體資產是否正確設定

```csharp
// 調試：檢查字體配置
Debug.Log($"Current Language: {FontManager.Instance.GetCurrentLanguageCode()}");
var config = FontManager.Instance.GetFontConfigForLanguage("zh-TW");
Debug.Log($"Font Config: {config?.fontAsset?.name}");
```

#### 2. 本地化文字顯示為 Key
**問題**: UI 顯示 "menu.start" 而不是 "開始遊戲"
**解決**:
- 檢查 String Table 是否正確建立
- 確認 Key 名稱是否正確
- 檢查 LocalizedUIHelper 是否已初始化

```csharp
// 調試：檢查本地化狀態
Debug.Log($"Localization Initialized: {LocalizedUIHelper.Instance.IsInitialized()}");
Debug.Log($"Available Languages: {string.Join(", ", LocalizedUIHelper.Instance.GetAvailableLanguages())}");
```

#### 3. 對話沒有本地化
**問題**: 對話仍然顯示原始語言
**解決**:
- 檢查 Dialog_Tables String Table 是否包含對應的 Key
- 確認 LocalizedDialogManager 設定正確
- 檢查 JSON 格式是否正確

```csharp
// 調試：檢查對話設定
Debug.Log($"Using Localized Strings: {LocalizedDialogManager.Instance.IsUsingLocalizedStrings()}");
```

#### 4. 語言設定沒有保存
**問題**: 重啟遊戲後語言重置
**解決**:
- 確認 GameSettings.SaveSettings() 被呼叫
- 檢查 PlayerGameSettingsUI 的語言切換邏輯

```csharp
// 確保設定被保存
private void OnApplySettings()
{
    if (GameSettings.Instance != null)
    {
        GameSettings.Instance.SaveSettings();
    }
}
```

### 性能優化建議

#### 1. 字體資產優化
- 使用適當的 Atlas Resolution (建議 1024x1024)
- 只包含需要的字符集
- 考慮使用 Fallback 字體處理特殊字符

#### 2. 本地化文字緩存
```csharp
// LocalizedUIHelper 已內建緩存機制
// 避免重複載入相同的 String Table
```

#### 3. 組件管理
```csharp
// 使用全域 FontManager 而非大量 LocalizedFontUpdater
// 只在特殊需求時使用個別組件控制
```

### 除錯工具

#### 1. Console 日誌
所有管理器都有詳細的 Debug.Log 輸出，可以追蹤執行狀態。

#### 2. Editor 工具
使用 `Localization Tools` 窗口檢查組件狀態。

#### 3. Inspector 調試
FontManager 和 LocalizedDialogManager 在 Play Mode 中會顯示當前狀態。

---

## 📞 技術支援

如有問題或建議，請檢查：
1. Unity Console 的錯誤訊息
2. 本文件的故障排除章節
3. 各管理器的 Inspector 設定

祝你的多語言化開發順利！ 🌐✨