using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutThePhysics : MonoBehaviour
{
    public HeroMovement hero = null;

    public void HitTheGround()
    {
        hero.DropCollision();
        Camera.main.SendMessage("ReloadScene");
    }
}
