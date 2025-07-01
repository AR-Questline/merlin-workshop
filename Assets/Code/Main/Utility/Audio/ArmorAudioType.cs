using System;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using FMODUnity;

namespace Awaken.TG.Main.Utility.Audio {
    public class ArmorAudioType : ARAudioType<Item> {
        protected ArmorAudioType(string id, Func<Item, EventReference> getter, string inspectorCategory = "") : base(id, getter, inspectorCategory) { }

        [UnityEngine.Scripting.Preserve]
        public static readonly ArmorAudioType
            FootStep = new(nameof(FootStep), i => GetAudioContainer(i).footStep),
            BodyMovement = new(nameof(BodyMovement), i => GetAudioContainer(i).bodyMovement),
            BodyMovementFast = new(nameof(BodyMovementFast), i => GetAudioContainer(i).bodyMovementFast),
            EquipArmor = new(nameof(EquipArmor), i => GetAudioContainer(i).equipArmor),
            UnEquipArmor = new(nameof(UnEquipArmor), i => GetAudioContainer(i).unEquipArmor);

        static ItemAudioContainer GetAudioContainer(Item item) => item?.TryGetElement<ItemAudio>()?.AudioContainer ?? CommonReferences.Get.AudioConfig.DefaultItemAudioContainer;
    }
}