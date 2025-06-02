using System;
using System.Collections.Generic;

public static class KeyBindings
{
    public static readonly Dictionary<ConsoleKey, (int dx, int dy)> MovementKeys = new()
    {
        [ConsoleKey.W] = (0, -1),
        [ConsoleKey.UpArrow] = (0, -1),
        [ConsoleKey.S] = (0, 1),
        [ConsoleKey.DownArrow] = (0, 1),
        [ConsoleKey.A] = (-1, 0),
        [ConsoleKey.LeftArrow] = (-1, 0),
        [ConsoleKey.D] = (1, 0),
        [ConsoleKey.RightArrow] = (1, 0)
    };
}
