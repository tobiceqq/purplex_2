using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;

    private void Reset()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (animator == null || playerController == null)
            return;

        // Pøedávání rychlosti bìhu
        animator.SetFloat("Speed", playerController.CurrentMoveAmount);

        // Pøedávání informace, jestli jsme v módu koule (volitelné pro Animator)
        animator.SetBool("IsRolling", playerController.IsBallMode);
    }
}