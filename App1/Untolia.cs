using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Untolia.Core;
using Untolia.Core.Inventory;
using Untolia.Core.Maps;
using Untolia.Core.Player;
using Untolia.Core.RPG;
using Untolia.Core.UI;
using Untolia.Core.Dialogue;
using MessageBox = Untolia.Core.UI.MessageBox;

namespace Untolia;

public class Untolia : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly PlayerController _playerController = new();

    private MapData _currentMap = null!;
    private PlayerEntity _player = null!;
    private Camera2D _camera = new(); // camera that follows the player and clamps to map

    private float _portalCooldown = 0f;

    private bool _debugDraw = true; // toggle with F3
    private Texture2D? _debugPixel; // 1x1 pixel for drawing rects
    private SpriteFont? _debugFont; // font for debug labels "D" and "E"

    private KeyboardState _previousKeyboard;
    private SpriteBatch _spriteBatch = null!;

    public Untolia()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        SetWindowSize(Globals.ScreenSize);
    }

    private void SetWindowSize(Point size)
    {
        _graphics.PreferredBackBufferWidth = size.X;
        _graphics.PreferredBackBufferHeight = size.Y;
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        InitializeGlobals();
        Globals.Content = Content;

        InitializeUI();

        // Load a map via the unified manifest loader.
        // It will also fallback to folder heuristics if the manifest is missing.
        LoadMap("Forest01");

        // Load inventory item definitions from Content/Data/Items/...
        ItemLoader.LoadAllFromContent(Globals.Inventory);

        // Optional: seed some starting inventory (only if defs exist)
        TrySeed("healing_potion", 5);
        TrySeed("ether", 2);
        TrySeed("steel_longsword", 1);
        TrySeed("rusty_key", 1);

        // Load character definitions and fill party
        Globals.Characters.LoadAllFromFolder("Data/Characters");
        FillParty();

        InitializePlayer();
        // ShowTestDialogue(); // Uncomment to test dialogue

        // Debug font for labels
        _debugFont = Content.Load<SpriteFont>("Fonts/Default");
    }

    private void TrySeed(string itemId, int qty)
    {
        if (Globals.Inventory.GetDef(itemId) != null)
        {
            try { Globals.Inventory.Add(itemId, qty); } catch { /* ignore */ }
        }
    }

    private void FillParty()
    {
        Globals.Party.Recruit(Globals.Characters, "caelen", levelOverride: null, addToParty: true);
        Globals.Party.Recruit(Globals.Characters, "lyra", levelOverride: null, addToParty: true);
        Globals.Party.Recruit(Globals.Characters, "thalen", levelOverride: null, addToParty: true);
    }

    private void InitializeGlobals()
    {
        Globals.GraphicsDevice = GraphicsDevice;
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Globals.SpriteBatch = _spriteBatch;

        // 1x1 white pixel for debug drawing
        _debugPixel = new Texture2D(GraphicsDevice, 1, 1);
        _debugPixel.SetData(new[] { Color.White });
    }

    private void InitializeUI()
    {
        UIAssets.Initialize(GraphicsDevice, Content);
    }

    private void LoadMap(string mapId)
    {
        _currentMap = MapLoader.LoadFromManifest(GraphicsDevice, Content, $"Maps/{mapId}");
    }

    private void InitializePlayer()
    {
        Assets.EnsurePlayerTexture(GraphicsDevice, Content);
        _player = new PlayerEntity(new Vector2(_currentMap.Spawn.X, _currentMap.Spawn.Y), new Point(16, 16));

        // Initialize camera after player and map are ready
        _camera.ViewportSize = Globals.ScreenSize;
        _camera.Bounds = _currentMap.CameraBounds;
        _camera.Zoom = 1f;
        _camera.Follow(_player.Position);

        // Process map onEnter events
        Globals.Log.Info($"ProcessOnEnterEvents: entering map '{_currentMap.Id}'");
        ProcessOnEnterEvents();
    }


    private void ShowTestDialogue()
    {
        var dialogue = new[]
        {
            new DialogueLine("System", "Welcome to Untolia"),
            new DialogueLine("Guide", "Use WASD or arrow keys to move around."),
            new DialogueLine("Guide", "Press T to show a test message box."),
            new DialogueLine("Guide", "Press M to open the game menu.")
        };

        var dialogueBox = new DialogueBox(dialogue, () =>
        {
            var elementToRemove = Globals.UI.Elements.FirstOrDefault(e => e is DialogueBox);
            if (elementToRemove != null)
                Globals.UI.Remove(elementToRemove);
        });

        Globals.UI.Add(dialogueBox);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        // Example: quick EXP test
        if (_previousKeyboard.IsKeyDown(Keys.E))
        {
            Globals.Party.AwardExpAll(
                amount: 150,
                registry: Globals.Characters,
                onLevelUp: (member, levelsGained) =>
                {
                    var msg = new MessageBox(
                        $"{member.Name} leveled up! (+{levelsGained})",
                        null,
                        UIPosition.TopRight
                    );
                    Globals.UI.Add(msg);
                });
        }

        Globals.GameTime = gameTime;
        Input.Update();

        // Update UI
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Globals.UI.Update(deltaTime);

        // Update game only if no modal UI is active
        if (!Globals.UI.HasModalElements())
            UpdatePlayer(gameTime);

        // Update camera to follow player after movement
        _camera.Follow(_player.Position);

        // Handle portals
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_portalCooldown > 0f) _portalCooldown -= dt;
        TryEnterPortal();
        
        // New: tile-trigger events
        TryTriggerTileEvents();

        // Toggle debug overlay
        if (!_previousKeyboard.IsKeyDown(Keys.F3) && keyboard.IsKeyDown(Keys.F3))
            _debugDraw = !_debugDraw;

        // Toggle game menu
        if (!_previousKeyboard.IsKeyDown(Keys.Tab) && keyboard.IsKeyDown(Keys.Tab))
            ToggleGameMenu();

        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    private void TryTriggerTileEvents()
    {
        if (_currentMap.Events == null || _currentMap.Events.Count == 0) return;

        // Use the player's feet/center point; adjust if needed for your sprite origin
        var px = (int)(_player.Position.X + 8);
        var py = (int)(_player.Position.Y + 8);
        var pPoint = new Point(px, py);

        foreach (var ev in _currentMap.Events)
        {
            if (!string.Equals(ev.When, "onTileEnter", System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Must have an area to test
            if (!ev.Area.HasValue) continue;

            // Respect one-time tracking
            var key = $"{_currentMap.Id}:{ev.Id}";
            if (ev.OneTime && Globals.TriggeredEvents.Contains(key)) continue;

            if (ev.Area.Value.Contains(pPoint))
            {
                bool handled = HandleEvent(ev);
                if (handled && ev.OneTime)
                {
                    Globals.TriggeredEvents.Add(key);
                    Globals.Log.Info($"TryTriggerTileEvents: marked one-time event triggered '{key}'");
                }

                // If events are exclusive, you can break here; otherwise continue to allow multiple areas to fire
                // break;
            }
        }
    }
    
    private bool HandleEvent(MapEvent ev)
    {
        bool handled = false;

        switch (ev.Type)
        {
            case "setFlag":
                if (!string.IsNullOrWhiteSpace(ev.Flag) && ev.Value.HasValue)
                {
                    if (ev.Value.Value)
                    {
                        Globals.GameFlags.Add(ev.Flag);
                        Globals.Log.Info($"Event setFlag: '{ev.Flag}' = true");
                    }
                    else
                    {
                        Globals.GameFlags.Remove(ev.Flag);
                        Globals.Log.Info($"Event setFlag: '{ev.Flag}' = false");
                    }
                    handled = true;

                    if (_debugDraw)
                    {
                        var msg = new MessageBox("Event",
                            $"Flag '{ev.Flag}' set to {ev.Value.Value}",
                            () =>
                            {
                                var el = Globals.UI.Elements.LastOrDefault(e => e is MessageBox);
                                if (el != null) Globals.UI.Remove(el);
                            },
                            UIPosition.TopRight);
                        Globals.UI.Add(msg);
                    }
                }
                break;

            case "showDialogue":
                if (!string.IsNullOrWhiteSpace(ev.DialogueKey))
                {
                    var lines = DialogueLoader.Load(ev.DialogueKey);
                    if (lines.Length > 0)
                    {
                        var dlg = new DialogueBox(lines, () =>
                        {
                            var elementToRemove = Globals.UI.Elements.FirstOrDefault(e => e is DialogueBox);
                            if (elementToRemove != null)
                                Globals.UI.Remove(elementToRemove);
                        });
                        Globals.UI.Add(dlg);
                        handled = true;
                    }
                    else
                    {
                        Globals.Log.Warn($"showDialogue: no lines found for key '{ev.DialogueKey}'");
                        var msg = new MessageBox("Notice", $"Dialogue '{ev.DialogueKey}' missing or empty.",
                            () =>
                            {
                                var el = Globals.UI.Elements.LastOrDefault(e => e is MessageBox);
                                if (el != null) Globals.UI.Remove(el);
                            },
                            UIPosition.TopRight);
                        Globals.UI.Add(msg);
                        handled = true;
                    }
                }
                break;

            default:
                Globals.Log.Warn($"HandleEvent: unknown event type '{ev.Type}' (id='{ev.Id}')");
                break;
        }

        return handled;
    }


    
    private void ShowTestMessage()
    {
        var messageBox = new MessageBox(
            "Notice",
            "This is a test message box!\nPress ENTER or SPACE to close.",
            () =>
            {
                var elementToRemove = Globals.UI.Elements.LastOrDefault(e => e is MessageBox);
                if (elementToRemove != null)
                    Globals.UI.Remove(elementToRemove);
            },
            UIPosition.TopRight
        );
        Globals.UI.Add(messageBox);
    }

    private void ToggleGameMenu()
    {
        var existingMenu = Globals.UI.Elements.FirstOrDefault(e => e is GameMenu);
        if (existingMenu != null)
            Globals.UI.Remove(existingMenu);
        else
            Globals.UI.Add(new GameMenu());
    }

    private static bool IsExitRequested(KeyboardState keyboard)
    {
        return keyboard.IsKeyDown(Keys.Escape);
    }

    private void UpdatePlayer(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _playerController.Update(
            ref _player.Position,
            deltaTime,
            _currentMap.Size,
            Assets.Player,
            _currentMap.CollisionBlocked,
            _currentMap.CollisionWidth,
            _currentMap.CollisionHeight
        );
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(transformMatrix: _camera.View, samplerState: SamplerState.PointClamp);

        DrawMap();
        DrawPlayer();
        DrawOverlayLayer();

        // Debug: draw portals and events on top of world layers
        if (_debugDraw)
        {
            DrawPortalsDebug();
            DrawEventsDebug();
        }

        Globals.UI.Draw(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawMap()
    {
        _spriteBatch.Draw(_currentMap.Bg, Vector2.Zero, Color.White);
    }

    private void DrawPlayer()
    {
        _player.Draw(_spriteBatch, Assets.Player);
    }

    private void DrawOverlayLayer()
    {
        _currentMap.Over?.Let(texture =>
            _spriteBatch.Draw(texture, Vector2.Zero, Color.White));
    }

    private void DrawPortalsDebug()
    {
        if (_debugPixel == null) return;
        if (_currentMap?.Portals == null || _currentMap.Portals.Count == 0) return;

        var fillColor = new Color(0, 255, 0) * 0.18f;   // translucent green fill
        var edgeColor = new Color(0, 220, 0) * 0.9f;    // bright green border

        foreach (var portal in _currentMap.Portals)
        {
            var r = portal.Area;

            // Fill
            _spriteBatch.Draw(_debugPixel, new Rectangle(r.X, r.Y, r.Width, r.Height), fillColor);

            // Outline (thickness = 1)
            DrawRectOutline(r, edgeColor, 1);
        }
    }

    private void DrawEventsDebug()
    {
        if (_debugPixel == null) return;
        if (_currentMap?.Events == null || _currentMap.Events.Count == 0) return;

        foreach (var ev in _currentMap.Events)
        {
            // Only draw events that have an area (tile triggers)
            if (!ev.Area.HasValue) continue;

            var r = ev.Area.Value;

            // Color coding:
            // - One-time not yet triggered: yellow
            // - One-time already triggered: gray
            // - Repeatable: cyan
            var key = $"{_currentMap.Id}:{ev.Id}";
            bool already = ev.OneTime && Globals.TriggeredEvents.Contains(key);

            var fill = ev.OneTime
                ? (already ? new Color(150, 150, 150) * 0.16f : new Color(255, 235, 59) * 0.16f)
                : new Color(0, 255, 255) * 0.16f;

            var border = ev.OneTime
                ? (already ? new Color(180, 180, 180) : new Color(255, 215, 0))
                : new Color(0, 220, 255);

            // Fill and outline
            _spriteBatch.Draw(_debugPixel, r, fill);
            DrawRectOutline(r, border, 1);

            // Label "E" (event)
            if (_debugFont != null)
            {
                const string label = "E";
                var size = _debugFont.MeasureString(label);
                var center = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f);
                var pos = center - size * 0.5f;
                _spriteBatch.DrawString(_debugFont, label, pos + new Vector2(1, 1), Color.Black * 0.8f);
                _spriteBatch.DrawString(_debugFont, label, pos, border);
            }
        }
    }

    private void DrawRectOutline(Rectangle r, Color color, int thickness)
    {
        if (_debugPixel == null) return;

        // top
        _spriteBatch.Draw(_debugPixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
        // left
        _spriteBatch.Draw(_debugPixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        // right
        _spriteBatch.Draw(_debugPixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
        // bottom
        _spriteBatch.Draw(_debugPixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
    }

    private void TryEnterPortal()
    {
        if (_currentMap.Portals == null || _currentMap.Portals.Count == 0) return;
        if (_portalCooldown > 0f) return;

        // Assuming player sprite is 16x16 as constructed.
        var playerRect = new Rectangle((int)_player.Position.X, (int)_player.Position.Y, 16, 16);

        foreach (var p in _currentMap.Portals)
        {
            if (p.Area.Intersects(playerRect))
            {
                EnterPortal(p);
                _portalCooldown = 0.25f; // basic debounce to avoid re-trigger
                break;
            }
        }
    }

    private void EnterPortal(Portal portal)
    {
        // Load the target map
        LoadMap(portal.TargetMap);

        // Place player at the target spawn
        _player.Position = new Vector2(portal.TargetSpawn.X, portal.TargetSpawn.Y);

        // Rebind camera to new map bounds and follow player
        _camera.Bounds = _currentMap.CameraBounds;
        _camera.Follow(_player.Position);

        // Run onEnter events for the new map
        Globals.Log.Info($"ProcessOnEnterEvents: entering map '{_currentMap.Id}' via portal '{portal.Id}'");
        ProcessOnEnterEvents();
    }


    private void ProcessOnEnterEvents()
    {
        if (_currentMap.Events == null || _currentMap.Events.Count == 0)
        {
            Globals.Log.Debug("ProcessOnEnterEvents: no events on this map");
            return;
        }

        int processed = 0;
        foreach (var ev in _currentMap.Events)
        {
            if (!string.Equals(ev.When, "onEnter", System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Build a stable key per map+event
            var key = $"{_currentMap.Id}:{ev.Id}";

            // Skip if this is a one-time event and already triggered before
            if (ev.OneTime && Globals.TriggeredEvents.Contains(key))
            {
                Globals.Log.Debug($"ProcessOnEnterEvents: skipping one-time event '{key}' (already triggered)");
                continue;
            }

            bool handled = false;
            Globals.Log.Debug($"ProcessOnEnterEvents: handling event id='{ev.Id}', type='{ev.Type}', oneTime={ev.OneTime}");

            switch (ev.Type)
            {
                case "setFlag":
                    if (!string.IsNullOrWhiteSpace(ev.Flag) && ev.Value.HasValue)
                    {
                        if (ev.Value.Value)
                        {
                            Globals.GameFlags.Add(ev.Flag);
                            Globals.Log.Info($"Event setFlag: '{ev.Flag}' = true");
                        }
                        else
                        {
                            Globals.GameFlags.Remove(ev.Flag);
                            Globals.Log.Info($"Event setFlag: '{ev.Flag}' = false");
                        }
                        handled = true;

                        // Optional: visual confirmation if debug overlay is enabled
                        if (_debugDraw)
                        {
                            var msg = new MessageBox("Event",
                                $"Flag '{ev.Flag}' set to {ev.Value.Value}",
                                () =>
                                {
                                    var el = Globals.UI.Elements.LastOrDefault(e => e is MessageBox);
                                    if (el != null) Globals.UI.Remove(el);
                                },
                                UIPosition.TopRight);
                            Globals.UI.Add(msg);
                        }
                    }
                    break;

                case "showDialogue":
                    if (!string.IsNullOrWhiteSpace(ev.DialogueKey))
                    {
                        var lines = DialogueLoader.Load(ev.DialogueKey);
                        if (lines.Length > 0)
                        {
                            var dlg = new DialogueBox(lines, () =>
                            {
                                var elementToRemove = Globals.UI.Elements.FirstOrDefault(e => e is DialogueBox);
                                if (elementToRemove != null)
                                    Globals.UI.Remove(elementToRemove);
                            });
                            Globals.UI.Add(dlg);
                            handled = true;
                        }
                        else
                        {
                            Globals.Log.Warn($"showDialogue: no lines found for key '{ev.DialogueKey}'");
                            var msg = new MessageBox("Notice", $"Dialogue '{ev.DialogueKey}' missing or empty.",
                                () =>
                                {
                                    var el = Globals.UI.Elements.LastOrDefault(e => e is MessageBox);
                                    if (el != null) Globals.UI.Remove(el);
                                },
                                UIPosition.TopRight);
                            Globals.UI.Add(msg);
                            handled = true;
                        }
                    }
                    break;

                default:
                    Globals.Log.Warn($"ProcessOnEnterEvents: unknown event type '{ev.Type}' (id='{ev.Id}')");
                    break;
            }

            // Mark one-time events as triggered if they were handled
            if (handled)
            {
                processed++;
                if (ev.OneTime)
                {
                    Globals.TriggeredEvents.Add(key);
                    Globals.Log.Info($"ProcessOnEnterEvents: marked one-time event triggered '{key}'");
                }
            }
        }

        Globals.Log.Info($"ProcessOnEnterEvents: processed {processed} onEnter event(s)");
    }



    protected override void UnloadContent()
    {
        Assets.Dispose();
        UIAssets.Dispose();
        base.UnloadContent();
    }
}

public static class Extensions
{
    public static void Let<T>(this T? obj, System.Action<T> action) where T : class
    {
        if (obj != null) action(obj);
    }
}
