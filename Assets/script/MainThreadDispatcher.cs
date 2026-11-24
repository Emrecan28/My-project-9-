using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// iOS'ten gelen (veya baska bir thread'den gelen) cagrilari Unity'nin ana thread'ine (Main Thread) tasiyan yardimci sinif.
/// Unity'de UI degisiklikleri ve bircok API sadece ana thread uzerinden cagrilabilir. Aksi halde oyun coker (Crash).
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executionQueue = new Queue<Action>();
    private static MainThreadDispatcher instance;

    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }

        // Not: Dispatcher objesi AdsManager.Awake icinde otomatik olusturulur.
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    readonly List<Action> runBuffer = new List<Action>();

    void Update()
    {
        // Kuyrugu kilit altinda kopyala, aksiyonlari kilit disinda calistir
        lock (executionQueue)
        {
            while (executionQueue.Count > 0) runBuffer.Add(executionQueue.Dequeue());
        }

        for (int i = 0; i < runBuffer.Count; i++)
        {
            try
            {
                runBuffer[i].Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("MainThreadDispatcher Error: " + e);
            }
        }
        runBuffer.Clear();
    }
}
