namespace Backrooms.Online;

public interface IState
{
    void Deserialize(byte[] data, int length);
    byte[] Serialize(byte[] fieldKeys);
}