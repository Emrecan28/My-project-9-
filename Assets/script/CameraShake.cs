using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    Vector3 basePosition;
    Coroutine shakeRoutine;
    float currentMagnitude;

    void Awake()
    {
        if (Instance == null) Instance = this;
        basePosition = transform.localPosition;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Kamerayi sallar. Ust uste cagrilsa bile kamera hep ayni noktaya geri doner
    // (eskiden ikinci sallama, sallanmis pozisyonu "orijinal" sanip kamerayi kaydiriyordu).
    public void Shake(float duration, float magnitude)
    {
        if (!GameSettings.EffectsOn) return; // Ayarlar > EFFECTS kapali
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            magnitude = Mathf.Max(magnitude, currentMagnitude);
        }
        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        currentMagnitude = magnitude;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float fade = 1f - (elapsed / duration); // Sona dogru yumusak bitis
            float x = Random.Range(-1f, 1f) * magnitude * fade;
            float y = Random.Range(-1f, 1f) * magnitude * fade;
            transform.localPosition = basePosition + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = basePosition;
        currentMagnitude = 0f;
        shakeRoutine = null;
    }
}
