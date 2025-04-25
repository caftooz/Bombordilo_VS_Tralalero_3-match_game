using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEditor.Progress;

public class Tile : MonoBehaviour
{
    public void SetTileProp(int x, int y, float itemSize)
    {
        X = x;
        Y = y;

        _itemSize = itemSize;
    }

    public bool IsSelected { get; private set; } = false;

    public int X { get; private set; }
    public int Y { get; private set; }

    public Item Item { get; set; }

    private float _itemSize;
    public void CreateItem(Item item)
    {
        if (item == null) { throw new System.Exception($"Невозможно присвоить Item данному Tile[{X},{Y}]. Нельзя присвоить свойству Item значение null"); }
        if (_itemSize == 0 && X == 0 && Y == 0) { throw new System.Exception($"Невозможно присвоить Item данному Tile[{X},{Y}]. У Tail не заданы свойства"); }

        Item = Instantiate(item, transform).GetComponent<Item>();
        Item.transform.localScale = Vector3.one * _itemSize;
        Item.SetName();
    }
    public void ClearItem()
    {
        if (Item != null) { Destroy(Item.gameObject); Item = null; }
    }

    public void Select(Color selectedColor, int mpSelectedSizePercent)
    {
        IsSelected = true;
        Item.GetComponent<SpriteRenderer>().color = selectedColor;
        Item.transform.localScale = Vector3.one * _itemSize * mpSelectedSizePercent / 100;
    }

    public void Deselect()
    {
        IsSelected = false;
        Item.GetComponent<SpriteRenderer>().color = Color.white;
        Item.transform.localScale = Vector3.one * _itemSize;
    }

    void OnMouseDown()
    {
        StartCoroutine(GetComponentInParent<Board>().ClickOnTile(this));
    }
}
