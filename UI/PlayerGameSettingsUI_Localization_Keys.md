# PlayerGameSettingsUI 本地化鍵值參考

此文件列出了 PlayerGameSettingsUI 需要在 Unity Localization 的 UI_Tables String Table 中添加的鍵值。

## 在 Unity Editor 中的操作步驟

1. 打開 Window → Asset Management → Localization Tables
2. 選擇 UI_Tables String Table 
3. 添加以下鍵值和對應的多語言文字

## 鍵值列表

### 設定選項標籤
| Key | 繁體中文 (zh-TW) | 簡體中文 (zh-CN) | English (en) | 日本語 (ja) |
|-----|------------------|------------------|--------------|-------------|
| `settings.graphics.resolution` | 解析度 | 解析度 | Resolution | 解像度 |
| `settings.graphics.fps_limit` | FPS限制 | FPS限制 | FPS Limit | FPS制限 |
| `settings.graphics.vsync` | 垂直同步 | 垂直同步 | VSync | 垂直同期 |
| `settings.graphics.antialiasing` | 抗鋸齒 | 抗锯齿 | Anti-aliasing | アンチエイリアシング |
| `settings.audio.volume` | 音量 | 音量 | Volume | 音量 |
| `settings.language.title` | 語言 | 语言 | Language | 言語 |

### 按鈕文字
| Key | 繁體中文 (zh-TW) | 簡體中文 (zh-CN) | English (en) | 日本語 (ja) |
|-----|------------------|------------------|--------------|-------------|
| `settings.button.reset` | 重置 | 重置 | Reset | リセット |
| `settings.button.back` | 返回 | 返回 | Back | 戻る |

### 設定選項值
| Key | 繁體中文 (zh-TW) | 簡體中文 (zh-CN) | English (en) | 日本語 (ja) |
|-----|------------------|------------------|--------------|-------------|
| `settings.options.disabled` | 關閉 | 关闭 | Disabled | 無効 |
| `settings.options.enabled` | 開啟 | 开启 | Enabled | 有効 |
| `settings.options.unlimited` | 無限制 | 无限制 | Unlimited | 無制限 |

### FPS 選項值
| Key | 繁體中文 (zh-TW) | 簡體中文 (zh-CN) | English (en) | 日本語 (ja) |
|-----|------------------|------------------|--------------|-------------|
| `settings.fps.30` | 30 FPS | 30 FPS | 30 FPS | 30 FPS |
| `settings.fps.60` | 60 FPS | 60 FPS | 60 FPS | 60 FPS |
| `settings.fps.120` | 120 FPS | 120 FPS | 120 FPS | 120 FPS |
| `settings.fps.144` | 144 FPS | 144 FPS | 144 FPS | 144 FPS |

### 抗鋸齒選項值
| Key | 繁體中文 (zh-TW) | 簡體中文 (zh-CN) | English (en) | 日本語 (ja) |
|-----|------------------|------------------|--------------|-------------|
| `settings.antialiasing.off` | 關閉 | 关闭 | Off | オフ |
| `settings.antialiasing.2x` | MSAA 2x | MSAA 2x | MSAA 2x | MSAA 2x |
| `settings.antialiasing.4x` | MSAA 4x | MSAA 4x | MSAA 4x | MSAA 4x |
| `settings.antialiasing.8x` | MSAA 8x | MSAA 8x | MSAA 8x | MSAA 8x |

### 音量選項值（0% - 100%）
音量選項為數字顯示，不需額外本地化。

### 語言選項值
| Key | 繁體中文 (zh-TW) | 簡體中文 (zh-CN) | English (en) | 日本語 (ja) |
|-----|------------------|------------------|--------------|-------------|
| `settings.language.zh_tw` | 繁體中文 | 繁体中文 | Traditional Chinese | 繁体中国語 |
| `settings.language.zh_cn` | 簡體中文 | 简体中文 | Simplified Chinese | 簡体中国語 |
| `settings.language.en` | English | English | English | 英語 |
| `settings.language.ja` | 日本語 | 日本语 | Japanese | 日本語 |

## 使用說明

在完成 Unity Editor 中的 String Table 設定後，PlayerGameSettingsUI.cs 將會使用這些鍵值來顯示本地化文字。語言切換時，所有UI文字都會自動更新為對應語言的文字。