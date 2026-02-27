using UnityEngine;

public partial class FloatingLogo : MonoBehaviour
{
    public float amplitude = 10f; // Zýplama yüksekliði
    public float frequency = 1f;  // Saniyedeki hýz (1 = saniyede bir tur)

    Vector3 posOffset = new Vector3();
    Vector3 tempPos = new Vector3();

    void Start()
    {
        posOffset = transform.localPosition;
    }

    void Update()
    {
        tempPos = posOffset;
        // Sinüs dalgasý kullanarak yumuþak bir hareket saðlýyoruz
        tempPos.y += Mathf.Sin(Time.fixedTime * Mathf.PI * frequency) * amplitude;

        transform.localPosition = tempPos;
    }
}