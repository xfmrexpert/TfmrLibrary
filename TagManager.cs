using GeometryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public enum TagType
    {
        None,
        Axis,
        TopYoke,
        BottomYoke,
        OuterBoundary,
        ConductorBoundary,
        ConductorSurface,
        InsulationBoundary,
        InsulationSurface
    }

    // Immutable composite key for the winding location
    public readonly record struct LocationKey(
        int WindingId, int SegmentId, int TurnNumber, int StrandNumber);

    public sealed class EntityIndex
    {
        // Outer: location -> Inner: tag -> entity
        private readonly Dictionary<LocationKey, Dictionary<TagType, int>> _map = new();

        // Pick your string semantics; OrdinalIgnoreCase if you want case-insensitive lookups.
        private static Dictionary<TagType, int> NewInner() =>
            new();

        public void Add(LocationKey loc, TagType type, int entity)
        {
            if (!_map.TryGetValue(loc, out var inner))
            {
                inner = NewInner();
                _map[loc] = inner;
            }
            inner[type] = entity;
        }

        public bool TryGet(LocationKey loc, TagType type, out int? entity)
        {
            entity = null;
            if (_map.TryGetValue(loc, out var inner) && inner.TryGetValue(type, out var value))
            {
                entity = value;
                return true;
            }
            return false;
        }

        public bool TryGetLocationByTag(int tag, out LocationKey loc, out TagType type)
        {
            loc = default;
            type = TagType.None;
            foreach (var kvp in _map)
            {
                foreach (var innerKvp in kvp.Value)
                {
                    if (innerKvp.Value == tag)
                    {
                        loc = kvp.Key;
                        type = innerKvp.Key;
                        return true;
                    }
                }
            }
            return false;
        }

        public IReadOnlyDictionary<TagType, int> GetAll(LocationKey loc) =>
            _map.TryGetValue(loc, out var inner) ? inner : Empty;

        private static readonly IReadOnlyDictionary<TagType, int> Empty =
            new Dictionary<TagType, int>(0);
    }

    public class TagManager
    {
        private int nextTag = 0;
        private Dictionary<int, GeomEntity> tagToEntity = new();
        private Dictionary<string, int> entityStringIndex = new();
        private EntityIndex entityLocationIndex = new();

        public int TagEntityByLocation(GeomEntity entity, LocationKey loc, TagType type)
        {
            int tag = 0;
            if (tagToEntity.ContainsValue(entity))
                tag = tagToEntity.First(kv => kv.Value == entity).Key;
            else
                tag = nextTag++;
            entity.Tag = tag;
            tagToEntity[tag] = entity;
            entityLocationIndex.Add(loc, type, tag);
            return tag;
        }

        public int TagEntityByString(GeomEntity entity, string key)
        {
            int tag = 0;
            if (tagToEntity.ContainsValue(entity))
                tag = tagToEntity.First(kv => kv.Value == entity).Key;
            else
                tag = nextTag++;
            entity.Tag = tag;
            tagToEntity[tag] = entity;
            entityStringIndex.Add(key, tag);
            return tag;
        }

        public bool TryGetEntityByLocation(LocationKey loc, TagType type, out GeomEntity? entity)
        {
            entity = null;
            if (entityLocationIndex.TryGet(loc, type, out var tag) && tag.HasValue && tagToEntity.TryGetValue(tag.Value, out var ent))
            {
                entity = ent;
                return true;
            }
            return false;
        }

        public bool TryGetEntityByString(string key, out GeomEntity? entity)
        {
            entity = null;
            if (entityStringIndex.TryGetValue(key, out var tag) && tagToEntity.TryGetValue(tag, out var ent))
            {
                entity = ent;
                return true;
            }
            return false;
        }

        public bool TryGetLocationByTag(int index, out LocationKey loc, out TagType type)
        {
            return entityLocationIndex.TryGetLocationByTag(index, out loc, out type);
        }

        public void ClearTags()
        {
            nextTag = 0;
            tagToEntity.Clear();
            entityStringIndex.Clear();
            entityLocationIndex = new EntityIndex();
        }
    }
}
