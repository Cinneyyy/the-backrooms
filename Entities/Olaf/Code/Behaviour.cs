global using static Backrooms.Debugging.Logger;

using System.Collections;
using Backrooms;
using Backrooms.Entities;
using Backrooms.Coroutines;

namespace Olaf;

public class Behaviour(EntityManager manager, EntityType type) : EntityInstance(manager, type)
{
    public float minRoamSeconds = 30f, maxRoamSeconds = 70f, minChaseSeconds = 10f, maxChaseSeconds = 22.5f, minSearchSeconds = 25f, maxSearchSeconds = 40f, maxStalkDist = 40f, chaseDist = 15f;
    public int playerSearchSpread = 30;
    public AiState aiState;
    public Vec2f targetPos;

    private Coroutine aiCoroutine;


    public override void Tick(float dt)
    {
        sprRend.pos = pos;
        TraversePath(aiState switch
        {
            AiState.Roaming => 3f,
            AiState.Searching => 1.75f,
            AiState.Stalking => lineOfSightToPlayer ? 4f : .5f,
            AiState.Chasing => 4f,
            AiState.Fleeing => 3.5f,
            _ => 0f
        }, dt);
    }

    public override void Pulse()
        => Out(Log.Entity, $"AI State: {aiState}; Pos: {pos.Floor()}; Target: {targetPos}");

    public override void GenerateMap(Vec2f center)
    {
        aiCoroutine?.Cancel();
        aiCoroutine = AiIterator().StartCoroutine(manager.window);

        sprRend.enabled = true;
        audioPlayback.Play();
        pos = manager.map.RandomTile();
    }

    public override void Destroy() { }


    private IEnumerator AiIterator()
    {
        Window win = manager.window;
        Map map = manager.map;

        aiState = AiState.Roaming;
        targetPos = manager.map.RandomTile();

        float roamTime = 0f, chaseTime = 0f, searchTime = 0f;
        float maxRoamTime = RNG.Range(minRoamSeconds, maxRoamSeconds), maxChaseTime = RNG.Range(minChaseSeconds, maxChaseSeconds), maxSearchTime = RNG.Range(minSearchSeconds, maxSearchSeconds);
        while(true)
        {
            //Out(Log.Entity, aiState);

            switch(aiState)
            {
                case AiState.Roaming or AiState.Searching or AiState.Fleeing:
                {
                    if(aiState != AiState.Fleeing && lineOfSightToPlayer && (playerPos - pos).sqrLength < maxStalkDist*maxStalkDist) // Player located, start stalking them
                    {
                        aiState = AiState.Stalking;
                        targetPos = pos;
                        break;
                    }

                    if(aiState == AiState.Roaming)
                        roamTime += 1f;
                    else if(aiState == AiState.Searching)
                        searchTime += 1f;

                    if(roamTime > maxRoamTime || searchTime > maxSearchTime || (pos - targetPos).sqrLength < 2f) // Max roam time exceeded or target location reached
                    {
                        roamTime = 0f;
                        searchTime = 0f;

                        if(RNG.coinToss) // Keep roaming
                        {
                            aiState = AiState.Roaming;
                            maxRoamTime = RNG.Range(minRoamSeconds, maxRoamSeconds);
                            targetPos = map.RandomTile();
                        }
                        else // Search player
                        {
                            aiState = AiState.Searching;
                            maxSearchTime = RNG.Range(minSearchSeconds, maxSearchSeconds);
                            targetPos = map.RandomTile(playerPos.Floor(), playerSearchSpread);
                        }

                        break;
                    }

                    currPath = type.pathfinding.FindPath(map, pos.Floor(), targetPos.Floor());

                    yield return new WaitSeconds(1f);
                    break;
                }

                case AiState.Chasing:
                {
                    chaseTime += win.deltaTime;
                    if(chaseTime > maxChaseTime)
                    {
                        chaseTime = 0f;
                        aiState = AiState.Fleeing;
                        targetPos = map.RandomTile();
                        break;
                    }

                    currPath = type.pathfinding.FindPath(map, pos.Floor(), playerPos.Floor());

                    yield return new WaitFrame();
                    break;
                }

                case AiState.Stalking:
                {
                    float sqrDist = (playerPos - pos).sqrLength;
                    if(sqrDist < chaseDist*chaseDist) // Got close enough to player
                    {
                        aiState = AiState.Chasing;
                        maxChaseTime = RNG.Range(minChaseSeconds, maxChaseSeconds);
                        break;
                    }
                    else if(sqrDist > maxStalkDist*maxStalkDist) // Out of range for stalking
                    {
                        aiState = AiState.Searching;
                        maxSearchTime = RNG.Range(minSearchSeconds, maxSearchSeconds);
                        targetPos = map.RandomTile();
                        break;
                    }

                    targetPos = map.RandomTile(playerPos.Floor(), 15);

                    yield return new WaitSeconds(1f);
                    break;
                }
            }
        }
    }
}