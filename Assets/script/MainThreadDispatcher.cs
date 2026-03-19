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

        // Eger sahnede henuz Dispatcher yoksa olustur
        if (instance == null)
        {
            // Unity objelerini main thread disinda yaratmak tehlikeli olabilir ama 
            // cogu zaman bu metod Start() veya Awake() sirasinda cagrildigi icin guvenlidir.
            // Yine de onlem olarak, bu scripti ilk sahneye (Intro veya MainMenu) bos bir objeye eklemek en garantisidir.
        }
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

    void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                var action = executionQueue.Dequeue();
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError("MainThreadDispatcher Error: " + e.Message);
                }
            }
        }
    }
}
