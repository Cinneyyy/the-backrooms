namespace Backrooms;

public interface IVector<TSelf> where TSelf : IVector<TSelf>
{
    static abstract TSelf Parse(string[] components);
}