using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float health;
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilFactor;
    [SerializeField] protected bool isRecoiling = false;
    [SerializeField] protected float damage;
    [SerializeField] protected float speed;
    [SerializeField] protected float recoilDelay = 0.1f;
    protected bool isRecoilDelayed = false;

    protected float recoilTimer;
    protected Rigidbody2D rb;
    protected HitEffect hitEffect;


    // Start is called before the first frame update
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hitEffect = GetComponent<HitEffect>();
    }
    // Update is called once per frame
    protected virtual void Update()
    {
        if(GameManager.Instance.gameIsPaused)
        {
            return;
        }

        if(health <= 0)
        {
            Destroy(gameObject);
        }
        if(isRecoiling)
        {
            if(recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoiling = false;
                recoilTimer = 0;
            }
        }
    }

    public virtual void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        health -= _damageDone;
        if (hitEffect != null)
        {
            hitEffect.OnHit();
        }
        if(!isRecoiling && !isRecoilDelayed)
        {
            StartCoroutine(DelayedRecoil(_hitDirection, _hitForce));
            isRecoiling = true;
        }
    }

    protected IEnumerator DelayedRecoil(Vector2 _hitDirection, float _hitForce)
    {
        isRecoilDelayed = true;
        yield return new WaitForSeconds(recoilDelay);
        rb.AddForce(-_hitForce * recoilFactor * _hitDirection);
        isRecoiling = true;
        isRecoilDelayed = false;
    }

    protected void OnTriggerStay2D(Collider2D _other)
    {
        if (_other.CompareTag("Player"))
        {
            PlayerController playerController = _other.GetComponent<PlayerController>();
            if (playerController != null && playerController.pState != null && !playerController.pState.invincible && health > 0)
            {
                Attack();
                if (playerController.pState.alive)
                {
                    playerController.HitStopTime(0, 5, 0.5f);
                }
            }
        }
    }

    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(damage);
    }
}