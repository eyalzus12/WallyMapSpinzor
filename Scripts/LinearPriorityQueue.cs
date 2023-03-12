using Godot;
using System;
using System.Collections.Generic;

public class LinearPriorityQueue<TElement>
{
    public List<Queue<TElement>> PriorityList{get; set;}
    public int MinimumUsedPriority{get; private set;}

    public LinearPriorityQueue(int priorityCount)
    {
        PriorityList = new(priorityCount);
        for(int i = 0; i < priorityCount; ++i) PriorityList.Add(new());
        MinimumUsedPriority = priorityCount;
    }

    public void Enqueue(TElement element, int priority)
    {
        if(priority < MinimumUsedPriority) MinimumUsedPriority = priority;
        PriorityList[priority].Enqueue(element);
    }

    public TElement Dequeue()
    {
        var result = PriorityList[MinimumUsedPriority].Dequeue();
        while(MinimumUsedPriority < PriorityList.Count && PriorityList[MinimumUsedPriority].Count == 0)
            MinimumUsedPriority++;
        return result;
    }

    public void Clear()
    {
        for(int i = 0; i < PriorityList.Count; ++i) PriorityList[i].Clear();
    }

    public bool Empty => MinimumUsedPriority >= PriorityList.Count;
}
