using UnityEngine;
using TMPro; // Nutné pro text
using UnityEngine.UI; // Nutné pro obrázek
using UnityEngine.Events;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Reference na UI v Canvasu")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI titleUI; // Sem pøetáhni Text z toho Panelu
    public Image imageUI; // Sem pøetáhni Image z toho Panelu



    [Header("Nastavení pro TENTO trigger")]
    public string titleText;
    public Sprite tutorialSprite;
    public UnityEvent onGotItPressed;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Dosadíme text a obrázek z tohoto triggeru do hlavního UI
            titleUI.text = titleText;
            imageUI.sprite = tutorialSprite;

            // 2. Zobrazíme panel a zastavíme hru
            tutorialPanel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        onGotItPressed.Invoke();
        gameObject.SetActive(false); // Znièíme trigger, aby se neukázal znovu
    }
}