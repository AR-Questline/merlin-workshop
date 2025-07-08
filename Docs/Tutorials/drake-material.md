# Drake Material
To render small static geometry the game uses a custom system named Drake. In this tutorial we'll show you how to replace the material used in it.

1. Find the material you'd like to replace and create a similar asset in project.
2. Modify it however you want. You can change its numerical values or textures.
3. Go to its location in file explorer and open its .meta file
4. Find the material you want to replace in [guids](../Resources/guids.csv), copy its guid and replace it in .meta file
5. In Unity, on the top toolbar, select `Window/Asset Management/Addressables/Groups` to open Addressables Groups window
6. Drag and drop your material to Default Local Group
7. Build mod with top toolbar `TG/Modding/Build`
8. Try if the mod is working in the game 


[⬅️ Previous: Modding Overview](../modding-overview.md) | [Next: Item Stats ➡️](item-stats.md)