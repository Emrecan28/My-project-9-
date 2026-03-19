using UnityEngine;
using System.Runtime.InteropServices;
using AOT;
using System;

public class ATTWrapper
{
    public delegate void ATTCallback(int status);

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _RequestTrackingAuthorization(ATTCallback callback);
#endif

    [MonoPInvokeCallback(typeof(ATTCallback))]
    private static void OnRequestTrackingAuthorization(int status)
    {
        // iOS tarafindan gelen callback arka plan thread'inde (background thread) calisabilir.
        // Unity API'leri ana thread disinda cagrilirsa Crash'e (Cokmeye) neden olur.
        // Bu yuzden bu fonksiyonu MainThread'de calistirmaliyiz.
        // Unity 2021 ve sonrasinda SynchronizationContext veya Dispatcher kullanilabilir
        // ancak en basit ve guvenli yol bir action kuyruguna atmaktir.
        
        if (callbackAction != null)
        {
            var actionToRun = callbackAction;
            callbackAction = null;
            
            // Ana thread'de calismasini sagla
            MainThreadDispatcher.Enqueue(() => {
                actionToRun(status);
            });
        }
    }

    private static Action<int> callbackAction;

    public static void RequestTrackingAuthorization(Action<int> callback)
    {
#if UNITY_IOS && !UNITY_EDITOR
        callbackAction = callback;
        _RequestTrackingAuthorization(OnRequestTrackingAuthorization);
#else
        Debug.Log("ATTWrapper: Simulating iOS ATT Request -> Authorized");
        callback?.Invoke(3); // 3 = Authorized
#endif
    }
}
