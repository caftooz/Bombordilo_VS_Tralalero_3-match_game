using UnityEngine;

public abstract class Powerup : Item
{
    public abstract void Use();
    public abstract int CombinationNum { get;}
}
