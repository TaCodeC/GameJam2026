using System;
using UnityEngine;

namespace GameJam.Input
{
    public interface IGameInput
    {
        event Action<GameAction> ActionPressed;
        event Action<GameAction> ActionReleased;

        Vector2 Move { get; }
        Vector2 Look { get; }

        bool IsHeld(GameAction action);
    }
}
