using UnityEngine;
using Cinemachine;

/// <summary>
/// An add-on module for Cinemachine to shake the camera
/// </summary>
[AddComponentMenu("")] // Hide in menu
public class Screenshake : CinemachineExtension
{
    public AnimationCurve curve;

    private float m_Range = 0.0f;
    private float timer = -1f;
    private float AnimationLength => curve.keys[curve.length - 1].time;

    public void Trigger()
    {
        timer = AnimationLength;
    }

    private void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            m_Range = curve.Evaluate(Mathf.Max(timer, 0f));
        }
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            Vector3 shakeAmount = GetOffset();
            state.PositionCorrection += shakeAmount;
        }
    }

    Vector3 GetOffset()
    {
        // Note: change this to something more interesting!
        return new Vector3(
            Random.Range(-m_Range, m_Range),
            Random.Range(-m_Range, m_Range),
            Random.Range(-m_Range, m_Range));
    }
}
