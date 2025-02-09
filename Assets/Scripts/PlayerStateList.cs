using UnityEngine;

public class PlayerStateList : MonoBehaviour
{
    public bool jumping = false;
    public bool DodgeRolling = false;
    public bool recoilingX, recoilingY;
    public bool lookingRight;
    public bool invincible;
    public bool cutscene = false;
    public bool alive = true;
    public bool dying = false;  // New state for death animation

    private void Awake()
    {
        alive = true;
        dying = false;
    }
}
