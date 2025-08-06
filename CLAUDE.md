# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 3D game project featuring a modular creature control system, inventory management, dialog system, and scene management. The project uses C# and Unity's component-based architecture with a focus on extensibility and maintainability.

## Key Development Commands

### Unity Development
- **Open Project**: Use Unity Hub to open the project located at the Assets folder
- **Build Project**: Use Unity's Build Settings (File > Build Settings)
- **Test Playmode**: Use Unity's Play button to enter Play Mode for testing

### No Traditional Build System
This is a Unity project without traditional package.json, Makefile, or similar build systems. All building and testing is done through Unity's built-in systems.

## Architecture Overview

### Core Systems

#### 1. Creature Control System (`Source/Input/`)
- **PersistentPlayerControllerInputSystem**: Cross-scene persistent controller using Singleton pattern
- **SceneCreatureManager**: Scene-specific creature management and registration
- **IControllable Interface**: Defines controllable behavior contract
- Supports multi-creature switching with Tab key and number keys (1-9)
- Automatic camera following and scene transition handling

#### 2. Inventory System (`Source/Inventory/`)
- **InventoryManager**: Main inventory controller with 4-region UI (list, preview, description, actions)
- **CreatureInventory**: Per-creature inventory management
- **Item**: ScriptableObject-based item system with rarity, types, and usage patterns
- **InventorySlot**: Individual slot management
- Supports keyboard (WASD/arrows), mouse, and scroll wheel navigation
- 3D item preview system with automatic model cleanup

#### 3. Dialog System (`Source/Dialog/`)
- **DialogManager**: Handles both CSV (legacy) and JSON (recommended) dialog formats
- **DialogCacheManager**: Smart caching for dialog files
- **DialogDataLoader**: Unified loading for different formats
- JSON format supports rich metadata, branching conversations, and events
- Keyboard navigation with arrow keys and WASD

#### 4. Scene Management (`Source/Scene/`)
- **GameSceneManager**: Async scene loading with progress tracking
- **Portal**: Multi-trigger scene transition system
- **LoadingScreenManager**: Loading screen with progress and tips
- **AddressableResourceManager**: Advanced resource preloading and management
- Supports both traditional Unity scenes and Addressable system

#### 5. UI Management (`Source/UI/`)
- **InputSystemWrapper**: Unity輸入系統包裝器
- **UIStateManager**: UI狀態管理和邏輯協調
- **GameMenuManager**: Game menu with save/load/settings integration
- **LoadingScreenManager**: Loading screen coordination
- **SaveUIController**: Save/load UI functionality
- Unified ESC/Tab key handling across all UI systems

### State Management

#### UI States
- **None**: Normal gameplay
- **Inventory**: Item management open
- **GameMenu**: Main menu open (pauses game)
- **Dialog**: Conversation active
- **Settings**: Settings menu open

#### Game State Control
- **Time Pause**: GameMenu and Settings pause `Time.timeScale`
- **Input Blocking**: All non-None states block character movement
- **UI Priority**: Dialog state prevents other UI from opening

### Core Interfaces

#### IControllable
```csharp
public interface IControllable
{
    bool IsControllable { get; }
    void HandleMovementInput(Vector2 input);
    void HandleJumpInput(bool pressed);
    void HandleAttackInput(bool pressed);
    void HandleInteractInput(bool pressed);
    void OnControlGained();
    void OnControlLost();
    Transform GetTransform();
}
```

#### IDamagable
```csharp
public interface IDamagable
{
    void TakeDamage(int damage);
    void Heal(int amount);
}
```

### Key Design Patterns
- **Singleton Pattern**: Used for managers (GameSettings, InventoryManager, DialogManager, etc.)
- **State Machine Pattern**: Creature behavior states (Idle, Move, Attack, Death)
- **Observer Pattern**: Event-driven communication between systems
- **Strategy Pattern**: Different item types and usage behaviors

## Development Workflow

**IMPORTANT**: AI assistants MUST strictly follow the three-stage workflow defined below when modifying code.

### AI助手核心規則 - 三階段工作流程

#### 階段一：分析問題

**聲明格式**：`【分析問題】`

**目的**
因為可能存在多個可選方案，要做出正確的決策，需要足夠的依據。

**必須做的事**：
- 理解我的意圖，如果有歧義請問我
- 搜尋所有相關程式碼
- 識別問題根因

**主動發現問題**
- 發現重複程式碼
- 識別不合理的命名
- 發現多餘的程式碼、類別
- 發現可能過時的設計
- 發現過於複雜的設計、呼叫
- 發現不一致的類型定義
- 進一步搜尋程式碼，看是否更大範圍內有類似問題
做完以上事項，就可以向我提問了。

**絕對禁止**：
- ❌ 修改任何程式碼
- ❌ 急於給出解決方案
- ❌ 跳過搜尋和理解步驟
- ❌ 不分析就推薦方案

**階段轉換規則**
本階段你要向我提問。
如果存在多個你無法抉擇的方案，要問我，作為提問的一部分。
如果沒有需要問我的，則直接進入下一階段。

#### 階段二：制定方案

**聲明格式**：`【制定方案】`

**前置條件**：
- 我明確回答了關鍵技術決策。

**必須做的事**：
- 列出變更（新增、修改、刪除）的檔案，簡要描述每個檔案的變化
- 消除重複邏輯：如果發現重複程式碼，必須透過複用或抽象來消除
- 確保修改後的程式碼符合DRY原則和良好的架構設計
如果新發現了向我收集的關鍵決策，在這個階段你還可以繼續問我，直到沒有不明確的問題之後，本階段結束。
本階段不允許自動切換到下一階段。

#### 階段三：執行方案

**聲明格式**：`【執行方案】`

**必須做的事**：
- 嚴格按照選定方案實作
- 修改後執行型別檢查

**絕對禁止**：
- ❌ 提交程式碼（除非使用者明確要求）
- 啟動開發伺服器
如果在這個階段發現了拿不準的問題，請向我提問。
收到使用者訊息時，一般從【分析問題】階段開始，除非使用者明確指定階段的名字。

### General Development Guidelines

#### Adding New Features
1. Follow the existing manager pattern for new systems
2. Use the established event system for inter-component communication
3. Integrate with UIInputManager for input handling
4. Follow the ScriptableObject pattern for data assets

### Testing
1. Use Unity's Play Mode for runtime testing
2. Test multi-scene scenarios by switching between SampleScene and SampleScene2
3. Test UI state transitions using ESC and Tab keys
4. Verify creature switching with Tab and number keys

### Working with Systems

#### Dialog System
- JSON format preferred over CSV for new dialogs
- Files go in `Assets/Dialogs/` directory
- Use automatic format detection: `DialogManager.Instance.LoadDialog("filename")`
- Test with existing files: `testDialog.json`, `complexDialog.json`

#### Inventory System
- Items are ScriptableObjects in `Assets/TestItems/`
- Use prefab-based UI system for visual customization
- Multi-creature inventories switch automatically with creature control

#### Scene Management
- Use `GameSceneManager.Instance.LoadScene(sceneName, showLoading)` for scene transitions
- Configure Addressable resources in Unity's Addressable Groups window
- Test with Portal components for scene transitions

## File Organization

```
Assets/Source/
├── Core/                    # Core game systems and settings
├── Creature/               # Creature classes and behaviors
├── Dialog/                 # Dialog management system
├── Input/                  # Input handling and creature control
├── Interaction/            # Object interaction system
├── Interfaces/             # Core game interfaces
├── Inventory/              # Inventory and item management
├── Scene/                  # Scene loading and management
├── StateMachine/           # State machine for creature behaviors
├── Tools/                  # Development and debugging tools
├── UI/                     # UI management and controllers
└── Various README files    # Detailed system documentation
```

## Important Notes

### Input System Integration
- The project uses Unity's new Input System (`InputSystem_Actions`)
- Creature control integrates with UI state checking
- All input passes through centralized state management

### Cross-Scene Persistence
- `PersistentPlayerControllerInputSystem` persists across scene changes
- UI managers use DontDestroyOnLoad for consistency
- Addressable system supports remote and local resource management

### AI Development Workflow (中文)
The `workflow.md` file contains a 3-stage development process in Chinese:
1. **分析問題** (Problem Analysis): Understanding and code searching
2. **制定方案** (Solution Planning): Planning changes and decisions
3. **執行方案** (Implementation): Code execution and type checking

### Performance Considerations
- Use object pooling for frequently created/destroyed UI elements
- Addressable system for large asset management
- Automatic cleanup of 3D preview objects in inventory
- Smart caching for dialog files

This architecture provides a robust foundation for a multi-scene Unity game with complex character control, inventory management, and dialog systems.