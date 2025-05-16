using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Scriptable Objects/Level")]
public class Level : ScriptableObject
{
    public int LevelNumber;

    public Boss Boss;

    public Sprite Background;

    public int PointsOnSilver;
    public int PointsOnGold;

    public int Jellies;
    public int Locks;
}
