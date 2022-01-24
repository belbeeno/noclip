using System;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "Sentry Define", menuName = "NoClip/Create New Sentry Define...")]
public class SentryDefine : ScriptableObject
{
    public LayerMask TargetMask;
    public BulletBehaviour BulletPrefab = null;

    [Header("Stats")]
    public float RateOfRotation = 45f;
    public float RateOfFire = 1f;
    public float TargetingRange = 10f;
    public float TrackingDuration = 3f;
    public float TrackingThreshhold = 0.8f;
}
