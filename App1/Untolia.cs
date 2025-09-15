using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Untolia.Core;
using Untolia.Core.UI;
using Untolia.Maps;
using MessageBox = Untolia.Core.UI.MessageBox;

namespace Untolia;

public class Untolia : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly PlayerController _playerController = new();
    private MapData _currentMap = null!;
    private Vector2 _playerPosition;
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
        InitializeUI();
        LoadMap("Forest01");
        InitializePlayer();

        // Test the dialogue system
        ShowTestDialogue();
    }

    private void InitializeGlobals()
    {
        Globals.GraphicsDevice = GraphicsDevice;
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Globals.SpriteBatch = _spriteBatch;
    }

    private void InitializeUI()
    {
        UIAssets.Initialize(GraphicsDevice, Content);
    }

    private void LoadMap(string mapId)
    {
        var provider = new ContentPipelineAssetProvider(
            Content,
            GraphicsDevice,
            $"maps/{mapId}/",
            Path.Combine("Content", "maps", mapId)
        );

        _currentMap = MapLoader.Load(GraphicsDevice, provider, "map_data.json");
    }

    private void InitializePlayer()
    {
        Assets.EnsurePlayerTexture(GraphicsDevice, Content);
        _playerPosition = new Vector2(_currentMap.Spawn.X, _currentMap.Spawn.Y);
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

        if (IsExitRequested(keyboard))
            Exit();

        Globals.GameTime = gameTime;
        Input.Update();

        // Handle UI input first
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Globals.UI.Update(deltaTime);

        // Only update player if no modal UI is active
        if (!Globals.UI.HasModalElements()) UpdatePlayer(gameTime);

        // Test message box
        if (!_previousKeyboard.IsKeyDown(Keys.T) && keyboard.IsKeyDown(Keys.T)) ShowTestMessage();

        // Main menu toggle
        if (!_previousKeyboard.IsKeyDown(Keys.M) && keyboard.IsKeyDown(Keys.M)) ToggleGameMenu();

        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    private void ShowTestMessage()
    {
        var messageBox = new MessageBox(
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
        // Check if menu is already open
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
            ref _playerPosition,
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
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawMap();
        DrawPlayer();
        DrawOverlayLayer();

        // Draw UI on top
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
        _spriteBatch.Draw(Assets.Player, _playerPosition, Color.White);
    }

    private void DrawOverlayLayer()
    {
        _currentMap.Over?.Let(texture =>
            _spriteBatch.Draw(texture, Vector2.Zero, Color.White));
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
    public static void Let<T>(this T? obj, Action<T> action) where T : class
    {
        if (obj != null) action(obj);
    }
}