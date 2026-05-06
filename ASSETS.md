# 素材取得清單

> 本文件對應 `DEVELOPMENT_PLAN.md` §4。所有路徑與檔名以該文件為準，本文件指引「如何把實際下載到的檔案，重新命名/裁切成 §4.3 的格式」。

---

## 0. 三條取得路線（依推薦順序）

### 路線 A：Kenney.nl（首選）
- 全站 CC0 授權，免註明來源、可商用、可修改
- 風格統一、品質穩定
- 下載：直接到 https://kenney.nl/assets 找對應包，每包都是 ZIP 直接下載
- 缺點：沒有「公主」「獨角獸」「Poli」這類特定 IP 角色，需要替代或加工

### 路線 B：itch.io 免費 CC0 sprite 包（補強）
- 進 https://itch.io/game-assets/free 並用標籤 `cc0` + 關鍵字搜尋
- 推薦關鍵字：
  - `princess sprite top down cc0`
  - `unicorn sprite cc0`
  - `pixel horse cc0`
- **下載前務必看 License 欄**，只用 CC0 / Public Domain / 明確標示「Free for any use」的
- 風格不一定和 Kenney 統一，可接受程度看你

### 路線 C：AI 生成（最有彈性）
- 用 ChatGPT / Midjourney / Stable Diffusion 等生成你想要的精確角色
- 對「公主」「獨角獸」「Poli」這種有想像畫面的角色最有效
- **僅供自家小孩玩，不上架不販售** → 沒有授權問題
- 提示詞範例：
  - `cute pixel art princess riding unicorn, top-down view, 4 directions walking animation, 64x64, transparent background, simple flat colors`
  - `cute cartoon blue rescue police car, top-down view, 4 directions, kid friendly, transparent background, 48x48`
- 後製：用 GIMP / Photoshop / Aseprite 切成 spritesheet，存成 PNG

---

## 1. Kenney 推薦下載清單

開啟 https://kenney.nl/assets 後，搜尋下列名稱即可下載 ZIP：

| Kenney 包名 | 用途 | 取出哪些 |
|---|---|---|
| **Tiny Town** | 環境 tile、樹、石頭、人物、馬 | 草地 tile、路徑 tile、樹、馬（改獨角獸用）|
| **Top-Down Tanks Redux** | 車輛俯視角 | 警車（改 Poli 用）|
| **Animal Pack** | 動物 sprite（2D 的那版） | 小貓、小狗、小兔 |
| **Particle Pack** | 粒子貼圖 | 圓點、星星、火花、煙霧 |
| **UI Pack** | UI 元件 | 進度條底框、按鈕、面板 |
| **UI Pack RPG Expansion** | RPG 風格 UI | 進度條填充、邊框 |
| **Interface Sounds** | UI 音效 | 按鈕、提示 |
| **Impact Sounds** | 衝擊音效 | 救援完成的「轟」 |
| **Music Jingles** | 短音樂片段 | 慶祝音樂、勝利音 |
| **RPG Audio** | 環境音樂 | 主關 BGM |

> 若 Kenney 的某包名稱有版本變動，搜尋關鍵字找最新即可。所有 Kenney 內容皆 CC0。

---

## 2. 素材對應表（Kenney 取出 → 重命名為計畫書檔名）

下載完上述 Kenney 包後，照下表把檔案重命名/搬到 `assets/` 對應路徑。

### 2.1 角色（characters/）

| 目標檔名 | 來源 | 處理方式 |
|---|---|---|
| `characters/unicorn.png` | Tiny Town 內的馬 sprite，或 itch.io 找 `pixel unicorn` | 取 4 方向（上下左右）排成橫向 4 格 spritesheet。原圖太小可放大 4× nearest-neighbor。若是馬→自己畫一根犄角貼上即可變獨角獸 |
| `characters/princess_overlay.png` | itch.io 找 `pixel princess top down`，或 AI 生成 | 4 方向坐姿剪影，疊在獨角獸正上方。若找不到可省略，獨角獸單獨上場也行 |
| `characters/poli.png` | Top-Down Tanks Redux 內的警車（藍色 + 紅頂燈） | 取出 4 方向，重命名。原圖應該是 PNG with transparency。沒有 4 方向的話用旋轉產生（Raylib 也支援執行時旋轉，可只給 1 方向圖） |

> **重要替代策略**：Poli 的關鍵特徵是「藍色車身 + 警示燈」，不必一模一樣。Kenney 的警車已經很接近。

### 2.2 動物（animals/）

| 目標檔名 | 來源 | 處理方式 |
|---|---|---|
| `animals/kitten.png` | Animal Pack（cat 系列） | 取 2 frame idle，橫向排列 |
| `animals/puppy.png` | Animal Pack（dog 系列） | 同上 |
| `animals/bunny.png` | Animal Pack（rabbit）或慶祝畫面省略 | 同上 |

### 2.3 場景（world/）

| 目標檔名 | 來源 | 處理方式 |
|---|---|---|
| `world/grass_tile.png` | Tiny Town 內草地 tile | 64×64，可平鋪 |
| `world/path_tile.png` | Tiny Town 內路徑/泥土 tile | 64×64 |
| `world/log.png` | Tiny Town 內倒木 / 木頭裝飾 | 96×32 橫躺 |
| `world/tree.png` | Tiny Town 內大樹 | 96×128 |
| `world/puddle.png` | Tiny Town 內水池，或自己畫藍色橢圓 | 96×48 |
| `world/balloons.png` | itch.io 搜 `balloon sprite cc0`，或 AI 生成 3 顆綁一起的氣球 | 64×96 |
| `world/crystal.png` | Tiny Town 內水晶/寶石，或 Particle Pack 裡的菱形 | 24×32，4 frame 旋轉 |

### 2.4 特效（effects/）

| 目標檔名 | 來源 | 處理方式 |
|---|---|---|
| `effects/rainbow_particle.png` | Particle Pack 內圓形粒子，白色 | 16×16 白色，**程式裡用 ColorTint 上彩虹色循環** |
| `effects/heart_particle.png` | itch.io 搜 `heart pixel cc0`，或自己用任何繪圖軟體畫 | 16×16 粉紅愛心 |
| `effects/star_particle.png` | Particle Pack 內星形 | 16×16 黃色 |
| `effects/firework.png` | Particle Pack 內 burst/spark frames | 32×32，8 frame |

### 2.5 UI（ui/）

| 目標檔名 | 來源 |
|---|---|
| `ui/progress_bar_bg.png` | UI Pack 內 bar 底框 |
| `ui/progress_bar_fill.png` | UI Pack 內 bar 填充（可改色） |
| `ui/celebration_banner.png` | UI Pack 內大型 banner / 自製文字底圖 |

### 2.6 字型（fonts/）

| 目標檔名 | 來源 |
|---|---|
| `fonts/NotoSansTC-Regular.ttf` | https://fonts.google.com/noto/specimen/Noto+Sans+TC 下載 → 解壓縮取 `NotoSansTC-Regular.ttf` |

> 必須用支援繁中的字型，否則中文會顯示為方塊。Noto 系列為 Google 開源、SIL OFL 授權、可商用。

### 2.7 音效（audio/）

| 目標檔名 | 來源 | 挑選原則 |
|---|---|---|
| `audio/bgm_main.ogg` | RPG Audio 或 Music Jingles 裡循環片段 | 輕快、不刺耳、可長時間聽不煩 |
| `audio/bgm_celebration.ogg` | Music Jingles 內勝利樂句 | 短而歡樂（5 秒夠） |
| `audio/sfx_collect.ogg` | Interface Sounds 內 chime/ding | 清脆叮咚 |
| `audio/sfx_button.ogg` | Interface Sounds 內 click/blip | 短促可愛 |
| `audio/sfx_rescue_hit.ogg` | Impact Sounds 內輕擊 | 有力但不嚇人 |
| `audio/sfx_rescue_complete.ogg` | Music Jingles 內 fanfare 短句 | 1–2 秒勝利感 |
| `audio/sfx_meow.ogg` | freesound.org 搜 `meow cc0` | CC0 授權 |
| `audio/sfx_woof.ogg` | freesound.org 搜 `dog bark cute cc0` | CC0 授權 |

> Kenney 的音效原檔多為 `.wav` 或 `.ogg`，若是 `.wav` 可用 [ffmpeg](https://ffmpeg.org/) 一行轉檔：
> ```
> ffmpeg -i input.wav -c:a libvorbis output.ogg
> ```
> 或讓 Codex 在程式裡同時支援 `.wav` 和 `.ogg`（Raylib 都支援）。

---

## 3. 缺料時的程式 fallback（Codex 已在 §4.4 處理）

完全不準備素材也能跑起來看到角色在動，因為 `AssetManager` 會：

- 圖片缺失 → 回傳 16×16 洋紅色佔位紋理
- 音效缺失 → 略過播放
- 字型缺失 → 退回 Raylib 內建字型

實作優先序建議：
1. **先讓程式跑起來**（用 fallback 色塊）→ 驗證遊戲邏輯
2. **再放素材**（先放角色＋環境）→ 視覺上看得懂
3. **最後放音效＋音樂**（聽覺打磨）

---

## 4. 一鍵下載清單（給長輩照著做）

如果是非工程師家長要幫忙準備素材，可以照下面流程：

1. 開 https://kenney.nl/assets，逐一下載第 1 節表中 10 個包的 ZIP
2. 開 https://fonts.google.com/noto/specimen/Noto+Sans+TC 下載字型 ZIP
3. 在 `KidsGame/` 下建立 `assets/` 資料夾與第 2 節表中所有子資料夾
4. 把每個檔案找到對應位置，照表上 **目標檔名** 重新命名
5. 找不到的檔案先**不要放**，程式會自動用色塊代替（不會崩）
6. 執行 `dotnet run` 看效果，再回頭補缺的檔

---

## 5. 我的建議：分階段準備

不要一次準備全部 30 多個檔，依里程碑（見計畫書 §6）對應準備：

| 對應里程碑 | 必備素材 | 可以省略 |
|---|---|---|
| M1 專案骨架 | 無 | 全部 |
| M2 AssetManager | 無（測試 fallback） | 全部 |
| M3 獨角獸 | `unicorn.png` | 其他 |
| M4 Poli 跟隨 | `poli.png`, `sfx_button.ogg` | 其他 |
| M5 地圖 | `grass_tile.png`, `tree.png`, `crystal.png` | 其他 |
| M6 水晶 | `sfx_collect.ogg`, `effects/star_particle.png` | 其他 |
| M7 粒子 | 4 個 effects PNG | — |
| M8 救援 | `kitten.png`, `puppy.png`, `log.png`, `puddle.png`, `balloons.png`, `sfx_rescue_*` | — |
| M9 UI | `NotoSansTC-Regular.ttf`, `progress_bar_*.png` | celebration_banner |
| M10 場景 | `celebration_banner.png` | — |
| M11 音樂 | `bgm_main.ogg`, `bgm_celebration.ogg` | — |

---

## 6. 風險與替代方案

| 風險 | 替代方案 |
|---|---|
| 找不到合適的「獨角獸」sprite | 用普通馬 + 自己用 Aseprite/小畫家在馬頭上點一根白色三角形當犄角，5 分鐘搞定 |
| 找不到「公主」overlay | 直接讓獨角獸是主角，省略疊圖（4 歲認得獨角獸就夠開心） |
| Poli 看起來不夠像 | 重點是「藍車 + 紅藍頂燈閃爍」這個視覺特徵，畫面上閃就會被認成 Poli |
| 中文字型沒裝 | 程式 fallback 為英文字串（顯示 "Crystals: 3" 而非「水晶：3」），仍可玩 |
| 自己畫不來、AI 生成試很久都不滿意 | 直接全用 Kenney 色塊風格，不用追求精緻——孩子在意的是會動會發光不是寫實 |

---

## 7. 先做這 3 件事即可開工

如果你想最低成本先看到 demo：

1. 下載 **Kenney 的 Tiny Town** + **Top-Down Tanks Redux**（2 個 ZIP，10 分鐘）
2. 下載 **NotoSansTC-Regular.ttf**（1 分鐘）
3. 把 Kenney 警車重命名成 `poli.png`、馬重命名成 `unicorn.png`、草地 tile 重命名成 `grass_tile.png`、樹重命名成 `tree.png`，丟進 `assets/` 對應子資料夾

剩下的一律讓程式 fallback。Codex 寫完第一版後，你和孩子玩著玩著再慢慢補素材即可。
