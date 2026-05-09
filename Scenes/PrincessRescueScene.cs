using System.Numerics;
using KidsGame.Effects;
using KidsGame.Util;
using Raylib_cs;

namespace KidsGame.Scenes;

public sealed class PrincessRescueScene : IScene
{
    private sealed class Food
    {
        public Vector2 Position { get; }
        public RescueSprite Sprite { get; }
        public bool Eaten { get; set; }

        public Food(Vector2 position, RescueSprite sprite)
        {
            Position = position;
            Sprite = sprite;
        }
    }

    private sealed class CompanionPrincess
    {
        public RescueSprite Sprite { get; }
        public Vector2 Position;
        public Vector2 Velocity;
        public bool Following;
        public float Phase { get; }

        public CompanionPrincess(RescueSprite sprite, Vector2 position, float phase)
        {
            Sprite = sprite;
            Position = position;
            Phase = phase;
        }
    }

    private sealed class Decoration
    {
        public RescueSprite Sprite { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public float Rotation { get; }
        public byte Alpha { get; }
        public float Phase { get; }

        public Decoration(RescueSprite sprite, Vector2 position, Vector2 size, float rotation, byte alpha, float phase)
        {
            Sprite = sprite;
            Position = position;
            Size = size;
            Rotation = rotation;
            Alpha = alpha;
            Phase = phase;
        }
    }

    private sealed class MagicMatch
    {
        public string Name { get; }
        public string OrbTexture { get; }
        public string GateTexture { get; }
        public Vector2 OrbPosition { get; }
        public Vector2 GatePosition { get; }
        public Color Accent { get; }
        public bool Collected { get; set; }
        public bool Matched { get; set; }
        public float Phase { get; }

        public MagicMatch(string name, string orbTexture, string gateTexture, Vector2 orbPosition, Vector2 gatePosition, Color accent, float phase)
        {
            Name = name;
            OrbTexture = orbTexture;
            GateTexture = gateTexture;
            OrbPosition = orbPosition;
            GatePosition = gatePosition;
            Accent = accent;
            Phase = phase;
        }
    }

    private sealed class DressupAccessory
    {
        public string Name { get; }
        public string Texture { get; }
        public Vector2 Position;
        public Vector2 Size { get; }
        public bool Placed { get; set; }
        public float Rotation { get; }

        public DressupAccessory(string name, string texture, Vector2 position, Vector2 size, float rotation)
        {
            Name = name;
            Texture = texture;
            Position = position;
            Size = size;
            Rotation = rotation;
        }
    }

    private const float NormalMaxSpeed = 240f;
    private const float PoweredMaxSpeed = 310f;
    private const float NormalAccel = 1700f;
    private const float PoweredAccel = 2200f;
    private const float Friction = 8f;

    private readonly Game _game;
    private readonly ParticleSystem _particles = new();
    private readonly FloatingTextSystem _floatingTexts = new();
    private readonly int _stage;
    private readonly Food[] _foods;
    private readonly CompanionPrincess[] _companions;
    private readonly Decoration[] _decorations;
    private readonly MagicMatch[] _magicMatches;
    private readonly DressupAccessory[] _dressupAccessories;
    private readonly Vector2 _castlePosition;
    private readonly Vector2 _housePosition;

    private Vector2 _princessPosition;
    private Vector2 _princessVelocity;
    private float _princessFacing = 1f;
    private float _walkTime;
    private float _dustTimer;
    private float _idleTimer;

    private float _time;
    private int _houseHits;
    private bool _powered;
    private bool _complete;
    private float _hitShake;
    private float _smashTimer;
    private float _completeTimer;

    private float _zoomPunch;
    private float _globalShake;
    private float _slowMoTimer;
    private float _flashAmount;
    private float _ambientSparkleTimer;
    private int _heldMagicIndex = -1;
    private float _magicFeedbackTimer;
    private string _magicFeedbackText = "";
    private int _currentAccessoryIndex;
    private float _dressupThinkingTimer;
    private bool _dressupThinking;
    private float _dressupHeartPulse;

    public PrincessRescueScene(Game game) : this(game, 1) { }

    public PrincessRescueScene(Game game, int stage)
    {
        _game = game;
        _stage = Math.Clamp(stage, 1, 3);
        _princessPosition = _stage switch
        {
            1 => new Vector2(185, 560),
            2 => new Vector2(180, 560),
            _ => new Vector2(640, 548),
        };
        _castlePosition = _stage == 1 ? new Vector2(640, 260) : new Vector2(622, 248);
        _housePosition = _stage == 1 ? new Vector2(1032, 342) : new Vector2(1018, 346);
        var rand = new Random(Environment.TickCount ^ Guid.NewGuid().GetHashCode() ^ _stage * 97);
        var occupied = new List<Vector2> { _princessPosition, _castlePosition, _housePosition };
        _foods = _stage == 1 ? CreateFoods(rand, occupied) : [];
        _companions = _stage == 1 ? CreateCompanions(rand, occupied) : [];
        _decorations = _stage == 1 ? CreateDecorations(rand) : [];
        _magicMatches = _stage == 2 ? CreateMagicMatches() : [];
        _dressupAccessories = _stage == 3 ? CreateDressupAccessories() : [];
    }

    private Food[] CreateFoods(Random rand, List<Vector2> occupied)
    {
        RescueSprite[] sprites = _stage == 1
            ? [RescueSprite.Apple, RescueSprite.Carrot, RescueSprite.Apple, RescueSprite.Carrot, RescueSprite.Apple, RescueSprite.Carrot, RescueSprite.Apple, RescueSprite.Carrot]
            : [RescueSprite.Apple, RescueSprite.Carrot, RescueSprite.Apple, RescueSprite.Carrot, RescueSprite.Apple, RescueSprite.Carrot, RescueSprite.Apple, RescueSprite.Carrot, RescueSprite.Apple, RescueSprite.Carrot];
        var foods = new Food[sprites.Length];
        for (var i = 0; i < foods.Length; i++)
        {
            var position = PickOpenPosition(rand, occupied, 112f, 76f);
            occupied.Add(position);
            foods[i] = new Food(position, sprites[i]);
        }

        return foods;
    }

    private CompanionPrincess[] CreateCompanions(Random rand, List<Vector2> occupied)
    {
        var sprites = new[]
        {
            _stage == 1 ? RescueSprite.PrincessMermaid : RescueSprite.PrincessRose,
            _stage == 1 ? RescueSprite.PrincessIce : RescueSprite.PrincessSnow,
            _stage == 1 ? RescueSprite.PrincessSnow : RescueSprite.PrincessCinderella,
            _stage == 1 ? RescueSprite.PrincessCinderella : RescueSprite.PrincessMermaid,
            _stage == 1 ? RescueSprite.PrincessRapunzel : RescueSprite.PrincessIce,
            _stage == 1 ? RescueSprite.PrincessRose : RescueSprite.PrincessRapunzel,
        };
        var companions = new CompanionPrincess[sprites.Length];
        for (var i = 0; i < companions.Length; i++)
        {
            var position = PickOpenPosition(rand, occupied, 132f, 82f);
            occupied.Add(position);
            companions[i] = new CompanionPrincess(sprites[i], position, NextFloat(rand, 0f, MathF.Tau));
        }

        return companions;
    }

    private Decoration[] CreateDecorations(Random rand)
    {
        var sprites = new[]
        {
            RescueSprite.ForestBroadleaf,
            RescueSprite.ForestPine,
            RescueSprite.ForestBush,
            RescueSprite.ForestMushroom,
            RescueSprite.ForestLog,
            RescueSprite.ForestRocks,
            RescueSprite.ForestFlowers,
            RescueSprite.ForestPath,
            RescueSprite.GardenArch,
            RescueSprite.RoyalFountain,
        };
        var decorations = new Decoration[22];
        var occupied = new List<Vector2> { _castlePosition, _housePosition, _princessPosition };
        for (var i = 0; i < decorations.Length; i++)
        {
            var sprite = sprites[rand.Next(sprites.Length)];
            var position = PickOpenPosition(rand, occupied, 92f, 70f);
            occupied.Add(position);
            var size = GetDecorationSize(sprite) * NextFloat(rand, 0.82f, 1.25f);
            var rotation = NextFloat(rand, -7f, 7f);
            decorations[i] = new Decoration(sprite, position, size, rotation, (byte)rand.Next(210, 246), NextFloat(rand, 0f, MathF.Tau));
        }

        return decorations;
    }

    private static MagicMatch[] CreateMagicMatches()
    {
        return
        [
            new("粉紅", "magic_orb_pink", "magic_gate_pink", new Vector2(245, 250), new Vector2(980, 236), new Color(255, 107, 186, 255), 0.2f),
            new("藍色", "magic_orb_blue", "magic_gate_blue", new Vector2(355, 540), new Vector2(672, 178), new Color(74, 174, 255, 255), 1.8f),
            new("黃色", "magic_orb_yellow", "magic_gate_yellow", new Vector2(790, 558), new Vector2(1114, 470), new Color(255, 214, 77, 255), 3.2f),
        ];
    }

    private static DressupAccessory[] CreateDressupAccessories()
    {
        return
        [
            new("皇冠", "unicorn_crown", new Vector2(184, 210), new Vector2(112, 74), -5f),
            new("蝴蝶結", "unicorn_bow", new Vector2(184, 360), new Vector2(112, 96), 5f),
            new("星星披風", "unicorn_blanket", new Vector2(184, 510), new Vector2(156, 98), -2f),
        ];
    }

    private Vector2 PickOpenPosition(Random rand, List<Vector2> occupied, float minDistance, float margin)
    {
        for (var attempt = 0; attempt < 180; attempt++)
        {
            var position = new Vector2(
                NextFloat(rand, margin, Game.ScreenWidth - margin),
                NextFloat(rand, 116f, Game.ScreenHeight - margin));
            if (IsBlockedSpawn(position)) continue;
            if (occupied.Any(p => Vector2.Distance(p, position) < minDistance)) continue;

            return position;
        }

        return new Vector2(NextFloat(rand, margin, Game.ScreenWidth - margin), NextFloat(rand, 136f, Game.ScreenHeight - margin));
    }

    private bool IsBlockedSpawn(Vector2 position)
    {
        return IsInside(position, new Rectangle(_castlePosition.X - 190, _castlePosition.Y - 190, 380, 500))
            || IsInside(position, new Rectangle(_housePosition.X - 210, _housePosition.Y - 170, 420, 330))
            || IsInside(position, new Rectangle(562, 166, 156, 480))
            || IsInside(position, new Rectangle(574, 506, 432, 132));
    }

    private static bool IsInside(Vector2 point, Rectangle rect)
    {
        return point.X >= rect.X && point.X <= rect.X + rect.Width && point.Y >= rect.Y && point.Y <= rect.Y + rect.Height;
    }

    private static Vector2 GetDecorationSize(RescueSprite sprite)
    {
        return sprite switch
        {
            RescueSprite.ForestBroadleaf => new Vector2(210, 160),
            RescueSprite.ForestPine => new Vector2(205, 165),
            RescueSprite.ForestBush => new Vector2(185, 124),
            RescueSprite.ForestMushroom => new Vector2(150, 104),
            RescueSprite.ForestLog => new Vector2(178, 98),
            RescueSprite.ForestRocks => new Vector2(142, 104),
            RescueSprite.ForestFlowers => new Vector2(160, 102),
            RescueSprite.ForestPath => new Vector2(230, 106),
            RescueSprite.GardenArch => new Vector2(122, 160),
            RescueSprite.RoyalFountain => new Vector2(154, 108),
            _ => new Vector2(150, 110),
        };
    }

    private static float NextFloat(Random rand, float min, float max)
    {
        return min + (float)rand.NextDouble() * (max - min);
    }

    private int CurrentRequiredHits => _stage == 1 ? 24 : 32;

    private bool HasNextStage => _stage < 3;

    private string CurrentStageLabel => _stage switch
    {
        1 => "第一關",
        2 => "第二關",
        _ => "第三關",
    };

    private string CurrentRescueName => _stage switch
    {
        1 => "小公主",
        2 => "顏色魔法",
        _ => "獨角獸",
    };

    private string CurrentRescuePlace => _stage == 1 ? "房子" : "同顏色魔法門";

    public void Update(float dt)
    {
        _time += dt;
        _hitShake = MathF.Max(0f, _hitShake - dt * 2.8f);
        _smashTimer = MathF.Max(0f, _smashTimer - dt);
        _zoomPunch = MathF.Max(0f, _zoomPunch - dt * 3.2f);
        _globalShake = MathF.Max(0f, _globalShake - dt * 9f);
        _flashAmount = MathF.Max(0f, _flashAmount - dt * 2.4f);

        var effectiveDt = dt;
        if (_slowMoTimer > 0f)
        {
            _slowMoTimer -= dt;
            effectiveDt = dt * 0.42f;
        }

        if (_complete)
        {
            _completeTimer += effectiveDt;
            _particles.Update(effectiveDt);
            _floatingTexts.Update(dt);

            _ambientSparkleTimer -= dt;
            if (_completeTimer < 4f && _ambientSparkleTimer <= 0f)
            {
                _ambientSparkleTimer = 0.05f;
                var x = (float)Random.Shared.NextDouble() * Game.ScreenWidth;
                _particles.EmitConfetti(new Vector2(x, -10), 1);
            }

            if (HasNextStage && _completeTimer > 2.6f)
            {
                _game.ChangeScene(new PrincessRescueScene(_game, _stage + 1));
                return;
            }

            if (_completeTimer > 0.6f && Raylib.GetKeyPressed() != 0)
            {
                _game.ChangeScene(HasNextStage ? new PrincessRescueScene(_game, _stage + 1) : new TitleScene(_game));
            }
            return;
        }

        if (_stage == 2)
        {
            UpdateColorMatchLevel(effectiveDt);
            return;
        }

        if (_stage == 3)
        {
            UpdateDressupLevel(effectiveDt);
            return;
        }

        MovePrincess(effectiveDt);
        UpdateFood();
        UpdateCompanions(effectiveDt);

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            if (_powered && Vector2.Distance(_princessPosition, _housePosition) <= 165f)
            {
                HitHouse();
            }
            else
            {
                _game.Audio.Play("sfx_button");
                _particles.EmitStars(_princessPosition + new Vector2(0, -44), 4);
                _zoomPunch = MathF.Max(_zoomPunch, 0.15f);
            }
        }

        _ambientSparkleTimer -= effectiveDt;
        if (_ambientSparkleTimer <= 0f)
        {
            _ambientSparkleTimer = 0.16f;
            var x = (float)Random.Shared.NextDouble() * Game.ScreenWidth;
            var y = 50f + (float)Random.Shared.NextDouble() * (Game.ScreenHeight - 180f);
            _particles.EmitSparkle(new Vector2(x, y), 1);
        }

        _particles.Update(effectiveDt);
        _floatingTexts.Update(dt);
    }

    private void UpdateColorMatchLevel(float dt)
    {
        MovePrincess(dt);
        _magicFeedbackTimer = MathF.Max(0f, _magicFeedbackTimer - dt);

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            _game.Audio.Play("sfx_button");
            _particles.EmitStars(_princessPosition + new Vector2(0, -42), 5);
            _zoomPunch = MathF.Max(_zoomPunch, 0.16f);
        }

        if (_heldMagicIndex < 0)
        {
            for (var i = 0; i < _magicMatches.Length; i++)
            {
                var match = _magicMatches[i];
                if (match.Collected || match.Matched) continue;
                if (Vector2.Distance(_princessPosition, match.OrbPosition) > 64f) continue;

                match.Collected = true;
                _heldMagicIndex = i;
                _game.Audio.Play("sfx_collect");
                _particles.EmitStars(match.OrbPosition, 18);
                _particles.EmitHearts(match.OrbPosition, 8);
                _floatingTexts.Spawn($"{match.Name}魔法!", match.OrbPosition + new Vector2(0, -70), match.Accent, 31f, 1.4f);
                _zoomPunch = MathF.Max(_zoomPunch, 0.35f);
                break;
            }
        }
        else
        {
            var held = _magicMatches[_heldMagicIndex];
            for (var i = 0; i < _magicMatches.Length; i++)
            {
                var gate = _magicMatches[i];
                if (gate.Matched || Vector2.Distance(_princessPosition, gate.GatePosition) > 92f) continue;

                if (i == _heldMagicIndex)
                {
                    gate.Matched = true;
                    _heldMagicIndex = -1;
                    _game.Audio.Play("sfx_rescue_complete");
                    _particles.EmitRainbow(gate.GatePosition, 36);
                    _particles.EmitHearts(gate.GatePosition, 24);
                    _particles.EmitStars(gate.GatePosition, 28);
                    _floatingTexts.Spawn("配對成功!", gate.GatePosition + new Vector2(0, -112), gate.Accent, 36f, 1.6f);
                    _zoomPunch = MathF.Max(_zoomPunch, 0.6f);
                    _flashAmount = 0.45f;
                }
                else if (_magicFeedbackTimer <= 0f)
                {
                    _magicFeedbackTimer = 1.1f;
                    _magicFeedbackText = $"找{held.Name}門";
                    _game.Audio.Play("sfx_button");
                    _particles.EmitStars(gate.GatePosition + new Vector2(0, -42), 6);
                    _floatingTexts.Spawn(_magicFeedbackText, gate.GatePosition + new Vector2(0, -118), held.Accent, 29f, 1.0f);
                }
                break;
            }
        }

        if (!_complete && _magicMatches.All(m => m.Matched))
        {
            _complete = true;
            _completeTimer = 0f;
            _game.Audio.Play("sfx_rescue_complete");
            _particles.EmitFirework(new Vector2(640, 340), 70);
            _particles.EmitRainbow(new Vector2(640, 342), 52);
            _particles.EmitHearts(new Vector2(640, 320), 44);
            _floatingTexts.Spawn("顏色魔法完成!", new Vector2(640, 210), new Color(255, 108, 190, 255), 42f, 2.0f);
            _zoomPunch = 1f;
            _slowMoTimer = 0.5f;
            _globalShake = 10f;
            _flashAmount = 0.85f;
        }

        UpdateAmbientMagic(dt);
        _particles.Update(dt);
        _floatingTexts.Update(dt);
    }

    private void UpdateDressupLevel(float dt)
    {
        if (_dressupThinking)
        {
            _dressupThinkingTimer -= dt;
            _dressupHeartPulse += dt;
            if (_dressupThinkingTimer <= 0f)
            {
                _dressupThinking = false;
                _complete = true;
                _completeTimer = 0f;
                _game.Audio.Play("sfx_rescue_complete");
                _particles.EmitHearts(new Vector2(704, 250), 70);
                _particles.EmitStars(new Vector2(704, 250), 44);
                _particles.EmitRainbow(new Vector2(704, 300), 42);
                _floatingTexts.Spawn("獨角獸喜歡!", new Vector2(704, 154), new Color(255, 96, 168, 255), 44f, 2.0f);
                _zoomPunch = 0.9f;
                _flashAmount = 0.75f;
            }

            UpdateAmbientMagic(dt);
            _particles.Update(dt);
            _floatingTexts.Update(dt);
            return;
        }

        if (_currentAccessoryIndex < _dressupAccessories.Length)
        {
            var current = _dressupAccessories[_currentAccessoryIndex];
            var input = Vector2.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.Left)) input.X -= 1f;
            if (Raylib.IsKeyDown(KeyboardKey.Right)) input.X += 1f;
            if (Raylib.IsKeyDown(KeyboardKey.Up)) input.Y -= 1f;
            if (Raylib.IsKeyDown(KeyboardKey.Down)) input.Y += 1f;

            if (input.LengthSquared() > 0.01f)
            {
                input = Vector2.Normalize(input);
                current.Position += input * 310f * dt;
                current.Position = Mathf.Clamp(current.Position, 76, 118, Game.ScreenWidth - 76, Game.ScreenHeight - 76);
                if (Random.Shared.NextDouble() < 0.18)
                {
                    _particles.EmitSparkle(current.Position + new Vector2(0, -18), 1);
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                if (IsAccessoryOnUnicorn(current.Position))
                {
                    PlaceCurrentAccessory();
                }
                else if (_magicFeedbackTimer <= 0f)
                {
                    _magicFeedbackTimer = 0.9f;
                    _game.Audio.Play("sfx_button");
                    _particles.EmitStars(current.Position + new Vector2(0, -18), 6);
                    _floatingTexts.Spawn("放到獨角獸身上", current.Position + new Vector2(0, -70), new Color(95, 58, 128, 255), 28f, 1.1f);
                }
            }
        }
        else if (!_dressupThinking && !_complete)
        {
            _dressupThinking = true;
            _dressupThinkingTimer = 2.1f;
            _dressupHeartPulse = 0f;
            _game.Audio.Play("sfx_button");
            _floatingTexts.Spawn("獨角獸想一想...", new Vector2(704, 150), new Color(95, 58, 128, 255), 36f, 1.6f);
        }

        UpdateAmbientMagic(dt);
        _particles.Update(dt);
        _floatingTexts.Update(dt);
    }

    private void PlaceCurrentAccessory()
    {
        if (_currentAccessoryIndex >= _dressupAccessories.Length) return;

        var current = _dressupAccessories[_currentAccessoryIndex];
        current.Placed = true;
        _currentAccessoryIndex++;
        _game.Audio.Play("sfx_collect");
        _particles.EmitHearts(current.Position, 20);
        _particles.EmitStars(current.Position + new Vector2(0, -20), 20);
        _floatingTexts.Spawn($"{current.Name}好漂亮!", current.Position + new Vector2(0, -74), new Color(255, 108, 190, 255), 30f, 1.3f);
        _zoomPunch = MathF.Max(_zoomPunch, 0.34f);
    }

    private static bool IsAccessoryOnUnicorn(Vector2 position)
    {
        return position.X >= 430f && position.X <= 850f
            && position.Y >= 150f && position.Y <= 610f;
    }

    private void UpdateAmbientMagic(float dt)
    {
        _ambientSparkleTimer -= dt;
        if (_ambientSparkleTimer > 0f) return;

        _ambientSparkleTimer = _stage == 3 ? 0.11f : 0.14f;
        var x = 60f + (float)Random.Shared.NextDouble() * (Game.ScreenWidth - 120f);
        var y = 88f + (float)Random.Shared.NextDouble() * (Game.ScreenHeight - 190f);
        _particles.EmitSparkle(new Vector2(x, y), 1);
    }

    public void Draw()
    {
        if (_stage == 2)
        {
            DrawColorMatchLevel();
            return;
        }

        if (_stage == 3)
        {
            DrawDressupLevel();
            return;
        }

        Raylib.ClearBackground(_stage == 1 ? new Color(123, 205, 128, 255) : new Color(88, 154, 176, 255));

        var shake = Vector2.Zero;
        if (_globalShake > 0.01f)
        {
            var t = (float)Raylib.GetTime();
            shake = new Vector2(MathF.Sin(t * 53f) * _globalShake, MathF.Cos(t * 47f) * _globalShake);
        }
        var zoom = 1f + _zoomPunch * 0.06f;
        var center = new Vector2(Game.ScreenWidth / 2f, Game.ScreenHeight / 2f);
        Raylib.BeginMode2D(new Camera2D
        {
            Target = center,
            Offset = center + shake,
            Rotation = 0,
            Zoom = zoom,
        });

        DrawForest();
        DrawCastle();

        foreach (var food in _foods)
        {
            DrawFood(food);
        }

        DrawHouseEvent();
        DrawCompanions();
        DrawPrincess();

        _particles.Draw(_game.Assets);
        _floatingTexts.Draw(_game.Assets.GetFont());

        Raylib.EndMode2D();

        DrawVignette();

        if (_flashAmount > 0.01f)
        {
            var alpha = (int)(220 * Math.Clamp(_flashAmount, 0f, 1f));
            Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(255, 255, 255, alpha));
        }

        DrawHud();
    }

    private void DrawColorMatchLevel()
    {
        Raylib.ClearBackground(new Color(118, 205, 184, 255));
        var shake = CurrentShake();
        var zoom = 1f + _zoomPunch * 0.06f;
        var center = new Vector2(Game.ScreenWidth / 2f, Game.ScreenHeight / 2f);
        Raylib.BeginMode2D(new Camera2D
        {
            Target = center,
            Offset = center + shake,
            Rotation = 0,
            Zoom = zoom,
        });

        DrawColorMatchBackdrop();
        DrawMagicMatches();
        DrawPrincess();
        _particles.Draw(_game.Assets);
        _floatingTexts.Draw(_game.Assets.GetFont());

        Raylib.EndMode2D();
        DrawVignette();
        DrawFlash();
        DrawColorMatchHud();
    }

    private void DrawDressupLevel()
    {
        Raylib.ClearBackground(new Color(255, 231, 243, 255));
        var zoom = 1f + _zoomPunch * 0.05f;
        var center = new Vector2(Game.ScreenWidth / 2f, Game.ScreenHeight / 2f);
        Raylib.BeginMode2D(new Camera2D
        {
            Target = center,
            Offset = center + CurrentShake(),
            Rotation = 0,
            Zoom = zoom,
        });

        DrawDressupBackdrop();
        DrawDressupUnicorn();
        DrawDressupAccessories();
        DrawDressupThinking();
        _particles.Draw(_game.Assets);
        _floatingTexts.Draw(_game.Assets.GetFont());

        Raylib.EndMode2D();
        DrawVignette();
        DrawFlash();
        DrawDressupHud();
    }

    private Vector2 CurrentShake()
    {
        if (_globalShake <= 0.01f) return Vector2.Zero;

        var t = (float)Raylib.GetTime();
        return new Vector2(MathF.Sin(t * 53f) * _globalShake, MathF.Cos(t * 47f) * _globalShake);
    }

    private void DrawFlash()
    {
        if (_flashAmount <= 0.01f) return;

        var alpha = (int)(220 * Math.Clamp(_flashAmount, 0f, 1f));
        Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(255, 255, 255, alpha));
    }

    private void DrawColorMatchBackdrop()
    {
        for (var y = 0; y < Game.ScreenHeight; y += 8)
        {
            var t = y / (float)Game.ScreenHeight;
            Raylib.DrawRectangle(0, y, Game.ScreenWidth, 8, new Color((int)(118 + t * 38), (int)(205 + t * 28), (int)(184 + t * 38), 255));
        }

        Raylib.DrawCircleV(new Vector2(640, 342), 310, new Color(255, 255, 255, 32));
        Raylib.DrawCircleV(new Vector2(292, 228), 118, new Color(255, 134, 190, 34));
        Raylib.DrawCircleV(new Vector2(985, 242), 132, new Color(87, 181, 255, 34));
        Raylib.DrawCircleV(new Vector2(938, 552), 124, new Color(255, 220, 86, 38));

        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestFlowers, new Rectangle(158, 656, 210, 138), new Color(255, 255, 255, 220), -6f);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestBush, new Rectangle(1110, 642, 220, 148), new Color(255, 255, 255, 220), 5f);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.GardenArch, new Rectangle(642, 588, 130, 170), new Color(255, 255, 255, 155));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RoyalFountain, new Rectangle(642, 406, 164, 116), new Color(255, 255, 255, 180));

        for (var i = 0; i < 34; i++)
        {
            var x = 42 + i * 37;
            var y = 112 + (i * 53 % 490);
            var color = (i % 3) switch
            {
                0 => new Color(255, 132, 190, 165),
                1 => new Color(106, 190, 255, 165),
                _ => new Color(255, 221, 90, 165),
            };
            DrawFlower(new Vector2(x, y + MathF.Sin(_time * 1.5f + i) * 1.2f), color, 5f);
        }
    }

    private void DrawMagicMatches()
    {
        var held = _heldMagicIndex >= 0 ? _magicMatches[_heldMagicIndex] : null;
        foreach (var match in _magicMatches)
        {
            DrawMagicGate(match);
        }

        foreach (var match in _magicMatches)
        {
            if (!match.Collected && !match.Matched)
            {
                DrawMagicOrb(match, match.OrbPosition);
            }
        }

        if (held is not null && !held.Matched)
        {
            var hover = _princessPosition + new Vector2(0, -86 + MathF.Sin(_time * 5f) * 6f);
            DrawMagicOrb(held, hover, 0.78f);
            Raylib.DrawLineEx(_princessPosition + new Vector2(0, -42), hover + new Vector2(0, 34), 3f, new Color(255, 255, 255, 165));
        }
    }

    private void DrawMagicGate(MagicMatch match)
    {
        var pulse = (MathF.Sin(_time * 3.2f + match.Phase) + 1f) * 0.5f;
        var size = match.Matched ? 150f + pulse * 18f : 132f + pulse * 8f;
        Raylib.DrawCircleV(match.GatePosition + new Vector2(0, 16), size * 0.72f, new Color((int)match.Accent.R, (int)match.Accent.G, (int)match.Accent.B, match.Matched ? 62 : 34));
        DrawNamedTexture(match.GateTexture, new Rectangle(match.GatePosition.X, match.GatePosition.Y, 164, 210), match.Matched ? Color.White : new Color(255, 255, 255, 225));
        if (match.Matched)
        {
            Raylib.DrawCircleLines((int)match.GatePosition.X, (int)match.GatePosition.Y, (int)(92 + pulse * 18f), new Color(255, 255, 255, 180));
            Raylib.DrawTextEx(_game.Assets.GetFont(), "OK", match.GatePosition + new Vector2(-18, -126), 24, 1, match.Accent);
        }
    }

    private void DrawMagicOrb(MagicMatch match, Vector2 position, float scale = 1f)
    {
        var bob = MathF.Sin(_time * 4f + match.Phase) * 6f;
        var pulse = (MathF.Sin(_time * 5f + match.Phase) + 1f) * 0.5f;
        Raylib.DrawCircleV(position + new Vector2(0, bob), 48f * scale + pulse * 7f, new Color((int)match.Accent.R, (int)match.Accent.G, (int)match.Accent.B, 58));
        DrawNamedTexture(match.OrbTexture, new Rectangle(position.X, position.Y + bob, 78 * scale, 86 * scale), Color.White);
    }

    private void DrawDressupBackdrop()
    {
        Raylib.DrawRectangleGradientV(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(255, 233, 246, 255), new Color(212, 240, 255, 255));
        Raylib.DrawCircleV(new Vector2(704, 378), 260, new Color(255, 255, 255, 86));
        Raylib.DrawCircleV(new Vector2(704, 378), 184, new Color(255, 245, 170, 58));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestFlowers, new Rectangle(215, 652, 230, 148), new Color(255, 255, 255, 210), -4f);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestFlowers, new Rectangle(1095, 650, 230, 148), new Color(255, 255, 255, 210), 5f);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RoyalFountain, new Rectangle(1022, 290, 150, 106), new Color(255, 255, 255, 160));

        for (var i = 0; i < 36; i++)
        {
            var angle = i * MathF.Tau / 36f + _time * 0.12f;
            var radius = 230f + MathF.Sin(_time * 1.8f + i) * 10f;
            var p = new Vector2(704, 374) + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Raylib.DrawCircleV(p, 4f + i % 3, new Color(255, 155, 206, 125));
        }
    }

    private void DrawDressupUnicorn()
    {
        Raylib.DrawEllipse(650, 616, 168, 26, new Color(0, 0, 0, 52));
        DrawNamedTexture("dressup_unicorn", new Rectangle(640, 382, 430, 430), Color.White);
    }

    private void DrawDressupAccessories()
    {
        for (var i = 0; i < _dressupAccessories.Length; i++)
        {
            var accessory = _dressupAccessories[i];
            if (!accessory.Placed) continue;
            DrawAccessory(accessory, accessory.Position, 1f, 255);
        }

        if (_currentAccessoryIndex >= _dressupAccessories.Length || _dressupThinking || _complete) return;

        var current = _dressupAccessories[_currentAccessoryIndex];
        var pulse = (MathF.Sin(_time * 5f) + 1f) * 0.5f;
        if (IsAccessoryOnUnicorn(current.Position))
        {
            Raylib.DrawCircleV(current.Position, 58f + pulse * 10f, new Color(255, 255, 255, 78));
            Raylib.DrawCircleLines((int)current.Position.X, (int)current.Position.Y, (int)(66f + pulse * 10f), new Color(255, 128, 190, 165));
        }
        DrawAccessory(current, current.Position, 1f + pulse * 0.05f, 255);
    }

    private void DrawAccessory(DressupAccessory accessory, Vector2 position, float scale, int alpha)
    {
        DrawNamedTexture(
            accessory.Texture,
            new Rectangle(position.X, position.Y, accessory.Size.X * scale, accessory.Size.Y * scale),
            new Color(255, 255, 255, alpha),
            accessory.Rotation);
    }

    private void DrawDressupThinking()
    {
        if (!_dressupThinking && !_complete) return;

        var font = _game.Assets.GetFont();
        var head = new Vector2(732, 190);
        if (_dressupThinking)
        {
            var pulse = (MathF.Sin(_time * 4f) + 1f) * 0.5f;
            Raylib.DrawCircleV(head + new Vector2(46, -40), 24 + pulse * 4f, new Color(255, 255, 255, 230));
            Raylib.DrawCircleV(head + new Vector2(20, -16), 12, new Color(255, 255, 255, 210));
            Raylib.DrawCircleV(head + new Vector2(2, 4), 7, new Color(255, 255, 255, 190));
            Raylib.DrawTextEx(font, "嗯...", head + new Vector2(22, -56), 24, 1, new Color(95, 58, 128, 255));
            return;
        }

        var heartPulse = (MathF.Sin(_completeTimer * 8f) + 1f) * 0.5f;
        DrawHeart(head + new Vector2(44, -42), 34f + heartPulse * 7f, new Color(255, 90, 160, 230));
        Raylib.DrawTextEx(font, "喜歡!", head + new Vector2(2, -92), 32, 1, new Color(255, 90, 160, 255));
    }

    private void DrawColorMatchHud()
    {
        var font = _game.Assets.GetFont();
        DrawPill(new Rectangle(24, 22, 284, 46), $"配對 {_magicMatches.Count(m => m.Matched)} / {_magicMatches.Length}", new Color(255, 108, 190, 255));
        DrawPill(new Rectangle(548, 22, 184, 46), CurrentStageLabel, new Color(186, 138, 255, 255));
        var heldText = _heldMagicIndex >= 0 ? $"拿著 {_magicMatches[_heldMagicIndex].Name}" : "找一顆魔法球";
        DrawPill(new Rectangle(Game.ScreenWidth - 304, 22, 280, 46), heldText, _heldMagicIndex >= 0 ? _magicMatches[_heldMagicIndex].Accent : new Color(95, 174, 110, 255));

        var tip = _complete ? "顏色配對完成! 下一關裝扮獨角獸" : "方向鍵移動，把魔法球送到同顏色門";
        var measured = Raylib.MeasureTextEx(font, tip, 23, 1);
        Raylib.DrawTextEx(font, tip, new Vector2((Game.ScreenWidth - measured.X) / 2f, Game.ScreenHeight - 44), 23, 1, new Color(60, 85, 100, 230));
    }

    private void DrawDressupHud()
    {
        var font = _game.Assets.GetFont();
        DrawPill(new Rectangle(24, 22, 284, 46), $"裝飾 {_dressupAccessories.Count(a => a.Placed)} / {_dressupAccessories.Length}", new Color(255, 108, 190, 255));
        DrawPill(new Rectangle(548, 22, 184, 46), CurrentStageLabel, new Color(186, 138, 255, 255));
        var currentText = _currentAccessoryIndex < _dressupAccessories.Length ? _dressupAccessories[_currentAccessoryIndex].Name : "完成";
        DrawPill(new Rectangle(Game.ScreenWidth - 304, 22, 280, 46), currentText, new Color(95, 174, 220, 255));

        var tip = _complete
            ? "獨角獸很喜歡! 按任意鍵回選單"
            : _dressupThinking
                ? "獨角獸正在想一想"
                : "方向鍵移動飾品，放到獨角獸身上按空白";
        var measured = Raylib.MeasureTextEx(font, tip, 23, 1);
        Raylib.DrawTextEx(font, tip, new Vector2((Game.ScreenWidth - measured.X) / 2f, Game.ScreenHeight - 44), 23, 1, new Color(80, 70, 120, 230));
    }

    private void MovePrincess(float dt)
    {
        var input = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.Left)) input.X -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Right)) input.X += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Up)) input.Y -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Down)) input.Y += 1f;

        var maxSpeed = _powered ? PoweredMaxSpeed : NormalMaxSpeed;
        var accel = _powered ? PoweredAccel : NormalAccel;

        if (input.LengthSquared() > 0.01f)
        {
            input = Vector2.Normalize(input);
            _princessVelocity += input * accel * dt;
            var len = _princessVelocity.Length();
            if (len > maxSpeed) _princessVelocity = _princessVelocity / len * maxSpeed;
            if (Math.Abs(input.X) > 0.05f) _princessFacing = MathF.Sign(input.X);
            _idleTimer = 0f;
        }
        else
        {
            var decay = MathF.Exp(-Friction * dt);
            _princessVelocity *= decay;
            if (_princessVelocity.LengthSquared() < 4f) _princessVelocity = Vector2.Zero;
            _idleTimer += dt;
        }

        _princessPosition += _princessVelocity * dt;
        _princessPosition = Mathf.Clamp(_princessPosition, 62, 132, Game.ScreenWidth - 62, Game.ScreenHeight - 64);

        var speed = _princessVelocity.Length();
        if (speed > 30f)
        {
            _walkTime += dt * Math.Max(0.5f, speed / 200f);
            _dustTimer += dt;
            if (_dustTimer > 0.08f && speed > 90f)
            {
                _dustTimer = 0f;
                var back = speed > 0.01f ? -_princessVelocity / speed : Vector2.Zero;
                _particles.EmitDust(_princessPosition + back * 14f + new Vector2(0, 38), _powered ? 3 : 2);
            }
        }
    }

    private void UpdateFood()
    {
        foreach (var food in _foods)
        {
            if (food.Eaten || Vector2.Distance(_princessPosition, food.Position) > 48f) continue;

            food.Eaten = true;
            _game.Audio.Play("sfx_collect");
            _particles.EmitStars(food.Position, 14);
            _particles.EmitHearts(food.Position, 5);
            _floatingTexts.Spawn("好吃!", food.Position + new Vector2(0, -34), new Color(255, 108, 168, 255), 28f);
            _zoomPunch = MathF.Max(_zoomPunch, 0.3f);
        }

        if (!_powered && FoodCount == _foods.Length)
        {
            _powered = true;
            _game.Audio.Play("sfx_rescue_complete");
            _particles.EmitRainbow(_princessPosition + new Vector2(0, -50), 36);
            _particles.EmitStars(_princessPosition + new Vector2(0, -56), 26);
            _particles.EmitHearts(_princessPosition + new Vector2(0, -48), 18);
            _floatingTexts.Spawn("公主有力氣了!", _princessPosition + new Vector2(0, -78), new Color(255, 90, 160, 255), 36f, 1.8f);
            _zoomPunch = MathF.Max(_zoomPunch, 0.7f);
            _flashAmount = 0.55f;
            _slowMoTimer = 0.3f;
            _globalShake = 6f;
        }
    }

    private void UpdateCompanions(float dt)
    {
        var followingSlot = 0;
        var trailTarget = _princessPosition;
        var moving = _princessVelocity.LengthSquared() > 36f;
        var trailDirection = moving
            ? Vector2.Normalize(_princessVelocity)
            : new Vector2(-_princessFacing, 0);

        foreach (var companion in _companions)
        {
            if (!companion.Following && Vector2.Distance(_princessPosition, companion.Position) <= 58f)
            {
                companion.Following = true;
                companion.Velocity = Vector2.Zero;
                _game.Audio.Play("sfx_collect");
                _particles.EmitHearts(companion.Position + new Vector2(0, -34), 14);
                _particles.EmitStars(companion.Position + new Vector2(0, -48), 10);
                _floatingTexts.Spawn("公主加入隊伍!", companion.Position + new Vector2(0, -72), new Color(255, 90, 160, 255), 30f, 1.4f);
                _zoomPunch = MathF.Max(_zoomPunch, 0.34f);
            }

            if (!companion.Following) continue;

            var side = new Vector2(-trailDirection.Y, trailDirection.X);
            var sideOffset = side * (followingSlot % 2 == 0 ? -18f : 18f);
            var target = trailTarget - trailDirection * (58f + followingSlot * 7f) + sideOffset;
            var toTarget = target - companion.Position;
            companion.Velocity += toTarget * (8f + followingSlot * 0.8f) * dt;
            companion.Velocity *= MathF.Exp(-6.2f * dt);
            var maxSpeed = 270f + followingSlot * 18f;
            var speed = companion.Velocity.Length();
            if (speed > maxSpeed) companion.Velocity = companion.Velocity / speed * maxSpeed;
            companion.Position += companion.Velocity * dt;
            companion.Position = Mathf.Clamp(companion.Position, 56, 120, Game.ScreenWidth - 56, Game.ScreenHeight - 58);
            trailTarget = companion.Position;
            followingSlot++;
        }
    }

    private void HitHouse()
    {
        _houseHits++;
        _hitShake = 0.45f;
        _smashTimer = 0.32f;
        _game.Audio.Play("sfx_rescue_hit");
        _particles.EmitFirework(_housePosition + new Vector2(-8, 18), 8);
        _particles.EmitStars(_housePosition + new Vector2(0, -36), 5);
        _zoomPunch = MathF.Max(_zoomPunch, 0.4f);
        _globalShake = MathF.Max(_globalShake, 4f);

        if (_houseHits >= CurrentRequiredHits)
        {
            _complete = true;
            _completeTimer = 0f;
            _game.Audio.Play("sfx_rescue_complete");
            _particles.EmitFirework(_housePosition, 60);
            _particles.EmitHearts(_housePosition + new Vector2(0, -30), 50);
            _particles.EmitStars(_housePosition, 40);
            _particles.EmitConfetti(_housePosition + new Vector2(0, -40), 36);
            _floatingTexts.Spawn($"救出{CurrentRescueName}!", _housePosition + new Vector2(0, -150), new Color(255, 90, 160, 255), 42f, 2.0f);
            _zoomPunch = 1f;
            _slowMoTimer = 0.5f;
            _globalShake = 14f;
            _flashAmount = 0.9f;
        }
    }

    private int FoodCount => _foods.Count(f => f.Eaten);

    private void DrawForest()
    {
        var grass = _game.Assets.GetTexture("grass_tile");
        for (var y = 0; y < Game.ScreenHeight; y += 64)
        {
            for (var x = 0; x < Game.ScreenWidth; x += 64)
            {
                var tint = _stage == 1 ? new Color(230, 255, 226, 255) : new Color(190, 232, 218, 255);
                Raylib.DrawTexturePro(grass, new Rectangle(0, 0, grass.Width, grass.Height), new Rectangle(x, y, 64, 64), Vector2.Zero, 0, tint);
            }
        }

        DrawWindRipple();
        if (_stage == 2)
        {
            DrawMoonGardenGlow();
        }

        Raylib.DrawCircleV(new Vector2(640, 312), 255, new Color(255, 255, 255, 30));
        var pathColor = _stage == 1 ? new Color(214, 187, 128, 255) : new Color(185, 174, 142, 255);
        Raylib.DrawRectangleRounded(new Rectangle(584, 190, 112, 420), 0.35f, 18, pathColor);
        Raylib.DrawRectangleRounded(new Rectangle(600, 534, 360, 82), 0.35f, 18, pathColor);
        Raylib.DrawRectangleRoundedLines(new Rectangle(584, 190, 112, 420), 0.35f, 18, new Color(182, 151, 95, 210));
        Raylib.DrawRectangleRoundedLines(new Rectangle(600, 534, 360, 82), 0.35f, 18, new Color(182, 151, 95, 210));

        for (var i = 0; i < 34; i++)
        {
            var x = 38 + i * 38;
            var y = 125 + (i * 47 % 470);
            var color = i % 3 == 0 ? new Color(255, 226, 92, 210) : new Color(255, 143, 190, 190);
            var bob = MathF.Sin(_time * 1.6f + i * 0.4f) * 0.8f;
            DrawFlower(new Vector2(x, y + bob), color, 4.5f + i % 3);
        }

        for (var i = 0; i < 18; i++)
        {
            var x = 58 + i * 72;
            DrawTree(new Vector2(x, 84 + (i % 3) * 18), 0.58f + (i % 4) * 0.06f);
            if (i % 2 == 0) DrawTree(new Vector2(x + 18, 678 - (i % 4) * 18), 0.54f);
        }

        DrawTree(new Vector2(72, 372), 0.82f);
        DrawTree(new Vector2(1182, 174), 0.78f);
        DrawTree(new Vector2(1124, 640), 0.66f);
        DrawTree(new Vector2(930, 94), 0.6f);
        DrawForestDecorations();
    }

    private void DrawWindRipple()
    {
        for (var i = 0; i < 2; i++)
        {
            var phase = (_time * 55f + i * 380f) % (Game.ScreenWidth + 320f) - 160f;
            for (var k = -2; k <= 2; k++)
            {
                var alpha = (int)(24 - Math.Abs(k) * 7);
                if (alpha <= 0) continue;
                Raylib.DrawRectangle((int)(phase + k * 12) - 8, 0, 80, Game.ScreenHeight, new Color(255, 255, 255, alpha));
            }
        }
    }

    private void DrawMoonGardenGlow()
    {
        Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(52, 86, 150, 38));
        Raylib.DrawCircleV(new Vector2(1108, 88), 58, new Color(255, 245, 194, 160));
        Raylib.DrawCircleV(new Vector2(1090, 72), 44, new Color(88, 154, 176, 255));
        for (var i = 0; i < 46; i++)
        {
            var x = 60 + (i * 83 % 1160);
            var y = 48 + (i * 37 % 210);
            var twinkle = (MathF.Sin(_time * 2.3f + i * 0.71f) + 1f) * 0.5f;
            Raylib.DrawCircle(x, y, 2f + twinkle * 1.4f, new Color(255, 246, 190, (int)(120 + twinkle * 95)));
        }
        Raylib.DrawCircleV(_housePosition + new Vector2(0, 18), 182, new Color(255, 146, 202, 34));
    }

    private void DrawForestDecorations()
    {
        foreach (var decoration in _decorations)
        {
            var bob = MathF.Sin(_time * 1.4f + decoration.Phase) * 2.5f;
            var rect = new Rectangle(decoration.Position.X, decoration.Position.Y + bob, decoration.Size.X, decoration.Size.Y);
            GeneratedSprites.TryDraw(_game.Assets, decoration.Sprite, rect, new Color(255, 255, 255, (int)decoration.Alpha), decoration.Rotation);
        }
    }

    private void DrawCompanions()
    {
        foreach (var companion in _companions)
        {
            var bob = MathF.Sin(_time * (companion.Following ? 5.4f : 3.1f) + companion.Phase) * (companion.Following ? 3.6f : 4.6f);
            var size = companion.Following ? 86f : 96f;
            var glow = companion.Following ? 0.7f : (MathF.Sin(_time * 3f + companion.Phase) + 1f) * 0.5f;
            var drawPosition = companion.Position + new Vector2(0, bob);

            Raylib.DrawEllipse((int)drawPosition.X, (int)(drawPosition.Y + size * 0.42f), (int)(size * 0.28f), 7, new Color(0, 0, 0, companion.Following ? 56 : 46));
            Raylib.DrawCircleV(drawPosition + new Vector2(0, -12), 44f + glow * 6f, new Color(255, 238, 170, companion.Following ? 38 : 52));
            if (!companion.Following)
            {
                Raylib.DrawCircleLines((int)drawPosition.X, (int)(drawPosition.Y - 12), (int)(50f + glow * 6f), new Color(255, 255, 255, 120));
            }

            GeneratedSprites.TryDraw(_game.Assets, companion.Sprite, new Rectangle(drawPosition.X, drawPosition.Y, size, size * 1.12f), Color.White, MathF.Sin(_time * 2.5f + companion.Phase) * 2f);
        }
    }

    private static void DrawFlower(Vector2 center, Color color, float size)
    {
        for (var i = 0; i < 5; i++)
        {
            var angle = i * MathF.Tau / 5f;
            Raylib.DrawCircleV(center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * size, size * 0.58f, color);
        }
        Raylib.DrawCircleV(center, size * 0.5f, new Color(255, 245, 150, 235));
    }

    private void DrawTree(Vector2 position, float scale)
    {
        var tree = _game.Assets.GetTexture("tree");
        var sway = MathF.Sin(_time * 1.1f + position.X * 0.014f) * 1.6f;
        Raylib.DrawTexturePro(tree, new Rectangle(0, 0, tree.Width, tree.Height), new Rectangle(position.X, position.Y, 96 * scale, 110 * scale), new Vector2(48 * scale, 86 * scale), sway, Color.White);
    }

    private void DrawCastle()
    {
        Raylib.DrawEllipse((int)_castlePosition.X, (int)(_castlePosition.Y + 168), 126, 16, new Color(0, 0, 0, 50));
        Raylib.DrawCircleV(_castlePosition + new Vector2(0, 20), 142, new Color(255, 236, 178, 38));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.CastleRoyal, new Rectangle(_castlePosition.X, _castlePosition.Y + 50, 282, 244), Color.White);
        var princeBob = MathF.Sin(_time * 4f) * 4f;
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrinceWindow, new Rectangle(_castlePosition.X + 6, _castlePosition.Y - 92 + princeBob, 118, 118), Color.White, MathF.Sin(_time * 5f) * 2.5f);

        var font = _game.Assets.GetFont();
        var text = _stage == 1 ? "王子在揮手" : "玫瑰花園發光了";
        DrawSpeech(font, text, _castlePosition + new Vector2(-96, -168), _stage == 1 ? 156 : 196);
    }

    private void DrawFood(Food food)
    {
        if (food.Eaten) return;

        var bob = MathF.Sin(_time * 3f + food.Position.X * 0.02f) * 5f;
        var glow = (MathF.Sin(_time * 5f + food.Position.Y) + 1f) * 0.5f;
        var distance = Vector2.Distance(_princessPosition, food.Position);
        var attract = distance < 180f ? 1f - distance / 180f : 0f;

        Raylib.DrawCircleV(food.Position + new Vector2(0, bob), 34f + glow * 5f + attract * 8f, new Color(255, 255, 210, (int)(65 + attract * 60)));
        if (attract > 0.1f)
        {
            Raylib.DrawCircleLines((int)food.Position.X, (int)(food.Position.Y + bob), (int)(38f + glow * 5f + attract * 12f), new Color(255, 255, 210, (int)(120 * attract)));
        }
        GeneratedSprites.TryDraw(_game.Assets, food.Sprite, new Rectangle(food.Position.X, food.Position.Y + bob, 74, 74), Color.White);
    }

    private void DrawHouseEvent()
    {
        var progress = Math.Clamp(_houseHits / (float)CurrentRequiredHits, 0f, 1f);
        var shakeX = _hitShake > 0f ? MathF.Sin(_time * 68f) * _hitShake * 18f : 0f;
        var shakeY = _hitShake > 0f ? MathF.Cos(_time * 60f) * _hitShake * 7f : 0f;
        var houseCenter = _housePosition + new Vector2(shakeX, shakeY);
        var impact = Math.Clamp(_smashTimer / 0.32f, 0f, 1f);
        var impactPulse = MathF.Sin(impact * MathF.PI);

        if (_stage == 2)
        {
            DrawMagicGardenGate(progress, houseCenter, impactPulse, shakeX);
            return;
        }

        var stage = progress switch
        {
            >= 0.66f => RescueSprite.CottageBroken,
            >= 0.34f => RescueSprite.CottageDamaged,
            _ => RescueSprite.Cottage,
        };
        Raylib.DrawEllipse((int)_housePosition.X, (int)(_housePosition.Y + 118), 142, 17, new Color(0, 0, 0, 55));
        GeneratedSprites.TryDraw(_game.Assets, stage, new Rectangle(houseCenter.X, houseCenter.Y - impactPulse * 5f, 258 + impactPulse * 18f, 258 + impactPulse * 12f), Color.White, shakeX * 0.08f + impactPulse * 2.5f);
        DrawHouseDamageEffects(progress, impactPulse);

        if (_complete)
        {
            var girlBob = MathF.Sin(_time * 5f) * 7f;
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessFree, new Rectangle(_housePosition.X + 154, _housePosition.Y + 82 + girlBob, 128, 128), Color.White);
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessFriends, new Rectangle(_housePosition.X - 18, _housePosition.Y + 136 + girlBob * 0.4f, 126, 106), Color.White);
            GeneratedSprites.TryDrawFinal(_game.Assets, FinalSprite.WitchDefeated, new Rectangle(_housePosition.X - 150, _housePosition.Y + 92, 128, 128), Color.White, MathF.Sin(_time * 6f) * 5f);
        }
        else
        {
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Witch, new Rectangle(_housePosition.X + 114, _housePosition.Y + 54, 118, 118), Color.White, MathF.Sin(_time * 4f) * 3f);
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessCaged, new Rectangle(_housePosition.X - 128, _housePosition.Y + 70, 126, 126), Color.White);
        }

        if (_powered && !_complete && Vector2.Distance(_princessPosition, _housePosition) <= 180f)
        {
            DrawHouseProgress();
        }
    }

    private void DrawMagicGardenGate(float progress, Vector2 gateCenter, float impactPulse, float shakeX)
    {
        Raylib.DrawEllipse((int)_housePosition.X, (int)(_housePosition.Y + 116), 152, 18, new Color(0, 0, 0, 55));
        Raylib.DrawCircleV(_housePosition + new Vector2(0, 8), 150 + impactPulse * 16f, new Color(255, 146, 202, 45));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.GardenArch, new Rectangle(gateCenter.X - 58, gateCenter.Y + 2 - impactPulse * 5f, 152 + impactPulse * 12f, 210 + impactPulse * 12f), Color.White, shakeX * 0.06f);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.CastleGate, new Rectangle(gateCenter.X + 34, gateCenter.Y + 52 - impactPulse * 4f, 238 + impactPulse * 14f, 214 + impactPulse * 10f), Color.White, shakeX * 0.04f);

        if (!_complete)
        {
            DrawMagicBarrier(progress, impactPulse);
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Witch, new Rectangle(_housePosition.X + 124, _housePosition.Y + 36, 116, 116), Color.White, MathF.Sin(_time * 4f) * 3f);
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessCaged, new Rectangle(_housePosition.X - 108, _housePosition.Y + 62, 116, 116), Color.White);
        }
        else
        {
            var bob = MathF.Sin(_time * 5f) * 7f;
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessRose, new Rectangle(_housePosition.X - 100, _housePosition.Y + 48 + bob, 126, 142), Color.White);
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessFriends, new Rectangle(_housePosition.X + 46, _housePosition.Y + 132 + bob * 0.4f, 136, 116), Color.White);
            GeneratedSprites.TryDrawFinal(_game.Assets, FinalSprite.WitchDefeated, new Rectangle(_housePosition.X + 154, _housePosition.Y + 96, 122, 122), Color.White, MathF.Sin(_time * 6f) * 5f);
        }

        if (_powered && !_complete && Vector2.Distance(_princessPosition, _housePosition) <= 180f)
        {
            DrawHouseProgress();
        }
    }

    private void DrawMagicBarrier(float progress, float impactPulse)
    {
        var alpha = (int)(160 * (1f - progress * 0.58f));
        var barrierColor = new Color(180, 220, 255, Math.Clamp(alpha, 45, 160));
        Raylib.DrawCircleV(_housePosition + new Vector2(-44, 36), 58 + impactPulse * 18f, barrierColor);
        Raylib.DrawCircleLines((int)(_housePosition.X - 44), (int)(_housePosition.Y + 36), (int)(70 + impactPulse * 30f), new Color(255, 255, 255, 160));

        var crackCount = Math.Clamp((int)(progress * 9f) + (_smashTimer > 0 ? 2 : 0), 0, 11);
        for (var i = 0; i < crackCount; i++)
        {
            var start = _housePosition + new Vector2(-96 + i * 11, -8 + (i % 4) * 17);
            var mid = start + new Vector2(9 + (i % 2) * 7, 15);
            var end = mid + new Vector2(-6 + (i % 3) * 7, 18);
            Raylib.DrawLineEx(start, mid, 2.6f, new Color(255, 255, 255, 220));
            Raylib.DrawLineEx(mid, end, 2.6f, new Color(130, 194, 255, 220));
        }

        if (impactPulse <= 0.01f) return;

        Raylib.DrawLineEx(_princessPosition + new Vector2(22, -20), _housePosition + new Vector2(-82, 16), 6f + impactPulse * 5f, new Color(255, 219, 84, (int)(190 * impactPulse)));
        for (var i = 0; i < 14; i++)
        {
            var angle = i * MathF.Tau / 14f + _time;
            var p = _housePosition + new Vector2(MathF.Cos(angle) * (86f + impactPulse * 42f), MathF.Sin(angle) * (62f + impactPulse * 28f));
            Raylib.DrawCircleV(p, 5f + i % 3, new Color(255, 188, 226, (int)(230 * impactPulse)));
        }
    }

    private static void DrawTinyBars(Vector2 center)
    {
        Raylib.DrawRectangleRounded(new Rectangle(center.X - 42, center.Y - 42, 84, 84), 0.18f, 8, new Color(190, 205, 215, 75));
        for (var i = -2; i <= 2; i++)
        {
            Raylib.DrawLineEx(center + new Vector2(i * 16, -42), center + new Vector2(i * 16, 42), 3f, new Color(92, 98, 115, 185));
        }
    }

    private void DrawHouseDamageEffects(float progress, float impactPulse)
    {
        if (progress <= 0.01f && impactPulse <= 0.01f) return;

        var crackColor = new Color(95, 66, 74, 210);
        var crackCount = Math.Clamp((int)(progress * 8f) + (_smashTimer > 0 ? 2 : 0), 0, 9);
        for (var i = 0; i < crackCount; i++)
        {
            var start = _housePosition + new Vector2(-66 + i * 17, -26 + (i % 3) * 20);
            var mid = start + new Vector2(10 + (i % 2) * 8, 18);
            var end = mid + new Vector2(-7 + (i % 3) * 8, 18);
            Raylib.DrawLineEx(start, mid, 2.4f, crackColor);
            Raylib.DrawLineEx(mid, end, 2.4f, crackColor);
        }

        if (impactPulse <= 0.01f) return;

        for (var i = 0; i < 12; i++)
        {
            var angle = -MathF.PI * 0.85f + i * MathF.PI * 1.7f / 11f;
            var distance = 56f + impactPulse * (24f + i % 4 * 8f);
            var center = _housePosition + new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance * 0.7f);
            var size = 5f + (i % 3) * 2f;
            Raylib.DrawRectanglePro(new Rectangle(center.X, center.Y, size * 1.7f, size), new Vector2(size * 0.85f, size * 0.5f), angle * 57.2958f, new Color(158, 116, 82, (int)(230 * impactPulse)));
        }

        Raylib.DrawCircleLines((int)_housePosition.X, (int)(_housePosition.Y + 6), (int)(72 + impactPulse * 52f), new Color(255, 240, 150, (int)(180 * impactPulse)));
        Raylib.DrawLineEx(_princessPosition + new Vector2(22, -20), _housePosition + new Vector2(-82, 16), 6f + impactPulse * 5f, new Color(255, 219, 84, (int)(190 * impactPulse)));
    }

    private void DrawPrincess()
    {
        var speed = _princessVelocity.Length();
        var moving = speed > 30f;
        var maxSpeed = _powered ? PoweredMaxSpeed : NormalMaxSpeed;
        var speedT = Math.Clamp(speed / maxSpeed, 0f, 1f);

        var bob = moving
            ? MathF.Sin(_walkTime * 12f) * (4f + speedT * 4f)
            : MathF.Sin(_time * 2.4f) * 2.4f;
        var squashAmt = moving ? MathF.Sin(_walkTime * 12f) * (0.04f + speedT * 0.06f) : 0f;
        var baseScale = _powered ? 1.12f + MathF.Sin(_time * 7f) * 0.02f : 1f;

        var lean = MathF.Sign(_princessVelocity.X) * speedT * 5f;
        var drawPos = _princessPosition;
        if (_smashTimer > 0f && Vector2.Distance(_princessPosition, _housePosition) <= 185f)
        {
            var direction = _housePosition - _princessPosition;
            if (direction.LengthSquared() > 0.01f)
            {
                direction = Vector2.Normalize(direction);
                var t = Math.Clamp(_smashTimer / 0.32f, 0f, 1f);
                drawPos += direction * MathF.Sin(t * MathF.PI) * 28f;
            }
        }

        Raylib.DrawEllipse((int)drawPos.X, (int)(drawPos.Y + 48), (int)(34 * baseScale * (1f + speedT * 0.25f)), 8, new Color(0, 0, 0, 70));

        if (_powered)
        {
            var glowPulse = (MathF.Sin(_time * 6f) + 1f) * 0.5f;
            Raylib.DrawCircleV(drawPos + new Vector2(0, -12 + bob), 72f + glowPulse * 7f, new Color(255, 238, 120, 60));
            Raylib.DrawCircleLines((int)drawPos.X, (int)(drawPos.Y - 12 + bob), (int)(82f + glowPulse * 7f), new Color(255, 255, 255, 115));

            if (moving && Random.Shared.NextDouble() < 0.4)
            {
                _particles.EmitSparkle(drawPos + new Vector2((float)Random.Shared.NextDouble() * 30 - 15, (float)Random.Shared.NextDouble() * 30 - 30), 1);
            }
        }

        if (!moving && _idleTimer > 1.5f && Random.Shared.NextDouble() < 0.05)
        {
            _particles.EmitStars(drawPos + new Vector2(0, -56), 1);
        }

        var rotation = lean + (_smashTimer > 0f ? MathF.Sin(_time * 34f) * 3.5f : 0f);
        var princess = _game.Assets.FindTexture("sprite_princess");
        if (princess.HasValue)
        {
            var w = 112f * baseScale * (1f + squashAmt);
            var h = 130f * baseScale * (1f - squashAmt);
            var flip = _princessFacing < 0;
            var scale = MathF.Min(w / princess.Value.Width, h / princess.Value.Height);
            var fw = princess.Value.Width * scale;
            var fh = princess.Value.Height * scale;
            // Negative source width flips horizontally in raylib
            var src = new Rectangle(0, 0, princess.Value.Width * (flip ? -1f : 1f), princess.Value.Height);
            var dest = new Rectangle(drawPos.X, drawPos.Y + bob, fw, fh);
            Raylib.DrawTexturePro(princess.Value, src, dest, new Vector2(fw / 2f, fh / 2f), rotation, Color.White);
        }
        else
        {
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Princess, new Rectangle(drawPos.X, drawPos.Y + bob, 112 * baseScale, 130 * baseScale), Color.White, rotation);
        }
    }

    private void DrawHouseProgress()
    {
        var font = _game.Assets.GetFont();
        var ratio = Math.Clamp(_houseHits / (float)CurrentRequiredHits, 0f, 1f);
        var rect = new Rectangle(_housePosition.X - 88, _housePosition.Y - 154, 176, 20);
        Raylib.DrawRectangleRounded(rect, 0.45f, 10, new Color(255, 255, 255, 228));
        Raylib.DrawRectangleRounded(new Rectangle(rect.X + 4, rect.Y + 4, (rect.Width - 8) * ratio, rect.Height - 8), 0.45f, 10, new Color(255, 119, 168, 255));
        Raylib.DrawRectangleRoundedLines(rect, 0.45f, 10, Color.DarkPurple);
        var text = $"{_houseHits}/{CurrentRequiredHits}";
        var measured = Raylib.MeasureTextEx(font, text, 20, 1);
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + (rect.Width - measured.X) / 2f, rect.Y - 25), 20, 1, Color.DarkPurple);
    }

    private void DrawHud()
    {
        var font = _game.Assets.GetFont();
        DrawPill(new Rectangle(24, 22, 240, 46), $"食物 {FoodCount} / {_foods.Length}", new Color(255, 152, 75, 255));
        DrawPill(new Rectangle(548, 22, 184, 46), CurrentStageLabel, new Color(186, 138, 255, 255));
        DrawPill(new Rectangle(Game.ScreenWidth - 290, 22, 266, 46), _powered ? "力氣滿了!" : "還需要更多食物", _powered ? new Color(255, 105, 180, 255) : new Color(95, 174, 110, 255));

        var tip = _complete
            ? (HasNextStage ? $"{CurrentRescueName}成功! 下一關玫瑰花園" : $"{CurrentRescueName}成功! 按任意鍵回選單")
            : _powered
                ? $"靠近{CurrentRescuePlace}連點空白鍵救{CurrentRescueName}"
                : "方向鍵移動公主，尋找水果跟青菜";
        var measured = Raylib.MeasureTextEx(font, tip, 23, 1);
        Raylib.DrawTextEx(font, tip, new Vector2((Game.ScreenWidth - measured.X) / 2f, Game.ScreenHeight - 44), 23, 1, new Color(60, 85, 100, 230));
    }

    private void DrawPill(Rectangle rect, string text, Color accent)
    {
        var font = _game.Assets.GetFont();
        Raylib.DrawRectangleRounded(rect, 0.55f, 12, new Color(255, 255, 255, 230));
        Raylib.DrawCircle((int)(rect.X + 25), (int)(rect.Y + 23), 13, accent);
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + 48, rect.Y + 11), 23, 1, new Color(65, 61, 93, 255));
    }

    private bool DrawNamedTexture(string textureName, Rectangle box, Color tint, float rotation = 0f)
    {
        var texture = _game.Assets.FindTexture(textureName);
        if (!texture.HasValue) return false;

        var scale = MathF.Min(box.Width / texture.Value.Width, box.Height / texture.Value.Height);
        var width = texture.Value.Width * scale;
        var height = texture.Value.Height * scale;
        Raylib.DrawTexturePro(
            texture.Value,
            new Rectangle(0, 0, texture.Value.Width, texture.Value.Height),
            new Rectangle(box.X, box.Y, width, height),
            new Vector2(width / 2f, height / 2f),
            rotation,
            tint);
        return true;
    }

    private static void DrawHeart(Vector2 center, float size, Color color)
    {
        var left = center + new Vector2(-size * 0.28f, -size * 0.18f);
        var right = center + new Vector2(size * 0.28f, -size * 0.18f);
        Raylib.DrawCircleV(left, size * 0.34f, color);
        Raylib.DrawCircleV(right, size * 0.34f, color);
        Raylib.DrawTriangle(
            center + new Vector2(-size * 0.66f, -size * 0.04f),
            center + new Vector2(size * 0.66f, -size * 0.04f),
            center + new Vector2(0, size * 0.74f),
            color);
    }

    private void DrawVignette()
    {
        const int edge = 90;
        Raylib.DrawRectangleGradientV(0, 0, Game.ScreenWidth, edge, new Color(0, 0, 0, 70), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientV(0, Game.ScreenHeight - edge, Game.ScreenWidth, edge, new Color(0, 0, 0, 0), new Color(0, 0, 0, 80));
        Raylib.DrawRectangleGradientH(0, 0, edge, Game.ScreenHeight, new Color(0, 0, 0, 60), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientH(Game.ScreenWidth - edge, 0, edge, Game.ScreenHeight, new Color(0, 0, 0, 0), new Color(0, 0, 0, 60));
    }

    private static void DrawSpeech(Font font, string text, Vector2 position, float width)
    {
        var rect = new Rectangle(position.X, position.Y, width, 38);
        Raylib.DrawRectangleRounded(rect, 0.4f, 12, new Color(255, 255, 255, 228));
        Raylib.DrawTriangle(new Vector2(rect.X + 48, rect.Y + rect.Height), new Vector2(rect.X + 68, rect.Y + rect.Height), new Vector2(rect.X + 54, rect.Y + rect.Height + 18), new Color(255, 255, 255, 228));
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + 18, rect.Y + 8), 20, 1, new Color(95, 58, 128, 255));
    }
}
