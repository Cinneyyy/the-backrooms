using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Backrooms;

public class PriorityQueue<T> : IEnumerable<T>
{
    private readonly SortedDictionary<int, Queue<T>> dict = [];


    public int Count => dict.Count;


    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public IEnumerator<T> GetEnumerator()
    {
        foreach(Queue<T> queue in from kvp in dict
                                  select kvp.Value)
            foreach(T item in queue)
                yield return item;
    }

    public void Enqueue(T item, int priority)
    {
        if(!dict.ContainsKey(priority))
            dict[priority] = [];

        dict[priority].Enqueue(item);
    }

    public T Dequeue()
    {
        KeyValuePair<int, Queue<T>> firstPair = dict.First();
        Queue<T> queue = firstPair.Value;
        T item = queue.Dequeue();

        if(queue.Count == 0)
            dict.Remove(firstPair.Key);

        return item;
    }
}