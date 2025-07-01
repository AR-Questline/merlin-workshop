using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle {
    [Serializable]
    public partial struct IdlePosition {
        public ushort TypeForSerialization => SavedTypes.IdlePosition;

        [Saved] public Space space;
        [Saved] public Vector3 position;

#pragma warning disable CS0612
        public readonly Space IdleSpace => space == Space.DEPRECATED_CurrentLoc ? Space.NpcSpawn : space;
#pragma warning restore CS0612
        
        public IdlePosition(Space space, Vector3 position) {
            this.space = space;
            this.position = position;
        }

        public static IdlePosition Self => new(Space.NpcSpawn, Vector3.zero);
        public static IdlePosition NpcSpawn => new(Space.NpcSpawn, Vector3.zero);
        public static IdlePosition World(Vector3 position) => new(Space.World, position);

        public enum Space : byte {
            [Obsolete] DEPRECATED_CurrentLoc,
            NpcSpawn,
            World,
        }

        public readonly Vector3 WorldPosition(Location location, IIdleDataSource data = null) {
            return IdleSpace switch {
                Space.NpcSpawn => UseAttachmentSpace(data) ? ByAttachment(data, position) : BySpawn(location, position),
                Space.World => position,
                _ => throw new ArgumentOutOfRangeException()
            };
            static Vector3 BySpawn(Location location, Vector3 position) {
                return Ground.SnapNpcToGround(location.SpecInitialPosition + position + Vector3.up);
            }

            static Vector3 ByAttachment(IIdleDataSource data, Vector3 position) {
                return Ground.SnapNpcToGround((data?.AttachmentPosition ?? Vector3.zero) + position + Vector3.up);
            }
        }
        
        public readonly Vector3 WorldForward(Location location, IIdleDataSource data = null) {
            return IdleSpace switch {
                Space.NpcSpawn => UseAttachmentSpace(data) ? ByAttachment(data) : BySpawn(location),
                Space.World => position,
                _ => throw new ArgumentOutOfRangeException()
            };
            static Vector3 BySpawn(Location location) => location.SpecInitialForward;
            static Vector3 ByAttachment(IIdleDataSource data) => data?.AttachmentForward ?? Vector3.forward;
        }
        
        static bool UseAttachmentSpace(IIdleDataSource data) {
            return data is { UseAttachmentSpace: true };
        }
    }
}