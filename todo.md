# The Backrooms; to-do

---
## Features
- Finish up main menu
- Actual gameplay
    -> Objectives
- More varied enemies & enemy AI
- More level variation
    -> Not too much, it is the backrooms
- Post processing effects
- Entities as zip files
    -> Custom behaviour/pathfinding in C# files
- Maybe maybe rewrite multiplayer code (not MpHandler itself, just the way it is used)

---
## Bugfixes
- Audio on custom entities
- GUI elements do not correctly detect cursor position at virtual ratios different from the physical ratio
- Multiplayer
    -> Client disconnect handling
- Ceiling/floor rendering is **extremely** slow (~150fps -> ~60fps in debug mode)
- Fix pathfinding bugs
    -> Dijkstra's shortest path
    -> Pathfinding shortcuts
- Parallelize/optimize sprite rendering