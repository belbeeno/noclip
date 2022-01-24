using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class HeroMovement : MonoBehaviour
{
    [SerializeField]
    private CharacterController _cachedCollider = null;
    [SerializeField]
    private CinemachineVirtualCamera _corporealCamera = null;
    [SerializeField]
    private CinemachineVirtualCamera _spectralCamera = null;
    [SerializeField]
    private Health health = null;

    public bool IsDead => health?.IsDead == true;

    #region Look Group
    [Header("Look")]
    public Transform lookTransform = null;
    [Range(0.1f, 1000f)]
    public float horizontalSens = 5f;
    public bool splitVerticalSens = false;
    [Range(0.1f, 1000f)]
    public float verticalSens = 3f;
    [Range(0f, 90f)]
    public float verticalClamp = 75f;
    public bool invertY = true;
    #endregion

    #region Corporeal Movement Group
    [Header("Corporeal Movement")]
    public Vector2 acceleration = new Vector2(1f, 1f);
    public Vector2 maxVelocity = new Vector2(10f, 10f);
    private Vector3 _velocity = new Vector3();
    public Vector3 Velocity { get => _velocity; }
    private Vector3 _velocityNormalized = new Vector3();
    public Vector3 VelocityNormalized {
        get
        {
            _velocityNormalized.Set(Velocity.x / maxVelocity.x, Velocity.y / Physics.gravity.y, Velocity.z / maxVelocity.y);
            return _velocityNormalized;
        }
    }
    public float brakingMultiplier = 3f;
    #endregion

    #region Ethereal Movement Group
    [Header("Ethereal Movement")]
    public Transform spiritPivot = null;
    public float verticalAcceleration = 10f;
    public float maxVerticalVelocity = 0.2f;
    public float speedMultiplier = 2f;
    [Range(0.01f, 5f)]
    public float spectralRechargeRate = 1f;
    public Shapes.Disc spectralChargeDisplay = null;
    public PID.InitParams spectralDisplayParams = new PID.InitParams() { oMin = 0f, oMax = 1f };
    private PID.PIDFloat spectralDisplayPID;
    private float spectralCharge = 1f;
    private readonly float TAU = Mathf.PI * 2f;
    public float GetSpectralChargeCurrent() => spectralChargeDisplay.AngRadiansEnd / TAU;
    public float GetSpectralChargeTarget() => spectralCharge;
    public void SetSpectralChargeCurrent(float value)
    {
        spectralChargeDisplay.AngRadiansEnd = value * TAU;
    }

    private bool spiritTransfer = true;
    private bool wasSpiritTransferred = true;
    public bool SpiritJustTransferred => spiritTransfer && !wasSpiritTransferred;
    private Vector3 _flyVelocity = new Vector3();
    [Header("Reclip")]
    public CSGModelHolder reclipMesh = null;
    [System.Serializable]
    public struct CapsuleDefine
    {
        public Vector3 localStart;
        public Vector3 localEnd;
        public float width;
    }
    public LayerMask reclipableLayers;
    public CapsuleDefine reclipCapsuleDefine = new CapsuleDefine();
    private RaycastHit[] _cachedReclipCollisions = new RaycastHit[5];
    SightlinesManager _sightlinesInstance = null;
    SightlinesManager sightlines
    {
        get
        {
            if (_sightlinesInstance == null)
            {
                _sightlinesInstance = Camera.main.GetComponent<SightlinesManager>();
            }
            return _sightlinesInstance;
        }
    }
    public bool IsStuck { get; private set; } = false;

    [Header("Events")]
    public UnityEvent OnStuck;
    public UnityEvent OnNoClip;
    public UnityEvent OnAppear;

    [Header("Sounds")]
    public AudioSource etherealMovementSound;
    public AudioClip[] reclipSounds = new AudioClip[0];
    public AudioSource reclipSound;

    #endregion

    private void OnEnable()
    {
        _velocity = Vector3.zero;
        _flyVelocity = Vector3.zero;
        spectralCharge = 1f;
        Cursor.lockState = CursorLockMode.Locked;

        spectralDisplayPID = new PID.PIDFloat(spectralDisplayParams, GetSpectralChargeCurrent, GetSpectralChargeTarget, SetSpectralChargeCurrent);

    }
    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        float start = (min + max) * 0.5f - 180;
        float floor = Mathf.FloorToInt((angle - start) / 360) * 360;
        min += floor;
        max += floor;
        return Mathf.Clamp(angle, min, max);
    }

    void UpdateCorporeal()
    {
        _spectralCamera.Priority = 9;
        _corporealCamera.Priority = 10;
        float yaw = Input.GetAxis("Mouse X");
        float vertSens = verticalSens;
        if (!splitVerticalSens)
        {
            vertSens = Screen.height / Screen.width * horizontalSens;
        }
        float pitch = lookTransform.localRotation.eulerAngles.x + Input.GetAxis("Mouse Y") * verticalSens * Time.deltaTime * (invertY ? 1 : -1);
        transform.Rotate(0f, yaw * horizontalSens * Time.deltaTime, 0f, Space.Self);
        pitch = ClampAngle(pitch, -verticalClamp, verticalClamp);
        lookTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float right = Input.GetAxis("Horizontal");
        float fwd = Input.GetAxis("Vertical");
        _velocity.x = Mathf.MoveTowards(_velocity.x, right * maxVelocity.x, acceleration.x * (Mathf.Approximately(right, 0f) ? brakingMultiplier : 1f) * Time.deltaTime);
        _velocity.y = Physics.gravity.y;
        _velocity.z = Mathf.MoveTowards(_velocity.z, fwd * maxVelocity.y, acceleration.y * (Mathf.Approximately(fwd, 0f) ? brakingMultiplier : 1f) * Time.deltaTime);
        
        _cachedCollider?.Move(transform.localToWorldMatrix.MultiplyVector(Velocity) * Time.deltaTime);

        if (spectralCharge < 1f)
        {
            spectralCharge += Time.deltaTime / spectralRechargeRate;
        }
    }
    void UpdateEthereal()
    {
        Debug.Assert(spiritPivot != null);
        _spectralCamera.Priority = 10;
        _corporealCamera.Priority = 9;
        float yaw = Input.GetAxis("Mouse X");
        float vertSens = verticalSens;
        if (!splitVerticalSens)
        {
            vertSens = Screen.height / Screen.width * horizontalSens;
        }
        float pitch = lookTransform.localRotation.eulerAngles.x + Input.GetAxis("Mouse Y") * verticalSens * Time.unscaledDeltaTime * (invertY ? 1 : -1);
        spiritPivot.Rotate(0f, yaw * horizontalSens * Time.unscaledDeltaTime, 0f, Space.Self);
        pitch = ClampAngle(pitch, -89f, 89f);
        lookTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float right = Input.GetAxisRaw("Horizontal");
        float fwd = Input.GetAxisRaw("Vertical");
        float up = Input.GetAxisRaw("Elevate");
        float modifier = (Input.GetButton("Run") ? speedMultiplier : 1f);
        right = Mathf.MoveTowards(_flyVelocity.x, right * maxVelocity.x * modifier, acceleration.x * (Mathf.Approximately(right, 0f) ? brakingMultiplier : 1f) * Time.unscaledDeltaTime);
        up = Mathf.MoveTowards(_flyVelocity.y, up * maxVerticalVelocity * modifier, verticalAcceleration * (Mathf.Approximately(up, 0f) ? brakingMultiplier : 1f) * Time.unscaledDeltaTime);
        fwd = Mathf.MoveTowards(_flyVelocity.z, fwd * maxVelocity.y * modifier, acceleration.y * (Mathf.Approximately(fwd, 0f) ? brakingMultiplier : 1f) * Time.unscaledDeltaTime);
        _flyVelocity.Set(right, up, fwd);

        Vector3 velocity = _flyVelocity * Time.unscaledDeltaTime;
        velocity.y = 0f;
        spiritPivot.position = spiritPivot.position + lookTransform.localToWorldMatrix.MultiplyVector(velocity) + Vector3.up * up;
    }
    bool ReClipTest()
    {
        Vector3 start = spiritPivot.localToWorldMatrix.MultiplyPoint(reclipCapsuleDefine.localStart);
        Vector3 end = spiritPivot.localToWorldMatrix.MultiplyPoint(reclipCapsuleDefine.localEnd);
        Vector3 dir = end - start;
        float mag = dir.magnitude;
        dir /= mag;
        Physics.CheckCapsule(start, end, reclipCapsuleDefine.width, reclipableLayers);

        int count = Physics.SphereCastNonAlloc(start, reclipCapsuleDefine.width, dir, _cachedReclipCollisions, mag, reclipableLayers);
        for (int i = 0; i < count; ++i)
        {
            GameObject target = _cachedReclipCollisions[i].collider.gameObject;
            CSGModelHolder targetCSG = target.GetComponent<CSGModelHolder>();
            Debug.Assert(targetCSG);
            CSGModelHolder.CreateCompositeAndDisable(targetCSG, reclipMesh.gameObject);
        }
        return count > 0;
    }

    void Update()
    {
        if (IsStuck) return;

        wasSpiritTransferred = spiritTransfer;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            _cachedCollider?.SimpleMove(Vector3.zero);
            return;
        }
        else if (Input.GetButtonDown("Cancel"))
        {
            Cursor.lockState = CursorLockMode.None;
            _velocity = Vector3.zero;
            _flyVelocity = Vector3.zero;
            _cachedCollider?.SimpleMove(Vector3.zero);
            return;
        }

        if (Debug.isDebugBuild && Input.GetButtonUp("Debug Reset"))
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            lookTransform.localPosition = Vector3.zero;
            lookTransform.localRotation = Quaternion.identity;
        }

        if (health?.IsDead == true)
        {
            _cachedCollider?.SimpleMove(Vector3.zero);
            return;
        }

        if (Input.GetButtonDown("Fire2") && spectralCharge >= 1f)
        {
            // First time; assign the states
            spiritTransfer = false;
            if (_cachedCollider)
            {
                _cachedCollider.Move(Vector3.zero);
                _cachedCollider.enabled = false;
            }
            _flyVelocity = Vector3.zero;
            etherealMovementSound?.Play();
            OnNoClip.Invoke();
            Time.timeScale = 0f;
        }

        if (Time.timeScale < 0.5f && Input.GetButton("Fire2") && !spiritTransfer)
        {
            if (Input.GetButtonUp("Fire1"))
            {
                // While were holding down right click; go to destination
                spiritTransfer = true;
                bool dead = sightlines != null;
                if (sightlines?.TestForSightlines(spiritPivot.position) == true)
                {
                    RaycastHit info;
                    if (Physics.Raycast(spiritPivot.position, Vector3.down, out info, 100f, LayerMask.GetMask("Environment")))
                    {
                        if (!info.transform.CompareTag("Ceiling"))
                        {
                            dead = false;
                            if (info.distance < _cachedCollider.height)
                            {
                                spiritPivot.position = info.point + Vector3.up * _cachedCollider.height;
                            }
                        }
                        else
                        {
                            Debug.Log("Hit a ceiling");
                        }
                    }
                    else
                    {
                        Debug.Log("No environment under me");
                    }
                }
                else
                {
                    Debug.Log("Failed sightlines");
                }
                
                if (dead)
                {
                    // Uh oh
                    Camera.main.SendMessage("CutReloadScene");
                    transform.position = spiritPivot.position;
                    transform.rotation = spiritPivot.rotation;
                    spiritPivot.localPosition = Vector3.zero;
                    spiritPivot.localRotation = Quaternion.identity;
                    Time.timeScale = 1f;
                    etherealMovementSound?.Stop();
                    if (_cachedCollider) Destroy(_cachedCollider);
                    IsStuck = true;
                    OnStuck.Invoke();
                    return;
                }
                else
                {
                    if (ReClipTest() && reclipSound && reclipSounds.Length > 0)
                    {
                        if (!reclipSound.isPlaying)
                        {
                            reclipSound.clip = reclipSounds[Random.Range(0, reclipSounds.Length)];
                            reclipSound.Play();
                        }
                    }
                    else
                    {
                        OnAppear.Invoke();
                    }
                    transform.position = spiritPivot.position;
                    transform.rotation = spiritPivot.rotation;
                    spiritPivot.localPosition = Vector3.zero;
                    spiritPivot.localRotation = Quaternion.identity;
                    Time.timeScale = 1f;
                    etherealMovementSound?.Stop();
                    if (_cachedCollider) _cachedCollider.enabled = true;
                    spectralCharge = 0f;
                }
            }
        }
        else if (Time.timeScale < 0.5f)
        {
            spiritPivot.localPosition = Vector3.zero;
            spiritPivot.localRotation = Quaternion.identity;
            Time.timeScale = 1f;
            etherealMovementSound?.Stop();
            if (_cachedCollider) _cachedCollider.enabled = true;
        }

        if (Time.timeScale > 0.5f)
        {
            UpdateCorporeal();
        }
        else //if (Time.timeScale < 0.5f)
        {
            UpdateEthereal();
        }

        spectralDisplayPID.Compute();
        if (spectralCharge < 1f)
        {
            spectralChargeDisplay.Color = Color.red;
        }
        else
        {
            spectralChargeDisplay.Color = Color.white;
        }
    }

    public void DropCollision()
    {
        Destroy(_cachedCollider);
        _cachedCollider = null;
        gameObject.AddComponent<BoxCollider>();
        gameObject.AddComponent<Rigidbody>();
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;

        var start = spiritPivot.localToWorldMatrix.MultiplyPoint(reclipCapsuleDefine.localStart);
        var end = spiritPivot.localToWorldMatrix.MultiplyPoint(reclipCapsuleDefine.localEnd);
        var width = reclipCapsuleDefine.width;
        Gizmos.DrawWireSphere(start, width);
        Gizmos.DrawWireSphere(end, width);
        Gizmos.DrawLine(start + Vector3.right * width, end + Vector3.right * width);
        Gizmos.DrawLine(start + Vector3.forward * width, end + Vector3.forward * width);
        Gizmos.DrawLine(start + Vector3.back * width, end + Vector3.back * width);
        Gizmos.DrawLine(start + Vector3.left * width, end + Vector3.left * width);
    }

    public bool gotIt = false;
    public void SecretCollect()
    {
        gotIt = true;
    }
}
