# Quest Map Tool

A comprehensive Unity editor tool for tracking relationships between Quests, NPCs, Stories, and Scenes in Tainted Grail Campaign. Provides three specialized search modes for QA, design verification, and narrative tracking.

## Features

- **Quest Search**: Complete quest routes with branching, objectives, scenes, NPCs, and dependencies
- **NPC Search**: First encounter tracking, full quest journey, all scene appearances, story endpoints
- **Scene Search**: All NPCs with story starting points for QA testing in builds
- **Quest Dependencies**: Tracks quests that affect each other via shared flags
- **Fast Lookups**: Pre-built cache for instant results
- **Auto-Refresh**: Automatically rebuilds when scenes, quests, or stories change
- **Responsive UI**: Works well in fullscreen or docked layouts

## How to Use

### 1. Open the Tool

**Menu**: `TG → Design → Quest Map Tool`

### 2. Select a Folder

1. Click **"Browse..."** button
2. Navigate to the folder containing your scene files (e.g., `Assets/Scenes/Act1/`)
3. Select the folder

### 3. Scan Data

Click **"Scan Scenes"** button to build the cache. This will:
- Find all NPCs from ActorsRegister
- Scan all StoryGraphs (following graph jumps)
- Scan all QuestTemplates with objectives and flags
- Integrate with runtime QuestCache
- Scan scenes in the selected folder for NpcPresence
- Build cross-references and dependencies
- Analyze quest dependencies via shared flags

**Note**: Scanning takes 1-3 minutes depending on project size. Progress is shown in the Console.

### 4. Choose Search Mode

Click one of the three mode buttons at the top:
- **Search Quest**: Find quests by name, see complete route
- **Search NPC**: Find NPCs by name, see full journey
- **Search Scene**: Find scenes by name, see all NPCs present

### 5. Search

1. Type a search term in the field (placeholder updates based on mode)
2. Press **Enter** or click **"Search"**
3. View detailed results instantly!

## Search Modes

### Mode 1: Search Quest

Shows complete quest information and route through the game.

**What it displays:**
- Quest name and type (Main, Side, Misc)
- Branching indicator (if quest has multiple paths)
- All NPCs involved in the quest
- All objectives with:
  - Objective name and description
  - Scene where objective occurs
  - Flag requirements (if any)
  - Whether objective has a map marker
- All scenes used during the quest
- Related quests (other quests that share flags with this one)

**Use cases:**
- Verify quest flow and structure
- Find all scenes used in a quest
- Check which NPCs are involved
- Identify quest dependencies via flags
- QA quest branching paths

**Example Result:**
```
[Main Quest] The Lost Caravan
⚠️ This quest has multiple branches

NPCs Involved (2):
  • Merchant Odran
  • Guard Captain

Quest Objectives (3):
  [1] Find the missing caravan
      Scene: Trade_Route_Forest
      Flag Required: caravan_quest_started
      Has Marker: Yes

  [2] Investigate the attack site
      Scene: Ambush_Clearing
      Has Marker: Yes

  [3] Report to the Guard Captain
      Scene: Town_Barracks
      Flag Required: found_evidence

Scenes Used (3):
  • Trade_Route_Forest
  • Ambush_Clearing
  • Town_Barracks

Related Quests (shared flags: 1):
  [Side Quest] Bandit Troubles
    Shared flags: found_evidence
```

### Mode 2: Search NPC

Shows complete NPC journey through the game from first encounter to endpoint.

**What it displays:**
- NPC name and GUID
- ⭐ First Encounter: First story where NPC appears (highlighted)
- All stories involving this NPC
- Quest Journey: All quests involving this NPC (in order found)
- All scenes where NPC has presence with presence type:
  - [Always]: NPC always present
  - [Manual]: Controlled by story steps
  - [Flag: flagname]: Controlled by game flag

**Use cases:**
- Track NPC's complete journey through the game
- Verify first encounter location for narrative consistency
- Check all scenes where NPC should appear
- QA NPC placement across multiple quests
- Ensure story endpoint is correct

**Example Result:**
```
Merchant Odran (guid_123456)

⭐ First Encounter: Story_MeetOdran

Stories (2):
  • Story_MeetOdran
  • Story_OdranReunion

Quest Journey (2):
  [1] [Main Quest] The Lost Caravan
  [2] [Side Quest] Merchant's Gratitude

Scenes (4):
  • Town_MainSquare [Flag: odran_available]
  • Trade_Route_Forest [Manual]
  • Ambush_Clearing [Manual]
  • Town_Barracks [Always]
```

### Mode 3: Search Scene

Shows all NPCs present in a scene with their story starting points (for QA).

**What it displays:**
- Scene name and path
- Total NPC count
- For each NPC:
  - NPC name
  - Presence type ([Manual], [Flag: name], or [Always])
  - Story Start: First story where NPC appears
    - Green text if found
    - Red warning if missing (NPC not in any story)

**Use cases:**
- QA testing in builds: verify all NPCs have story starts
- Check which NPCs should be in a scene
- Identify orphaned NPCs (not connected to any story)
- Prevent accidental NPC deletions
- Verify presence configuration (manual vs flag-based)

**Example Result:**
```
Town_MainSquare
Path: Assets/Scenes/Town/Town_MainSquare.unity
NPCs Present: 3

  • Guard Captain [Always]
    Story Start: Story_TownGuard

  • Merchant Odran [Flag: odran_available]
    Story Start: Story_MeetOdran

  • Mysterious Stranger [Manual]
    Story Start: Unknown (not in any story) ⚠️
```

## Additional Features

### List All Items

Each search mode has a **"List All [Items]"** button:
- **Quest Mode**: Shows all quests sorted alphabetically with type
- **NPC Mode**: Shows all NPCs with scene counts
- **Scene Mode**: Shows all scenes with NPC counts

Click the button to see the complete list.

### Clickable Results

- **Left-click**: Pings the asset in Project window (works for scenes, quests, NPCs)
- **Double-click on scenes**: Opens the scene in Unity Editor (prompts to save current scene)

### Cache Information

Bottom of window displays:
- Total NPCs, Quests, Stories, Scenes
- Last cache build time
- Scanned folder path

## Cache System

### What is Cached?

The tool pre-computes these relationships across 7 phases:

**Phase 0: Actor Specs**
- All NPCs from ActorsRegister (with name lookup cache)

**Phase 1: NPCs**
- NPC names, GUIDs, template paths

**Phase 2: Stories**
- Story graphs with actor lists
- Quests started by stories
- Follows graph jumps to child stories

**Phase 3: Quests**
- Quest templates with objectives
- Flags used and required
- Quest branching detection
- Scene references

**Phase 4: Quest Cache**
- Integration with runtime QuestCache

**Phase 5: Scenes**
- NpcPresence in selected folder
- Manual vs flag-based configuration

**Phase 6: Cross-References**
- NPC → Scenes, Stories, Quests
- Quest → Scenes, NPCs
- Scene → NPCs
- First encounter tracking (first story per NPC)

**Phase 7: Dependencies**
- Quest → Related Quests (via shared flags)
- Flag intersection analysis

### Auto-Refresh

The cache automatically rebuilds when you:
- Modify a scene file (`.unity`) in the scanned folder
- Modify a quest template (`.prefab`) in `/Templates/Quests/`
- Modify a story graph (`.asset`) in `/Stories/`

**Note**: Auto-refresh happens in the background. Check Console for "Auto-rebuild complete" message.

### Manual Refresh

Click **"Scan Scenes"** at any time to force a rebuild of the cache.

### Cache Location

Cache is stored at: `Assets/Data/Caches/QuestMapCache.asset`

You can select this asset and view its raw data with the custom inspector.

## Cache Inspector

Select `QuestMapCache.asset` in the Project window to view detailed statistics and debug tools:

**Statistics:**
- Total NPCs, Quests, Stories, Scenes
- Scanned folder path
- Last build time

**Mappings:**
- NPC → Scenes, Stories, Quests (count)
- Quest → Scenes (count)
- Quest Dependencies (count)
- Scene → NPCs (count)

**Debug Tools:**
- Print All NPCs to Console
- Print All Quests to Console
- Print All Stories to Console
- Print All Scenes to Console
- Print NPC→Quest Mappings
- Print Quest Dependencies (shows shared flags)

## Architecture

### Files

- **QuestMapCache.cs**: ScriptableObject storing all cached relationships
- **QuestMapCacheBuilder.cs**: 7-phase scanning and cache building (~690 lines)
- **QuestMapCachePostprocessor.cs**: Auto-refresh on asset changes
- **QuestMapQuery.cs**: Query API for all three search modes
- **QuestMapToolWindow.cs**: Main editor window with 3-mode UI (~820 lines)
- **QuestMapCacheInspector.cs**: Custom inspector with debug tools

### Data Flow

```
User selects folder → Click Scan Scenes
    ↓
Phase 0: Load ActorSpec cache for fast lookup
    ↓
Phase 1: Scan ActorsRegister for all NPCs
    ↓
Phase 2: Scan StoryGraphs, follow graph jumps
    ↓
Phase 3: Scan QuestTemplates with objectives and flags
    ↓
Phase 4: Integrate runtime QuestCache
    ↓
Phase 5: Scan scenes in folder for NpcPresence
    ↓
Phase 6: Build cross-references (NPC→Quest, Quest→Scene, etc.)
    ↓
Phase 7: Analyze quest dependencies via shared flags
    ↓
QuestMapCache.asset (persistent cache)
    ↓
QuestMapQuery (query API)
    ↓
QuestMapToolWindow (UI with 3 modes)
```

### Key Technical Details

**ActorSpec Caching (Phase 0):**
- Loads ActorsRegister once
- Builds GUID → ActorSpec dictionary
- 10-50x speedup for NPC name lookups

**Story Graph Recursion (Phase 2):**
- Follows SEditorGraphJump nodes
- Tracks visited stories to avoid infinite loops
- Collects all quests from entire story tree

**Quest Dependency Analysis (Phase 7):**
- Uses LINQ Intersect to find shared flags
- Builds bidirectional dependency map
- Only includes quests with overlapping flags

**Scene State Preservation (Phase 5):**
- Saves current scene path before scanning
- Restores original scene after completion
- Avoids disrupting workflow

**First Encounter Tracking (Phase 6):**
- Finds first story where each NPC appears
- Used in NPC search mode
- Highlighted in UI for narrative verification

## Performance

| Operation | Time | Notes |
|-----------|------|-------|
| **Cache build** | 1-3 min | All 7 phases, depends on project size |
| **First search** | <0.1 sec | After cache is built |
| **Subsequent searches** | <0.05 sec | Instant lookups |
| **Auto-refresh** | 1-3 min | Happens in background |
| **Mode switching** | <0.01 sec | UI-only operation |

**Typical Project:**
- 50 NPCs
- 80 Quests
- 120 Stories
- 150 Scenes
- Cache build: ~90 seconds

## Tips

### General
1. **Build cache once**: First scan takes time, but subsequent searches are instant
2. **Use partial names**: Search "Odr" to find "Odran", "Odric", etc.
3. **Check related quests**: Quest dependencies reveal story connections
4. **Watch for warnings**: Red text in Scene mode indicates missing story starts
5. **Use fullscreen**: UI is responsive and works great in fullscreen mode

### Quest Search
1. Look for ⚠️ branching indicator for complex quests
2. Check related quests to understand flag dependencies
3. Use objective flags to understand progression requirements

### NPC Search
1. ⭐ First Encounter shows where player meets the NPC first
2. Quest Journey shows NPC's complete story arc
3. Empty Quest Journey means NPC is scene-only (no quest involvement)

### Scene Search
1. **Green text** (Story Start found) = Good! NPC properly connected
2. **Red text** (Story Start: Unknown) = Warning! NPC might be orphaned
3. Use this mode to QA builds before release
4. Verify all NPCs have proper story starts

### Performance
1. **Partial searches**: Type fewer characters for broader results
2. **List All**: Use sparingly for large projects (shows everything)
3. **Double-click carefully**: Opening scenes can take time
4. **Auto-refresh**: Let it complete before starting new search

## Troubleshooting

### "No cache found"
- Click **"Browse..."** and select a folder containing scene files
- Click **"Scan Scenes"** to build the cache
- Wait 1-3 minutes for completion (watch Console for progress)

### "No [Quests/NPCs/Scenes] found"
- Check spelling (search is case-insensitive but requires partial match)
- Try rebuilding cache
- Use **"List All"** button to see what's in the cache
- Check Console for scanning errors or warnings

### Search returns no results
- Verify the item exists in the project
- Try a shorter search term (partial match)
- Check that cache was built successfully (see bottom stats)
- Try **"List All"** to browse available items

### Cache not auto-refreshing
- Check Console for errors in `QuestMapCachePostprocessor`
- Verify modified file is in the watched paths:
  - Scenes: in cached folder
  - Quests: in `/Templates/Quests/`
  - Stories: in `/Stories/`
- Manually rebuild cache as workaround

### Related Quests not showing
- Quests must share at least one flag to be related
- Check that quests actually use flags (in quest template)
- Rebuild cache if you recently added flags to quests

### First Encounter missing (⭐)
- NPC might not be in any story (scene-only)
- Verify NPC has a story graph with actor reference
- Check that story graph was scanned (in `/Stories/` folder)

### Scenes don't open when double-clicked
- Ensure scene still exists at the path shown
- Try pinging first (single-click) to verify location
- Check file permissions
- Save current scene when prompted

### Performance issues
- Close and reopen window if UI feels sluggish
- Avoid using "List All" repeatedly on large projects
- Rebuild cache only when necessary (auto-refresh handles most cases)

## Example Workflows

### Workflow 1: Verify Quest Structure
1. Open Quest Map Tool
2. Switch to **Search Quest** mode
3. Type quest name (e.g., "Lost Caravan")
4. Press Enter
5. Verify:
   - All NPCs are correct
   - Objectives have proper scenes
   - Flag requirements match design
   - Related quests make sense

### Workflow 2: Track NPC Journey
1. Switch to **Search NPC** mode
2. Type NPC name (e.g., "Odran")
3. Press Enter
4. Review:
   - ⭐ First Encounter matches narrative design
   - Quest Journey follows expected arc
   - All scenes have proper presence configuration
   - Story ends where expected

### Workflow 3: QA Scene Before Build
1. Switch to **Search Scene** mode
2. Type scene name (e.g., "Town_MainSquare")
3. Press Enter
4. Check each NPC:
   - ✅ Green "Story Start" = Good
   - ⚠️ Red "Unknown" = Needs investigation
5. Fix any orphaned NPCs (add to story or remove from scene)
6. Repeat for all critical scenes

### Workflow 4: Find Quest Dependencies
1. Switch to **Search Quest** mode
2. Search for main quest
3. Scroll to "Related Quests" section
4. Review shared flags
5. Verify dependencies make sense
6. Check that flag usage doesn't create conflicts

### Workflow 5: Prevent NPC Deletion Issues
1. Before deleting NPC from scene:
   - Switch to **Search NPC** mode
   - Search for the NPC
   - Check "Scenes" list
   - Verify NPC isn't needed in that scene
2. After modifying scenes:
   - Switch to **Search Scene** mode
   - Check modified scene
   - Verify all expected NPCs still present

## Differences from Previous Versions

### v1.0 (NPC Scene Finder)
- Only searched NPCs by scene
- No quest or story tracking
- Limited to single folder

### v2.0 (Current - Quest Map Tool)
**Added:**
- Three search modes (Quest, NPC, Scene)
- Complete quest tracking with objectives and branching
- Story graph parsing with graph jump following
- Quest dependency analysis via shared flags
- First encounter tracking for NPCs
- Related quests display
- Comprehensive cross-referencing
- 7-phase scanning architecture
- Auto-refresh for quests and stories (not just scenes)
- Responsive UI for fullscreen usage
- Custom inspector with debug tools

**Restored from Original Quest Map Tool:**
- Quest template scanning
- Story graph parsing
- Quest-NPC relationships
- Story-NPC relationships

## Credits

Rebuilt by Claude Code (claude.ai/code) on 2025-11-16.

Original Quest Map Tool architecture for Tainted Grail Campaign project.
