namespace Olaf;

public enum AiState
{
    Roaming, // Randomly walking around
    Fleeing, // Running away/hiding from player
    Searching, // Randomly walking around, to a location close to the player
    Stalking, // Player has been spotted, slowly approach player while seeking cover
    Chasing // Player spotted the entity stalking them, start running toward them
}