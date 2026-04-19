using UnityEngine;

public class ChestScript : MonoBehaviour
{
    public int energyAmount = 50;
    private bool isOpen = false;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !isOpen && Input.GetKeyDown(KeyCode.E))
        {
            other.GetComponent<PlayerStats>().AddEnergy(energyAmount);
            isOpen = true;
            gameObject.SetActive(false); 
        }
    }




}
