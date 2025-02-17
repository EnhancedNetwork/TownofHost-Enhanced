using System;
using UnityEngine;

namespace TOHE.Modules;

[Obfuscation(Exclude = true, Feature = "renaming", ApplyToMembers = true)]
public class Dispatcher : MonoBehaviour
{
    public static Dispatcher Instance;
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

    public void m_dispatch(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    public static void Dispatch(Action action)
    {
        Instance.m_dispatch(action);
    }
}
