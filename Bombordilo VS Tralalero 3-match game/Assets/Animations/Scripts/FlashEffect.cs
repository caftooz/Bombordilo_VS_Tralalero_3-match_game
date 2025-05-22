using UnityEngine;
using System.Collections;

public class FlashEffect : MonoBehaviour
{
    [Header("Настройки мигания")]
    [Tooltip("Цвет при мигании")]
    public Color flashColor = Color.gray;

    [Tooltip("Длительность одной вспышки")]
    public float flashDuration = 0.1f;

    [Tooltip("Сколько раз мигнуть")]
    public int flashCount = 2;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    void Awake()
    {
        // Автоматически находим все SpriteRenderer'ы на объекте и его детях
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];

        // Сохраняем оригинальные цвета
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    // Запустить мигание (можно вызывать из других скриптов)
    public void StartFlash(Color color)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        for (int i = 0; i < flashCount; i++)
        {
            // Устанавливаем цвет мигания
            foreach (var renderer in spriteRenderers)
            {
                renderer.color = color;
            }
            yield return new WaitForSeconds(flashDuration);

            // Возвращаем оригинальный цвет
            for (int j = 0; j < spriteRenderers.Length; j++)
            {
                spriteRenderers[j].color = originalColors[j];
            }
            yield return new WaitForSeconds(flashDuration);
        }
    }

    void OnDestroy()
    {
        // Восстанавливаем оригинальные цвета при уничтожении объекта
        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = originalColors[i];
                }
            }
        }
    }
}