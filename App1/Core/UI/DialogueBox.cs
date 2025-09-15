using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Untolia.Core.UI;

public sealed class DialogueBox : UIElement
{
    private readonly Queue<DialogueLine> _lines = new();
    private readonly Action? _onComplete;
    private DialogueLine? _currentLine;
    private KeyboardState _previousKeyboard;
    private int _revealedChars;
    private float _typewriterTimer;

    // Debounce to prevent advancing multiple lines per key press/hold
    private float _inputCooldown = 0f;
    private const float InputCooldownSeconds = 0.18f; // adjust to taste

    public DialogueBox(IEnumerable<DialogueLine> dialogue, Action? onComplete = null,
        UIPosition position = UIPosition.Bottom)
    {
        foreach (var line in dialogue)
            _lines.Enqueue(line);

        _onComplete = onComplete;
        IsModal = true;
        CanReceiveFocus = true;

        SetupBox(position);
        NextLine();
    }

    public override void OnAdded()
    {
        _previousKeyboard = Keyboard.GetState();
        _inputCooldown = 0.12f; // small delay after opening
    }

    public bool IsTyping => _currentLine != null && _revealedChars < _currentLine.Text.Length;
    public float TypewriterSpeed { get; set; } = 30f; // characters per second

    private void SetupBox(UIPosition position)
    {
        var boxWidth = Globals.ScreenSize.X - 100;
        var boxHeight = 150f;
        Size = new Vector2(boxWidth, boxHeight);

        var margin = 50f;
        Position = position switch
        {
            UIPosition.Top => new Vector2(margin, margin),
            UIPosition.Bottom => new Vector2(margin, Globals.ScreenSize.Y - boxHeight - margin),
            UIPosition.Center => new Vector2(margin, (Globals.ScreenSize.Y - boxHeight) / 2f),
            _ => new Vector2(margin, Globals.ScreenSize.Y - boxHeight - margin) // Default to bottom
        };
    }

    private void NextLine()
    {
        if (_lines.TryDequeue(out _currentLine))
        {
            _revealedChars = 0;
            _typewriterTimer = 0f;
        }
        else
        {
            _onComplete?.Invoke();
        }
    }

    public override void Update(float deltaTime)
    {
        if (_currentLine == null) return;

        // Tick cooldown
        if (_inputCooldown > 0f)
            _inputCooldown -= deltaTime;

        var keyboard = Keyboard.GetState();
        var actionPressed =
            (!_previousKeyboard.IsKeyDown(Keys.Enter) && keyboard.IsKeyDown(Keys.Enter)) ||
            (!_previousKeyboard.IsKeyDown(Keys.Space) && keyboard.IsKeyDown(Keys.Space));

        // Ignore input while cooling down
        if (_inputCooldown > 0f)
            actionPressed = false;

        if (IsTyping)
        {
            if (actionPressed)
            {
                // Skip to the end of the current line
                _revealedChars = _currentLine.Text.Length;
                _inputCooldown = InputCooldownSeconds;
            }
            else
            {
                // Typewriter reveal
                _typewriterTimer += deltaTime;
                var targetChars = (int)(_typewriterTimer * TypewriterSpeed);
                _revealedChars = Math.Min(targetChars, _currentLine.Text.Length);
            }
        }
        else if (actionPressed)
        {
            NextLine();
            _inputCooldown = InputCooldownSeconds;
        }

        _previousKeyboard = keyboard;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_currentLine == null) return;

        DrawBackground(spriteBatch);

        var textArea = new Rectangle(
            (int)Position.X + 20,
            (int)Position.Y + 20,
            (int)Size.X - 40,
            (int)Size.Y - 40
        );

        // Speaker
        var textY = textArea.Y;
        if (!string.IsNullOrEmpty(_currentLine.Speaker))
        {
            var speakerText = $"{_currentLine.Speaker}:";
            spriteBatch.DrawStringSafe(UIAssets.DefaultFont, speakerText,
                new Vector2(textArea.X, textY), Color.Yellow);
            textY += 25;
        }

        // Revealed text
        var revealedText = _currentLine.Text[.._revealedChars];
        spriteBatch.DrawStringSafe(UIAssets.DefaultFont, revealedText,
            new Vector2(textArea.X, textY), Color.White);

        // Continue indicator (ASCII)
        if (!IsTyping && _lines.Count > 0)
        {
            var indicator = "v";
            var indicatorPos = new Vector2(
                Position.X + Size.X - 30,
                Position.Y + Size.Y - 25
            );
            spriteBatch.DrawStringSafe(UIAssets.DefaultFont, indicator, indicatorPos, Color.White);
        }
    }
}

public sealed class DialogueLine
{
    public DialogueLine(string speaker, string text)
    {
        Speaker = speaker;
        Text = text;
    }

    public DialogueLine(string text) : this("", text) { }

    public string Speaker { get; }
    public string Text { get; }
}
