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

    private const int RequiredHouseHits = 24;
    private const float NormalMaxSpeed = 240f;
    private const float PoweredMaxSpeed = 310f;
    private const float NormalAccel = 1700f;
    private const float PoweredAccel = 2200f;
    private const float Friction = 8f;

    private readonly Game _game;
    private readonly ParticleSystem _particles = new();
    private readonly FloatingTextSystem _floatingTexts = new();
    private readonly Food[] _foods;
    private readonly Vector2 _castlePosition = new(640, 260);
    private readonly Vector2 _housePosition = new(1032, 342);

    private Vector2 _princessPosition = new(185, 560);
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

    public PrincessRescueScene(Game game)
    {
        _game = game;
        _foods =
        [
            new(new Vector2(165, 210), RescueSprite.Apple),
            new(new Vector2(350, 168), RescueSprite.Carrot),
            new(new Vector2(506, 520), RescueSprite.Apple),
            new(new Vector2(722, 584), RescueSprite.Carrot),
            new(new Vector2(866, 178), RescueSprite.Apple),
            new(new Vector2(1114, 542), RescueSprite.Carrot),
            new(new Vector2(310, 620), RescueSprite.Apple),
            new(new Vector2(610, 150), RescueSprite.Carrot),
        ];
    }

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

            if (_completeTimer > 0.6f && Raylib.GetKeyPressed() != 0)
            {
                _game.ChangeScene(new TitleScene(_game));
            }
            return;
        }

        MovePrincess(effectiveDt);
        UpdateFood();

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

    public void Draw()
    {
        Raylib.ClearBackground(new Color(123, 205, 128, 255));

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

        if (_houseHits >= RequiredHouseHits)
        {
            _complete = true;
            _completeTimer = 0f;
            _game.Audio.Play("sfx_rescue_complete");
            _particles.EmitFirework(_housePosition, 60);
            _particles.EmitHearts(_housePosition + new Vector2(0, -30), 50);
            _particles.EmitStars(_housePosition, 40);
            _particles.EmitConfetti(_housePosition + new Vector2(0, -40), 36);
            _floatingTexts.Spawn("救出小女孩!", _housePosition + new Vector2(0, -150), new Color(255, 90, 160, 255), 42f, 2.0f);
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
                Raylib.DrawTexturePro(grass, new Rectangle(0, 0, grass.Width, grass.Height), new Rectangle(x, y, 64, 64), Vector2.Zero, 0, new Color(230, 255, 226, 255));
            }
        }

        DrawWindRipple();

        Raylib.DrawCircleV(new Vector2(640, 312), 255, new Color(255, 255, 255, 30));
        Raylib.DrawRectangleRounded(new Rectangle(584, 190, 112, 420), 0.35f, 18, new Color(214, 187, 128, 255));
        Raylib.DrawRectangleRounded(new Rectangle(600, 534, 360, 82), 0.35f, 18, new Color(214, 187, 128, 255));
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

    private void DrawForestDecorations()
    {
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestPath, new Rectangle(760, 572, 255, 118), new Color(255, 255, 255, 230), -8f);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestBroadleaf, new Rectangle(86, 286, 210, 160), new Color(255, 255, 255, 235));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestPine, new Rectangle(430, 100, 210, 160), new Color(255, 255, 255, 235));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestBush, new Rectangle(820, 112, 190, 130), new Color(255, 255, 255, 235));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestMushroom, new Rectangle(1102, 510, 170, 120), new Color(255, 255, 255, 235));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestLog, new Rectangle(168, 626, 190, 105), new Color(255, 255, 255, 235));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestRocks, new Rectangle(714, 84, 155, 112), new Color(255, 255, 255, 225));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.ForestFlowers, new Rectangle(388, 592, 170, 112), new Color(255, 255, 255, 235));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.GardenArch, new Rectangle(748, 388, 130, 168), new Color(255, 255, 255, 225));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RoyalFountain, new Rectangle(484, 390, 170, 120), new Color(255, 255, 255, 230));
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
        DrawSpeech(font, "王子在揮手", _castlePosition + new Vector2(-88, -168), 156);
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
        var progress = Math.Clamp(_houseHits / (float)RequiredHouseHits, 0f, 1f);
        var shakeX = _hitShake > 0f ? MathF.Sin(_time * 68f) * _hitShake * 18f : 0f;
        var shakeY = _hitShake > 0f ? MathF.Cos(_time * 60f) * _hitShake * 7f : 0f;
        var houseCenter = _housePosition + new Vector2(shakeX, shakeY);
        var impact = Math.Clamp(_smashTimer / 0.32f, 0f, 1f);
        var impactPulse = MathF.Sin(impact * MathF.PI);

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
            GeneratedSprites.TryDrawFinal(_game.Assets, FinalSprite.GirlFree, new Rectangle(_housePosition.X + 154, _housePosition.Y + 82 + girlBob, 122, 122), Color.White);
            GeneratedSprites.TryDrawFinal(_game.Assets, FinalSprite.WitchDefeated, new Rectangle(_housePosition.X - 150, _housePosition.Y + 92, 128, 128), Color.White, MathF.Sin(_time * 6f) * 5f);
        }
        else
        {
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Witch, new Rectangle(_housePosition.X + 114, _housePosition.Y + 54, 118, 118), Color.White, MathF.Sin(_time * 4f) * 3f);
            GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Girl, new Rectangle(_housePosition.X - 128, _housePosition.Y + 70, 102, 102), Color.White);
            DrawTinyBars(_housePosition + new Vector2(-128, 70));
        }

        if (_powered && !_complete && Vector2.Distance(_princessPosition, _housePosition) <= 180f)
        {
            DrawHouseProgress();
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
        var ratio = Math.Clamp(_houseHits / (float)RequiredHouseHits, 0f, 1f);
        var rect = new Rectangle(_housePosition.X - 88, _housePosition.Y - 154, 176, 20);
        Raylib.DrawRectangleRounded(rect, 0.45f, 10, new Color(255, 255, 255, 228));
        Raylib.DrawRectangleRounded(new Rectangle(rect.X + 4, rect.Y + 4, (rect.Width - 8) * ratio, rect.Height - 8), 0.45f, 10, new Color(255, 119, 168, 255));
        Raylib.DrawRectangleRoundedLines(rect, 0.45f, 10, Color.DarkPurple);
        var text = $"{_houseHits}/{RequiredHouseHits}";
        var measured = Raylib.MeasureTextEx(font, text, 20, 1);
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + (rect.Width - measured.X) / 2f, rect.Y - 25), 20, 1, Color.DarkPurple);
    }

    private void DrawHud()
    {
        var font = _game.Assets.GetFont();
        DrawPill(new Rectangle(24, 22, 240, 46), $"食物 {FoodCount} / {_foods.Length}", new Color(255, 152, 75, 255));
        DrawPill(new Rectangle(Game.ScreenWidth - 290, 22, 266, 46), _powered ? "力氣滿了!" : "還需要更多食物", _powered ? new Color(255, 105, 180, 255) : new Color(95, 174, 110, 255));

        var tip = _complete
            ? "救出小女孩成功! 按任意鍵回選單"
            : _powered
                ? "靠近房子連點空白鍵拆掉房子"
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
