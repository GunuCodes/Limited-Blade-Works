using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private bool isInstantKill = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null && player.pState != null && !player.pState.invincible)
            {
                if (isInstantKill)
                {
                    player.InstantDeath();
                }
                else
                {
                    player.TakeHazardDamage(damage);
                }
            }
        }
    }
}
