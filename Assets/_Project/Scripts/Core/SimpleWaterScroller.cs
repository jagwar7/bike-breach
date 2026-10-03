using UnityEngine;

public class SimpleWaterScroller : MonoBehaviour
{
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.05f, 0.02f);
    private Material _mat;
    private Vector2 _offset;

    private void Start()
    {
        _mat = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        _offset += scrollSpeed * Time.deltaTime;
        _mat.mainTextureOffset = _offset;
    }
}