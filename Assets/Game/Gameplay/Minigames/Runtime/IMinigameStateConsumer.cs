#pragma warning disable 0649

namespace GameJam.Gameplay.Minigames
{
    public interface IMinigameStateConsumer
    {
        void BindState(MinigameObjectState objectState, string minigameId);
    }
}
