using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet", menuName = "NoClip/New Bullet Define...")]
public class BulletDefine : ScriptableObject
{
    public LayerMask collisionMask;
    public float Velocity = 10f;
    public float Damage = 1f;
    public float OnHitTimeout = 0.5f;
    public float OnFlightTimeout = 3f;
    public float MaxTailLength = 10f;
}