using UnityEngine;

public class TransformBroadcaster : MonoBehaviour
{
    [SerializeField] private TransformVariable targetChannel;

    private void OnEnable()
    {
        if (targetChannel != null)
        {
            targetChannel.SetValue(transform);
        }
    }

    private void OnDisable()
    {
        if (targetChannel != null)
        {
            targetChannel.Clear();
        }
    }
}