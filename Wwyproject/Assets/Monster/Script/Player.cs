using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public float HP = 100;
    private bool isDead = false;
    private Animator anim;
    private DeathFadeUI deathFadeUI;

    void Awake()
    {
        anim = GetComponent<Animator>();
        deathFadeUI = FindObjectOfType<DeathFadeUI>();
    }

    void Update()
    {
        if (!isDead && HP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        HP = 0;

        if (anim != null)
        {
            anim.ResetTrigger("Scream");
            anim.SetTrigger("Die");
            anim.CrossFadeInFixedTime("Die", 0.1f);
        }

        DragonExample dragon = GetComponent<DragonExample>();
        if (dragon != null)
        {
            dragon.enabled = false;
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);

        if (deathFadeUI != null)
        {
            yield return deathFadeUI.FadeAndReload(1.5f);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
