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
1. Post processing effects
    - Maybe more efficient ones also
1. InputFieldElement
    - Then a sensible host/join screen (yk, one where you don't select the ip-address using ValueSelectorElements)
    
---
## Technical Changes/Additions
1. Packet Compiler (-> Only 1 server write/frame, containing multiple smaller packets)
1. Entity syncing across clients
1. Switch from gridcast rendering to BSP rendering or do some rendering using WPP
1. Better/faster pathfinding
1. Seperate/compile settings into single class
1. Change entity code to mod code and make entities just be part of a "mod"
1. Search exact player location if in Tile.Air (Entity AI)
 
---
## Bugfixes
1. Invisible entities
1. Fix player sprites not showing up via adding logic to the welcome handler
1. Implement collision for WorldObjects
1. Fix pathfinding bugs
    - Dijkstra's shortest path
    - Pathfinding shortcuts
1. GUI elements do not correctly detect cursor position at virtual ratios different from the physical ratio and in fullScreen mode (GUI Groups)
    - May have something to do with screenOffset
1. Light sources interfering with each other at too little distances
1. Camera sometimes ends up in a colliding tile after generating the map somehow