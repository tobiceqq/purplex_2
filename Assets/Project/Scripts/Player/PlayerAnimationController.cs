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

        animator.SetFloat("Speed", playerController.CurrentMoveAmount);
        animator.SetBool("IsRolling", playerController.IsBallMode);

        // Pøidán parametr pro šplhání
        float moveSpeed = playerController.IsClimbing ? 0 : playerController.CurrentMoveAmount;
        animator.SetFloat("Speed", moveSpeed);
        animator.SetBool("IsClimbing", playerController.IsClimbing);
    }

    public void PlayJump()
    {
        animator.SetTrigger("Jump");
    }
}