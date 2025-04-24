using Unity.VisualScripting;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public void SetTileProp(int x, int y, float itemSize)
    {
        X = x;
        Y = y;

        _itemSize = itemSize;
    }
    public int X { get; private set; }
    public int Y { get; private set; }

    public Item Item { get; private set; }

    private float _itemSize;
    public void SetItem(Item item)
    {
        if (item == null) { throw new System.Exception($"Невозможно присвоить Item данному Tile[{X},{Y}]. Нельзя присвоить свойству Item значение null"); }
        if (_itemSize == 0 && X == 0 && Y == 0) { throw new System.Exception($"Невозможно присвоить Item данному Tile[{X},{Y}]. У Tail не заданы свойства"); }
        
        Item = Instantiate(item, transform).GetComponent<Item>();
        Item.transform.localScale *= _itemSize;
    }

    public void ClearItem()
    {
        if (Item != null) { Destroy(Item.gameObject); Item = null; }
    }
}
