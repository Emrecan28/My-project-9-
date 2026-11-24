using UnityEngine;

public class FloatingLogo : MonoBehaviour
{
    public float amplitude = 10f; // Ziplama yuksekligi
    public float frequency = 1f;  // Saniyedeki hiz (1 = saniyede bir tur)

    Vector3 posOffset;

    bool hasBase;

    void Start()
    {
        if (!hasBase) posOffset = transform.localPosition;
    }

    // Menu, logoyu kucuk ekranlara gore yeniden yerlestirdiginde cagirir
    public void SetBase(Vector3 p)
    {
        posOffset = p;
        hasBase = true;
    }

    void Update()
    {
        // Sinus dalgasi ile yumusak hareket.
        // Time.time kullaniliyor: Time.fixedTime 50Hz'de guncellendigi icin 60 FPS'te takiliyordu.
        Vector3 tempPos = posOffset;
        tempPos.y += Mathf.Sin(Time.time * Mathf.PI * frequency) * amplitude;
        transform.localPosition = tempPos;
    }
}
