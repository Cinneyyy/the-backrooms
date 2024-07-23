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
1. Post processing effects
    - Maybe more efficient ones also
1. Controller input
1. Make floor/ceil rendering horizontal again (#MRHA)
    - Also, there is probably so much possible optimization inside the Renderer
1. Graffitis
1. Switch from gridcast rendering to BSP rendering or do some rendering using WPP
1. Make post process effect type attribute based

---
## Bugfixes
1. Fix player sprites not showing up via adding logic to the welcome handler
1. Implement collision for WorldObjects
1. Multiplayer
    - Client disconnect handling
1. Ceiling/floor rendering is <span style="color: red;">**extremely**</span> slow
1. Fix pathfinding bugs
    - Dijkstra's shortest path
    - Pathfinding shortcuts
1. GUI elements do not correctly detect cursor position at virtual ratios different from the physical ratio
    - May have something to do with screenOffset