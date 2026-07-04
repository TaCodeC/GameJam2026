#pragma warning disable 0649

using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    public abstract class MeasurementToolBase : MonoBehaviour
    {
        public abstract float CurrentValue { get; }

        public abstract void SetUnit(string unit);

        public abstract void Refresh();
    }
}
