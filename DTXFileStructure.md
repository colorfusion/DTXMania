# DTX File

The DTX file structure contains the information required to play a song in the game. During loading, the file is reading in a specific order which loads the required media (image, chip sounds, animations) that is relative to the file. The file is broken down into different parts, which contains song information, list of chip sounds, animations, and the actual chips in the song itself.

## File structure

- Song information
- Optional text
- Chip sounds
- Images
- Videos
- Measure multiplier (TBC)
- BPM list
- Chip list
- Lane binded chip list (for DTXCreator only)
- Chip color list (for DTXCreator only)
- Image chip color list (for DTXCreator only)
- Video chip color list (for DTXCreator only)
- Chip palette (for DTXCreator only)
