using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    public abstract class FindCharactersUnit : ARUnit {
        protected override void Definition() {
            var inCharacter = FallbackARValueInput("character", flow => flow.stack.self);
            var inCenter = OptionalARValueInput<Vector3>("center");
            var inRange = OptionalARValueInput<float>("range");
            
            ValueOutput(CharacterAlias, flow => {
                var character = VGUtils.GetModel<ICharacter>(inCharacter.Value(flow));

                IEnumerable<ICharacter> characters;
                if (inCenter.HasValidConnection) {
                    characters = FindCharacters(character).InRange(inCenter.Value(flow), inRange.Value(flow));
                } else if (inRange.HasValidConnection) {
                    characters = FindCharacters(character).InRange(character.Coords, inRange.Value(flow));
                } else {
                    characters = FindCharacters(character);
                }

                return characters.Select(c => c.CharacterView.transform).ToArray();
            });
        }

        protected abstract string CharacterAlias { get; }
        protected abstract IEnumerable<ICharacter> FindCharacters(ICharacter character);
    }
}
