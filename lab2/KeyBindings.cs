using System.Collections.Generic;
using System.Windows.Forms;

public static class KeyBindings
{
    public static readonly Dictionary<Keys, (int dx, int dy)> MovementKeys = new()
    {
        [Keys.W] = (0, -1),
        [Keys.Up] = (0, -1),
        [Keys.S] = (0, 1),
        [Keys.Down] = (0, 1),
        [Keys.A] = (-1, 0),
        [Keys.Left] = (-1, 0),
        [Keys.D] = (1, 0),
        [Keys.Right] = (1, 0)
    };
}
