using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager; // Pøetáhni sem svùj ShopManager
    private bool playerInRange = false;

    private void Update()
    {
        if (this == null) return;
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            shopManager.OpenShop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // Tady bys mohl zapnout nìjaký nápis "Press E to Open Shop"
            Debug.Log("Mùžeš otevøít shop (E)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            shopManager.CloseShop(); // Automaticky zavøe shop, když hráè odejde
            Debug.Log("Odešel jsi od shopu");
        }
    }
}