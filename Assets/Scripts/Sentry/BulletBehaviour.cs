using Shapes;
using UnityEngine;

public class BulletBehaviour : PooledMonoBehaviour
{
    public BulletDefine define;
    public Renderer target = null;
    public Line line = null;

    float selfDestruct = -1f;
    float timeout = 3f;

    public override void OnInstantiate()
    {
        target.enabled = false;
        line.enabled = false;
    }

    //public override void OnInstantiate() { }
    public override void OnSpawn()
    {
        target.enabled = true;
        line.enabled = true;
        line.Start = Vector3.zero;
        line.End = Vector3.zero;
        selfDestruct = -1f;
        timeout = define.OnFlightTimeout;
    }

    public override void OnRecycle()
    {
        line.Start = Vector3.zero;
        line.End = Vector3.zero;
    }

    RaycastHit _cachedHit;
    private void Update()
    {
        if (Time.timeScale < 0.5f) return;

        timeout -= Time.deltaTime;
        if (timeout < 0f)
        {
            this.Recycle();
            return;
        }

        if (selfDestruct > 0f)
        {
            selfDestruct -= Time.deltaTime;
            if (selfDestruct <= 0)
            {
                this.Recycle();
            }
            else
            {
                line.Start = Vector3.zero;
                line.End = Vector3.zero;
            }
            return;
        }

        float distance = define.Velocity * Time.deltaTime;
        if (Physics.Raycast(transform.position, transform.forward, out _cachedHit, distance, define.collisionMask.value))
        {
            selfDestruct = define.OnHitTimeout;
            transform.position = _cachedHit.point;
            distance = _cachedHit.distance;

            OnInstantiate();

            HeroDamageReceiver receiver;
            if (_cachedHit.transform.TryGetComponent(out receiver))
            {
                receiver.ReceiveDamage(this, _cachedHit);
            }
            else
            {
                // perform the manager behaviour of applying a decal?
            }
        }
        else
        {
            transform.position = transform.position + transform.forward * distance;
        }
        line.End = Vector3.back * distance;
    }
}
