using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour
{
    void Start()
    {
        // Buton bilesenini otomatik bul ve tiklama olayini bagla
        // Bu sayede Inspector'daki referans kaybolsa bile kod uzerinden calisir
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(GoToMenu); // Varsa eskisini sil (mukerrer olmasin)
            btn.onClick.AddListener(GoToMenu);
        }
    }

    public void GoToMenu()
    {
        // LevelManager uzerinden menuye don
        // Not: SaveGame islemi LevelManager.LoadMenu icinde yapiliyor, burada tekrar yapmaya gerek yok.
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadMenu();
        }
        else
        {
            // LevelManager yoksa MainMenu sahnesine don
            SceneManager.LoadScene("MainMenu");
        }
    }
}
