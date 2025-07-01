using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class ItemInSlotsConverter : JsonConverter<ItemInSlots> {
        const string SlotsCountName = "SlotsCount";
        const string ItemsName = "Items";
        const string IndexName = "Index";
        const string ItemCount = "Item";

        public static void Write(JsonWriter writer, ItemInSlots value, JsonSerializer serializer) {
            var items = ItemInSlots.SerializationAccess.ItemsBySlot(ref value);
            var slotsCount = items.Length;
            writer.WriteStartObject();
            writer.WritePropertyName(SlotsCountName);
            writer.WriteValue(slotsCount);
            writer.WritePropertyName(ItemsName);
            writer.WriteStartArray();
            for (int i = 0; i < slotsCount; i++) {
                Item item = items[i];
                if (item != null) {
                    writer.WriteStartObject();
                    writer.WritePropertyName(IndexName);
                    writer.WriteValue(i);
                    writer.WritePropertyName(ItemCount);
                    serializer.Serialize(writer, item);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override void WriteJson(JsonWriter writer, ItemInSlots value, JsonSerializer serializer) {
            Write(writer, value, serializer);
        }

        public override ItemInSlots ReadJson(JsonReader reader, Type objectType, ItemInSlots existingValue, bool hasExistingValue, JsonSerializer serializer) {
            JObject jObject = JObject.Load(reader);
            var slotsCount = jObject[SlotsCountName].ToObject<int>();
            slotsCount = math.max(slotsCount, EquipmentSlotType.All.Length);
            var itemsArray = new Item[slotsCount];
            foreach (var item in jObject[ItemsName]) {
                var index = item[IndexName].ToObject<int>();
                var itemObject = item[ItemCount].ToObject<Item>(serializer);
                itemsArray[index] = itemObject;
            }
            return new ItemInSlots(itemsArray);
        }
    }
}