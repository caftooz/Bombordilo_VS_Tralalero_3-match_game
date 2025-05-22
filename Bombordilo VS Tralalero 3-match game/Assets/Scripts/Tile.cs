using System.Collections;
using UnityEngine;
using System;
using Unity.VisualScripting;

public class Tile : MonoBehaviour
{
    private float _burstMaxScale = 2f;
    private float _burstDuration = 0.2f;
    private AnimationCurve _burstScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private GameObject _burstItemPrefab;
    private event Action<FruitType> _damageBoss;
    private event Action<int> _addPoints;

    private float _moveToBossDuration = 0.2f;
    private AnimationCurve _moveToBossCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public void SetTileProp(int x, int y, float itemSize, float burstMaxScale, float burstDuration, AnimationCurve burstScaleCurve, GameObject burstItemPrefab, Action<FruitType> damageBoss, Action<int> addPoints, float moveToBossDuration, AnimationCurve moveToBossCurve)
    {
        X = x;
        Y = y;

        _itemSize = itemSize;
        _burstMaxScale = burstMaxScale;
        _burstDuration = burstDuration;
        _burstScaleCurve = burstScaleCurve;
        _burstItemPrefab = burstItemPrefab;
        _damageBoss = damageBoss;
        _addPoints = addPoints;
        _moveToBossCurve = moveToBossCurve;
        _moveToBossDuration = moveToBossDuration;
    }

    public bool IsSelected { get; set; } = false;

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
        if (Item != null)
        {
            if (Item is Fruit fruit)
            {
                _addPoints?.Invoke(10);
                StartCoroutine(MoveToBoss(Item));
            }
            else
            {
                Destroy(Item.gameObject);
            }

            Item = null;
        }
    }

    private IEnumerator burstItemCoroutine(Item item)
    {

        Transform itemTransform = item.transform;

        Vector3 startScale = itemTransform.localScale;
        Vector3 endScale   = itemTransform.localScale * _burstMaxScale;

        float timer = 0f;
        while (timer < _burstDuration)
        {
            float burstProgress = Mathf.Clamp01(timer / _burstDuration);

            float curveProgress = _burstScaleCurve.Evaluate(burstProgress);
            Vector3 burstScale = Vector3.Lerp(startScale, endScale, curveProgress);

            itemTransform.localScale = burstScale;
            timer += Time.deltaTime;
            yield return null;
        }

    }

    private IEnumerator MoveToBoss(Item item)
    {
        yield return StartCoroutine(burstItemCoroutine(item));

        Transform itemTransform = item.transform;
        Vector3 bossPosition = GameObject.FindGameObjectWithTag("Boss").transform.position;

        int randomx = UnityEngine.Random.Range(-2,1);
        int randomy = UnityEngine.Random.Range(-2, 2);

        Vector3 startPosition = itemTransform.position;
        Vector3 endPosotion = bossPosition + Vector3.up * randomy + Vector3.right * randomx;

        float timer = 0f;

        while (timer < _moveToBossDuration)
        {
            float moveToBossProgress = Mathf.Clamp01(timer / _moveToBossDuration);

            float curveProgress = _moveToBossCurve.Evaluate(moveToBossProgress);
            Vector3 moveToBossPosition = Vector3.Lerp(startPosition, endPosotion, curveProgress);

            itemTransform.position = moveToBossPosition;
            timer += Time.deltaTime;
            yield return null;
        }


        if (_burstItemPrefab != null)
        {
            GameObject burstItemEffect = Instantiate(_burstItemPrefab);
            burstItemEffect.transform.position = item.transform.transform.position;
            Destroy(burstItemEffect, 5);
        }

        if (item is Fruit fruit)
        {
            _damageBoss?.Invoke(fruit.fruitType);
        }

        Destroy(item.gameObject);
    }

    public void Select(Color selectedColor, int mpSelectedSizePercent = 100, bool condition = true)
    {
        if(condition) IsSelected = true;
        Item.GetComponent<SpriteRenderer>().color = selectedColor;
        Item.transform.localScale = Vector3.one * _itemSize * mpSelectedSizePercent / 100;
    }

    public void Deselect(bool condition = true)
    {
        if (Item == null) return;
        if(condition) IsSelected = false;
        Item.GetComponent<SpriteRenderer>().color = Color.white;
        Item.transform.localScale = Vector3.one * _itemSize;
    }

    void OnMouseDown()
    {
        StartCoroutine(GetComponentInParent<Board>().ClickOnTile(this));
    }

    void OnMouseOver()
    {
        GetComponentInParent<Board>().OnTileOver(this);
    }

    void OnMouseExit()
    {
        GetComponentInParent<Board>().OnTileExit(this);
    }
}
