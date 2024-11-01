using Backrooms.Serialization;

namespace Backrooms.SaveSystem;

public class SettingsFile() : Serializable<SettingsFile>()
{
    public bool showDebugInfo = true;
    public bool devConsole = true;
    public int resolutionIndex = 2;
}