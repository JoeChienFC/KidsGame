# 公主獨角獸與 Poli 救援隊 — 開發計畫書

> 給 AI（Codex）的實作指引。本文件為自包含規格，無需參考其他對話。

---

## 1. 專案概述

### 1.1 目標
一款 Windows 桌面遊戲，給 **2 歲弟弟** 與 **4 歲姐姐** 一起在同一台電腦上合作玩。姐姐喜歡公主與獨角獸，弟弟喜歡 Poli 救援車（Robocar Poli）。

### 1.2 核心遊玩循環
1. 姐姐用 **方向鍵** 控制獨角獸公主在地圖上探索
2. 弟弟用 **空白鍵** 讓自動跟隨的 Poli 救援車產生反應
3. 兩人合作找到「救援事件」（受困的小動物）
4. 弟弟狂按空白鍵推進救援進度
5. 救援完成 → 大量正向回饋（粒子、音效、動畫）

### 1.3 設計鐵律（**Codex 必讀**）
- **永不失敗**：沒有計時、沒有死亡、沒有 Game Over、沒有負面狀態
- **永遠有回饋**：弟弟按空白鍵不論身在何處都必須有可愛反應（音效＋動畫）
- **無敵路徑**：獨角獸碰到障礙物會在邊緣滑開，絕不卡死
- **2 歲容錯**：弟弟連按空白鍵不能讓畫面爆掉（粒子需有上限、音效需防止疊加）

---

## 2. 技術棧

| 項目 | 選擇 |
|---|---|
| 語言 | C# |
| 框架 | .NET 8 |
| 引擎 | Raylib-cs (NuGet: `Raylib-cs`) |
| 解析度 | 1280 × 720 視窗模式 |
| FPS 目標 | 60 |
| 平台 | Windows 10/11 |

### 2.1 專案建立指令
```bash
dotnet new console -n KidsGame
cd KidsGame
dotnet add package Raylib-cs
```

---

## 3. 遊戲設計規格

### 3.1 視角
- **純俯視角 2D**（top-down）
- 第一關地圖剛好等於畫面（1280×720），**不需鏡頭捲動**
- 所有事件、角色、收集物在開場即可見

### 3.2 角色

#### 獨角獸公主（玩家 1：姐姐）
| 屬性 | 值 |
|---|---|
| 操作 | 方向鍵（↑↓←→） |
| 移動速度 | 180 px/s |
| Sprite 尺寸 | 約 64×64 px |
| 朝向 | 4 方向（上下左右），依按鍵切換 |
| 特效 | 移動時每 0.1 秒發射 1 個彩虹粒子 |
| 碰撞 | 與地圖邊界硬碰撞、與裝飾物（樹/水坑）軟碰撞（沿邊滑動，絕不卡住） |

#### Poli 救援車（玩家 2：弟弟）
| 屬性 | 值 |
|---|---|
| 操作 | 空白鍵 |
| 移動方式 | 自動跟隨獨角獸 |
| 跟隨距離 | 維持 60–80 px |
| 跟隨演算法 | `position = Vector2.Lerp(position, target, 5f * dt)` |
| Sprite 尺寸 | 約 48×48 px |
| 朝向 | 依移動方向 4 方向切換 |

#### 空白鍵處理優先順序（**重要**）
```
按下空白鍵：
  IF Poli 位於任一 RescueEvent 半徑內 AND 該事件未完成：
      推進該事件進度（+1 hit）
      播放「轟」的救援音效
      該事件位置爆出愛心粒子
  ELSE：
      Poli 鳴笛＋頂燈閃爍（0.3 秒動畫）
      播放鳴笛音效
      Poli 周圍發射 5 個小煙火粒子
```
**任何按鍵都不可以「沒反應」**。

### 3.3 第一關地圖

```
┌──────────────────────────────────────┐
│  🌳[小貓事件]      🌳🌳[氣球事件]   │
│   倒木                樹＋氣球          │
│  💎                              💎  │
│                                      │
│        💎    [起始位置]    💎          │
│              🦄＋🚓                    │
│                                      │
│  💎                              💎  │
│           [小狗事件]                  │
│            水坑＋小狗                   │
└──────────────────────────────────────┘
```

#### 救援事件清單

| ID | 名稱 | 位置 | 需要按鍵次數 | 完成動畫 |
|---|---|---|---|---|
| `kitten` | 小貓被倒木壓住 | (200, 150) | 5 | 樹幹彈飛 → 小貓跳出 → 愛心爆發 → 「喵～」 |
| `balloon` | 氣球卡在大樹 | (1050, 150) | 6 | 彩虹粒子環繞樹 → 氣球飄走 → 「啵啵啵」 |
| `puppy` | 小狗被水坑擋住 | (640, 580) | 7 | 彩虹橋出現 → 小狗跑過來 → 「汪～」 |

#### 收集物
- **彩虹水晶** × 8 顆，散布在路徑上
- 獨角獸碰到 → 消失、播放清脆音效、UI 計數 +1、灑出 6 個星星粒子

#### 勝利條件
3 個救援事件全部完成 → 切換到 `CelebrationScene` → 播放慶祝音樂 5 秒、所有救出的小動物在畫面中央跳舞、煙火齊放 → 自動回 `TitleScene`

### 3.4 規則
- **無時間限制**
- **無失敗條件**
- **無計分**（只有可愛統計：「救了 3 隻小動物！收集了 8 顆水晶！」）
- **無懲罰**

---

## 4. 美術與音效素材

### 4.1 來源（皆為免費可商用）

| 來源 | 用途 | 授權 |
|---|---|---|
| [Kenney.nl](https://kenney.nl/assets) | 主要素材 | CC0 |
| [OpenGameArt.org](https://opengameart.org/) | 補充 | CC0 / CC-BY |
| [itch.io](https://itch.io/game-assets/free) | 公主/獨角獸特化 | 需逐一檢查授權 |

### 4.2 推薦 Kenney 素材包（直接下載 ZIP）
- `Toon Characters 1` — 角色基底（公主可由女性角色 + 皇冠 overlay）
- `Animal Pack Redux` — 小貓、小狗、小兔
- `Top-down Shooter` — 車輛（警車當 Poli 基底，紅藍頂燈）
- `Particle Pack` — 彩虹/愛心/星星/煙火
- `Tiny Town` — 樹、石頭、地形 tile
- `UI Pack RPG Expansion` — UI 邊框、進度條
- `Interface Sounds` + `Impact Sounds` — 音效
- `Music Jingles` — 短音樂

### 4.3 必要素材檔案結構（程式預期路徑）

```
assets/
├── characters/
│   ├── unicorn.png          # 4 方向 spritesheet, 4×64×64 px (橫向排列)
│   ├── princess_overlay.png # 疊在獨角獸上, 4×32×32 px
│   └── poli.png             # 4 方向 spritesheet, 4×48×48 px
├── animals/
│   ├── kitten.png           # 32×32 px, 2 frame idle
│   ├── puppy.png            # 32×32 px, 2 frame idle
│   └── bunny.png            # 32×32 px (慶祝畫面用)
├── world/
│   ├── grass_tile.png       # 64×64 px, 可平鋪
│   ├── path_tile.png        # 64×64 px, 可平鋪
│   ├── log.png              # 96×32 px (壓貓的樹幹)
│   ├── tree.png             # 96×128 px
│   ├── puddle.png           # 96×48 px
│   ├── balloons.png         # 64×96 px (3 顆氣球綁在一起)
│   └── crystal.png          # 24×32 px, 4 frame 旋轉動畫 (橫向排列)
├── effects/
│   ├── rainbow_particle.png # 16×16 px
│   ├── heart_particle.png   # 16×16 px
│   ├── star_particle.png    # 16×16 px
│   └── firework.png         # 32×32 px, 8 frame
├── ui/
│   ├── progress_bar_bg.png
│   ├── progress_bar_fill.png
│   └── celebration_banner.png
├── fonts/
│   └── NotoSansTC-Regular.ttf  # 必須支援繁體中文
└── audio/
    ├── bgm_main.ogg            # 主關卡 BGM, 可循環
    ├── bgm_celebration.ogg     # 慶祝音樂
    ├── sfx_collect.ogg         # 撿水晶
    ├── sfx_button.ogg          # 鳴笛
    ├── sfx_rescue_hit.ogg      # 救援按一次
    ├── sfx_rescue_complete.ogg # 救援完成
    ├── sfx_meow.ogg
    └── sfx_woof.ogg
```

### 4.4 缺素材時的後備方案
若某個素材找不到，**程式必須優雅降級**，不能崩潰：
- 圖片缺失 → 用對應顏色的 `Raylib.DrawRectangle` 或 `DrawCircle` 替代（小貓 = 橘色圓、Poli = 藍色矩形＋紅色頂燈閃爍）
- 音效缺失 → 略過播放，記錄 log
- 字型缺失 → 退回 Raylib 預設字型，但中文會變方塊（記得在執行說明中強調必裝字型）

---

## 5. 專案結構

```
KidsGame/
├── KidsGame.csproj
├── Program.cs                 # Main 進入點
├── Game.cs                    # 主迴圈、場景管理
├── Scenes/
│   ├── IScene.cs
│   ├── TitleScene.cs          # 開場，按任意鍵開始
│   ├── GameScene.cs           # 第一關
│   └── CelebrationScene.cs    # 通關慶祝
├── Entities/
│   ├── Unicorn.cs
│   ├── Poli.cs
│   ├── Crystal.cs
│   └── RescueEvent.cs
├── Effects/
│   ├── ParticleSystem.cs
│   └── Particle.cs
├── Audio/
│   └── AudioManager.cs        # 集中管理音效播放、防止疊加
├── Assets/
│   └── AssetManager.cs        # 集中載入所有圖片、字型、音效
├── Util/
│   └── Mathf.cs               # Lerp、Clamp 等小工具
└── assets/                    # 美術音效（見上一節）
```

---

## 6. 實作里程碑（建議順序）

每個里程碑結束都應可執行並看到結果。Codex 應依序完成並在每個里程碑結束時驗證。

### M1：專案骨架
- 建立 .NET 8 console project，加入 Raylib-cs
- 開啟 1280×720 視窗、灰底、顯示 "KidsGame" 文字
- ESC 關閉視窗

### M2：資產載入器（AssetManager）
- 載入所有 `assets/` 下圖片、字型、音效
- 缺檔時印 warning 但繼續執行
- 提供 `GetTexture(string name)`, `GetSound(string name)`, `GetFont()`

### M3：獨角獸玩家
- 載入 `unicorn.png` + `princess_overlay.png`
- 方向鍵 4 方向移動，180 px/s
- 邊界限制在 (0, 0) 到 (1280, 720)
- 移動時噴彩虹粒子（接後面 M7 粒子系統）

### M4：Poli 跟隨
- 載入 `poli.png`
- 用 Lerp 平滑跟隨獨角獸，距離 60–80 px
- 空白鍵按下：播放 `sfx_button.ogg`、Poli 跳一下（Y 軸短暫 +5 px 後回彈）

### M5：地圖與裝飾
- 平鋪 `grass_tile.png` 鋪滿背景
- 用 `path_tile.png` 鋪一個十字路徑
- 放置 3 個事件相關裝飾（樹、樹幹、水坑、氣球樹）在前述座標
- 散布 8 顆水晶（用 `crystal.png` 4 frame 旋轉動畫）

### M6：水晶收集
- `Crystal.cs`：位置、是否被收集、旋轉動畫
- 獨角獸 AABB 碰到水晶 → 消失、播 `sfx_collect.ogg`、噴 6 個星星粒子、UI 計數 +1

### M7：粒子系統
- `Particle.cs`：position, velocity, life, texture, color tint
- `ParticleSystem.cs`：陣列上限 500，超過時最舊的覆寫
- `Emit(Vector2 pos, ParticleType type, int count)` 支援彩虹、愛心、星星、煙火

### M8：救援事件系統
- `RescueEvent` 類別（見第 7 節）
- 三個實例：kitten, balloon, puppy
- Poli 在事件半徑（80 px）內按空白鍵 → progress +1
- 進度滿 → 觸發完成動畫（事件處爆出 30 個愛心粒子，播放對應音效，動物 sprite 改為「開心」frame）

### M9：UI
- 左上：水晶計數（💎 × N / 8）
- 右上：救援計數（💖 × N / 3）
- 救援事件靠近時，事件正上方顯示進度條
- 字型：`NotoSansTC-Regular.ttf`，配合 `Raylib.LoadFontEx` 並提供繁中 codepoints

### M10：場景流程
- `TitleScene`：標題「公主獨角獸與 Poli 救援隊」、副標「按任意鍵開始」、簡單動畫
- `GameScene`：第一關
- `CelebrationScene`：3 個事件完成時切換進來，5 秒後回 `TitleScene`

### M11：音樂與打磨
- 主關 BGM 循環播放
- 慶祝音樂在 CelebrationScene 播放
- 整體音量平衡（BGM 60%, SFX 100%）
- 確認 60 FPS 穩定

---

## 7. 程式架構（類別骨架）

> 以下為簽名與責任，非完整實作。Codex 依此擴展。

### `Game.cs`
```csharp
public class Game
{
    public AssetManager Assets { get; private set; }
    public AudioManager Audio { get; private set; }
    private IScene _currentScene;

    public void Run()
    {
        Raylib.InitWindow(1280, 720, "公主獨角獸與 Poli 救援隊");
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        Assets = new AssetManager();
        Assets.LoadAll();
        Audio = new AudioManager(Assets);

        _currentScene = new TitleScene(this);

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            _currentScene.Update(dt);

            Raylib.BeginDrawing();
            _currentScene.Draw();
            Raylib.EndDrawing();
        }

        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    public void ChangeScene(IScene next) => _currentScene = next;
}
```

### `IScene.cs`
```csharp
public interface IScene
{
    void Update(float dt);
    void Draw();
}
```

### `Unicorn.cs`
```csharp
public class Unicorn
{
    public Vector2 Position { get; private set; }
    public Vector2 LastDirection { get; private set; } = new(0, 1); // 預設朝下
    private const float Speed = 180f;
    private float _particleTimer = 0f;

    public Unicorn(Vector2 startPos) { Position = startPos; }

    public void Update(float dt, ParticleSystem particles)
    {
        Vector2 input = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.Up))    input.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.Down))  input.Y += 1;
        if (Raylib.IsKeyDown(KeyboardKey.Left))  input.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.Right)) input.X += 1;

        if (input.LengthSquared() > 0)
        {
            input = Vector2.Normalize(input);
            LastDirection = input;
            Position += input * Speed * dt;
            // 邊界
            Position = new Vector2(
                Math.Clamp(Position.X, 32, 1248),
                Math.Clamp(Position.Y, 32, 688));
            // 彩虹粒子
            _particleTimer += dt;
            if (_particleTimer >= 0.1f) { particles.EmitRainbow(Position, 1); _particleTimer = 0; }
        }
    }

    public void Draw(AssetManager assets) { /* 依 LastDirection 畫對應 frame */ }
}
```

### `Poli.cs`
```csharp
public class Poli
{
    public Vector2 Position { get; private set; }
    private Vector2 _facing = new(0, 1);
    private float _bounceTimer = 0f;

    public Poli(Vector2 startPos) { Position = startPos; }

    public void Update(float dt, Unicorn target, List<RescueEvent> events,
                       ParticleSystem particles, AudioManager audio)
    {
        // 跟隨：保持 60-80 px 距離
        Vector2 toTarget = target.Position - Position;
        float dist = toTarget.Length();
        if (dist > 70) Position = Vector2.Lerp(Position, target.Position, 5f * dt);
        if (dist > 1) _facing = Vector2.Normalize(toTarget);

        _bounceTimer = Math.Max(0, _bounceTimer - dt);

        // 空白鍵
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            RescueEvent? nearby = events.FirstOrDefault(e =>
                !e.Completed && Vector2.Distance(e.Position, Position) <= e.Radius);

            if (nearby != null)
            {
                nearby.OnHit(particles, audio);
            }
            else
            {
                audio.Play("sfx_button");
                particles.EmitFirework(Position, 5);
                _bounceTimer = 0.3f; // 觸發彈跳
            }
        }
    }

    public void Draw(AssetManager assets)
    {
        float bounceOffset = _bounceTimer > 0 ? -5f * MathF.Sin(_bounceTimer * MathF.PI / 0.3f) : 0;
        // 依 _facing 畫對應 frame，Y 加上 bounceOffset
    }
}
```

### `RescueEvent.cs`
```csharp
public class RescueEvent
{
    public Vector2 Position;
    public float Radius = 80f;
    public int RequiredHits;
    public int CurrentHits = 0;
    public bool Completed = false;
    public string Type;          // "kitten" / "balloon" / "puppy"
    public string CompleteSfx;   // "sfx_meow" / "sfx_woof" / ...

    public void OnHit(ParticleSystem particles, AudioManager audio)
    {
        if (Completed) return;
        CurrentHits++;
        audio.Play("sfx_rescue_hit");
        particles.EmitHearts(Position, 8);
        if (CurrentHits >= RequiredHits) OnComplete(particles, audio);
    }

    private void OnComplete(ParticleSystem particles, AudioManager audio)
    {
        Completed = true;
        audio.Play("sfx_rescue_complete");
        audio.Play(CompleteSfx);
        particles.EmitHearts(Position, 30);
        particles.EmitFirework(Position, 15);
    }

    public void Draw(AssetManager assets) { /* 畫場景物 + 動物 + 進度條 */ }
}
```

### `ParticleSystem.cs`
```csharp
public class ParticleSystem
{
    private const int MaxParticles = 500;
    private Particle[] _particles = new Particle[MaxParticles];
    private int _writeIdx = 0;

    public void EmitRainbow(Vector2 pos, int count) { /* 彩虹色循環 */ }
    public void EmitHearts(Vector2 pos, int count) { /* 粉紅愛心 */ }
    public void EmitStars(Vector2 pos, int count) { /* 黃色星星 */ }
    public void EmitFirework(Vector2 pos, int count) { /* 多色放射 */ }

    public void Update(float dt) { /* 更新所有 alive 粒子 */ }
    public void Draw(AssetManager assets) { /* 畫所有 alive 粒子 */ }
}
```

### `AudioManager.cs`
```csharp
public class AudioManager
{
    private readonly AssetManager _assets;
    private readonly Dictionary<string, double> _lastPlayedAt = new();
    private const double MinIntervalSeconds = 0.05; // 防止疊加

    public AudioManager(AssetManager assets) { _assets = assets; }

    public void Play(string name)
    {
        double now = Raylib.GetTime();
        if (_lastPlayedAt.TryGetValue(name, out double last) && now - last < MinIntervalSeconds)
            return;
        var sound = _assets.GetSound(name);
        if (sound.HasValue) Raylib.PlaySound(sound.Value);
        _lastPlayedAt[name] = now;
    }

    public void PlayMusic(string name) { /* 用 Music API 循環播放 */ }
    public void StopMusic() { /* */ }
}
```

---

## 8. 驗收標準（Codex 完工自檢）

執行 `dotnet run` 後，下列每一項都必須成立：

- [ ] 視窗 1280×720 開啟，標題列顯示「公主獨角獸與 Poli 救援隊」
- [ ] 標題畫面按任意鍵進入第一關，過程無延遲
- [ ] 方向鍵流暢控制獨角獸（無延遲、無卡頓、可斜向移動）
- [ ] 獨角獸不會走出畫面邊界
- [ ] Poli 平滑跟隨獨角獸，不會貼太近也不會掉太遠（保持 60–80 px）
- [ ] 空白鍵 **按下永遠有反應**：在事件範圍內會推進進度，否則鳴笛＋小煙火
- [ ] 走過水晶會消失、播放音效、UI 計數 +1、灑出星星粒子
- [ ] 走近三個救援事件時，事件正上方顯示進度條
- [ ] 連按空白鍵填滿進度條 → 救援完成動畫（愛心爆炸＋動物音效）
- [ ] 三個事件全部完成 → 切換到慶祝畫面 → 5 秒後自動回標題
- [ ] 全程沒有任何「失敗」「Game Over」「死亡」狀態
- [ ] 中文字顯示正常（不能是方塊或亂碼）
- [ ] 60 FPS 穩定，狂按空白鍵不會掉幀
- [ ] 缺少素材檔案時不會崩潰，有 fallback 顯示

---

## 9. 給 Codex 的特別提醒

1. **絕對不要加挑戰性元素**：沒有計時、沒有限制、沒有失敗、沒有「Game Over」。如果在實作中覺得「應該加個敵人會更好玩」——**不要加**。
2. **「亂按一定有用」原則**：弟弟亂按空白鍵時，永遠要有正向回饋。空白鍵的處理優先順序見 §3.2。
3. **錯誤容忍**：所有 `LoadTexture`/`LoadSound`/`LoadFontEx` 用 try-catch 包起來。缺檔時用色塊或預設字型代替，**程式絕不能崩潰**。
4. **中文字型**：使用 `Raylib.LoadFontEx` 載入 `NotoSansTC-Regular.ttf`，第三參數提供 codepoints 涵蓋所有 UI 用到的中文字（標題、副標、計數標籤等）。建議直接掃過程式碼字串收集 codepoints，或預先放一個常用 3500 字的陣列。
5. **粒子上限**：粒子總數上限 500。超過時用 ring buffer 覆寫最舊的，而不是丟棄新的。
6. **音效防止疊加**：相同音效間隔 50 ms 才能重新播放（見 `AudioManager.Play` 範例）。
7. **不要過度抽象**：這是 ~1500 行程式的小遊戲。不需要 ECS、不需要事件匯流排、不需要依賴注入容器。直接寫。
8. **不要寫單元測試**：這是給小孩玩的遊戲，靠手動驗收即可。把時間花在打磨手感與視覺。
9. **不要加註解講「做了什麼」**：好的識別命名已經自我說明。只有「為什麼這樣寫不那樣寫」的非顯而易見原因才寫註解。
10. **AssetManager 要有 fallback 紋理**：建立一個 16×16 的洋紅色紋理當作「找不到圖片」時的占位，這樣即使所有素材都沒準備好也能跑起來看到角色在動。

---

## 10. 後續延伸（v2.0，**先不做**）

- 第二、三關（不同主題：海邊、太空）
- 解鎖 Poli 變身（消防車、救護車、直升機）
- 公主換裝功能
- 弟弟空白鍵也能吸取附近水晶
- 兩人同時撿到水晶的合作獎勵
- 家長模式（看孩子玩了多久、救了幾次）

---

## 附錄 A：執行說明（README 內容草稿）

```
# 公主獨角獸與 Poli 救援隊

給 2-4 歲小朋友合作玩的 Windows 桌面遊戲。

## 操作
- 姐姐（玩家1）：方向鍵移動獨角獸公主
- 弟弟（玩家2）：空白鍵讓 Poli 救援車反應

## 執行
1. 安裝 .NET 8 SDK
2. 解壓縮素材到 assets/ 資料夾
3. dotnet run

## 退出
按 ESC
```
