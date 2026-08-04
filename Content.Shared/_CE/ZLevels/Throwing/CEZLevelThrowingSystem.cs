using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Throwing;

namespace Content.Shared._CE.ZLevels.Throwing;

public sealed partial class CEZLevelThrowingSystem : EntitySystem
{
    private const float NormalArcPeakHeight = 0.75f;
    private const float HighArcPeakHeight = 1.5f;

    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEZPhysicsComponent, ThrownEvent>(OnThrown);
    }

    private void OnThrown(Entity<CEZPhysicsComponent> ent, ref ThrownEvent args)
    {
        if (!TryComp<ThrownItemComponent>(ent, out var thrown)
            || thrown.LandTime is not { } landTime
            || thrown.ThrownTime is not { } thrownTime)
            return;

        var flyTime = (float)(landTime - thrownTime).TotalSeconds;
        if (flyTime <= 0f)
            return;

        var highThrow = args.User is { } user
            && TryComp<CEZLevelViewerComponent>(user, out var viewer)
            && viewer.LookUp;

        var targetHeight = highThrow ? 1f : 0f;
        var peakCap = highThrow ? HighArcPeakHeight : NormalArcPeakHeight;

        var distToGround = MathF.Max(0f, ent.Comp.LocalPosition - ent.Comp.CachedGroundHeight);
        var v0 = CESharedZLevelsSystem.ZGravityForce * flyTime * 0.5f + (targetHeight - distToGround) / flyTime;
        var maxV0 = MathF.Sqrt(2f * CESharedZLevelsSystem.ZGravityForce * peakCap);
        _zLevels.SetZVelocity((ent.Owner, ent.Comp), Math.Clamp(v0, 0f, maxV0));
    }
}
