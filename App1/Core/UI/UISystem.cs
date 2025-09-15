using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace Untolia.Core.UI;

public sealed class UISystem
{
    private readonly List<UIElement> _elements = new();
    private readonly List<UIElement> _toRemove = new();
    private readonly List<UIElement> _toAdd = new();
    private UIElement? _focusedElement;

    // Public property to access elements safely
    public IReadOnlyList<UIElement> Elements => _elements;

    public void Add(UIElement element)
    {
        _toAdd.Add(element);
        element.OnAdded(); // Prime input state to avoid instant actions on open
        if (element.CanReceiveFocus && _focusedElement == null)
            _focusedElement = element;
    }

    public void Remove(UIElement element)
    {
        _toRemove.Add(element);
        if (_focusedElement == element)
            _focusedElement = null;
    }

    public void Clear()
    {
        _elements.Clear();
        _toRemove.Clear();
        _toAdd.Clear();
        _focusedElement = null;
    }

    public void Update(float deltaTime)
    {
        // Add queued elements first
        foreach (var element in _toAdd)
            _elements.Add(element);
        _toAdd.Clear();

        // Remove queued elements
        foreach (var element in _toRemove)
            _elements.Remove(element);
        _toRemove.Clear();

        // Update all elements - snapshot to avoid modification during enumeration
        foreach (var element in _elements.ToList())
        {
            if (element.IsVisible)
                element.Update(deltaTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var element in _elements.ToList())
        {
            if (element.IsVisible)
                element.Draw(spriteBatch);
        }
    }

    public bool HasModalElements() => _elements.Any(e => e.IsModal && e.IsVisible);
}