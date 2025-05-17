using UnityEngine;
using System.Collections;

public class FlashEffect : MonoBehaviour
{
    [Header("��������� �������")]
    [Tooltip("���� ��� �������")]
    public Color flashColor = Color.gray;

    [Tooltip("������������ ����� �������")]
    public float flashDuration = 0.1f;

    [Tooltip("������� ��� �������")]
    public int flashCount = 2;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    void Awake()
    {
        // ������������� ������� ��� SpriteRenderer'� �� ������� � ��� �����
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];

        // ��������� ������������ �����
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    // ��������� ������� (����� �������� �� ������ ��������)
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
            // ������������� ���� �������
            foreach (var renderer in spriteRenderers)
            {
                renderer.color = color;
            }
            yield return new WaitForSeconds(flashDuration);

            // ���������� ������������ ����
            for (int j = 0; j < spriteRenderers.Length; j++)
            {
                spriteRenderers[j].color = originalColors[j];
            }
            yield return new WaitForSeconds(flashDuration);
        }
    }

    void OnDestroy()
    {
        // ��������������� ������������ ����� ��� ����������� �������
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