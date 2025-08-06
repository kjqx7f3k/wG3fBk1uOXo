# Boids 群集行為系統使用說明

## 概述

這個Boids系統實現了經典的三維群集行為算法，包含三個核心行為：
- **Separation（分離）**：避免與鄰近個體過於接近
- **Alignment（對齊）**：與鄰近個體保持相同方向  
- **Cohesion（聚合）**：向鄰近個體的中心移動

## 核心組件

### 1. Boids.cs
繼承自Creature的群集生物腳本，實現完整的3D boids算法。

**主要特性**：
- 完整的三維群集行為
- 可調整的行為參數
- 障礙物避障功能
- 邊界約束系統
- 鄰居檢測優化

### 2. FlockingState.cs
專用的群集行為狀態，整合到Creature的狀態機系統中。

### 3. BoidsManager.cs
群集管理器，用於批量創建和管理Boids群體。

## 使用方法

### 步驟1：創建Boids預製體
1. 創建一個空的GameObject
2. 添加Boids.cs腳本
3. 添加一個3D模型（如魚、鳥等）
4. 確保模型的前方是GameObject的forward方向
5. 保存為預製體

### 步驟2：設置BoidsManager
1. 在場景中創建空GameObject
2. 添加BoidsManager.cs腳本
3. 將創建的Boids預製體拖入"Boids Prefab"欄位
4. 調整參數後點擊Play或使用"生成Boids群體"按鈕

### 步驟3：參數調整

#### Boids群集參數
- **Detection Radius（檢測半徑）**：影響鄰居檢測範圍
- **Separation Radius（分離半徑）**：個體間的最小距離
- **Max Speed（最大速度）**：個體的最大移動速度
- **Max Force（最大轉向力）**：轉向時的最大力度

#### 行為權重
- **Separation Weight（分離權重）**：避免碰撞的強度
- **Alignment Weight（對齊權重）**：方向一致性的強度
- **Cohesion Weight（聚合權重）**：群體聚集的強度
- **Avoidance Weight（避障權重）**：障礙物迴避的強度

#### 邊界設定
- **Boundary Center（邊界中心）**：活動區域的中心點
- **Boundary Size（邊界大小）**：活動區域的尺寸

## 編輯器功能

### BoidsManager 右鍵選單
- **生成Boids群體**：創建新的群體
- **清除所有Boids**：移除所有現有的Boids
- **更新Boids參數**：將當前參數應用到所有Boids

### Gizmos顯示
- **綠色球體**：檢測半徑
- **紅色球體**：分離半徑
- **藍色射線**：當前速度方向
- **黃色射線**：避障檢測射線
- **青色線條**：鄰居連接線
- **白色框架**：邊界範圍

## 性能優化

### 鄰居檢測優化
- 使用定時更新機制（0.1秒間隔）
- 避免每幀重新計算鄰居列表

### 物理計算優化  
- 關閉重力影響
- 使用Rigidbody進行平滑移動
- 適度的阻力設置

## 擴展功能

### 自定義行為
可以通過修改權重來創建不同的群集行為：
- **緊密群體**：增加Cohesion權重
- **分散群體**：增加Separation權重
- **整齊隊形**：增加Alignment權重

### 避障系統
- 支援多方向射線檢測
- 可設置障礙物圖層
- 動態避障算法

### 邊界約束
- 軟邊界系統（推力回彈）
- 3D空間完整支援
- 可調整的邊界力度

## 故障排除

### 常見問題
1. **Boids不移動**
   - 檢查Rigidbody設置
   - 確認Max Speed > 0
   - 檢查是否有足夠的鄰居

2. **群體分散**
   - 增加Detection Radius
   - 調高Cohesion Weight
   - 減小Separation Weight

3. **個體碰撞**
   - 增加Separation Weight
   - 調小Separation Radius
   - 檢查Max Force設置

4. **性能問題**
   - 減少Boids數量
   - 增加鄰居更新間隔
   - 減小Detection Radius

## 技術細節

### 算法實現
- 使用加權向量合成
- 力的限制和速度限制
- 平滑的方向轉換

### 狀態機整合
- 完全整合Creature狀態系統
- 支援生命值和死亡狀態
- 可與其他狀態切換

### 記憶體管理
- 靜態群體列表管理
- 自動註冊和註銷
- 鄰居列表快取機制

這個Boids系統提供了完整的3D群集行為模擬，可以輕鬆整合到現有的遊戲項目中。