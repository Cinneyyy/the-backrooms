# The Backrooms; to-do
[Project start: 15/04/2024]

---
## Features
1. Add inventory functionality (use/drop/etc.)
1. Finish up main menu
    - Multiplayer/Singleplayer options
    - More settings
1. Actual gameplay
    - Objectives
1. More level variation
    - Not too much, it is the backrooms
1. More varied enemies & enemy AI
1. Controller input
1. Rewrite audio system as to not create a new device instance for every sound played
1. Lighting
1. Post processing effects
    - Maybe more efficient ones also
1. Switch from gridcast rendering to BSP rendering or do some rendering using WPP

---
## Bugfixes
1. Fix player sprites not showing up via adding logic to the welcome handler
1. Implement collision for WorldObjects
1. Fix pathfinding bugs
    - Dijkstra's shortest path
    - Pathfinding shortcuts
1. GUI elements do not correctly detect cursor position at virtual ratios different from the physical ratio and in fullScreen mode (GUI Groups)
    - May have something to do with screenOffset