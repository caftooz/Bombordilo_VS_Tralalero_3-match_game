using Unity.VisualScripting;
using UnityEngine;

public class BossFly : MonoBehaviour
{
    public GameObject objectToSpawn;

    public Vector3 spawnOffset;
    private Vector3 finalPosition;

    public Animator animator;

    public UIManager UIManager;

    public GameObject star;
    public void Fly()
    {
        animator.SetTrigger("TimeToFly");
        
    }

    public void OnFlyAwayComplete()
    {
        // Запоминаем конечную позицию (из текущего положения)
        finalPosition = transform.position;

        // Создаём новый объект
        if (objectToSpawn != null)
        {
            GameObject newObj = Instantiate(
                objectToSpawn,
                finalPosition + spawnOffset,
                Quaternion.identity
            );

            newObj.transform.localScale = Vector3.zero;

            // Запускаем его анимацию
            Animator newAnimator = newObj.GetComponent<Animator>();
            if (newAnimator != null)
                newAnimator.SetTrigger("Play");

            
            star = newObj;
            gameObject.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    public void EndLevel()
    {
        Destroy(star);
        UIManager.GameWin();
    }
}