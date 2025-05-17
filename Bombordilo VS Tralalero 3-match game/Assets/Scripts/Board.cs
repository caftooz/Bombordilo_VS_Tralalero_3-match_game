using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

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
    [SerializeField, Range(100, 200)] private int _mpSelectedSizePercent = 105;

    [Header("Fall Animation Settings")]
    [SerializeField] private float _baseFallSpeed = 5f;
    [SerializeField] private float _bounceHeight = 0.3f;
    [SerializeField] private float _bounceDurationRatio = 0.2f;
    [SerializeField] private float _rotationAmount = 15f;
    [SerializeField] private float _scalePulseAmount = 0.1f;
    [SerializeField] private AnimationCurve _fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField]
    private AnimationCurve _bounceCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.5f, 1),
        new Keyframe(1, 0)
    );

    [Header("Swap Animation Settings")]
    [SerializeField] private float _swapDuration = 0.1f;
    [SerializeField] private float _swapOvershoot = 0.2f;
    [SerializeField] private float _swapBounceDuration = 0.15f;

    [Header("Burst Animation Settings")]
    [SerializeField] private float _burstMaxScale = 2f;
    [SerializeField] private float _burstDuration = 0.2f;
    [SerializeField] private AnimationCurve _burstScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private GameObject _burstItemPrefab;

    [Header("MoveToBoss Items Animation Settings")]
    [SerializeField] private float _moveToBossDuration = 0.2f;
    [SerializeField] private AnimationCurve _moveToBossCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Firework Settings")]
    [SerializeField] private float _fireworkSpeed = 10f;
    [SerializeField] private float _fireworkDuration = 0.5f;
    [SerializeField] private GameObject _flyingFireworkPrefab;

    [Header("Bomb Settings")]
    [SerializeField] private int _explosionRadius = 3;
    [SerializeField] private float _explosionDuration = 0.5f;
    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private float _minExplosionScale = 1f;
    [SerializeField] private float _maxExplosionScale = 3f;

    [Header("Multifruit Settings")]
    [SerializeField] private GameObject _multifruitActivationPrefab;
    [SerializeField] private GameObject _laserPrefab;
    [SerializeField] private float _laserDuration = 0.3f;
    [SerializeField] private GameObject _laserHitPrefab;
    [SerializeField] private float _laserFadeDuration = 0.2f;

    private bool _isPlayingAnim;

    public event Action DoStep;
    public event Action<FruitType> DamageBoss;
    public event Action DamageBossFirework;
    public event Action<int> AddPoints;

    private Tile[,] _tiles;
    private List<Coroutine> _fallCoroutines = new List<Coroutine>();

    private bool isPaused = false;
    private void Update()
    {
        ManagePause();
    }

    public void ManagePause()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isPaused)
            {
                Time.timeScale = 1;
                isPaused = false;
            }
            else
            {
                Time.timeScale = 0;
                isPaused = true;
            }
        }
    }
    public void CreateAndFillBoard()
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
                    int random = UnityEngine.Random.Range(0, _fruitPrefabs.Length);

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
                        Destroy(tile.Item.gameObject);
                        tile.Item = null;
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
                tiles[x, y].SetTileProp(x, y, _itemSize, _burstMaxScale, _burstDuration, _burstScaleCurve, _burstItemPrefab, DamageBoss, AddPoints, _moveToBossDuration, _moveToBossCurve);
                tiles[x, y].transform.localScale = Vector3.one * _tileSize;
                tiles[x, y].transform.localPosition = new Vector3(x * (_tileSize), y * (_tileSize), 1);
                tiles[x, y].gameObject.name = $"Tile[{x},{y}]";
            }
        }
        return tiles;
    }
    private void ClearBoard()
    {
        _isPlayingAnim = false;
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
                            (_tiles[checkX, checkY].Item as Fruit)?.fruitType != fruitType)
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
    //==================================================================================
    private IEnumerator ClearAndFillTiles()
    {
        _isPlayingAnim = true;
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
        _isPlayingAnim = false;


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
                            _fruitPrefabs[UnityEngine.Random.Range(0, _fruitPrefabs.Length)],
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
            Transform itemTransform = item.transform;
            Vector3 startPosition = itemTransform.position;
            Vector3 endPosition = targetTile.transform.position;

            // Рассчитываем длительность падения на основе расстояния
            float distance = Mathf.Abs(startPosition.y - endPosition.y);
            float fallDuration = distance / _baseFallSpeed;

            // Эффект "подпрыгивания" в конце
            float bounceDuration = fallDuration * _bounceDurationRatio;
            float totalDuration = fallDuration + bounceDuration;

            // Начальные параметры для дополнительных эффектов
            Quaternion startRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-_rotationAmount, _rotationAmount) + itemTransform.localRotation.z);
            Quaternion endRotation = itemTransform.localRotation;
            Vector3 startScale = Vector3.one * (1f + _scalePulseAmount) * _itemSize;
            Vector3 endScale = Vector3.one * _itemSize;

            float elapsedTime = 0f;

            while (elapsedTime < totalDuration)
            {
                float fallProgress = Mathf.Clamp01(elapsedTime / fallDuration);
                float bounceProgress = Mathf.Clamp01((elapsedTime - fallDuration) / bounceDuration);

                // Основное падение
                float curveProgress = _fallCurve.Evaluate(fallProgress);
                Vector3 fallPosition = Vector3.Lerp(startPosition, endPosition, curveProgress);

                // Добавляем "подпрыгивание" в конце
                if (bounceProgress > 0)
                {
                    float bounceHeight = _bounceCurve.Evaluate(bounceProgress) * _bounceHeight;
                    fallPosition.y += bounceHeight;
                }

                // Дополнительные эффекты
                float rotationProgress = _fallCurve.Evaluate(fallProgress);
                float scaleProgress = 1f + (_scalePulseAmount * (1f - fallProgress));

                if (item == null) yield break;

                // Применяем все трансформации
                itemTransform.position = fallPosition;
                itemTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, rotationProgress);
                itemTransform.localScale = Vector3.one * scaleProgress * _itemSize;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Гарантируем точное попадание в конечную позицию
            itemTransform.position = endPosition;
            itemTransform.localRotation = endRotation;
            itemTransform.localScale = endScale;

            // Небольшой эффект приземления
            StartCoroutine(LandingEffect(item));
        }
        IEnumerator LandingEffect(Item item)
        {
            float effectDuration = 0.15f;
            float elapsedTime = 0f;
            Vector3 originalScale = item.transform.localScale;
            Vector3 targetScale = originalScale * 1.1f;

            while (elapsedTime < effectDuration)
            {
                if (item.IsDestroyed()) yield break;
                float progress = elapsedTime / effectDuration;
                progress = Mathf.SmoothStep(0f, 1f, progress);

                if (progress < 0.5f)
                {
                    item.transform.localScale = Vector3.Lerp(originalScale, targetScale, progress * 2f);
                }
                else
                {
                    item.transform.localScale = Vector3.Lerp(targetScale, originalScale, (progress - 0.5f) * 2f);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            item.transform.localScale = originalScale;
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
    private void ManageMatches(List<Tile[]> matches)
    {
        foreach (var match in matches)
        {

            AddPoints?.Invoke(50);
            int combinationNum = match.Length;
            Tile firstTile = match[0];
            firstTile.ClearItem();
            if (combinationNum > 6)
            {
                foreach (var powerup1 in _powerupPrefabs)
                {
                    if (powerup1.GetComponent<Powerup>().CombinationNum == 6)
                    {
                        firstTile.CreateItem(powerup1.GetComponent<Item>());
                    }
                }
            }
            else foreach (var powerup in _powerupPrefabs)
            {
                if (combinationNum == powerup.GetComponent<Powerup>().CombinationNum)
                {
                    if (powerup.GetComponent<Powerup>().powerupType == PowerupType.FireworkV ||
                        powerup.GetComponent<Powerup>().powerupType == PowerupType.FireworkH)
                    {
                        bool isHorizontal = isHorisontalMatch(match);
                        if (powerup.GetComponent<Powerup>().powerupType == PowerupType.FireworkV && isHorizontal) continue;
                        if (powerup.GetComponent<Powerup>().powerupType == PowerupType.FireworkH && !isHorizontal) continue;
                    }
                    firstTile.CreateItem(powerup.GetComponent<Item>());
                }
            }

            foreach (var tile in match)
            {
                if (tile == firstTile) continue;
                tile.ClearItem();
            }
        }

        bool isHorisontalMatch(Tile[] match)
        {
            if (match[0].Y - match[1].Y == 0) return true;
            return false;
        }
    }

    public void OnTileOver(Tile tile)
    {
        if (_previousSelected != null && !_isPlayingAnim)
        {
            if( (System.Math.Abs(_previousSelected.Y - tile.Y) == 1 && _previousSelected.X == tile.X) ||
                 System.Math.Abs(_previousSelected.X - tile.X) == 1 && _previousSelected.Y == tile.Y)
            {
                tile.Select(_selectedColor, condition: false);
            }
        }
    }

    public void OnTileExit(Tile tile)
    {
        if (tile != _previousSelected)
        {
            tile.Deselect(condition: false);
        }
    }

    public IEnumerator ClickOnTile(Tile tile)
    {

        if (tile.Item == null || _isPlayingAnim)
        {
            yield break;
        }

        if (tile.IsSelected)
        {
            tile.Deselect();
            _previousSelected = null;
            if (tile.Item is Powerup powerup)
            {
                yield return StartCoroutine(UsePowerup(powerup, null));
                yield return StartCoroutine(ClearAndFillTiles());
                DoStep?.Invoke();
            }
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
                if(tile.Item != null) tile.Deselect(condition: false);
                if ( System.Math.Abs(_previousSelected.X - tile.X) > 1 || System.Math.Abs(_previousSelected.Y - tile.Y) > 1 || 
                    System.Math.Abs(_previousSelected.X - tile.X) == System.Math.Abs(_previousSelected.Y - tile.Y))
                {
                    _previousSelected.Deselect();
                    _previousSelected = null;
                }
                else
                {
                    _isPlayingAnim = true;
                    _previousSelected.Deselect();
                    yield return StartCoroutine(SwapItemsWithAnimation(_previousSelected, tile));
                    if (_previousSelected.Item is Powerup || tile.Item is Powerup)
                    {
                        List<Tile[]> matches = FindMatches();
                        bool hasMatches = matches.Count > 0;
                        if ((_previousSelected.Item is not Powerup powerup1 || powerup1.powerupType != PowerupType.Multifruit) && 
                            (tile.Item is not Powerup powerup2 || powerup2.powerupType != PowerupType.Multifruit))
                            if (hasMatches) ManageMatches(matches);
                                
                        yield return StartCoroutine(UsePowerups(_previousSelected, tile));
                    }

                    if (tile.Item is Fruit && _previousSelected.Item is Fruit && FindMatches().Count <= 0)
                    {
                        yield return StartCoroutine(SwapItemsWithAnimation(_previousSelected, tile));
                        _previousSelected = null;

                        _isPlayingAnim = false;
                    }
                    else
                    {
                        _previousSelected = null;
                        yield return StartCoroutine(ClearAndFillTiles());
                        DoStep?.Invoke();
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
    //==================================================================================
    private IEnumerator UsePowerups(Tile tile1, Tile tile2)
    {
        if (tile1.Item is Powerup && tile2.Item is Powerup)
        {
            Powerup powerup1 = (Powerup)tile1.Item;
            Powerup powerup2 = (Powerup)tile2.Item;

            switch (powerup1.powerupType)
            {
                case PowerupType.FireworkH:
                    switch (powerup2.powerupType)
                    {
                        case PowerupType.FireworkH:
                        case PowerupType.FireworkV:
                            yield return StartCoroutine(UseFireworkCoss(powerup1, powerup2));
                            break;
                        case PowerupType.Bomb:
                            yield return StartCoroutine(UseFireworkTriple(powerup1, powerup2, true));
                            break;
                        case PowerupType.Multifruit:
                            yield return StartCoroutine(UseMultifruitWithPowerup(powerup2, powerup1));
                            break;
                    }
                    break;
                case PowerupType.FireworkV:
                    switch (powerup2.powerupType)
                    {
                        case PowerupType.FireworkH:
                        case PowerupType.FireworkV:
                            yield return StartCoroutine(UseFireworkCoss(powerup1, powerup2));
                            break;
                        case PowerupType.Bomb:
                            yield return StartCoroutine(UseFireworkTriple(powerup1, powerup2, false));
                            break;
                        case PowerupType.Multifruit:
                            yield return StartCoroutine(UseMultifruitWithPowerup(powerup2, powerup1));
                            break;
                    }
                    break;
                case PowerupType.Bomb:
                    switch (powerup2.powerupType)
                    {
                        case PowerupType.FireworkH:
                            yield return StartCoroutine(UseFireworkTriple(powerup2, powerup1, true));
                            break;
                        case PowerupType.FireworkV:
                            yield return StartCoroutine(UseFireworkTriple(powerup2, powerup1, false));
                            break;
                        case PowerupType.Bomb:
                            yield return StartCoroutine(UseSuperBomb(powerup1, powerup2));
                            break;
                        case PowerupType.Multifruit:
                            yield return StartCoroutine(UseMultifruitWithPowerup(powerup2, powerup1));
                            break;
                    }
                    break;
                case PowerupType.Multifruit:
                    switch (powerup2.powerupType)
                    {
                        case PowerupType.FireworkH:
                        case PowerupType.FireworkV:
                        case PowerupType.Bomb:
                            yield return StartCoroutine(UseMultifruitWithPowerup(powerup1, powerup2));
                            break;
                        case PowerupType.Multifruit:
                            UseMultifruitWithMultifruit(powerup1, powerup2);
                            break;
                    }
                    break;
            }
            IEnumerator UseFireworkCoss(Powerup firework1, Powerup firework2)
            {
                AddPoints?.Invoke(250);
                Tile f1Tile = firework1.GetComponentInParent<Tile>();
                Tile f2Tile = firework2.GetComponentInParent<Tile>();
                f1Tile.ClearItem();
                f2Tile.ClearItem();
             
                f1Tile.CreateItem(_powerupPrefabs.First(pu => pu.GetComponent<Powerup>().powerupType == PowerupType.FireworkH).GetComponent<Item>());
                StartCoroutine(UseFirework(f1Tile.Item as Powerup, true));
                yield return new WaitWhile(() => f1Tile.Item != null);
                f1Tile.CreateItem(_powerupPrefabs.First(pu => pu.GetComponent<Powerup>().powerupType == PowerupType.FireworkV).GetComponent<Item>());
                yield return StartCoroutine(UseFirework(f1Tile.Item as Powerup, false));
            }
            IEnumerator UseFireworkTriple(Powerup firework, Powerup bomb, bool isHorisontal)
            {

                AddPoints?.Invoke(400);
                Tile bombTile = bomb.GetComponentInParent<Tile>();
                bombTile.ClearItem();

                List<Coroutine> FierworkCoroutines = new List<Coroutine>();
                Tile fireworkTile = firework.GetComponentInParent<Tile>();
                FierworkCoroutines.Add(StartCoroutine(UseFirework(firework, isHorisontal)));
                List<Tile> tiles = new List<Tile>();
                if (isHorisontal)
                {
                    if (fireworkTile.Y < _height - 1)
                        tiles.Add(_tiles[fireworkTile.X, fireworkTile.Y + 1]);
                    if (fireworkTile.Y > 0)
                        tiles.Add(_tiles[fireworkTile.X, fireworkTile.Y - 1]);
                }
                else
                {
                    if (fireworkTile.X < _width - 1)
                        tiles.Add(_tiles[fireworkTile.X + 1, fireworkTile.Y]);
                    if (fireworkTile.X > 0)
                        tiles.Add(_tiles[fireworkTile.X - 1, fireworkTile.Y]);
                }

                foreach (var tile in tiles)
                {
                    tile.ClearItem();
                    if (isHorisontal)
                    {
                        tile.CreateItem(_powerupPrefabs.First(pu => pu.GetComponent<Powerup>().powerupType == PowerupType.FireworkH).GetComponent<Item>());
                        FierworkCoroutines.Add(StartCoroutine(UseFirework(tile.Item as Powerup, isHorisontal)));
                    }
                    else
                    {
                        tile.CreateItem(_powerupPrefabs.First(pu => pu.GetComponent<Powerup>().powerupType == PowerupType.FireworkV).GetComponent<Item>());
                        FierworkCoroutines.Add(StartCoroutine(UseFirework(tile.Item as Powerup, isHorisontal)));
                    }
                }

                yield return WaitCor(FierworkCoroutines);

                IEnumerator WaitCor(List<Coroutine> coroutines)
                {
                    foreach (var coroutine in coroutines)
                    {
                        yield return coroutine;
                    }
                }

            }
            IEnumerator UseSuperBomb(Powerup bomb1, Powerup bomb2)
            {
                AddPoints?.Invoke(500);
                Tile bomb2Tile = bomb2.GetComponentInParent<Tile>();
                Destroy(bomb2Tile.Item.gameObject);
                bomb2Tile.Item = null;
                yield return StartCoroutine(UseBomb(bomb1, _explosionRadius + 1));
            }
            IEnumerator UseMultifruitWithPowerup(Powerup multifruit, Powerup powerup)
            {
                AddPoints?.Invoke(600);
                Tile multifruitTile = multifruit.GetComponentInParent<Tile>();

                Vector3 startPosition = GetWorldPosition(multifruitTile.X, multifruitTile.Y);

                multifruitTile.ClearItem();
                powerup.GetComponentInParent<Tile>().ClearItem();

                GameObject activationEffect = Instantiate(_multifruitActivationPrefab, startPosition, Quaternion.identity);
                yield return new WaitForSeconds(0.3f); // Ждем начало анимации

                List<GameObject> lasers = new List<GameObject>();

                List<Coroutine> usePowerupCoroutins = new List<Coroutine>();
                var tilesFruit = (from Tile tile in _tiles where (tile.Item is Fruit) select tile).ToList<Tile>();

                List<Tile> tilsToDestroy = new List<Tile>();
                for (int i = 0; i < 5; i++)
                {
                    int random = UnityEngine.Random.Range(0, tilesFruit.Count());

                    GameObject laser = CreateLaser(startPosition, GetWorldPosition(tilesFruit[random].X, tilesFruit[random].Y));
                    lasers.Add(laser);

                    tilsToDestroy.Add(tilesFruit[random]);

                    tilesFruit.Remove(tilesFruit[random]);
                }

                yield return new WaitForSeconds(0.5f);

                foreach (var tile in tilsToDestroy)
                {
                    tile.ClearItem();
                    tile.CreateItem(_powerupPrefabs.First(pr => pr.GetComponent<Powerup>().powerupType == powerup.powerupType).GetComponent<Item>());
                    usePowerupCoroutins.Add(StartCoroutine(UsePowerup(tile.Item.GetComponent<Powerup>(), null)));
                }

                foreach (var coroutine in usePowerupCoroutins)
                {
                    yield return coroutine;
                }

                foreach (GameObject laser in lasers)
                {
                    StartCoroutine(FadeOutAndDestroyLaser(laser));
                }

                Destroy(activationEffect);
            }
            void UseMultifruitWithMultifruit(Powerup multifruit, Powerup powerup)
            {
                AddPoints?.Invoke(800);
                multifruit.GetComponentInParent<Tile>().ClearItem();
                powerup.GetComponentInParent<Tile>().ClearItem();
                foreach (var tile in _tiles)
                {
                    if (tile.Item is Powerup powerupT && powerupT.powerupType == PowerupType.FireworkH) StartCoroutine(UseFirework(powerupT, true));
                    else
                        tile.ClearItem();
                }
            }

        } 
        else if (tile1.Item is Powerup && tile2.Item is not Powerup)
        {
            Powerup powerup = (Powerup)tile1.Item;
            Fruit   fruit   = (Fruit)  tile2.Item;

            yield return StartCoroutine(UsePowerup(powerup, fruit));
        }
        else if (tile1.Item is not Powerup && tile2.Item is Powerup)
        {
            Powerup powerup = (Powerup)tile2.Item;
            Fruit   fruit   = (Fruit)  tile1.Item;

            yield return StartCoroutine(UsePowerup(powerup, fruit));
        }
    }
    private IEnumerator UsePowerup(Powerup powerup, Fruit fruit)
    {
        switch (powerup.powerupType)
        {
            case PowerupType.FireworkH:
                AddPoints?.Invoke(100);
                yield return StartCoroutine(UseFirework(powerup, isHorizontal: true));
                break;
            case PowerupType.FireworkV:
                AddPoints?.Invoke(100);
                yield return StartCoroutine(UseFirework(powerup, isHorizontal: false));
                break;
            case PowerupType.Bomb:
                AddPoints?.Invoke(200);
                yield return StartCoroutine(UseBomb(powerup, _explosionRadius));
                break;
            case PowerupType.Multifruit:
                AddPoints?.Invoke(300);
                yield return StartCoroutine(UseMultifruit(powerup, fruit));
                break;
            default:
                break;
        }
    }
    //==================================================================================
    private IEnumerator UseFirework(Powerup firework, bool isHorizontal)
    {
        yield return StartCoroutine(UseFireworkCoroutine(firework, isHorizontal));
    }
    private IEnumerator UseFireworkCoroutine(Powerup powerup, bool isHorizontal)
    {
        // Находим тайл, в котором находится этот фейерверк
        Tile fireworkTile = powerup.GetComponentInParent<Tile>();

        // Удаляем фейерверк с доски
        fireworkTile.ClearItem();

        // Создаем два фейерверка, которые летят в противоположные стороны
        Quaternion angle1 = isHorizontal ? Quaternion.Euler(0, 0, 0)   : Quaternion.Euler(0, 0,  90);
        Quaternion angle2 = isHorizontal ? Quaternion.Euler(0, 0, 180) : Quaternion.Euler(0, 0, -90);

        GameObject firework1 = Instantiate(_flyingFireworkPrefab, GetWorldPosition(fireworkTile.X, fireworkTile.Y), angle1);
        firework1.transform.localScale = Vector3.one * _itemSize;
        GameObject firework2 = Instantiate(_flyingFireworkPrefab, GetWorldPosition(fireworkTile.X, fireworkTile.Y), angle2);
        firework2.transform.localScale = Vector3.one * _itemSize;


        // Направления движения
        Vector3 dir1 = isHorizontal ? Vector3.right : Vector3.up;
        Vector3 dir2 = isHorizontal ? Vector3.left : Vector3.down;

        // Запускаем корутины движения фейерверков
        StartCoroutine(MoveFirework(firework1, fireworkTile.X, fireworkTile.Y, dir1, isHorizontal));
        StartCoroutine(MoveFirework(firework2, fireworkTile.X, fireworkTile.Y, dir2, isHorizontal));

        // Ждем завершения анимации
        yield return new WaitForSeconds(_fireworkDuration);

        // Уничтожаем фейерверки
        Destroy(firework1);
        Destroy(firework2);

    }
    private IEnumerator MoveFirework(GameObject firework, int startX, int startY, Vector3 direction, bool isHorizontal)
    {
        float distance = 0f;
        Vector3 startPos = GetWorldPosition(startX, startY);

        while (distance < Mathf.Max(_width, _height))
        {
            if (firework.IsDestroyed()) break;
            firework.transform.position = startPos + direction * distance;
            distance += _fireworkSpeed * Time.deltaTime;

            // Проверяем тайлы, через которые проходит фейерверк
            CheckTilesUnderFirework(startX, startY, direction, distance, isHorizontal);

            yield return null;
        }
        if (isHorizontal) DamageBossFirework?.Invoke();
    }
    private void CheckTilesUnderFirework(int startX, int startY, Vector3 direction, float distance, bool isHorizontal)
    {
        // Определяем текущую позицию фейерверка
        int currentX = startX + Mathf.RoundToInt(direction.x * distance);
        int currentY = startY + Mathf.RoundToInt(direction.y * distance);

        // Проверяем, находится ли позиция в пределах доски
        if (currentX >= 0 && currentX < _width && currentY >= 0 && currentY < _height)
        {
            Tile tile = _tiles[currentX, currentY];
            if (tile.Item != null)
            {
                if (tile.Item is Powerup p)
                {
                    StartCoroutine(UsePowerup(p, null));
                }
                else
                    tile.ClearItem();
            }
        }
    }
    //==================================================================================
    private Vector3 GetWorldPosition(int x, int y)
    {
        return _tiles[x,y].transform.position;
    }
    //==================================================================================
    private IEnumerator UseBomb(Powerup bomb, int explosionRadius)
    {
        // Находим тайл с бомбой
        Tile bombTile = bomb.GetComponentInParent<Tile>();
        if (bombTile == null) yield break;

        int bombX = bombTile.X;
        int bombY = bombTile.Y;

        // Удаляем бомбу с доски
        bombTile.ClearItem();

        // Создаем эффект взрыва
        GameObject explosion = Instantiate(_explosionPrefab, GetWorldPosition(bombX, bombY), Quaternion.identity);

        // Получаем все тайлы в радиусе взрыва
        List<Tile> affectedTiles = GetTilesInRadius(bombX, bombY, explosionRadius);

        // Анимация взрыва (можно добавить масштабирование или эффект волны)
        float timer = 0f;
        while (timer < _explosionDuration)
        {
            float scale = Mathf.Lerp(_minExplosionScale, _maxExplosionScale, timer / _explosionDuration);
            explosion.transform.localScale = Vector3.one * scale;
            timer += Time.deltaTime;
            yield return null;
        }

        // Уничтожаем предметы в пораженных тайлах
        foreach (Tile tile in affectedTiles)
        {

            if (tile.Item != null)
            {
                if (tile.Item is Powerup p)
                {
                    StartCoroutine(UsePowerup(p, null));
                }
                else
                    tile.ClearItem();
            }
        }

        // Уничтожаем эффект взрыва
        Destroy(explosion);

    }
    private List<Tile> GetTilesInRadius(int centerX, int centerY, int radius)
    {
        List<Tile> result = new List<Tile>();

        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                // Проверяем, находится ли тайл в пределах доски
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    // Для кругового радиуса можно добавить проверку расстояния:
                    float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));
                    if (distance <= radius)
                        result.Add(_tiles[x, y]);
                }
            }
        }

        return result;
    }
    //==================================================================================
    private IEnumerator UseMultifruit(Powerup multifruit, Fruit targetFruit)
    {
        // Находим тайл с мультифруктом
        Tile multifruitTile = multifruit.GetComponentInParent<Tile>();

        Vector3 startPosition = GetWorldPosition(multifruitTile.X, multifruitTile.Y);

        // Удаляем мультифрукт с доски
        multifruitTile.ClearItem();

        // Определяем какие фрукты будем уничтожать
        List<Fruit> fruitsToDestroy = new List<Fruit>();
        FruitType targetType;

        if (targetFruit != null)
        {
            targetType = targetFruit.fruitType;
            fruitsToDestroy = GetAllFruitsOfType(targetType);
        }
        else
        {
            // Режим 2: уничтожаем случайные 1/5 всех фруктов
            int fruitsCount = CountAllFruits();
            int fruitsToDestroyCount = Mathf.Max(1, fruitsCount / 5);
            fruitsToDestroy = GetRandomFruits(fruitsToDestroyCount);
        }

        // Создаем эффект активации в центре
        GameObject activationEffect = Instantiate(_multifruitActivationPrefab, startPosition, Quaternion.identity);
        yield return new WaitForSeconds(0.3f); // Ждем начало анимации

        // Запускаем лучи ко всем целям
        List<GameObject> lasers = new List<GameObject>();
        foreach (Fruit fruit in fruitsToDestroy)
        {
            if (fruit == null) continue;
            Tile fruitTile = fruit.GetComponentInParent<Tile>();
            if (fruitTile != null)
            {
                GameObject laser = CreateLaser(startPosition, GetWorldPosition(fruitTile.X, fruitTile.Y));
                lasers.Add(laser);
            }
        }

        // Ждем пока лучи достигнут целей
        yield return new WaitForSeconds(0.5f);

        // Уничтожаем все целевые фрукты
        foreach (Fruit fruit in fruitsToDestroy)
        {
            if (fruit == null) continue;
            Tile fruitTile = fruit.GetComponentInParent<Tile>();
            if (fruitTile != null && fruitTile.Item != null)
            {
                fruitTile.ClearItem();
            }
        }

        // Уничтожаем лучи с эффектом
        foreach (GameObject laser in lasers)
        {
            StartCoroutine(FadeOutAndDestroyLaser(laser));
        }

        // Уничтожаем эффект активации
        Destroy(activationEffect);
    }
    private GameObject CreateLaser(Vector3 startPos, Vector3 endPos)
    {
        GameObject laser = Instantiate(_laserPrefab, startPos, Quaternion.identity);
        LineRenderer lr = laser.GetComponent<LineRenderer>();

        // Настраиваем луч
        lr.SetPosition(0, startPos);
        lr.SetPosition(1, startPos); // Начинаем с точки

        // Анимация "выстрела" луча
        StartCoroutine(AnimateLaser(lr, startPos, endPos));

        return laser;
    }
    private IEnumerator AnimateLaser(LineRenderer lr, Vector3 startPos, Vector3 endPos)
    {
        float duration = _laserDuration; //0.3f
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            Vector3 currentEnd = Vector3.Lerp(startPos, endPos, progress);
            lr.SetPosition(1, currentEnd);
            yield return null;
        }

        // Эффект попадания
        Destroy(Instantiate(_laserHitPrefab, endPos, Quaternion.identity), 2);
    }
    private IEnumerator FadeOutAndDestroyLaser(GameObject laser)
    {
        LineRenderer lr = laser.GetComponent<LineRenderer>();
        Color startColor = lr.startColor;
        Color endColor = lr.endColor;

        float fadeDuration = _laserFadeDuration;//0.2f
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            lr.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            lr.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha);
            yield return null;
        }

        Destroy(laser);
    }
    private List<Fruit> GetAllFruitsOfType(FruitType type)
    {
        List<Fruit> result = new List<Fruit>();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_tiles[x, y].Item != null)
                if (_tiles[x, y].Item is Fruit fruit && fruit.fruitType == type)
                {
                    result.Add(fruit);
                }
            }
        }

        return result;
    }
    private List<Fruit> GetRandomFruits(int count)
    {
        List<Fruit> allFruits = new List<Fruit>();
        List<Fruit> result = new List<Fruit>();

        // Собираем все фрукты
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_tiles[x, y].Item != null)
                if (_tiles[x, y].Item is Fruit fruit)
                {
                    allFruits.Add(fruit);
                }
            }
        }

        // Выбираем случайные
        for (int i = 0; i < count && allFruits.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, allFruits.Count);
            result.Add(allFruits[randomIndex]);
            allFruits.RemoveAt(randomIndex);
        }

        return result;
    }
    private int CountAllFruits()
    {
        int count = 0;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_tiles[x, y].Item is Fruit)
                {
                    count++;
                }
            }
        }

        return count;
    }
}