using System.Numerics;
using Content.Shared._Battlefield14.Projectiles.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Battlefield14.Projectiles;


public sealed class CEZProjectileSystem : EntitySystem
{

    public const float MinZShotDistance = 3f;

    private const float MinTransitionDistance = 1.5f;

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zLevels = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEZProjectileComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zProj, out var physics, out _))
        {
            if (zProj.Transitioned || zProj.ZOffset == 0 || zProj.TransitionDistance <= 0f)
                continue;

            zProj.DistanceTraveled += physics.LinearVelocity.Length() * frameTime;

            if (zProj.DistanceTraveled < zProj.TransitionDistance)
                continue;

            if (_zLevels.TryMove(uid, zProj.ZOffset))
            {
                zProj.Transitioned = true;
                Dirty(uid, zProj);
            }
        }
    }

    public bool TryGetZShot(EntityUid? user, MapCoordinates from, Vector2 to, out int zOffset, out float transitionDistance)
    {
        zOffset = 0;
        transitionDistance = 0f;

        if (user is not { } shooter)
            return false;

        var shooterMapUid = Transform(shooter).MapUid;
        if (shooterMapUid is null || !_zLevels.TryGetMapNetwork(shooterMapUid.Value, out _))
            return false;

        if (Vector2.Distance(from.Position, to) < MinZShotDistance)
            return false;

        var lookUp = TryComp<CEZLevelViewerComponent>(shooter, out var viewer) && viewer.LookUp;

        var offset = lookUp ? 1 : -1;
        EntityUid openingMap;
        if (lookUp)
        {
            if (!_zLevels.TryMapUp(shooterMapUid.Value, out var aboveMap))
                return false;

            openingMap = aboveMap.Owner;
        }
        else
        {
            if (!_zLevels.TryMapDown(shooterMapUid.Value, out _))
                return false;

            openingMap = shooterMapUid.Value;
        }

        if (!TryFindOpeningPoint(openingMap, from.Position, to, out var openingPoint))
            return false;

        var transitionDist = Vector2.Distance(from.Position, openingPoint);
        if (transitionDist < MinTransitionDistance)
            return false;

        zOffset = offset;
        transitionDistance = transitionDist;
        return true;
    }

    private bool TryFindOpeningPoint(EntityUid mapUid, Vector2 from, Vector2 to, out Vector2 openingPoint)
    {
        openingPoint = default;

        if (!_mapManager.TryFindGridAt(mapUid, to, out var gridUid, out var grid) &&
            !_mapManager.TryFindGridAt(mapUid, from, out gridUid, out grid))
            return false;

        var startTile = _map.WorldToTile(gridUid, grid, from);
        var endTile = _map.WorldToTile(gridUid, grid, to);

        if (startTile == endTile)
            return false;

        var dx = Math.Abs(endTile.X - startTile.X);
        var dy = Math.Abs(endTile.Y - startTile.Y);
        var sx = startTile.X < endTile.X ? 1 : -1;
        var sy = startTile.Y < endTile.Y ? 1 : -1;
        var err = dx - dy;

        var current = startTile;
        var first = true;
        while (true)
        {
            if (!first && IsOpeningTile(gridUid, grid, current))
            {
                openingPoint = GetTileCenterWorld(gridUid, grid, current);
                if (Vector2.Distance(from, openingPoint) >= MinTransitionDistance)
                    return true;
            }
            first = false;

            if (current == endTile)
                break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                current.X += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                current.Y += sy;
            }
        }

        return false;
    }

    private bool IsOpeningTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return true;

        return ((ContentTileDefinition)_tileDef[tileRef.Tile.TypeId]).Transparent;
    }

    private Vector2 GetTileCenterWorld(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        return _map.ToCenterCoordinates(gridUid, tile, grid).ToMapPos(EntityManager, _transform);
    }
}
