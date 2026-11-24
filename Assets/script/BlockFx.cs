using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Oyun hissi efektleri: parcacik patlamasi, "+80" puan yazisi, "GREAT!" gibi ovgu kelimeleri,
// blok yerlesince zıplama. Sahneye eklemeye gerek yok; ilk kullanimda kendini olusturur.
public class BlockFx : MonoBehaviour
{
    static BlockFx instance;
    public static BlockFx I
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("BlockFx");
                instance = go.AddComponent<BlockFx>();
            }
            return instance;
        }
    }

    class Particle
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector3 velocity;
        public float spin, life, maxLife, startScale;
        public Color color;
    }

    readonly List<Particle> active = new List<Particle>();
    readonly Stack<Particle> pool = new Stack<Particle>();
    Canvas uiCanvas;

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ---------------- PARCACIK ----------------

    public void Burst(Vector3 worldPos, Color color, int count, float power = 1f)
    {
        if (!GameSettings.EffectsOn) count = Mathf.Max(1, count / 3); // Ayarlar > EFFECTS kapali
        for (int i = 0; i < count; i++)
        {
            Particle p = pool.Count > 0 ? pool.Pop() : CreateParticle();
            p.t.gameObject.SetActive(true);
            p.t.position = worldPos + (Vector3)(Random.insideUnitCircle * 0.25f);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(2.5f, 6.5f) * power;
            p.velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + Random.Range(1.5f, 4f), 0f);
            p.spin = Random.Range(-540f, 540f);
            p.maxLife = p.life = Random.Range(0.45f, 0.8f);
            p.startScale = Random.Range(0.55f, 1.1f);
            p.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.35f));
            p.sr.color = p.color;
            p.t.localScale = Vector3.one * p.startScale;
            p.t.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            active.Add(p);
        }
    }

    Particle CreateParticle()
    {
        var go = new GameObject("FxParticle");
        go.transform.SetParent(transform, false);
        var p = new Particle
        {
            t = go.transform,
            sr = go.AddComponent<SpriteRenderer>()
        };
        p.sr.sprite = BlockSkin.Particle;
        p.sr.sortingOrder = 150;
        return p;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Particle p = active[i];
            p.life -= dt;
            if (p.life <= 0f || p.t == null)
            {
                if (p.t != null) { p.t.gameObject.SetActive(false); pool.Push(p); }
                active.RemoveAt(i);
                continue;
            }
            p.velocity += Vector3.down * 14f * dt; // yercekimi
            p.t.position += p.velocity * dt;
            p.t.Rotate(0, 0, p.spin * dt);
            float k = p.life / p.maxLife;
            p.t.localScale = Vector3.one * p.startScale * Mathf.Lerp(0.2f, 1f, k);
            Color c = p.color;
            c.a = Mathf.Clamp01(k * 1.6f);
            p.sr.color = c;
        }
    }

    // ---------------- YERLESTIRME ZIPLAMASI ----------------

    public void Bounce(Transform target, float amount = 0.14f, float duration = 0.16f)
    {
        if (target != null) StartCoroutine(BounceRoutine(target, amount, duration));
    }

    IEnumerator BounceRoutine(Transform target, float amount, float duration)
    {
        Vector3 baseScale = target.localScale;
        float t = 0f;
        while (t < duration && target != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float s = 1f + Mathf.Sin(p * Mathf.PI) * amount;
            target.localScale = baseScale * s;
            yield return null;
        }
        if (target != null) target.localScale = baseScale;
    }

    // ---------------- UI YAZILARI ----------------

    Canvas GetCanvas()
    {
        if (uiCanvas != null) return uiCanvas;
        var go = new GameObject("FxCanvas");
        go.transform.SetParent(transform, false);
        uiCanvas = go.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 220;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        return uiCanvas;
    }

    Text MakeText(string message, int size, Color color, Color outlineColor)
    {
        var go = new GameObject("FxText");
        go.transform.SetParent(GetCanvas().transform, false);
        var text = go.AddComponent<Text>();
        text.font = NeoUI.Font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = message;
        text.color = color;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(5f, -5f);
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -9f);
        return text;
    }

    // Temizlenen bolgenin uzerinde yukari suzulen "+80"
    public void ScorePopup(Vector3 worldPos, int amount, Color color)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Text text = MakeText("+" + amount, amount >= 300 ? 96 : 76, Color.white,
            new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f));
        text.transform.position = cam.WorldToScreenPoint(worldPos);
        StartCoroutine(FloatUp(text, 0.9f, 140f));
    }

    // Ekran ortasinda "GREAT!" gibi buyuk ovgu (pop + kaybolma)
    public void Praise(string word, Color color, float yOffset = 260f)
    {
        Text text = MakeText(word, 130, color, Theme.SurfaceRim);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, yOffset);
        StartCoroutine(PopAndFade(text, 1.0f));
    }

    IEnumerator FloatUp(Text text, float duration, float distance)
    {
        Vector3 start = text.transform.position;
        float scaleFactor = GetCanvas().scaleFactor;
        float t = 0f;
        while (t < duration && text != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            text.transform.position = start + Vector3.up * distance * scaleFactor * ease;
            float pop = p < 0.15f ? Mathf.Lerp(0.6f, 1.15f, p / 0.15f) : Mathf.Lerp(1.15f, 1f, (p - 0.15f) / 0.85f);
            text.transform.localScale = Vector3.one * pop;
            Color c = text.color;
            c.a = p < 0.65f ? 1f : 1f - (p - 0.65f) / 0.35f;
            text.color = c;
            yield return null;
        }
        if (text != null) Destroy(text.gameObject);
    }

    IEnumerator PopAndFade(Text text, float duration)
    {
        float t = 0f;
        while (t < duration && text != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float scale;
            if (p < 0.18f) scale = Mathf.Lerp(0.3f, 1.25f, p / 0.18f);
            else if (p < 0.3f) scale = Mathf.Lerp(1.25f, 1f, (p - 0.18f) / 0.12f);
            else scale = 1f + (p - 0.3f) * 0.08f;
            text.transform.localScale = Vector3.one * scale;
            Color c = text.color;
            c.a = p < 0.7f ? 1f : 1f - (p - 0.7f) / 0.3f;
            text.color = c;
            yield return null;
        }
        if (text != null) Destroy(text.gameObject);
    }
}
