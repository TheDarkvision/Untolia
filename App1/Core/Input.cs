using Microsoft.Xna.Framework.Input;

namespace Untolia.Core;

public static class Input
{
    private static KeyboardState _prev, _curr;

    public static void Update()
    {
        _prev = _curr;
        _curr = Keyboard.GetState();
    }

    public static bool Down(Keys k)
    {
        return _curr.IsKeyDown(k);
    }

    public static bool Pressed(Keys k)
    {
        return _curr.IsKeyDown(k) && !_prev.IsKeyDown(k);
    }

    public static int AxisX()
    {
        return (Down(Keys.D) || Down(Keys.Right) ? 1 : 0) - (Down(Keys.A) || Down(Keys.Left) ? 1 : 0);
    }

    public static int AxisY()
    {
        return (Down(Keys.S) || Down(Keys.Down) ? 1 : 0) - (Down(Keys.W) || Down(Keys.Up) ? 1 : 0);
    }
}