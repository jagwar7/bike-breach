using UnityEngine;
using UnityEngine.Events;

public class CombatTrigger : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";

    [Header("Events")]
    [SerializeField] private UnityEvent onCombatEntered;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        // Check if the collider entering is the player/bike
        if (other.CompareTag(playerTag) || other.GetComponentInParent<PlayerCharacter>() != null)
        {
            _triggered = true;
            onCombatEntered?.Invoke();
        }
    }
}