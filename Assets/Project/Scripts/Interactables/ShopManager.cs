using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public PlayerStats stats;
    public GameObject shopUI;
    public int price = 30;

    public void OpenShop()
    {
        shopUI.SetActive(true);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BuyHyperRoll()
    {
        Debug.Log("1. Tlaèítko zaregistrovalo kliknutí.");

        if (stats == null)
        {
            Debug.LogError("2. CHYBA: ShopManager nemá v kolonce Stats nikoho!");
            return;
        }

        Debug.Log("3. Moje energie: " + stats.energy + " | Cena: " + price);

        if (stats.energy >= price)
        {
            stats.energy -= price;

            
            stats.UpdateUI();      

            stats.hasHyperRoll = true;
            Debug.Log("4. ÚSPÌCH: HyperRoll koupen! Zbývá energie: " + stats.energy);
            CloseShop();
        }
        else
        {
            Debug.LogWarning("4. SMÙLA: Máš málo energie.");
        }
    }

    public void CloseShop()
    {
        shopUI.SetActive(false);
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}