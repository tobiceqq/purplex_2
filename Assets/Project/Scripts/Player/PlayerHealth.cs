using UnityEngine;
using UnityEngine.UI; // Nutné pro práci s Image
using UnityEngine.SceneManagement; // Nutné pro restart hry

public class PlayerHealth : MonoBehaviour
{
    [Header("Nastavení Zdraví")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Reference")]
    public Image healthBarFill; // Sem v Inspectoru pøetáhni ten VYPLNÌNÝ obrázek

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Aby zdraví nešlo pod nulu

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            // Fill Amount funguje od 0.0 do 1.0, proto dìlíme
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    void GameOver()
    {
        Debug.Log("Purplex byl znièen!");
        // Pro teï jen restartujeme scénu, pozdìji zde zapneš Game Over Panel
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}