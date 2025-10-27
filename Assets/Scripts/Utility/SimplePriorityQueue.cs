using System.Collections.Generic;

internal class SimplePriorityQueue<T>
{
    private readonly List<(T item, float priority)> _elements = new();

    public int Count => _elements.Count;

    public void Enqueue(T item, float priority)
    {
        _elements.Add((item, priority));
    }

    public T Dequeue()
    {
        var bestIndex = 0;
        for (var i = 1; i < _elements.Count; i++)
        {
            if (_elements[i].priority < _elements[bestIndex].priority)
            {
                bestIndex = i;
            }
        }

        var bestItem = _elements[bestIndex].item;
        _elements.RemoveAt(bestIndex);
        return bestItem;
    }
}