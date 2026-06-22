using UnityEngine;

/// <summary>
/// PLAYTEST-ONLY first-person walk controller — this is NOT the shipping player.
/// The real player (<see cref="PlayerController"/>) is a Netcode <c>NetworkBehaviour</c> that only
/// moves under an NGO host, which makes a quick solo "walk the map" awkward. This is a self-contained
/// <see cref="CharacterController"/> rig that deliberately mirrors the real player's BODY
/// (height 2 / radius 0.4 / step 0.5 / slope 75) and FEEL (walk 4 / sprint 7 / crouch 2 / gravity -20 /
/// jump 5) so that whether the geometry is traversable here matches the real player.
///
/// Spawned by <c>Tools ▸ Black Commission ▸ Map ▸ Playtest Map 2 (Walk It)</c>. Uses the classic
/// Input API (the project's Active Input Handling is "Both", so this is safe).
///
/// Controls: WASD move · mouse look · LeftShift sprint · LeftCtrl crouch · Space jump ·
///           F toggle flythrough (noclip, to inspect from any angle) · Esc release cursor.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Map2PlaytestController : MonoBehaviour
{
    [Header("Mirrors PlayerController feel")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float crouchSpeed = 2f;
    public float gravity = -20f;
    public float jumpForce = 5f;

    [Header("Body")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    [Header("Look / fly")]
    public float mouseSensitivity = 2f;
    public float flySpeed = 12f;

    CharacterController _cc;
    Transform _cam;
    float _pitch;
    float _vY;
    bool _flying;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        var camComp = GetComponentInChildren<Camera>();
        _cam = camComp != null ? camComp.transform : transform;
        LockCursor(true);
    }

    void Update()
    {
        HandleLook();

        if (Input.GetKeyDown(KeyCode.F)) _flying = !_flying;

        bool crouch = !_flying && Input.GetKey(KeyCode.LeftControl);
        float targetH = crouch ? crouchHeight : standHeight;
        _cc.height = Mathf.Lerp(_cc.height, targetH, Time.deltaTime * 12f);
        _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);

        if (_flying) { Fly(); return; }

        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector3 wish = transform.right * ix + transform.forward * iz;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        float speed = crouch ? crouchSpeed : (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed);
        Vector3 move = wish * speed;

        if (_cc.isGrounded)
        {
            _vY = -2f; // small stick-to-ground bias
            if (Input.GetKeyDown(KeyCode.Space)) _vY = jumpForce;
        }
        _vY += gravity * Time.deltaTime;
        move.y = _vY;
        _cc.Move(move * Time.deltaTime);
    }

    void HandleLook()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        transform.Rotate(0f, mx, 0f);
        _pitch = Mathf.Clamp(_pitch - my, -85f, 85f);
        _cam.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void Fly()
    {
        float sp = Input.GetKey(KeyCode.LeftShift) ? flySpeed * 2.5f : flySpeed;
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector3 v = _cam.forward * iz + transform.right * ix;
        if (Input.GetKey(KeyCode.Space)) v += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) v += Vector3.down;
        _cc.Move(v * sp * Time.deltaTime);
        _vY = 0f;
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        style.normal.textColor = Color.white;
        GUI.Box(new Rect(8, 8, 920, 26), GUIContent.none);
        GUI.Label(new Rect(14, 11, 910, 20),
            "WALK Map 2   ·   WASD move · mouse look · Shift sprint · Ctrl crouch · Space jump · F flythrough · Esc cursor   |   pos "
            + transform.position.ToString("0.0"), style);
    }
}
