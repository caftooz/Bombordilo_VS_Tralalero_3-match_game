using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Board _board;
    [SerializeField] private UIManager _UIManager;
    [SerializeField] private Level[] _levels;

    [SerializeField] private int _baseFruitDamage;
    [SerializeField] private int _criticalFruitDamage;

    [SerializeField] private int _baseFireworkDamage;
    [SerializeField] private int _criticalFireworkDamage;

    private int _currentLevelNumber = 1;
    private Level _currentLevel;
    private Boss _currentBoss;
    private int _currentBossPhase = 1;

    private int _currentPoints;
    private GameState gameState;

    private enum GameState
    {
        GameWin,
        GameOver,
        GamePlaying
    }
    void Start()
    {
        _board.DoStep += _UIManager.SpendStep;
        _board.DamageBoss += DamageBoss;
        _board.DamageBossFirework += DamageBossFirework;
        _board.AddPoints += AddPoints;

        _UIManager.OnBossDeath += BossDeath;
        _UIManager.OnStepEnd += GameOver;
    }

    private void AddPoints(int points)
    {
        _currentPoints += points;
        _UIManager.SetPoints(_currentPoints);
    }
    private void DamageBoss(FruitType fruitType)
    {
        if (_currentBoss is StandartBoss standartBoss && standartBoss.BossCriticalFruit == fruitType)
        {

            _UIManager.SpendBossHP(_criticalFruitDamage, true);
            
        }
        else
        {
            _UIManager.SpendBossHP(_baseFruitDamage, false);
        }
    }
    private void DamageBossFirework()
    {
        if (_currentBoss is SuperBoss)
        {
            _UIManager.SpendBossHP(_criticalFireworkDamage, true);
        }
        else
        {
            _UIManager.SpendBossHP(_baseFireworkDamage, false);
        }
    }
    public void SetLevel(int levelNumber)
    {
        _currentLevelNumber = levelNumber;

        _board.CreateAndFillBoard();

        _currentLevel = _levels.Where(l => l.LevelNumber == levelNumber).First();
        _currentBoss = _currentLevel.Boss;

        _currentBossPhase = 1;
        _currentPoints = 0;

        _UIManager.SetPoints(_currentPoints);
        _UIManager.DeactivatePanels();
        _UIManager.SetBackground(_currentLevel.Background);
        _UIManager.ResetStepSlider();
        _UIManager.SetBoss(
            bossHP:     _currentBoss.BossHp,
            bossMaxHP:  _currentBoss.BossHp,
            bossSprite: _currentBoss.BossSprite
        );

        gameState = GameState.GamePlaying;
    }
    private void BossDeath()
    {
        if ( _currentBoss is SuperBoss superBoss && _currentBossPhase == 1)
        {
            _currentBossPhase++;

            _UIManager.SetBoss(
                bossHP:     superBoss.BossPhase2Hp,
                bossMaxHP:  superBoss.BossHp,
                bossSprite: superBoss.BossPhase2Sprite
            );
        }
        else
        {
            GameWin();
        }
    }
    private void GameOver()
    {
        if (gameState == GameState.GamePlaying)
        {
            gameState = GameState.GameOver;
            _UIManager.GameOver();
        }
    }
    private void GameWin()
    {
        if (gameState == GameState.GamePlaying)
        {
            gameState = GameState.GameWin;
            _UIManager.GameWin();

            _UIManager.AvtivateStars(1);
            if (_currentPoints >= _currentLevel.PointsOnSilver) _UIManager.AvtivateStars(2);
            if (_currentPoints >= _currentLevel.PointsOnGold) _UIManager.AvtivateStars(3);
        }
    }
    public void NextLevel()
    {
        if (_currentLevelNumber == _levels.Max(level => level.LevelNumber))
        {
            SetLevel(1);
        }
        else
        {
            SetLevel(_currentLevelNumber + 1);
        }
    }
    public void ResetLevel()
    {
        SetLevel(_currentLevelNumber);
    }
}
