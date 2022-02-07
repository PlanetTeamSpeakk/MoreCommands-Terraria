using System;
using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Exceptions;
using Microsoft.Xna.Framework;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ID;

namespace MoreCommands.ArgumentTypes.Entities;

public class EntitySelector
{
    private static readonly SimpleCommandExceptionType NotAllowedException = new(new LiteralMessage("Selector not allowed"));
    private static readonly SimpleCommandExceptionType EntityNotFoundException = new(new LiteralMessage("No entity was found"));
    private static readonly SimpleCommandExceptionType TooManyEntitiesException = new(new LiteralMessage("Only one entity is allowed, but the provided selector allows more than one"));
    private static readonly SimpleCommandExceptionType PlayerNotFoundException = new(new LiteralMessage("No player was found"));
    public int Limit { get; }
    public bool IncludesNonPlayers { get; }
    public bool SenderOnly { get; }
    private readonly Predicate<Entity> _basePredicate;
    private readonly FloatRange _distance;
    private readonly Func<Vector2, Vector2> _positionOffset;
    private readonly Action<Vector2, List<Entity>> _sorter;
    private readonly string _playerName;
    private readonly bool _usesAt;

    public EntitySelector(int count, bool includesNonPlayers, Predicate<Entity> basePredicate, FloatRange distance, Func<Vector2, Vector2> positionOffset, 
        Action<Vector2, List<Entity>> sorter, bool senderOnly, string playerName, bool usesAt)
    {
        Limit = count;
        IncludesNonPlayers = includesNonPlayers;
        _basePredicate = basePredicate;
        _distance = distance;
        _positionOffset = positionOffset;
        _sorter = sorter;
        SenderOnly = senderOnly;
        _playerName = playerName;
        _usesAt = usesAt;
    }

    private void CheckSourcePermission(CommandSource source)
    {
        if (_usesAt && !source.IsOp)
            throw NotAllowedException.Create();
    }

    public Entity GetEntity(CommandSource source)
    {
        CheckSourcePermission(source);

        List<Entity> list = GetEntities(source);

        return list.Count switch
        {
            0 => throw EntityNotFoundException.Create(),
            > 1 => throw TooManyEntitiesException.Create(),
            _ => list[0]
        };
    }

    public List<Entity> GetEntities(CommandSource source)
    {
        CheckSourcePermission(source);

        if (!IncludesNonPlayers)
            return GetPlayers(source).Select(player => (Entity) player).ToList();

        if (_playerName != null)
        {
            Player player = Main.player.FirstOrDefault(player => player.name.Equals(_playerName, StringComparison.OrdinalIgnoreCase));
            return player == null ? new List<Entity>() : Util.Singleton((Entity) player).ToList();
        }

        Vector2 posOffset = _positionOffset(source.Pos);
        Predicate<Entity> posPredicate = GetPositionPredicate(posOffset);

        if (SenderOnly)
            return source.IsPlayer && posPredicate(source.Player) ? Util.Singleton((Entity) source.Player).ToList() : new List<Entity>();

        List<Entity> entities = ((IEnumerable<Entity>) Main.player.Where(p => p.active)).Concat(Main.npc.Where(npc => npc.type != NPCID.None))
            .Where(entity => posPredicate(entity)).ToList();

        return GetEntities(posOffset, entities);
    }

    public Player GetPlayer(CommandSource source)
    {
        CheckSourcePermission(source);

        List<Player> list = GetPlayers(source);
        if (list.Count != 1)
            throw PlayerNotFoundException.Create();

        return list[0];
    }

    public List<Player> GetPlayers(CommandSource source)
    {
        CheckSourcePermission(source);

        if (_playerName != null)
        {
            Player player = Main.player.FirstOrDefault(player => player.name == _playerName);
            return player == null ? new List<Player>() : Util.Singleton(player).ToList();
        }

        Vector2 posOffset = _positionOffset(source.Pos);
        Predicate<Entity> posPredicate = GetPositionPredicate(posOffset);

        if (SenderOnly)
            return source.IsPlayer && posPredicate(source.Player) ? Util.Singleton(source.Player).ToList() : new List<Player>();

        List<Player> players = Main.player.Where(player => player.active && posPredicate(player)).ToList();
        return GetEntities(posOffset, players);
    }

    private Predicate<Entity> GetPositionPredicate(Vector2 pos)
    {
        Predicate<Entity> predicate = _basePredicate;
        if (_distance.From == null && _distance.To == null) return predicate;
        
        Predicate<Entity> old = predicate;
        predicate = entity => old(entity) && (_distance.From * 16 ?? 0) <= entity.Distance(pos) && (_distance.To * 16 ?? float.MaxValue) >= entity.Distance(pos);
        
        return predicate;
    }

    private List<T> GetEntities<T>(Vector2 pos, List<T> entities) where T : Entity
    {
        if (entities.Count > 1)
            _sorter(pos, entities.Select(entity => (Entity) entity).ToList());

        return entities.GetRange(0, Math.Min(Limit, entities.Count));
    }
}
