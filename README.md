# Unity 3D 遊戲專案

這是一個功能豐富的Unity 3D遊戲專案，採用模組化的生物控制系統、物品管理、對話系統和場景管理。專案使用C#和Unity的組件式架構，專注於擴展性和維護性。

## 專案特色

- **多生物控制系統**：支援在多個可控制生物之間切換
- **智能庫存管理**：包含4區域UI的完整物品系統
- **多語言對話系統**：支援JSON和二進制格式的對話文件
- **存檔系統**：二進制格式的存檔和載入
- **異步場景載入**：包含進度追踪和Addressable資源管理
- **統一UI狀態管理**：協調所有UI元素的顯示和輸入處理

## 檔案結構與功能介紹

### 🧠 Core (核心系統)
- **AddressableResourceManager.cs** - 管理Addressable資源的預載入和異步載入功能。提供統一的資源管理介面。
- **FontManager.cs** - 處理多語言字型管理和動態字型載入。確保不同語言的文字正確顯示。
- **GameSettings.cs** - 管理遊戲的全域設定和配置參數。提供單例模式的設定存取。
- **LocalizedUIHelper.cs** - 協助UI元素進行多語言本地化處理。提供文字翻譯和UI適配功能。
- **SaveManager.cs** - 負責遊戲存檔和讀檔功能的管理。處理存檔數據的序列化和持久化。
- **TagSystem.cs** - 提供標籤系統用於物件分類和查詢。支援動態標籤管理和條件檢查。

### 🐾 Creature (生物系統)
- **Boids.cs** - 實現群體行為算法的個體生物組件。提供分離、對齊、聚合等群體移動行為。
- **BoidsManager.cs** - 管理多個Boids個體的群體行為控制器。協調群體移動和行為參數。
- **ControllableCreature.cs** - 繼承自Creature的可控制生物類別。實現IControllable介面提供玩家控制功能。
- **Creature.cs** - 基礎生物類別，實現IDamagable介面。提供生命值系統和狀態機管理。

### 💬 Dialog (對話系統)
- **DialogCacheManager.cs** - 管理對話文件的緩存機制以提升載入效能。提供智能緩存清理功能。
- **DialogConditionChecker.cs** - 檢查對話條件是否滿足的靜態工具類。支援多種條件類型的邏輯判斷。
- **DialogDataLoader.cs** - 統一的對話數據載入器，支援JSON和CSV格式。提供多語言對話文件管理。
- **DialogEventProcessor.cs** - 處理對話中觸發的各種遊戲事件。支援物品給予、標籤更新等操作。
- **DialogManager.cs** - 對話系統的核心管理器，繼承自UIPanel。提供完整的對話顯示、選項處理和終端效果。

### 🎮 Input (輸入系統)
- **InputSystemWrapper.cs** - Unity新輸入系統的包裝器類別。提供統一的輸入介面給其他系統使用。
- **PersistentPlayerControllerInputSystem.cs** - 跨場景持久化的玩家控制器系統。管理生物切換、攝影機跟隨和場景傳送。
- **SceneCreatureManager.cs** - 場景特定的生物管理器。負責註冊和管理當前場景中的可控制生物。

### 🔗 Interaction (互動系統)
- **InteractableObject.cs** - 定義可互動物件的基礎類別。提供統一的互動介面和事件處理。

### 🎯 Interfaces (介面定義)
- **IControllable.cs** - 定義可控制物件必須實現的介面。包含移動、攻擊、互動等控制方法。
- **IDamagable.cs** - 定義可受傷害物件的介面。提供傷害處理、治療和死亡相關方法。

### 🎒 Inventory (庫存系統)
- **CreatureInventory.cs** - 個別生物的庫存管理組件。處理生物特定的物品存儲和管理。
- **InventoryManager.cs** - 庫存系統的主要控制器，提供4區域UI。支援鍵盤和滑鼠導航，以及3D物品預覽。
- **InventorySlot.cs** - 庫存槽位的管理組件。處理物品的存放、移動和顯示邏輯。
- **Item.cs** - 基於ScriptableObject的物品系統基礎類別。定義物品的屬性、稀有度和使用模式。
- **ItemDatabase.cs** - 物品資料庫管理器，統一管理所有遊戲物品。提供物品查詢和載入功能。

### 🎬 Scene (場景管理)
- **CameraController.cs** - 攝影機控制組件，處理攝影機的移動和跟隨邏輯。支援平滑過渡和多種跟隨模式。
- **GameSceneManager.cs** - 遊戲場景管理器，提供異步場景載入功能。包含進度追踪和載入畫面管理。
- **InScenePortal.cs** - 場景內傳送門組件，處理同場景內的位置移動。支援玩家和攝影機的同步傳送。
- **Portal.cs** - 跨場景傳送門的多觸發系統組件。管理場景切換和生物狀態的保存與恢復。

### 🎰 StateMachine (狀態機系統)
- **AttackState.cs** - 攻擊狀態的具體實現。處理生物的攻擊行為和動畫控制。
- **CreatureState.cs** - 生物狀態的抽象基礎類別。定義狀態的生命週期和基本行為。
- **DeathState.cs** - 死亡狀態的實現。處理生物死亡時的行為和清理邏輯。
- **FlockingState.cs** - 群體行為狀態的實現。整合Boids算法實現群體移動。
- **IdleState.cs** - 閒置狀態的實現。處理生物的待機行為和狀態轉換。
- **MoveState.cs** - 移動狀態的實現。處理生物的移動邏輯和物理運動。
- **StateMachine.cs** - 狀態機的核心實現類別。管理狀態轉換和當前狀態的執行。

### 🛠️ Tools (開發工具)
- **LocalizationEditorTools.cs** - 編輯器下的本地化開發工具。提供多語言資源的管理和驗證功能。
- **SceneMemoryAnalyzer.cs** - 場景記憶體分析工具。幫助開發者監控和優化記憶體使用情況。

### 🎨 UI (用戶介面)
- **GameMenuManager.cs** - 遊戲主選單管理器，整合存檔載入設定功能。提供統一的選單導航和遊戲暫停控制。
- **LoadingScreenManager.cs** - 載入畫面管理器，顯示載入進度和提示。協調場景切換時的用戶體驗。
- **LocalizedFontUpdater.cs** - 本地化字型更新器，根據語言切換字型。確保UI文字在不同語言下的正確顯示。
- **PlayerGameSettingsUI.cs** - 玩家遊戲設定UI控制器。處理音效、圖像品質等設定的用戶介面。
- **SaveUIController.cs** - 存檔UI控制器，處理存檔和讀檔的用戶介面。提供存檔槽位管理和預覽功能。
- **UIPanel.cs** - UI面板的基礎類別，提供統一的UI管理機制。處理面板的開啟、關閉和輸入封鎖邏輯。