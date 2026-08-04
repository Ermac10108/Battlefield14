using Robust.Shared.GameStates;

namespace Content.Shared._Battlefield14.Projectiles.Components;

// Marks a projectile that travels between z-levels (maps) mid-flight, so shots can hit targets standing on the level above or below through openings in the floor/ceiling.

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEZProjectileComponent : Component
{
    // Z-level offset the projectile travels. +1 = up, -1 = down.
    [DataField, AutoNetworkedField]
    public int ZOffset;

    // Horizontal distance the projectile must cover before transitioning to the adjacent z-level.
    [DataField, AutoNetworkedField]
    public float TransitionDistance;

    // Cumulative distance the projectile has traveled since it was fired.
    [DataField, AutoNetworkedField]
    public float DistanceTraveled;

    // Whether the projectile has already transitioned between z-levels.
    [DataField, AutoNetworkedField]
    public bool Transitioned;
}
