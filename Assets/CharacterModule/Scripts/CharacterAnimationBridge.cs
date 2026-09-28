using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterAnimationBridge : MonoBehaviour
{
    private Animator animator;
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayShootAnimation()
    {
        animator.SetTrigger(ShootHash);
    }
}