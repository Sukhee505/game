using UnityEngine;

namespace SevenMinutes.Environment
{
    public enum SurfaceType
    {
        Wood,
        Concrete,
        Metal,
        Tile,
        Grass
    }

    /// <summary>Tells the footstep system what the floor is made of.</summary>
    public class FootstepSurface : MonoBehaviour
    {
        public SurfaceType Surface = SurfaceType.Wood;
    }
}
