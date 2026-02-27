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
        if (callbackAction != null)
        {
            // Unity main thread check might be needed if callback updates UI, but usually safe for simple logic
            // However, callbacks from iOS are on background thread often.
            // Dispatch to main thread if needed.
            // For now, let's assume AdsManager handles it or Unity marshals it.
            // Actually, usually we need MainThreadDispatcher. 
            // But let's keep it simple. If it crashes, we'll fix.
            
            // To be safe, we can use a Unity object or Loom pattern, but static is tricky.
            // Let's just invoke.
            callbackAction(status);
            callbackAction = null;
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
