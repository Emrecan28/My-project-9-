using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class EventSystemAutoSwitch : MonoBehaviour
{
    void Awake()
    {
        var es = EventSystem.current;
        if (es == null) return;
#if ENABLE_INPUT_SYSTEM
        var newModule = es.GetComponent<InputSystemUIInputModule>();
        if (newModule == null) newModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
#endif
        var oldModule = es.GetComponent<StandaloneInputModule>();
        if (oldModule != null) oldModule.enabled = false;
    }
}
