using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Board : MonoBehaviour
{
    [SerializeField] private int _width;
    [SerializeField] private int _height;

    [SerializeField] private GameObject[] _fruits;
    [SerializeField] private GameObject[] _powerups;

    [SerializeField] private GameObject _tilePrefab;

    [SerializeField] private float _tileSize;
    [SerializeField] private float _itemSize;

    private Tile _previousSelected;
    [SerializeField] private Color _selectedColor = Color.gray;

    private Tile[,] _tiles;

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
                    int random = Random.Range(0, _fruits.Length);

                    tile.SetItem(_fruits[random].GetComponent<Item>());
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
        int width = _tiles.GetLength(0);
        int height = _tiles.GetLength(1);
        bool[,] checkedTiles = new bool[width, height];

        // 1. Сначала находим ВСЕ линии (горизонтальные и вертикальные)
        List<Tile[]> horizontalLines = FindLines(checkedTiles, true);
        List<Tile[]> verticalLines = FindLines(checkedTiles, false);

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
        List<Tile[]> FindLines(bool[,] checkedTiles, bool isHorizontal)
        {
            List<Tile[]> lines = new List<Tile[]>();
            int width = _tiles.GetLength(0);
            int height = _tiles.GetLength(1);

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

    private void ClearAndFillTiles()
    {
        List<Tile[]> matches = FindMatches();
        bool condition = false;
        do
        {
            bool hasMatches = matches.Count > 0;
            if (hasMatches) ManageMatches(matches);

            for (int column = 0; column < _tiles.GetLength(0); column++)
            {
                FillColumn(column);
            }

            matches = FindMatches();
            condition = matches.Count > 0;
        } while (condition);

    }
    private void ManageMatches(List<Tile[]> matches)
    {
        foreach(var match in matches)
        {
            int combinationNum = match.Length;
            foreach (var tile in match)
            {
                tile.ClearItem();
            }
        }
    }

    private void FillColumn(int column)
    {
        Tile[] tiles = new Tile[_tiles.GetLength(1)];
        for (int i = 0; i < _tiles.GetLength(1); i++)
        {
            tiles[i] = _tiles[column, i];
        }

        bool hasNullItemUnder;
        do
        {
            hasNullItemUnder = false;
            foreach (var tile in tiles)
            {
                if (tile.Y == 0 || tile.Item == null) continue;
                if (tiles[tile.Y - 1].Item == null)
                {
                    hasNullItemUnder = true;

                    tiles[tile.Y - 1].SetItem(tile.Item);
                    tile.ClearItem();
                }
            }
        } while (hasNullItemUnder);

        foreach (var tile in tiles)
        {
            if (tile.Item == null)
            {
                int random = Random.Range(0, _fruits.Length);
                tile.SetItem(_fruits[random].GetComponent<Item>());
            }
        }
    }

    public void ClickOnTile(Tile tile)
    {

        if (tile.Item == null)
        {
            return;
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
                tile.Select(_selectedColor);
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
                    SwapItemInTwoTiles(_previousSelected, tile);

                    if (tile.Item is Fruit && _previousSelected.Item is Fruit && FindMatches().Count <= 0)
                    {
                        SwapItemInTwoTiles(_previousSelected, tile);
                        _previousSelected = null;
                    }
                    else
                    {
                        _previousSelected = null;
                        ClearAndFillTiles();
                    }

                }
            }
        }
    }

    private void SwapItemInTwoTiles(Tile tile1, Tile tile2)
    {
        Item tempItem = tile1.Item;

        tile1.ClearItem();
        tile1.SetItem(tile2.Item);

        tile2.ClearItem();
        tile2.SetItem(tempItem);
    }
}


