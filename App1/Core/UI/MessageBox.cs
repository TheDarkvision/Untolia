using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Untolia.Core.UI;

public enum UIPosition
{
    Center,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public sealed class MessageBox : UIElement
{
    private readonly string _message;
    private readonly Action? _onClose;
    private KeyboardState _previousKeyboard;

    // Prevent immediate close from the same key press and rapid repeats
    private float _closeCooldown = 0.15f;

    public MessageBox(string message, Action? onClose = null, UIPosition position = UIPosition.Center)
    {
        _message = message;
        _onClose = onClose;
        IsModal = true;
        CanReceiveFocus = true;
        
        CalculateSize();
        SetPosition(position);
    }

    public override void OnAdded()
    {
        _previousKeyboard = Keyboard.GetState();
        _closeCooldown = 0.15f;
    }

    private void CalculateSize()
    {
        var textSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, _message);
        Size = new Vector2(
            Math.Max(200, textSize.X + 40),
            Math.Max(100, textSize.Y + 60)
        );
    }

    private void SetPosition(UIPosition position)
    {
        var screenWidth = Globals.ScreenSize.X;
        var screenHeight = Globals.ScreenSize.Y;
        var margin = 50f;

        Position = position switch
        {
            UIPosition.Center => new Vector2(
                (screenWidth - Size.X) / 2f,
                (screenHeight - Size.Y) / 2f),
            UIPosition.Top => new Vector2(
                (screenWidth - Size.X) / 2f,
                margin),
            UIPosition.Bottom => new Vector2(
                (screenWidth - Size.X) / 2f,
                screenHeight - Size.Y - margin),
            UIPosition.TopLeft => new Vector2(margin, margin),
            UIPosition.TopRight => new Vector2(
                screenWidth - Size.X - margin,
                margin),
            UIPosition.BottomLeft => new Vector2(
                margin,
                screenHeight - Size.Y - margin),
            UIPosition.BottomRight => new Vector2(
                screenWidth - Size.X - margin,
                screenHeight - Size.Y - margin),
            _ => new Vector2((screenWidth - Size.X) / 2f, (screenHeight - Size.Y) / 2f)
        };
    }

    public override void Update(float deltaTime)
    {
        if (_closeCooldown > 0f)
            _closeCooldown -= deltaTime;

        var keyboard = Keyboard.GetState();

        var closePressed =
            (!_previousKeyboard.IsKeyDown(Keys.Enter) && keyboard.IsKeyDown(Keys.Enter)) ||
            (!_previousKeyboard.IsKeyDown(Keys.Space) && keyboard.IsKeyDown(Keys.Space)) ||
            (!_previousKeyboard.IsKeyDown(Keys.Escape) && keyboard.IsKeyDown(Keys.Escape));

        if (_closeCooldown <= 0f && closePressed)
        {
            Globals.UI.Remove(this);
            _onClose?.Invoke();
            _closeCooldown = 0.15f;
        }

        _previousKeyboard = keyboard;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DrawBackground(spriteBatch);

        var textPos = Position + Size / 2f - UIAssets.MeasureStringSafe(UIAssets.DefaultFont, _message) / 2f;
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, _message, textPos, Color.White);

        var instruction = "Press ENTER/SPACE/ESC to close";
        var instSize = UIAssets.MeasureStringSafe(UIAssets.DefaultFont, instruction);
        var instPos = new Vector2(Position.X + Size.X / 2f - instSize.X / 2f, Position.Y + Size.Y - 25);
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, instruction, instPos, Color.Gray);
    }
}
