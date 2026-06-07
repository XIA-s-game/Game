# Test Report

## Project: Magic Forest: Apprentice Wizard

---

## I. Test Overview

| Item | Content |
|------|---------|
| Test Version | v1.0 |
| Test Platform | Windows 11 / Unity 2022.3 LTS |
| Test Period | June 1, 2026 - June 7, 2026 |
| Test Personnel | Developer self-test + Peer testing |

---

## II. Test Scope

| Module | Test Content |
|--------|--------------|
| Chapter 1 | Tutorial, altar stone puzzle, save the fairy, portal activation |
| Chapter 2 | Hopscotch mini-game, maze exploration, quality quiz (10 questions) |
| Chapter 3 | Four-color key collection (Yellow/Red/Green/Blue), five mini-games, unlock door, treasure chest |
| Chapter 4 | Witch divination, feather collection, ladder climbing, mushroom jumping, old tree trial |
| Chapter 5 | Moonlit Glade, Granny Meriel dialogue, ending text display |
| Side Quests | Merchant quest, old tree side quest, fairy memory puzzle |
| Interaction System | Press E to interact, dialogue trigger, item pickup |
| UI System | Quest hints, interaction prompts, card game interface, ending text |
| Audio System | Background music, sound effects trigger, audio overlapping |
| Physics System | Character movement, collision detection, fall reset |

---

## III. Test Cases and Results

### 3.1 Chapter 1: Magic Forest

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Tutorial controls | Player learns WASD movement, jump, interact | Normal | ✅ Pass |
| Altar stone pushing | Push stones in correct order, altar activates | Normal | ✅ Pass |
| Save the fairy | Fairy appears after correct stone order | Normal | ✅ Pass |
| Obtain first page | Fairy gives spellbook page | Normal | ✅ Pass |
| Guard defeats monster | Animation plays, guard dialogue | Normal | ✅ Pass |
| Portal activation | Guard gives portal, enter Chapter 2 | Normal | ✅ Pass |
| Side quest: Puzzle | Complete puzzle, unlock fairy memory | Normal | ✅ Pass |
| Complete side quest first then main quest | Main quest interaction should trigger normally | Normal | ✅ Pass (fixed 6.5) |

### 3.2 Chapter 2: Forest Maze

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Hopscotch mini-game | Complete game to get entry ticket | Normal | ✅ Pass |
| Maze exploration | Player can find exit | Normal | ✅ Pass |
| Quality info collection | Maze contains hints about five qualities | Normal | ✅ Pass |
| Guard asks questions | 10-question UI pops up | Normal | ✅ Pass |
| Answer 8+ correctly | Obtain second spellbook page | Normal | ✅ Pass |
| Answer fewer than 8 correctly | No page, can retry | Normal | ✅ Pass |
| Barrier falls | Barrier disappears after obtaining page | Normal | ✅ Pass |

### 3.3 Chapter 3: Forest Treehouse

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Talk to passerby | Get four-color key clues | Normal | ✅ Pass |

**Yellow Key:**

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Talk to baker | Learn need for honey | Normal | ✅ Pass |
| Talk to bear | Bear asks for silver herb | Normal | ✅ Pass |
| Find silver herb | Can be picked up in scene | Normal | ✅ Pass |
| Give to bear | Obtain honey | Normal | ✅ Pass |
| Give to baker | Obtain yellow key | Normal | ✅ Pass |

**Red Key:**

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Piano memory game | Listen to sequence, play correctly | Normal | ✅ Pass |
| Wrong note retry | Can restart | Normal | ✅ Pass |
| Play correctly | Obtain red key | Normal | ✅ Pass |

**Green Key:**

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Card memory game | Four cards shuffled | Normal | ✅ Pass |
| Designated card type | Old man randomly selects one | Normal | ✅ Pass |
| Choose correctly | Obtain green key | Normal | ✅ Pass |
| Choose incorrectly | Can retry | Normal | ✅ Pass |

**Blue Key:**

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Level 1 (30 sec) | Stand on correct color square | Normal | ✅ Pass |
| Level 2 (20 sec) | Difficulty increases | Normal | ✅ Pass |
| Level 3 (15 sec) | Difficulty increases | Normal | ✅ Pass |
| Level 4 (10 sec) | Difficulty increases | Normal | ✅ Pass |
| Level 5 (5 sec) | Difficulty increases | Normal | ✅ Pass |
| Wrong square selection | Reset to start | Normal | ✅ Pass |
| Complete all 5 levels | Obtain blue key | Normal | ✅ Pass |

**Unlocking the Chest:**

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Collect all four keys | Can open locked door | Normal | ✅ Pass |
| Spellbook page in chest | Obtain third page | Normal | ✅ Pass |
| Portal appears | Enter Chapter 4 | Normal | ✅ Pass |

**Side Quest: Merchant's Trouble**

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Talk to merchant | Learn goods haven't arrived | Normal | ✅ Pass |
| Find worker and cart | Cart is broken | Normal | ✅ Pass |
| Find tools to repair | Tools can be picked up nearby | Normal | ✅ Pass |
| Report back to merchant | Obtain glowing berry | Normal | ✅ Pass |

### 3.4 Chapter 4: Forest Swamp

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Find Luna crying | Dialogue triggers | Normal | ✅ Pass |
| Find witch | Witch divination | Normal | ✅ Pass |
| Find four feathers | Can be picked up in swamp | Normal | ✅ Pass |
| Exchange feathers for fan | Witch gives clue | Normal | ✅ Pass |
| Climb ladder to get key | Obtain key from magpie nest | Normal | ✅ Pass |
| Mushroom jumping | Jump to Luna's home | Normal | ✅ Pass |
| Return key | Luna gives fourth page | Normal | ✅ Pass |

**Side Quest: Old Tree's Trial**

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Insult old tree | Attacked by old tree | Normal | ✅ Pass |
| Option A: Destroy eggs | Cursed and despised | Normal | ✅ Pass |
| Option B: Respect nature | Obtain reward | Normal | ✅ Pass |
| Option C: Help build nest | Collect 7 fences + 2 saplings | Normal | ✅ Pass |

### 3.5 Chapter 5: Moonlit Glade

| Test Case | Expected Result | Actual Result | Status |
|-----------|---|----------|------|
| Enter Moonlit Glade | Fireflies, floating steps | Normal | ✅ Pass |
| Granny Meriel dialogue | Voice appears | Normal | ✅ Pass |
| Player expresses frustration | Dialogue options | Normal | ✅ Pass |
| Granny lists qualities | References player's journey | Normal | ✅ Pass |
| Obtain fifth page | Page appears | Normal | ✅ Pass |
| Ending text display | Each line 5 seconds, sequential | Normal | ✅ Pass |
| Sequel teaser | Text prompt | Normal | ✅ Pass |

---

## IV. Bug and Issue Log

| Bug ID | Description | Severity | Status | Fix Date |
|--------|-------------|----------|--------|----------|
| B-01 | Mushroom jump game - can't fall down, collision issue | Medium | ✅ Fixed | June 3 |
| B-02 | Forest witch animation not displaying | Medium | ✅ Fixed | June 4 |
| B-03 | Completing side quest first then main quest in Chapter 1 causes interaction failure | High | ✅ Fixed | June 5 |
| B-04 | Some sound effects covered by background music | Low | ⚠️ Partially fixed | June 5 |
| B-05 | Ending text display timing inconsistent | Low | ✅ Fixed | June 6 |
| B-06 | Map missing key location markers | Medium | ⚠️ Pending | - |
| B-07 | Map shows only player real-time position, main interactive locations not marked, difficult for players to find target destinations | High | ⚠️ Pending | - |

---

## V. Test Conclusion

| Item | Conclusion |
|------|------------|
| Main Quest Line | ✅ Can complete fully |
| Side Quests | ✅ Can complete normally |
| Five Spellbook Pages | ✅ All obtainable |
| Four Keys | ✅ All obtainable |
| Interaction System | ✅ Mostly normal |
| UI System | ⚠️ Partial optimization needed |
| Audio System | ⚠️ Partial optimization needed |
| Physics/Collision | ⚠️ Partial clipping issues remain |
| Map System | ⚠️ Incomplete functionality, lacks guidance |

**Overall Assessment:** The core gameplay is complete. The main quest line can be fully completed. Puzzle designs are interesting. There are minor issues with clipping, audio, UI, and map guidance. The map issue is most critical for player experience and will be prioritized in the next version.

---

## VI. Optimization Checklist

| Category | Issue | Priority | Status |
|----------|-------|----------|--------|
| Physics | Character clips through bridges, ladders | High | Pending |
| Physics | Mushroom jump game - can't fall down, collision issue | High | ✅ Fixed |
| UI | Some Scene dialog box positions incorrect | Medium | Pending manual adjustment |
| Map | Shows only player real-time position, main interactive locations not marked | **Highest** | Pending |
| Map | Players have difficulty finding target destinations, easy to get lost | **Highest** | Pending |
| Audio | Some sound effects covered by background music | Low | Pending |
| Audio | Missing sound effects for key actions (open cage, flip cards, get keys) | Medium | Pending |
| Animation | Forest witch animation not displaying | Medium | ✅ Fixed |
| Story | Ending feels rushed, emotional impact insufficient | Medium | Pending (add flashbacks and dialogue) |

---

## VII. Detailed Optimization Explanations

### 7.1 Map Optimization (Highest Priority)

| Issue | Current State | Improvement Plan |
|-------|---------------|------------------|
| Shows only player real-time position | Map only has an icon representing the player, no other locations marked | Mark all main interactive locations on the map |
| Difficulty finding target destinations | Players don't know where bakery, bear cave, piano house, card house are | Add name labels and icons for each key location |
| Lack of guidance | Players often get lost, don't know which direction to go | Add quest target highlighting on the map with click-to-track functionality |

**Locations that need to be marked:**

| Location | Type | Priority |
|----------|------|----------|
| Bakery | Quest location (Yellow Key) | High |
| Bear Cave | Quest location (Get honey) | High |
| Piano House | Quest location (Red Key) | High |
| Card House | Quest location (Green Key) | High |
| Color Square Challenge | Quest location (Blue Key) | High |
| Locked House | Main quest target | High |
| Portal | Chapter transition point | Medium |
| Witch's Hut | Chapter 4 quest point | Medium |
| Old Tree | Side quest point | Low |
| Starting Point | Initial spawn location | Low |

### 7.2 UI Optimization

| Issue | Current State | Improvement Plan |
|-------|---------------|------------------|
| Dialog box positions | Some scenes have dialog boxes in wrong positions, blocking view or offset from characters | Manually adjust RectTransform positions scene by scene |

### 7.3 Audio Optimization

| Issue | Current State | Improvement Plan |
|-------|---------------|------------------|
| Sound overlapping | Multiple sounds playing simultaneously - important sounds covered by BGM | Use Unity Audio Mixer to group sounds (SFX, BGM, UI), adjust priorities |
| Missing sound effects | Actions like opening cage, flipping cards, getting keys have no sound | Select and add corresponding sound effects from library |

### 7.4 Ending Optimization

| Issue | Current State | Improvement Plan |
|-------|---------------|------------------|
| Rushed pacing | After Granny Meriel's dialogue, directly goes to black screen text | Add flashback scenes showing characters player helped, extend dialogue time |
| Insufficient emotion | Player's emotional response not deep enough | Add more dialogue lines for Granny, include reminiscence elements |

---

## VIII. Recommendations

1. **Map Optimization (Highest Priority)**: Mark all main interactive locations (bakery, bear cave, piano house, card house, color square challenge point, locked house, etc.) on the map with name labels and icons, enabling players to clearly find target destinations
2. **Fix Clipping Issues**: Focus on collision detection at bridges, ladders, mushrooms
3. **Adjust UI Positions**: Manually adjust dialog box positions scene by scene to ensure no important content is blocked
4. **Optimize Audio**: Use Unity Audio Mixer to adjust volume groups, prevent sound overlap, add missing sound effects
5. **Enhance Ending**: Add Granny Meriel dialogue, incorporate flashback effects

---

**End of Report**
