using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class ListParser {
        public static void ClearList(ClearList clearList, FunctionScript script) {
            script.AddFlow($"{script.Variable(clearList.listInput)}.Clear();");
            if (clearList.listOutput.connections.Any()) {
                script.AddFlow($"{script.Type<IList>()} {script.Variable(clearList.listOutput)} = {script.Variable(clearList.listInput)};");
            }
        }

        public static void ListContainsItem(ListContainsItem contains, FunctionScript script) {
            script.AddFlow($"{script.Type(contains.contains)} {script.Variable(contains.contains)} = {script.Variable(contains.list)}.Contains({script.Variable(contains.item)});");
        }

        public static void AddListItem(AddListItem add, FunctionScript script) {
            script.AddFlow($"{script.Variable(add.listInput)}.Add({script.Variable(add.item)});");
            if (add.listOutput.validConnections.Any()) {
                script.AddFlow($"{script.Type(add.listOutput)} {script.Variable(add.listOutput)} = {script.Variable(add.listInput)};");
            }
        }

        public static void RemoveListItem(RemoveListItem remove, FunctionScript script) {
            script.AddFlow($"{script.Variable(remove.listInput)}.Remove({script.Variable(remove.item)});");
            if (remove.listOutput.validConnections.Any()) {
                script.AddFlow($"{script.Type(remove.listOutput)} {script.Variable(remove.listOutput)} = {script.Variable(remove.listInput)};");
            }
        }
        
        public static void RemoveListItemAt(RemoveListItemAt remove, FunctionScript script) {
            script.AddFlow($"{script.Variable(remove.listInput)}.RemoveAt({script.Variable(remove.index)});");
            if (remove.listOutput.validConnections.Any()) {
                script.AddFlow($"{script.Type(remove.listOutput)} {script.Variable(remove.listOutput)} = {script.Variable(remove.listInput)};");
            }
        }

        public static void CountItems(CountItems count, FunctionScript script) {
            script.AddUsing("System.Linq");
            script.AddFlow($"int {script.Variable(count.count)} = {script.Variable(count.collection)}.Count();");
        }

        public static void GetListItem(GetListItem getItem, FunctionScript script) {
            script.AddFlow($"var {script.Variable(getItem.item)} = {script.Variable(getItem.list)}[{script.Variable(getItem.index)}];");
        }
        
        public static void SetListItem(SetListItem setItem, FunctionScript script) {
            script.AddFlow($"{script.Variable(setItem.list)}[{script.Variable(setItem.index)}] = {script.Variable(setItem.item)};");
        }

        public static void CreateList(CreateList create, FunctionScript script) {
            script.AddFlow($"var {script.Variable(create.list)} = new {script.Type<List<object>>()}();");
            for (int i = 0; i < create.inputCount; i++) {
                script.AddFlow($"{script.Variable(create.list)}.Add({script.Variable(create.multiInputs[i])});");
            }
        }

        public static void InsertListItem(InsertListItem insert, FunctionScript script) {
            script.AddFlow($"{script.Variable(insert.listInput)}.Insert({script.Variable(insert.index)}, {script.Variable(insert.item)});");
            if (insert.listOutput.validConnections.Any()) {
                script.AddFlow($"{script.Type(insert.listOutput)} {script.Variable(insert.listOutput)} = {script.Variable(insert.listInput)};");
            }
        }

        public static void MergeLists(MergeLists merge, FunctionScript script) {
            script.AddFlow($"var {script.Variable(merge.list)} = new {script.Type<List<object>>()}();");
            for (int i = 0; i < merge.inputCount; i++) {
                script.AddFlow($"{script.Variable(merge.list)}.AddRange({script.Variable(merge.multiInputs[i])});");
            }
        }
    }
}