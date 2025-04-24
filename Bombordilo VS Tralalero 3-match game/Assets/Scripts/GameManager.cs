using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Board board;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        board.CrerateAndFillBoard();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
