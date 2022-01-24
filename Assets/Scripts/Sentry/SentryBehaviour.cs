using Shapes;
using System;
using UnityEngine;
using UnityEngine.Events;

public class SentryBehaviour : MonoBehaviour
{
    public SentryDefine define = null;
    private readonly Color _cachedSeekingColor = new Color(1f, 0f, 0f, 0f);
    public Transform Target
    {
        get;
        private set;
    }
    public LayerMask collidesWithRaycast = new LayerMask();
    private bool _canDrop = true; 

    public enum State
    {
        Idle,
        Seeking,
        Tracking,
        Disabled,
    }
    private State _currentState = State.Idle;
    public State CurrentState
    {
        get => _currentState;
        private set
        {
            _currentState = value;
            if (_currentState == State.Idle)
            {
                if (trackingAnim?.isPlaying == false)
                {
                    trackingAnim.Play();
                }
            }
            else
            {
                if (trackingAnim?.isPlaying == true)
                {
                    trackingAnim.Stop();
                }
            }
        }
    }

    [Header("Animation")]
    public Animation trackingAnim = null;
    public Transform trackingTransform = null;
    public Line trackingLine = null;
    public Animator muffleFireAnim = null;
    private int _tActionNameHash = -1;

    [Header("Audio")]
    public AudioSource FireAudio = null;
    public AudioSource AlertAudio = null;

    #region Timers
    private float _firingTimer = 0f;
    private float _attentionSpan = 0f;
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & define.TargetMask.value) == 0) return;

        Target = other.transform;
        _firingTimer = define.RateOfFire;
        _attentionSpan = define.TrackingDuration;
        _canDrop = false;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.transform == Target)
        {
            _canDrop = true;
        }
    }

    private void OnEnable()
    {
        if (!ObjectPool.HasPool(define.BulletPrefab))
        {
            define.BulletPrefab.CreatePool();
        }

        if (muffleFireAnim)
        {
            foreach (var param in muffleFireAnim.parameters)
            {
                if (param.name.Equals("Action"))
                {
                    _tActionNameHash = param.nameHash;
                    break;
                }
            }
            Debug.Assert(_tActionNameHash != -1);
        }
    }

    private Vector3 _cachedTargetRay = new Vector3();
    private Vector3 _cachedLineLength = new Vector3();
    private RaycastHit _cachedHit;
    public bool UpdateSeeking()
    {
        if (Target)
        {
            _cachedTargetRay = Target.position - trackingTransform.position;
        }

        if (Target != null && _cachedTargetRay.sqrMagnitude < (define.TargetingRange * define.TargetingRange))
        {
            _cachedTargetRay.Normalize();
            if (Physics.Raycast(trackingTransform.position, _cachedTargetRay, out _cachedHit, define.TargetingRange, collidesWithRaycast.value))
            {
                // We hit something
                _cachedLineLength.Set(0f, 0f, _cachedHit.distance);
                trackingLine.End = _cachedLineLength;

                if (((1 << _cachedHit.transform.gameObject.layer) & define.TargetMask.value) != 0)
                {
                    // Hitting the target
                    if (CurrentState == State.Seeking)
                    {
                        if (_firingTimer >= define.RateOfFire && AlertAudio?.isPlaying == false)
                        {
                            AlertAudio.Play();
                        }
                    }
                    _attentionSpan = define.TrackingDuration;
                    CurrentState = State.Tracking;
                    return true;
                }
            }
        }

        // We aren't focused on the target
        _firingTimer = Mathf.Min(_firingTimer + Time.deltaTime, define.RateOfFire);
        _attentionSpan -= Time.deltaTime;
        if (_attentionSpan < 0f)
        {
            CurrentState = State.Idle;
            if (_canDrop)
            {
                Target = null;
            }
        }
        else
        {
            CurrentState = State.Seeking;
        }

        return false;
    }

    public void UpdateTracking()
    {
        trackingTransform.rotation = Quaternion.RotateTowards(trackingTransform.rotation, Quaternion.LookRotation(_cachedTargetRay, Vector3.up), define.RateOfRotation * Time.deltaTime);
        if (Vector3.Dot(trackingTransform.forward, _cachedTargetRay) >= define.TrackingThreshhold)
        {
            // In our sights!
            _firingTimer -= Time.deltaTime;
        }
    }

    public void Fire()
    {
        _attentionSpan = define.TrackingDuration;
        _firingTimer = define.RateOfFire;
        if (muffleFireAnim)
        {
            muffleFireAnim.SetTrigger(_tActionNameHash);
        }

        FireAudio?.Play();
        define.BulletPrefab.Spawn(muffleFireAnim.transform.position, muffleFireAnim.transform.rotation);
        OnFire.Invoke();
    }

    public void Disable()
    {
        CurrentState = State.Disabled;
        trackingAnim.Stop();
        StartCoroutine(DisableRoutine());
    }
    public AnimationCurve deathLaserAnim;
    public UnityEvent OnFire;
    private System.Collections.IEnumerator DisableRoutine()
    {
        float timer = 0f;
        Color color = trackingLine.ColorStart;
        float end = deathLaserAnim.keys[deathLaserAnim.length-1].time;

        while (timer <= end)
        {
            color.a = deathLaserAnim.Evaluate(timer);
            trackingLine.ColorStart = color;
            timer += Time.deltaTime;
            yield return 0;
        }

        color.a = deathLaserAnim.Evaluate(end);
        trackingLine.ColorStart = color;
    }

    private void Update()
    {
        if (CurrentState == State.Disabled)
        {
            // Sparks?
            return;
        }

        if (Target)
        {
            if (UpdateSeeking())
            {
                UpdateTracking();
            }
            if (_firingTimer <= 0f)
            {
                Fire();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        switch (CurrentState)
        {
            case State.Idle:
                Gizmos.color = Color.green;
                break;
            case State.Seeking:
                Gizmos.color = Color.yellow;
                break;
            case State.Tracking:
                Gizmos.color = Color.red;
                break;
            case State.Disabled:
                Gizmos.color = Color.black;
                break;
            default:
                Debug.LogError("Unknown CurrentState " + CurrentState.ToString());
                break;
        }
        Gizmos.DrawRay(trackingTransform.position, _cachedTargetRay * define.TargetingRange);
        Gizmos.DrawWireSphere(trackingTransform.position, define.TargetingRange);
    }
}
