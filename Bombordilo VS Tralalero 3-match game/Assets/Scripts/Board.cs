using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Board : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int _width;
    [SerializeField] private int _height;

    [Header("Item Prefabs")]
    [SerializeField] private GameObject[] _fruitPrefabs;
    [SerializeField] private GameObject[] _powerupPrefabs;

    [Header("Tile Settings")]
    [SerializeField] private GameObject _tilePrefab;

    [SerializeField] private float _tileSize;

    [Header("Item Settings")]
    [SerializeField] private float _itemSize;

    private Tile _previousSelected;
    [SerializeField] private Color _selectedColor = Color.gray;
    [SerializeField][Range(100, 200)] private int _mpSelectedSizePercent = 105;

    [SerializeField] private float _fallSpeed = 5f;
    
    [Header("Swap Animation Settings")]
    [SerializeField] private float _swapDuration = 0.1f;
    [SerializeField] private float _swapOvershoot = 0.2f;
    [SerializeField] private float _swapBounceDuration = 0.15f; 

    private Tile[,] _tiles;
    private List<Coroutine> _fallCoroutines = new List<Coroutine>();

    public void CrerateAndFillBoard()
    {
        ClearBoard();

        _tiles = CreateBoard(_width, _height);


        bool hasMatches;
        do
        {
            foreach (Tile tile in _tiles)
            {
                if (tile.Item == null)
                {
                    int random = Random.Range(0, _fruitPrefabs.Length);

                    tile.CreateItem(_fruitPrefabs[random].GetComponent<Item>());
                }

            }

            List<Tile[]> matches = FindMatches();
            hasMatches = matches.Count > 0;
            if (hasMatches)
            {
                foreach (var match in matches)
                {
                    foreach (Tile tile in match)
                    {
                        tile.ClearItem();
                    }
                }
            }
        } while (hasMatches);
    }
    private Tile[,] CreateBoard(int width, int height)
    {
        Tile[,] tiles = new Tile[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = Instantiate(_tilePrefab, transform).GetComponent<Tile>();
                tiles[x, y].SetTileProp(x, y, _itemSize);
                tiles[x, y].transform.localScale = Vector3.one * _tileSize;
                tiles[x, y].transform.localPosition = new Vector3(x * (_tileSize), y * (_tileSize), 1);
                tiles[x, y].gameObject.name = $"Tile[{x},{y}]";
            }
        }
        return tiles;
    }
    private void ClearBoard()
    {
        if (_tiles == null) return;

        int width = _tiles.GetLength(0);
        int height = _tiles.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_tiles[x, y] == null) continue;
                Destroy(_tiles[x, y].gameObject);
            }
        }
    }
    private List<Tile[]> FindMatches()
    {
        List<Tile[]> allMatches = new List<Tile[]>();

        // 1. Сначала находим ВСЕ линии (горизонтальные и вертикальные)
        List<Tile[]> horizontalLines = FindLines(true);
        List<Tile[]> verticalLines = FindLines(false);

        // 2. Объединяем пересекающиеся линии в одну комбинацию
        List<Tile[]> mergedMatches = MergeIntersectingMatches(horizontalLines, verticalLines);

        // 3. Добавляем только те, где хотя бы одна линия >= 3
        foreach (Tile[] match in mergedMatches)
        {
            if (match.Length >= 3)
                allMatches.Add(match);
        }

        return allMatches;


        // Поиск линий (горизонтальных или вертикальных)
        List<Tile[]> FindLines(bool isHorizontal)
        {
            int width = _tiles.GetLength(0);
            int height = _tiles.GetLength(1);

            bool[,] checkedTiles = new bool[width, height];
            List<Tile[]> lines = new List<Tile[]>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (checkedTiles[x, y] || _tiles[x, y].Item == null || _tiles[x, y].Item is not Fruit)
                        continue;

                    FruitType fruitType = ((Fruit)_tiles[x, y].Item).fruitType;
                    List<Tile> line = new List<Tile>();

                    // Проверяем линию (горизонтальную или вертикальную)
                    int step = 0;
                    while (true)
                    {
                        int checkX = isHorizontal ? x + step : x;
                        int checkY = isHorizontal ? y : y + step;

                        // Если вышли за границы или тип не совпадает → стоп
                        if (checkX >= width || checkY >= height ||
                            _tiles[checkX, checkY].Item == null || _tiles[x, y].Item is not Fruit ||
                            ((Fruit)_tiles[checkX, checkY].Item).fruitType != fruitType)
                            break;

                        line.Add(_tiles[checkX, checkY]);
                        step++;
                    }

                    // Если линия >= 3, добавляем
                    if (line.Count >= 3)
                    {
                        lines.Add(line.ToArray());
                        foreach (Tile tile in line)
                            checkedTiles[tile.X, tile.Y] = true; // Помечаем проверенными
                    }
                }
            }

            return lines;
        }

        // Объединяет пересекающиеся линии в одну комбинацию
        List<Tile[]> MergeIntersectingMatches(List<Tile[]> horizontalLines, List<Tile[]> verticalLines)
        {
            List<Tile[]> mergedMatches = new List<Tile[]>();
            List<HashSet<Tile>> matchGroups = new List<HashSet<Tile>>();

            // Добавляем все линии в группы
            foreach (Tile[] line in horizontalLines)
                AddLineToGroups(line, matchGroups);

            foreach (Tile[] line in verticalLines)
                AddLineToGroups(line, matchGroups);

            // Преобразуем группы в массивы
            foreach (var group in matchGroups)
                mergedMatches.Add(group.ToArray());

            return mergedMatches;
        }

        // Добавляет линию в соответствующую группу (или создаёт новую)
        void AddLineToGroups(Tile[] line, List<HashSet<Tile>> matchGroups)
        {
            HashSet<Tile> existingGroup = null;

            // Ищем группу, в которой уже есть хотя бы один тайл из линии
            foreach (Tile tile in line)
            {
                foreach (var group in matchGroups)
                {
                    if (group.Contains(tile))
                    {
                        existingGroup = group;
                        break;
                    }
                }
                if (existingGroup != null)
                    break;
            }

            // Если нашли группу → добавляем в неё все тайлы линии
            if (existingGroup != null)
            {
                foreach (Tile tile in line)
                    existingGroup.Add(tile);
            }
            else // Иначе создаём новую группу
            {
                HashSet<Tile> newGroup = new HashSet<Tile>(line);
                matchGroups.Add(newGroup);
            }
        }
    }

    private IEnumerator ClearAndFillTiles()
    {
        List<Tile[]> matches = FindMatches();
        bool condition = false;
        do
        {
            bool hasMatches = matches.Count > 0;
            if (hasMatches) ManageMatches(matches);

            yield return StartCoroutine(FillBoard());

            matches = FindMatches();
            condition = matches.Count > 0;
        } while (condition);
        
        
        void ManageMatches(List<Tile[]> matches)
        {
            foreach (var match in matches)
            {
                int combinationNum = match.Length;
                Tile firstTile = match[0];
                firstTile.ClearItem();
                foreach (var powerup in _powerupPrefabs)
                {
                    if (combinationNum == powerup.GetComponent<Powerup>().CombinationNum)
                    {
                        firstTile.CreateItem(powerup.GetComponent<Item>());
                    }
                }

                foreach (var tile in match)
                {
                    if (tile == firstTile) continue;
                    tile.ClearItem();
                }
            }
        }
        IEnumerator FillBoard()
        {
            bool needsRefill = true;

            while (needsRefill)
            {
                yield return new WaitForSeconds(0.2f);
                _fallCoroutines.Clear();

                // Падение существующих фруктов
                FallExistingFruits();

                // Заполнение пустых мест новыми фруктами
                FillEmptyTiles();

                // Ожидание завершения всех анимаций
                yield return WaitForAllCoroutines();

                needsRefill = CheckForEmptyTiles();
            }
        }
        void FallExistingFruits()
        {
            // Проходим снизу вверх (по Y)
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_tiles[x, y].Item == null)
                    {
                        // Ищем первый фрукт выше текущей позиции
                        for (int ny = y + 1; ny < _height; ny++)
                        {
                            if (_tiles[x, ny].Item != null)
                            {
                                Item item = _tiles[x, ny].Item;
                                _tiles[x, ny].Item = null;
                                _tiles[x, y].Item = item;
                                item.transform.SetParent(_tiles[x, y].transform, true);

                                StartFallAnimation(item, _tiles[x, y]);
                                break;
                            }
                        }
                    }
                }
            }
        }
        void FillEmptyTiles()
        {
            // Проходим сверху вниз (по Y)
            for (int x = 0; x < _width; x++)
            {
                for (int y = _height - 1; y >= 0; y--)
                {
                    if (_tiles[x, y].Item == null)
                    {
                        // Создаём новый фрукт выше верхней границы
                        Vector3 spawnPosition = new Vector3(
                            _tiles[x, y].transform.position.x,
                            _tiles[x, _height - 1].transform.position.y + y * _tileSize, // Добавляем смещение для эффекта каскада
                            1
                        );

                        GameObject newFruit = Instantiate(
                            _fruitPrefabs[Random.Range(0, _fruitPrefabs.Length)],
                            spawnPosition,
                            Quaternion.identity
                        );

                        Item item = newFruit.GetComponent<Item>();
                        _tiles[x, y].Item = item;
                        item.transform.SetParent(_tiles[x, y].transform);

                        item.transform.localScale = Vector3.one * _itemSize;
                        item.SetName();

                        StartFallAnimation(item, _tiles[x, y]);
                    }
                }
            }
        }
        void StartFallAnimation(Item item, Tile targetTile)
        {
            Coroutine fallCoroutine = StartCoroutine(AnimateItemFall(item, targetTile));
            _fallCoroutines.Add(fallCoroutine);
        }
        IEnumerator AnimateItemFall(Item item, Tile targetTile)
        {
            Vector3 startPosition = item.transform.position;
            Vector3 endPosition = targetTile.transform.position;
            float distance = Mathf.Abs(startPosition.y - endPosition.y);
            float duration = distance / _fallSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                // Используем квадратичную easing-функцию для более естественного падения
                t = Mathf.SmoothStep(0f, 1f, t);
                item.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            item.transform.position = endPosition;
        }
        IEnumerator WaitForAllCoroutines()
        {
            foreach (Coroutine coroutine in _fallCoroutines)
            {
                yield return coroutine;
            }
        }
        bool CheckForEmptyTiles()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_tiles[x, y].Item == null)
                        return true;
                }
            }
            return false;
        }
    }

    public IEnumerator ClickOnTile(Tile tile)
    {

        if (tile.Item == null)
        {
            yield break;
        }

        if (tile.IsSelected)
        {
            tile.Deselect();
            _previousSelected = null;
        }
        else
        {
            if (_previousSelected == null)
            {
                tile.Select(_selectedColor, _mpSelectedSizePercent);
                _previousSelected = tile;
            }
            else
            {
                if ( System.Math.Abs(_previousSelected.X - tile.X) > 1 || System.Math.Abs(_previousSelected.Y - tile.Y) > 1 || 
                    System.Math.Abs(_previousSelected.X - tile.X) == System.Math.Abs(_previousSelected.Y - tile.Y))
                {
                    _previousSelected.Deselect();
                    _previousSelected = null;
                }
                else
                {
                    
                    _previousSelected.Deselect();
                    yield return StartCoroutine(SwapItemsWithAnimation(_previousSelected, tile));

                    if (tile.Item is Fruit && _previousSelected.Item is Fruit && FindMatches().Count <= 0)
                    {
                        yield return StartCoroutine(SwapItemsWithAnimation(_previousSelected, tile));
                        _previousSelected = null;
                    }
                    else
                    {
                        _previousSelected = null;
                        yield return StartCoroutine(ClearAndFillTiles());
                    }

                }
            }
        }
    }
    private IEnumerator SwapItemsWithAnimation(Tile tile1, Tile tile2)
    {
        // Получаем предметы из тайлов
        Item item1 = tile1.Item;
        Item item2 = tile2.Item;

        // Если один из предметов отсутствует, не выполняем анимацию
        if (item1 == null || item2 == null)
        {
            SwapItemInTwoTiles(tile1, tile2);
            yield break;
        }

        // Запоминаем начальные позиции
        Vector3 startPos1 = item1.transform.position;
        Vector3 startPos2 = item2.transform.position;

        // Параметры анимации
        float duration = _swapDuration; // Длительность анимации в секундах
        float elapsedTime = 0f;

        if (item1.TryGetComponent<SpriteRenderer>(out var sr1))
            sr1.sortingOrder = 10; // Поднимаем над другими предметами

        if (item2.TryGetComponent<SpriteRenderer>(out var sr2))
            sr2.sortingOrder = 10;


        // Анимация перемещения
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            // Используем SmoothStep для более естественного движения
            progress = Mathf.SmoothStep(0f, 1f, progress);

            // Плавно перемещаем предметы
            item1.transform.position = Vector3.Lerp(startPos1, startPos2, progress);
            item2.transform.position = Vector3.Lerp(startPos2, startPos1, progress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        float overshoot = _swapOvershoot; // Насколько сильно предметы "залетят" дальше цели
        float bounceDuration = _swapBounceDuration;

        Vector3 overshootPos1 = startPos2 + (startPos2 - startPos1).normalized * overshoot;
        Vector3 overshootPos2 = startPos1 + (startPos1 - startPos2).normalized * overshoot;

        elapsedTime = 0f;
        while (elapsedTime < bounceDuration)
        {
            float progress = elapsedTime / bounceDuration;
            item1.transform.position = Vector3.Lerp(overshootPos1, startPos2, progress);
            item2.transform.position = Vector3.Lerp(overshootPos2, startPos1, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        sr1.sortingOrder = 1;
        sr2.sortingOrder = 1;

        // Убедимся, что предметы точно встали на свои места
        item1.transform.position = startPos2;
        item2.transform.position = startPos1;

        // Фактически меняем предметы местами в данных
        SwapItemInTwoTiles(tile1, tile2);

        void SwapItemInTwoTiles(Tile tile1, Tile tile2)
        {
            Item temp = tile1.Item;
            tile1.Item = tile2.Item;
            tile2.Item = temp;

            // Обновляем родительские transform'ы
            if (tile1.Item != null)
                tile1.Item.transform.SetParent(tile1.transform);
            if (tile2.Item != null)
                tile2.Item.transform.SetParent(tile2.transform);
        }
    }
}