Monday Plan

1. Fix model clipping issues

Check the matching between character colliders and model meshes

Adjust collider ranges for ground, platforms, treehouses, and other scene objects

Pay special attention to clipping between the protagonist and interactive objects such as bridges, ladders, mushrooms, and cages

Adjust animation state machine position offsets or add Root Motion correction if necessary

2. Implement the ending sequence

Set up the ending trigger logic in Scene_Meryl

Configure the trigger on lt1 to start video audio playback when stepped on

Make Magic_box_low appear and restore player movement after the video ends

Implement interaction where pressing E near the Magic_box_low gives the spellbook (show "book")

Create a blackout cutscene with text displayed line by line for the ending

Ensure the emotional tone of the ending is appropriate and each line of text stays on screen for a reasonable duration (5 seconds per line)

3. Prepare and polish the opening video and cover

Confirm that the opening video (intro cutscene) has been exported in a suitable format (e.g., MP4)

Import the video into Unity and set up the VideoPlayer component

Ensure the opening video plays normally when the game starts or before entering the main menu

Prepare game cover images (main menu background, title image, icons, etc.)

Check the transition between the cover and video for smoothness and adjust if necessary
