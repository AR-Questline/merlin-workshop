# Drake Material
To render small static geometry the game uses custom system named Drake. In this tutorial we'll show you how to replace material used in it.

1. Find the material you'd like to replace and create similar asset in project.
2. Modify it however you want. You can change its numerical values or textures.
3. Go to its location in file explorer and open its .meta file
4. Change guid in .meta file to match the one in [guids](../Resources/guids.txt)
5. In Unity on top toolbar select `Window/Asset Management/Addressables/Groups` to open Addressables Groups window
6. Drag and drop your material to Default Local Group
7. Build mod with top toolbar `TG/Modding/Build`
8. Try if mod is working in game 


<br/>
<p align="center">
    <video src="../Resources/tut-ragdoll-force.mp4" controls width="600">
    Your browser does not support the video tag.
    </video>
</p>
<br/>

<div style="display: flex; justify-content: space-between; width: 100%;">
  <a href="../modding-overview.md" style="text-decoration: none;">⬅️ Previous: Modding Overview</a>
  <a href="ragdoll-force.md" style="text-decoration: none;">Next: Ragdoll Force ➡️</a>
</div>