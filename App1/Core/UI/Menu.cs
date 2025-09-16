using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Untolia.Core.UI;

public abstract class Menu : UIElement
{
    private const float NavCooldownSeconds = 0.14f; // between Up/Down moves
    private const float SelectCooldownSeconds = 0.18f; // between Enter/Escape
    protected readonly List<MenuItem> _items = new();

    // Debounce timers
    protected float _navCooldown; // was private
    protected KeyboardState _previousKeyboard; // was private
    protected float _selectCooldown; // was private
    protected int _selectedIndex;

    public Menu()
    {
        IsModal = true;
        CanReceiveFocus = true;
        BackgroundColor = Color.Black * 0.9f;
        BorderColor = Color.White;
        BorderThickness = 3;
    }

    public override void OnAdded()
    {
        // Prime input to avoid invoking first item due to lingering Enter
        _previousKeyboard = Keyboard.GetState();
        _selectCooldown = SelectCooldownSeconds; // short delay after opening
        _navCooldown = 0.08f; // slight delay before first navigation
    }

    protected void AddMenuItem(MenuItem item)
    {
        _items.Add(item);
    }

    public override void Update(float deltaTime)
    {
        if (_items.Count == 0) return;

        // Tick cooldowns
        if (_navCooldown > 0f) _navCooldown -= deltaTime;
        if (_selectCooldown > 0f) _selectCooldown -= deltaTime;

        var keyboard = Keyboard.GetState();

        // Navigation (debounced)
        var upPressed = !_previousKeyboard.IsKeyDown(Keys.Up) && keyboard.IsKeyDown(Keys.Up);
        var downPressed = !_previousKeyboard.IsKeyDown(Keys.Down) && keyboard.IsKeyDown(Keys.Down);

        if (_navCooldown <= 0f && (upPressed || downPressed))
        {
            _selectedIndex = upPressed
                ? (_selectedIndex - 1 + _items.Count) % _items.Count
                : (_selectedIndex + 1) % _items.Count;

            _navCooldown = NavCooldownSeconds;
        }

        // Selection (debounced)
        var enterPressed = !_previousKeyboard.IsKeyDown(Keys.Enter) && keyboard.IsKeyDown(Keys.Enter);
        if (_selectCooldown <= 0f && enterPressed)
        {
            _items[_selectedIndex].Action?.Invoke();
            _selectCooldown = SelectCooldownSeconds;
        }

        // Close (debounced)
        var escPressed = !_previousKeyboard.IsKeyDown(Keys.Escape) && keyboard.IsKeyDown(Keys.Escape);
        if (_selectCooldown <= 0f && escPressed)
        {
            CloseMenu();
            _selectCooldown = SelectCooldownSeconds;
        }

        _previousKeyboard = keyboard;
    }

    protected abstract void CloseMenu();

    public override void Draw(SpriteBatch spriteBatch)
    {
        DrawBackground(spriteBatch);
        DrawTitle(spriteBatch);
        DrawMenuItems(spriteBatch);
        DrawInstructions(spriteBatch);
    }

    protected virtual void DrawTitle(SpriteBatch spriteBatch)
    {
        // Optional override
    }

    private void DrawMenuItems(SpriteBatch spriteBatch)
    {
        var startY = Position.Y + 60; // space for title
        var itemHeight = 35f;

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var itemPos = new Vector2(Position.X + 40, startY + i * itemHeight);

            var color = i == _selectedIndex ? Color.Yellow : Color.White;

            if (i == _selectedIndex)
            {
                const string indicator = "> ";
                spriteBatch.DrawStringSafe(UIAssets.DefaultFont, indicator,
                    new Vector2(itemPos.X - 25, itemPos.Y), Color.Yellow);
            }

            spriteBatch.DrawStringSafe(UIAssets.DefaultFont, item.Text, itemPos, color);
        }
    }

    private void DrawInstructions(SpriteBatch spriteBatch)
    {
        var instructions = "UP/DOWN: Navigate  ENTER: Select  ESC: Close";
        var instructionPos = new Vector2(
            Position.X + 20,
            Position.Y + Size.Y - 30
        );
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, instructions, instructionPos, Color.Gray);
    }
}

public class MenuItem
{
    public MenuItem(string text, Action? action = null)
    {
        Text = text;
        Action = action;
    }

    public string Text { get; }
    public Action? Action { get; }
}