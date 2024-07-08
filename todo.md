# The Backrooms; to-do

---
## Features
1. Implement arrays/lists into serialization system
1. Add inventory functionality
1. Finish up main menu
    - Multiplayer/Singleplayer options
    - More settings
1. Actual gameplay
    - Objectives
1. More level variation
    - Not too much, it is the backrooms
1. More varied enemies & enemy AI
1. Post processing effects
    - Maybe more efficient ones also
1. Out() categories
    - e.g. Debug, Load, Error, etc.
1. GUI edit mode
1. Switch from wolfenstein-style-rendering to doom-style-rendering

---
## Bugfixes
1. Entity loader speedup: have precompiled class that entities derive from
1. Settings don't work
1. Fix player sprites not showing up via adding logic to the welcome handler
1. Custom entity audio is sometimes stuttery
1. GUI elements do not correctly detect cursor position at virtual ratios different from the physical ratio
    - May have something to do with screenOffset
1. Multiplayer
    - Client disconnect handling
1. Ceiling/floor rendering is **extremely** slow
1. Fix pathfinding bugs
    - Dijkstra's shortest path
    - Pathfinding shortcuts