using SevenMinutes.Player;

namespace SevenMinutes.Core
{
    public enum GameState
    {
        MainMenu,
        Introduction,
        Playing,
        Paused,
        Victory,
        Defeat
    }

    public enum FailKind
    {
        Hazard,
        Fall,
        Burn,
        Laser,
        Crush
    }

    /// <summary>Anything the player can use with [E].</summary>
    public interface IInteractable
    {
        /// <summary>Text shown after "[E] ", e.g. "Search Drawer".</summary>
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact(PlayerController player);
    }

    /// <summary>Trigger volumes that react to the player entering / leaving them.</summary>
    public interface IPlayerTrigger
    {
        void OnPlayerEnter(PlayerController player);
        void OnPlayerExit(PlayerController player);
    }

    /// <summary>Touching a dangerous hazard sends the player back to the level checkpoint.</summary>
    public interface IHazard
    {
        bool IsDangerous { get; }
        string DeathMessage { get; }
        FailKind Kind { get; }
    }

    /// <summary>Floor pieces that want to know when the player stands on them.</summary>
    public interface ISteppable
    {
        void OnPlayerStep(PlayerController player);
    }
}
