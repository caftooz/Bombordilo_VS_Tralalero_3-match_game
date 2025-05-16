using UnityEngine;

public class Spining : MonoBehaviour
{
    private RectTransform _transform;
    private float x, y;
    void Start()
    {
        _transform = GetComponentInParent<RectTransform>();

        x = _transform.position.x + 469.5f;
        y = _transform.position.y + 270.3687f;
        
    }
    void Update()
    {
        _transform.localPosition = new Vector3(x, y, 1);
    }
}
