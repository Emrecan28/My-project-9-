using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro namespace ekledik

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;
    public Transform contentPanel;
    public Button backButton;
    
    void Start()
    {
        if (LevelManager.Instance == null)
        {
            var lmObj = new GameObject("LevelManager");
            lmObj.AddComponent<LevelManager>();
        }

        if (backButton != null)
        {
            // Ana Menuye don
            backButton.onClick.RemoveAllListeners(); // Varsa eski listener'lari temizle
            backButton.onClick.AddListener(() => {
                if (LevelManager.Instance != null)
                {
                    // LevelManager uzerinden Menu'yu yukle (Panel degisimi)
                    LevelManager.Instance.LoadMenu();
                }
                else
                {
                    // LevelManager yoksa Scene 0'a donmeyi dene (Guvenli liman)
                    SceneManager.LoadScene(0);
                }
            });
        }
        
        // Button prefab kontrolu
        if (buttonPrefab == null)
        {
            Debug.LogError("LevelSelectUI: Button Prefab atanmamis!");
            return;
        }

        RefreshUI();
    }
    
    // OnEnable cagirildiginda UI guncelle
    void OnEnable()
    {
        RefreshUI();
    }
    
    public void RefreshUI()
    {
        // Temizle
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
        
        // LevelManager yoksa hata vermesin
        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager bulunamadi! Lutfen sahneye ekleyin.");
            return;
        }
        
        int unlockedLevel = LevelManager.Instance.GetUnlockedLevel();
        
        foreach (var level in LevelManager.Instance.levels)
        {
            GameObject btnObj = Instantiate(buttonPrefab, contentPanel);
            Button btn = btnObj.GetComponent<Button>();
            
            // Text bilesenini bulma - Guclendirilmis Yontem
            Text btnText = btnObj.GetComponentInChildren<Text>(true); // true: inactive olanlari da bul
            TextMeshProUGUI btnTmpText = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);

            // Oncelik TextMeshPro'da
            if (btnTmpText != null)
            {
                btnTmpText.text = level.levelNumber.ToString();
            }
            else if (btnText != null)
            {
                btnText.text = level.levelNumber.ToString();
            }
            else
            {
                // Eger hicbir text bileseni bulunamazsa isme gore aramayi dene
                Transform textObj = btnObj.transform.Find("Text");
                if (textObj == null) textObj = btnObj.transform.Find("Label"); // Bazi UI kitlerinde Label olur
                
                if (textObj != null)
                {
                    // Bulunan objede tekrar dene
                    var t = textObj.GetComponent<Text>();
                    var tmp = textObj.GetComponent<TextMeshProUGUI>();
                    
                    if (tmp != null) tmp.text = level.levelNumber.ToString();
                    else if (t != null) t.text = level.levelNumber.ToString();
                }
                else
                {
                    Debug.LogWarning($"Level butonu ({level.levelNumber}) icin Text bileseni bulunamadi! Prefab'i kontrol edin.");
                }
            }
            
            Image btnImage = btnObj.GetComponent<Image>();
                        
            if (level.levelNumber <= unlockedLevel)
            {
                btn.interactable = true;
                int lvlNum = level.levelNumber; // Capture variable
                btn.onClick.AddListener(() => {
                    LevelManager.Instance.LoadLevel(lvlNum);
                });

                // Renk Ayarlari
                if (btnImage != null)
                {
                    if (level.levelNumber < unlockedLevel)
                    {
                        // Tamamlanmis Level (Yesil tonu)
                        btnImage.color = new Color(0.4f, 0.8f, 0.4f); 
                    }
                    else
                    {
                        // Su anki Level (Sari/Turuncu tonu)
                        btnImage.color = new Color(1f, 0.8f, 0.2f);
                    }
                }
            }
            else
            {
                // Kilitli Level
                btn.interactable = false;
                if (btnImage != null) btnImage.color = Color.gray;
                
                if (btnText != null) btnText.color = new Color(0.3f, 0.3f, 0.3f);
                else if (btnTmpText != null) btnTmpText.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }
}
