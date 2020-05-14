﻿#pragma warning disable 0649

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Goldenwere.Unity.Controller
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class FirstPersonController : MonoBehaviour
    {
        /// <summary>
        /// Variables related to physical/directional movement 
        /// </summary>
        [Serializable] public class MovementSettings
        {
            [Tooltip("Whether the player can crouch")]
            /**************/    public  bool            canCrouch;
            [Tooltip("Whether the player can move at all (best used for pausing)")]
            /**************/    public  bool            canMove;
            [Tooltip("Whether the player can use fast movement (example: when stamina runs out)")]
            /**************/    public  bool            canMoveFast;
            [Tooltip("Whether the player can use slow movement (example: when cannot crouch/sneak)")]
            /**************/    public  bool            canMoveSlow;
            [Tooltip("Whether modifiers (fast, slow/crouch) are toggled or held")]
            /**************/    public  bool            modifiersAreToggled;

            [Tooltip("Whether the player can control movement while not grounded")]
            [SerializeField]    private bool            controlAirMovement;

            [Tooltip("The force to apply for jumping")]
            [SerializeField]    private float           forceJump;
            [Tooltip("Tendency to stick to ground (typically below 1)")]
            [SerializeField]    private float           forceStickToGround;
            [Tooltip("Multiplier for gravity")]
            [SerializeField]    private float           forceGravityMultiplier;

            [Tooltip("Reduces radius by one minus this value to avoid getting stuck in a wall (ideally set around 0.1f)")]
            [SerializeField]    private float           settingShellOffset;
            [Tooltip("Modifies speed on slopes")]
            [SerializeField]    private AnimationCurve  settingSlopeModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));

            [Tooltip("Multiplier to speed based on whether crouched or not")]
            [SerializeField]    private float           speedCrouchMultiplier;
            [Tooltip("Speed when a fast modifier is used (example: sprinting)")]
            [SerializeField]    private float           speedFast;
            [Tooltip("Speed when no modifier is used (example: jogging pace for action games, walking pace for scenic games)")]
            [SerializeField]    private float           speedNorm;
            [Tooltip("Speed when a slow modifier is used (example: crouching/sneaking, walking pace for action games)")]
            [SerializeField]    private float           speedSlow;

            public bool             ControlAirMovement          { get { return controlAirMovement; } }
            public float            ForceStickToGround          { get { return forceStickToGround; } }
            public float            ForceJump                   { get { return forceJump; } }
            public float            ForceGravityMultiplier      { get { return forceGravityMultiplier; } }
            public float            SettingShellOffset          { get { return settingShellOffset; } }
            public AnimationCurve   SettingSlopeModifier        { get { return settingSlopeModifier; } }
            public float            SpeedFast                   { get { return speedFast; } }
            public float            SpeedNorm                   { get { return speedNorm; } }
            public float            SpeedSlow                   { get { return speedSlow; } }
        }

        /// <summary>
        /// Variables related to camera/rotational movement
        /// </summary>
        [Serializable] public class CameraSettings
        {
            [Tooltip("Multiplier for camera sensitivity")]
            /**************/    public  float   cameraSensitivity;
            [Tooltip("Whether to use smoothing with camera movement")]
            /**************/    public  bool    smoothLook;
            [Tooltip("The speed at which camera smoothing is applied (the higher the number, the less time that the camera takes to rotate)")]
            /**************/    public  float   smoothSpeed;

            [Tooltip("The camera that is being manipulated by the controller")]
            [SerializeField]    private Camera  attachedCamera;
            [Tooltip("The minimum (x) and maximum (y) rotation in degrees that the camera can rotate vertically (ideally a range within -90 and 90 degrees)")]
            [SerializeField]    private Vector2 cameraClampVertical;

            public Camera   AttachedCamera      { get { return attachedCamera; } }
            public Vector2  CameraClampVertical { get { return cameraClampVertical; } }
        }

        [SerializeField]    private PlayerInput         attachedControls;
        [SerializeField]    private CameraSettings      settingsCamera;
        [SerializeField]    private MovementSettings    settingsMovement;
        /**************/    private CapsuleCollider     attachedCollider;
        /**************/    private Rigidbody           attachedRigidbody;
        /**************/    private bool                workingControlActionDoMovement;
        /**************/    private bool                workingControlActionDoRotation;
        /**************/    private bool                workingControlActionModifierMoveFast;
        /**************/    private bool                workingControlActionModifierMoveSlow;
        /**************/    private bool                workingDoJump;
        /**************/    private Vector3             workingGroundContactNormal;
        /**************/    private bool                workingGroundstateCurrent;
        /**************/    private Quaternion          workingRotationCamera;
        /**************/    private Quaternion          workingRotationController;

        public  bool                MovementIsRunning   { get { return false; } }
        public  CameraSettings      SettingsCamera      { get { return settingsCamera; } }
        public  MovementSettings    SettingsMovement    { get { return settingsMovement; } }

        private void Awake()
        {
            attachedCollider = gameObject.GetComponent<CapsuleCollider>();
            attachedRigidbody = gameObject.GetComponent<Rigidbody>();

            workingRotationCamera = settingsCamera.AttachedCamera.transform.localRotation;
            workingRotationController = transform.localRotation;
        }

        private void FixedUpdate()
        {
            if (settingsMovement.canMove)
            {
                HandleRotation();
                HandleMovement();
                HandleGravity();
            }
        }

        /// <summary>
        /// Handler for the Jump input event (defined in ControllerActions)
        /// </summary>
        /// <param name="context">The context associated with the input (unused)</param>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.canceled)
                workingDoJump = true;
        }

        /// <summary>
        /// Handler for the ModifierMovementFast input event (defined in ControllerActions)
        /// </summary>
        /// <param name="context">The context associated with the input</param>
        public void OnModifierMovementFast(InputAction.CallbackContext context)
        {
            if (settingsMovement.canMoveFast)
            {
                if (settingsMovement.modifiersAreToggled)
                    workingControlActionModifierMoveFast = !workingControlActionModifierMoveFast;
                else
                    workingControlActionModifierMoveFast = context.performed;
            }
        }

        /// <summary>
        /// Handler for the ModifierMovementSlow input event (defined in ControllerActions)
        /// </summary>
        /// <param name="context">The context associated with the input</param>
        public void OnModifierMovementSlow(InputAction.CallbackContext context)
        {
            if (settingsMovement.canMoveSlow)
            {
                if (settingsMovement.modifiersAreToggled)
                    workingControlActionModifierMoveSlow = !workingControlActionModifierMoveSlow;
                else
                    workingControlActionModifierMoveSlow = context.performed;
            }
        }

        /// <summary>
        /// Handler for the Movement input event (defined in ControllerActions)
        /// </summary>
        /// <param name="context">The context associated with the input</param>
        public void OnMovement(InputAction.CallbackContext context)
        {
            workingControlActionDoMovement = context.performed;
        }

        /// <summary>
        /// Handler for the Rotation input event (defined in ControllerActions)
        /// </summary>
        /// <param name="context">The context associated with the input</param>
        public void OnRotation(InputAction.CallbackContext context)
        {
            workingControlActionDoRotation = context.performed;
        }

        /// <summary>
        /// Determines the current ground state of the controller
        /// </summary>
        private void DetermineGroundstate()
        {
            workingGroundstateCurrent = Physics.SphereCast(transform.position,
                    attachedCollider.radius * (1.0f - settingsMovement.SettingShellOffset),
                    Vector3.down,
                    out RaycastHit hit,
                    ((attachedCollider.height / 2f) - attachedCollider.radius) + settingsMovement.ForceStickToGround,
                    Physics.AllLayers, QueryTriggerInteraction.Ignore);

            if (workingGroundstateCurrent)
                workingGroundContactNormal = hit.normal;
            else
                workingGroundContactNormal = Vector3.up;
        }

        /// <summary>
        /// Determines current controller speed based on input
        /// </summary>
        /// <returns>The speed that corresponds to input and movement settings</returns>
        private float DetermineDesiredSpeed()
        {
            // Slow movement is preferred over fast movement if both modifiers are held

            if (workingControlActionModifierMoveSlow)
                return settingsMovement.SpeedSlow * DetermineSlope();

            else if (workingControlActionModifierMoveFast)
                return settingsMovement.SpeedFast * DetermineSlope();

            else
                return settingsMovement.SpeedNorm * DetermineSlope();
        }

        /// <summary>
        /// Determines what modifier to apply to movement speed depending on slopes
        /// </summary>
        /// <returns>The speed modifier based on the current slope angle</returns>
        private float DetermineSlope()
        {
            float angle = Vector3.Angle(workingGroundContactNormal, Vector3.up);
            return settingsMovement.SettingSlopeModifier.Evaluate(angle);
        }

        /// <summary>
        /// Moves the player along the y axis based on input and/or gravity
        /// </summary>
        private void HandleGravity()
        {
            DetermineGroundstate();

            if (workingDoJump && workingGroundstateCurrent)
            {
                attachedRigidbody.AddForce(new Vector3(0f, settingsMovement.ForceJump * attachedRigidbody.mass, 0f), ForceMode.Impulse);
                workingDoJump = false;
            }

            else if (!workingGroundstateCurrent)
                attachedRigidbody.AddForce(Physics.gravity * settingsMovement.ForceGravityMultiplier * attachedRigidbody.drag * attachedRigidbody.mass, ForceMode.Force);
        }

        /// <summary>
        /// Moves the player along the xz (horizontal) plane based on input
        /// </summary>
        private void HandleMovement()
        {
            if (workingControlActionDoMovement)
            {
                Vector2 value = attachedControls.actions["Movement"].ReadValue<Vector2>().normalized;

                Vector3 dir = transform.forward * value.y + transform.right * value.x;
                float desiredSpeed = DetermineDesiredSpeed();
                Vector3 force = dir * desiredSpeed;
                force = Vector3.ProjectOnPlane(force, workingGroundContactNormal);

                if (attachedRigidbody.velocity.sqrMagnitude < Mathf.Pow(desiredSpeed, 2))
                    attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Rotates the player camera vertically and the controller horizontally based on input
        /// </summary>
        private void HandleRotation()
        {
            if (workingControlActionDoRotation)
            {
                Vector2 value = attachedControls.actions["Rotation"].ReadValue<Vector2>();

                workingRotationCamera *= Quaternion.Euler(-value.y * settingsCamera.cameraSensitivity, 0, 0);
                workingRotationCamera = workingRotationCamera.VerticalClampEuler(settingsCamera.CameraClampVertical.x, settingsCamera.CameraClampVertical.y);
                workingRotationController *= Quaternion.Euler(0, value.x * settingsCamera.cameraSensitivity, 0);
            }

            if (settingsCamera.smoothLook)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, workingRotationController, settingsCamera.smoothSpeed * Time.deltaTime);
                settingsCamera.AttachedCamera.transform.localRotation = Quaternion.Slerp(settingsCamera.AttachedCamera.transform.localRotation, workingRotationCamera, settingsCamera.smoothSpeed * Time.deltaTime);
            }

            else
            {
                transform.localRotation = workingRotationController;
                settingsCamera.AttachedCamera.transform.localRotation = workingRotationCamera;
            }
        }
    }
}