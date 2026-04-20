using UnityEngine;

public class ChestScript : MonoBehaviour
{
    public int energyAmount = 50;
    private bool isOpen = false;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !isOpen && Input.GetKeyDown(KeyCode.E))
        {
            PlayerStats ps = other.GetComponent<PlayerStats>();
            if (ps != null)
            {
                ps.AddEnergy(energyAmount);
                isOpen = true;
                Destroy(gameObject, 0.01f);
            }
        }
    }




}
