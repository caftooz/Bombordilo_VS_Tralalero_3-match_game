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
    void Start()
    {
        _board.DoStep += _UIManager.SpendStep;
        _board.DamageBoss += DamageBoss;
        _board.DamageBossFirework += DamageBossFirework;

        _UIManager.OnBossDeth += BossDeth;
        _UIManager.OnStepEnd += GameOver;

        SetLevel(_currentLevelNumber);
    }

    private void DamageBoss(FruitType fruitType)
    {
        if (_currentBoss is StandartBoss standartBoss && standartBoss.BossCriticalFruit == fruitType)
        {
            _UIManager.SpendBossHP(_criticalFruitDamage);
        }
        else
        {
            _UIManager.SpendBossHP(_baseFruitDamage);
        }
    }
    private void DamageBossFirework()
    {
        if (_currentBoss is SuperBoss)
        {
            _UIManager.SpendBossHP(_criticalFireworkDamage);
        }
        else
        {
            _UIManager.SpendBossHP(_baseFireworkDamage);
        }
    }
    public void SetLevel(int levelNumber)
    {
        _currentLevelNumber = levelNumber;

        _board.CrerateAndFillBoard();

        _currentLevel = _levels.Where(l => l.LevelNumber == levelNumber).First();
        _currentBoss = _currentLevel.Boss;

        _currentBossPhase = 1;

        _UIManager.SetBackground(_currentLevel.Background);
        _UIManager.ResetStepSlider();
        _UIManager.SetBoss(
            bossHP:     _currentBoss.BossHp,
            bossMaxHP:  _currentBoss.BossHp,
            bossSprite: _currentBoss.BossSprite
        );
    }
    private void BossDeth()
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
        ResetLevel();
    }
    private void GameWin()
    {
        NextLevel();
    }
    private void NextLevel()
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
    private void ResetLevel()
    {
        SetLevel(_currentLevelNumber);
    }
}
