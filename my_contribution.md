# My Own Contribution

In this project, I was responsible for all core creative and development work. AI acted as an assistant tool, helping me implement global UI control, game save progress functionality, and assisting with game structure organization. The game concept, story, gameplay design, scene construction, interaction flow, visual adjustment, testing, and final design decisions were completed by me.

## Game Concept and Story Design

I created the main concept, story direction, and world setting of the game. The game is about a magical journey through different fantasy scenes, including a forest, fairies, an old tree, a witch, a hero, Luna, and Meryl. The player explores different areas, solves puzzles, interacts with NPCs, collects important items, and gradually progresses through the story.

The overall concept and story are original. The only gameplay references I used were the coloured tile mini-game for obtaining the third page, which was inspired by a small level from Eggy Party, and the fairy memory section, which refers to the classic Klotski puzzle game. All other story flow, scene structure, character interactions, and quest design were planned by me.

The inspiration for this game came from my childhood imagination of wanting to have magic and become a magician. I also enjoy puzzle-solving games, so I designed the project around exploration, mystery, fantasy, and several small and large puzzle chapters.

I planned the purpose of each main scene:

- `Enchanted Forest`: main puzzle progression, fairy-related exploration, old tree interaction, and the first major story section.
- `Fae House`: side quests, merchant interaction, item collection, and several mini-games.
- `My Scene`: feather collection and interaction with Luna.
- `11 1`: the final emotional scene where the player is guided by Meryl and becomes a true magician.

## Scene Building and Environment Setup

I built and adjusted the main Unity scenes manually. This included terrain, buildings, NPCs, props, quest objects, interaction points, cameras, UI objects, map objects, and navigation elements.

I manually adjusted the player spawn positions, NPC and quest object placement, trigger areas, interaction ranges, camera angles, minimap object positions, UI placement, and unnecessary scene components. These details were repeatedly tested and changed in Unity through the Hierarchy, Scene view, Game view, and Inspector.

## Quest and Puzzle Design

I designed the main quest and side quest logic, including puzzle rules, interaction order, failure conditions, reset behavior, and completion states.

This includes the push-block puzzle, Ask Help interaction flow, old tree dialogue choices and attack sequence, Fae House merchant interaction, honey jar, key, dice, maze, ticket-related tasks, My Scene feather collection quest, side quest panel display, and quest counter behavior.

I also decided which progress should be saved, which unfinished tasks should reset, and which collected items should remain in the bag. These rules were designed based on the gameplay experience I wanted the player to have.

## UI and Player Experience

I adjusted the visual layout and user experience of the main menu, ESC in-game menu, settings panel, controls panel, bag UI, minimap, compass/navigation UI, top-left ESC prompt, top-right side quest panel, NPC dialogue panels, and quest counters such as `0/4`.

I repeatedly changed panel sizes, font sizes, text positions, button behavior, and UI alignment so the interface looked consistent across different scenes and did not block important gameplay content.

## Unity Integration and Inspector Setup

I connected the Unity scene objects, scripts, images, audio, video, animations, prefabs, and UI components through the Inspector.

This included assigning panels, buttons, images, text objects, AudioSources, cameras, map objects, NPC references, item objects, animation controllers, trigger colliders, and quest-related objects. I also checked missing references, removed unnecessary components, and fixed issues caused by empty Inspector fields or broken object links.

## Animation, Audio, and Video Integration

I imported and integrated external assets such as character animations, sound effects, background music, video files, textures, and environment models.

I tested and adjusted player movement animation, interaction animation, NPC animation, hero and monster animation, falling/lying/standing/attack animations, piano sound effects, background music, opening or ending video playback, and dialogue or quest-related sound effects.

## Save and Continue Rules

I designed the save and continue rules for different scenes.

Continue Game should return the player to the saved position. Items already obtained and placed in the bag should remain saved. Unfinished side quests should reset when loading. Completed puzzles should stay completed. Unfinished puzzles should return to their initial state. Tutorial should appear only in New Game, not Continue Game. Temporary progress, such as the middle steps of the key quest, should not be saved.

## Testing, Debugging, and Problem Solving

I repeatedly tested the game in Unity and checked different task orders, scene transitions, and interaction states.

I tested and fixed problems such as player interaction not working, push blocks stopping unexpectedly, Ask Help interaction breaking, minimap display problems, ESC menu mouse clicking problems, mouse not unlocking, bag UI not appearing, quest panel positioning issues, animation playback issues, scene loading state problems, missing Inspector references, and Unity Console errors.

Most of these problems could only be found through actual playtesting in Unity.

## Final Design Decisions

The final gameplay, quest order, UI appearance, scene states, save rules, and presentation content were decided by me. AI gave suggestions for code organization and debugging, but I chose what to use based on my own project goals and tested the final result in Unity.

## Summary

My main contributions include game design, story design, scene building, quest and puzzle design, UI adjustment, asset integration, Inspector setup, animation/audio/video setup, testing, debugging, and final experience control. The core creative direction and final playable result were completed by me.
