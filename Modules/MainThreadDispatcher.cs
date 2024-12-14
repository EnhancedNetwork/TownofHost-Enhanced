using System;
using UnityEngine;

namespace TOHE.Modules;

public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance;
    private readonly Queue<Action> _executionQueue = new();

    public void Awake()
    {
        Instance = this;
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void ExecuteOnMainThread(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}