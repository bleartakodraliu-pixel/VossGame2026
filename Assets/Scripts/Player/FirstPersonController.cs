using UnityEngine;
using Voss.Audio;
using Voss.Core;
using Voss.UI;

namespace Voss.Player
{
    /// <summary>First-person walk/look with hold-Shift sprint and cinematic/UI input locks.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float sprintSpeed = 4.8f;
        [SerializeField] private float lookSensitivity = 2.0f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private bool movementEnabled = true;
        [SerializeField] private bool lookEnabled = true;
        [SerializeField] private float footstepInterval = 0.55f;

        private CharacterController controller;
        private float pitch;
        private float yaw;
        private float verticalVelocity;
        private float stepTimer;
        private bool controlLocked;

        public Transform CameraPivot => cameraPivot;
        public bool MovementEnabled { get => movementEnabled; set => movementEnabled = value; }
        public bool LookEnabled { get => lookEnabled; set => lookEnabled = value; }
        public bool IsSprinting { get; private set; }
        public bool IsMoving { get; private set; }
        public bool ControlLocked => controlLocked;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null && Camera.main.transform.IsChildOf(transform))
                cameraPivot = Camera.main.transform;
            yaw = transform.eulerAngles.y;
            if (cameraPivot != null) pitch = cameraPivot.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }

        /// <summary>Hard lock used by cinematics. Independent of UI modal state.</summary>
        public void SetControlLocked(bool locked) { controlLocked = locked; }

        /// <summary>Cinematic: eases the view toward a world point. Call every frame while control is locked.</summary>
        public void LookTowards(Vector3 point, float sharpness = 3f)
        {
            if (cameraPivot == null) return;
            Vector3 to = point - cameraPivot.position;
            if (to.sqrMagnitude < 0.0001f) return;
            float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Clamp(-Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg, -maxPitch, maxPitch);
            float k = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, targetYaw, k);
            pitch = Mathf.Lerp(pitch, targetPitch, k);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void Teleport(Vector3 position, float yawDegrees)
        {
            controller.enabled = false;
            transform.position = position;
            yaw = yawDegrees;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            controller.enabled = true;
        }

        private void Update()
        {
            var ui = UIManager.Instance;
            bool blocked = controlLocked || (ui != null && ui.BlocksGameplay);

            if (!blocked && lookEnabled)
            {
                Vector2 look = VossInput.LookDelta() * lookSensitivity * GameSettings.MouseSensitivity;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector3 move = Vector3.zero;
            IsSprinting = false;
            if (!blocked && movementEnabled)
            {
                Vector2 input = VossInput.Move();
                IsSprinting = input.sqrMagnitude > 0.01f && VossInput.SprintHeld();
                float speed = IsSprinting ? Mathf.Max(moveSpeed, sprintSpeed) : moveSpeed;
                move = (transform.right * input.x + transform.forward * input.y) * speed;
            }
            IsMoving = new Vector2(move.x, move.z).sqrMagnitude > 0.01f;

            if (controller.isGrounded) verticalVelocity = -1f;
            else verticalVelocity += Physics.gravity.y * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);

            HandleFootsteps(move);
        }

        private void HandleFootsteps(Vector3 move)
        {
            var planar = new Vector3(move.x, 0f, move.z);
            if (planar.sqrMagnitude < 0.1f || !controller.isGrounded) { stepTimer = footstepInterval * 0.5f; return; }
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = IsSprinting ? footstepInterval * 0.65f : footstepInterval;
                GameManager.Instance?.Audio.Play(SfxId.Footstep, 0.35f);
            }
        }
    }
}
