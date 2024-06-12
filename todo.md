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
- Maybe maybe rewrite multiplayer code (not MpHandler itself, just the way it is used)
- Out() categories
    -> e.g. Debug, Load, Error, etc.

---
## Bugfixes
- Custom entity audio is sometimes stuttery
- GUI elements do not correctly detect cursor position at virtual ratios different from the physical ratio
- Multiplayer
    -> Client disconnect handling
- Ceiling/floor rendering is **extremely** slow
- Fix pathfinding bugs
    -> Dijkstra's shortest path
    -> Pathfinding shortcuts