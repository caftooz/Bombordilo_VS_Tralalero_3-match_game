using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public Sprite sprite;

    private void Awake()
    {
        GetComponent<SpriteRenderer>().sprite = sprite;
        
    }
}
