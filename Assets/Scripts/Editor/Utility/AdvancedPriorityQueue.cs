using System.Collections.Generic;

internal class AdvancedPriorityQueue<T>
{
    private readonly SortedSet<(float priority, int index, T item)> _elements = new(new PriorityComparer());
    private int _insertionIndex;
    public int Count => _elements.Count;

    public void Enqueue(T item, float priority)
    {
        _elements.Add((priority, _insertionIndex++, item));
    }

    public T Dequeue()
    {
        var bestElement = _elements.Min;
        _elements.Remove(bestElement);
        return bestElement.item;
    }

    private class PriorityComparer : IComparer<(float priority, int index, T item)>
    {
        public int Compare((float priority, int index, T item) x, (float priority, int index, T item) y)
        {
            var priorityComparison = x.priority.CompareTo(y.priority);
            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            return x.index.CompareTo(y.index);
        }
    }
}