using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTransformVariable", menuName = "Architecture/Variables/Transform")]
public class TransformVariable : ScriptableObject
{
    [NonSerialized] private Transform _currentTransform;

    public Transform Value => _currentTransform;
    public event Action<Transform> OnTransformChanged;

    private void OnEnable()
    {
        _currentTransform = null;
    }

    public void SetValue(Transform target)
    {
        _currentTransform = target;
        OnTransformChanged?.Invoke(_currentTransform);
    }

    public void Clear()
    {
        _currentTransform = null;
        OnTransformChanged?.Invoke(null);
    }
}