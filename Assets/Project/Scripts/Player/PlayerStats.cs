using UnityEngine;
using TMPro; 

public class PlayerStats : MonoBehaviour
{
    public int energy;
    public bool hasHyperRoll = false;

    [Header("UI")]
    public TextMeshProUGUI energyText;

    private void Start()
    {
        UpdateUI(); 
    }

    public void AddEnergy(int amount)
    {
        energy += amount;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (energyText != null)
        {
            energyText.text = "Energie: " + energy;
        }
    }
}