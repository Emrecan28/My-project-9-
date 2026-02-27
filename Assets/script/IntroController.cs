using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{
    public RectTransform grpText;
    public RectTransform studioText;
    public RectTransform unityLogo; // Unity Logosu icin yeni alan
    public float moveDuration = 0.7f;
    public float holdDuration = 1.0f;
    public float punchScale = 0.05f;
    public string menuSceneName;

    Vector2 grpTargetPos;
    Vector2 studioTargetPos;
    Vector2 unityTargetPos; // Unity logosu hedef pozisyonu

    Vector3 grpTargetScale;
    Vector3 studioTargetScale;
    Vector3 unityTargetScale; // Unity logosu hedef boyutu

    void Start()
    {
        // iOS icin FPS ayari
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (grpText == null || studioText == null)
        {
            LoadMenu();
            return;
        }

        grpTargetPos = grpText.anchoredPosition;
        studioTargetPos = studioText.anchoredPosition;
        grpTargetScale = grpText.localScale;
        studioTargetScale = studioText.localScale;

        // Unity logosu varsa onun da baslangic degerlerini al
        if (unityLogo != null)
        {
            unityTargetPos = unityLogo.anchoredPosition;
            unityTargetScale = unityLogo.localScale;
        }

        float offset = 600f;
        RectTransform parentRect = grpText.parent as RectTransform;
        if (parentRect != null)
        {
            offset = parentRect.rect.height * 0.6f;
        }

        grpText.anchoredPosition = grpTargetPos + new Vector2(0f, offset);
        studioText.anchoredPosition = studioTargetPos + new Vector2(0f, -offset);
        
        // Unity logosunu da yukaridan veya asagidan baslatabiliriz (Orn: Grp ile ayni yerden)
        if (unityLogo != null)
        {
            unityLogo.anchoredPosition = unityTargetPos + new Vector2(0f, offset);
        }

        StartCoroutine(PlayIntro());
    }

    System.Collections.IEnumerator PlayIntro()
    {
        Vector2 grpStartPos = grpText.anchoredPosition;
        Vector2 studioStartPos = studioText.anchoredPosition;
        
        Vector2 unityStartPos = Vector2.zero;
        if (unityLogo != null) unityStartPos = unityLogo.anchoredPosition;

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveDuration);
            k = k * k * (3f - 2f * k);

            grpText.anchoredPosition = Vector2.Lerp(grpStartPos, grpTargetPos, k);
            studioText.anchoredPosition = Vector2.Lerp(studioStartPos, studioTargetPos, k);
            
            if (unityLogo != null)
            {
                unityLogo.anchoredPosition = Vector2.Lerp(unityStartPos, unityTargetPos, k);
            }
            
            yield return null;
        }

        grpText.anchoredPosition = grpTargetPos;
        studioText.anchoredPosition = studioTargetPos;
        if (unityLogo != null) unityLogo.anchoredPosition = unityTargetPos;

        float punchDuration = 0.2f;
        t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / punchDuration);
            float s = 1f + Mathf.Sin(k * Mathf.PI) * punchScale;
            
            grpText.localScale = grpTargetScale * s;
            studioText.localScale = studioTargetScale; // Studio sabit kalsin veya o da oynasin
            
            if (unityLogo != null)
            {
                unityLogo.localScale = unityTargetScale * s;
            }
            
            yield return null;
        }

        grpText.localScale = grpTargetScale;
        studioText.localScale = studioTargetScale;
        if (unityLogo != null) unityLogo.localScale = unityTargetScale;

        yield return new WaitForSeconds(holdDuration);

        LoadMenu();
    }

    void LoadMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
