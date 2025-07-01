using System;
using System.Collections.Generic;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Stories.Core {
    public class NodeType : RichEnum {
        public string Name { get; }
        public Type Type { get; }
        public IEnumerable<Type> InitialSteps { get; }

        [UnityEngine.Scripting.Preserve]
        public static readonly NodeType
            Default = new NodeType(nameof(Default), "0000/Node", typeof(ChapterEditorNode), new Type[0]),
            Text = new NodeType(nameof(Text), "0001/Text", typeof(ChapterEditorNode), new[] { typeof(SEditorText) }),
            SimpleComment = new NodeType(nameof(SimpleComment), "0002/Task Comment", typeof(TaskNode), new Type[0]),
            Choice = new NodeType(nameof(Choice), "0003/Choice", typeof(ChapterEditorNode), new[] { typeof(SEditorChoice) }),
            
            Blank = new NodeType(nameof(Blank), "0010/      ", null, null),
            
            Branch = new NodeType(nameof(Branch), "0011/Branch", typeof(ChapterEditorNode), new[] { typeof(SEditorNodeJump) }),
            GraphJump = new NodeType(nameof(GraphJump), "0012/Graph Jump", typeof(ChapterEditorNode), new[] { typeof(SEditorGraphJump) }),
            Bookmark = new NodeType(nameof(Bookmark), "0013/Bookmark", typeof(ChapterEditorNode), new[] { typeof(SEditorBookmark) }),
            FlagChange = new NodeType(nameof(FlagChange), "0014/Flag: Change", typeof(ChapterEditorNode), new[] { typeof(SEditorFlagChange) }),
            
            Blank2 = new NodeType(nameof(Blank2), "0020/      ", null, null),
            // And node - 0021
            // Or node - 0022
            FlagCheck = new NodeType(nameof(FlagCheck), "0023/Flag: Check", typeof(AndEditorNode), new[] { typeof(CEditorFlag) }),
            // Space for attribute nodes
            
            Blank3 = new NodeType(nameof(Blank3), "0030/      ", null, null),
            
            StoryStartChoice = new NodeType(nameof(StoryStartChoice), "0031/Story Start Choice", typeof(ChapterEditorNode), new[] { typeof(SEditorStoryStartChoice) }),
            StatChange = new NodeType(nameof(StatChange), "0031/Stat: Change", typeof(ChapterEditorNode), new[] { typeof(SEditorStatChange) }),
            VariableChange = new NodeType(nameof(VariableChange), "0031/Variable: Set", typeof(ChapterEditorNode), new[] { typeof(SEditorVariableSet) }),
            VariableAdd = new NodeType(nameof(VariableAdd), "0031/Variable: Add", typeof(ChapterEditorNode), new[] { typeof(SEditorVariableAdd) }),
            Comment = new NodeType(nameof(Comment), "0066/Legacy Comment", typeof(CommentNode), new Type[0]);

        protected NodeType(string enumName, string name, Type type, IEnumerable<Type> steps) : base(enumName) {
            Name = name;
            Type = type;
            InitialSteps = steps;
        }
    }
}