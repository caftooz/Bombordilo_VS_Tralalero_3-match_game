using JetBrains.Annotations;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [SerializeField] private Sprite _sprite;
    [SerializeField] private string Name = "Item";

    private void Start()
    {
        GetComponent<SpriteRenderer>().sprite = _sprite;
        
    }
    public void SetName()
    {
        gameObject.name = Name;
    }
}
