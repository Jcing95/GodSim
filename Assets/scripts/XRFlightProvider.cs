using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Locomotion provider that allows the user to smoothly rotate their rig continuously over time.
    /// </summary>
    /// <seealso cref="LocomotionProvider"/>
    /// <seealso cref="SnapTurnProviderBase"/>
    public abstract class XRFlightProvider : LocomotionProvider
    {
        [SerializeField]
        [Tooltip("The number of degrees/second clockwise to rotate when turning clockwise.")]
        float m_TurnSpeed = 60f;
        /// <summary>
        /// The number of degrees/second clockwise to rotate when turning clockwise.
        /// </summary>
        public float turnSpeed
        {
            get => m_TurnSpeed;
            set => m_TurnSpeed = value;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            // Use the input amount to scale the turn speed.
            Vector2 leftInput = ReadLeftInput();
            var turnAmount = GetTurnAmount(leftInput);
            TurnRig(turnAmount);

            var xrOrigin = system.xrOrigin;
            if (xrOrigin == null)
                return;


            Vector2 rightInput = ReadRightInput();

            Debug.Log("turnInput: " + leftInput + " moveInput: " + rightInput);
            var translationInWorldSpace = ComputeDesiredMove(leftInput, rightInput);

            switch (m_GravityApplicationMode)
            {
                case GravityApplicationMode.Immediately:
                    MoveRig(translationInWorldSpace);
                    break;
                case GravityApplicationMode.AttemptingMove:
                    if ((rightInput != Vector2.zero || leftInput != Vector2.zero) || m_VerticalVelocity != Vector3.zero)
                    {
                        MoveRig(translationInWorldSpace);
                    }

                    break;
                default:
                    Assert.IsTrue(false, $"{nameof(m_GravityApplicationMode)}={m_GravityApplicationMode} outside expected range.");
                    break;
            }

        }

        /// <summary>
        /// Reads the current value of the turn input.
        /// </summary>
        /// <returns>Returns the input vector, such as from a thumbstick.</returns>
        protected abstract Vector2 ReadLeftInput();

        /// <summary>
        /// Reads the current value of the turn input.
        /// </summary>
        /// <returns>Returns the input vector, such as from a thumbstick.</returns>
        protected abstract Vector2 ReadRightInput();


        /// <summary>
        /// Determines the turn amount in degrees for the given <paramref name="input"/> vector.
        /// </summary>
        /// <param name="input">Input vector, such as from a thumbstick.</param>
        /// <returns>Returns the turn amount in degrees for the given <paramref name="input"/> vector.</returns>
        protected virtual float GetTurnAmount(Vector2 input)
        {
            if (input == Vector2.zero)
                return 0f;

            var cardinal = CardinalUtility.GetNearestCardinal(input);
            switch (cardinal)
            {
                case Cardinal.North:
                case Cardinal.South:
                    break;
                case Cardinal.East:
                case Cardinal.West:
                    return input.magnitude * (Mathf.Sign(input.x) * m_TurnSpeed * Time.deltaTime);
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                    break;
            }

            return 0f;
        }

        /// <summary>
        /// Rotates the rig by <paramref name="turnAmount"/> degrees.
        /// </summary>
        /// <param name="turnAmount">The amount of rotation in degrees.</param>
        protected void TurnRig(float turnAmount)
        {
            if (Mathf.Approximately( turnAmount, 0f))
                return;

            if (CanBeginLocomotion() && BeginLocomotion())
            {
                var xrOrigin = system.xrOrigin;
                if (xrOrigin != null)
                {
                    xrOrigin.RotateAroundCameraUsingOriginUp(turnAmount);
                }

                EndLocomotion();
            }
        }
    

    // <summary>
        /// Defines when gravity begins to take effect.
        /// </summary>
        /// <seealso cref="gravityApplicationMode"/>
        public enum GravityApplicationMode
        {
            /// <summary>
            /// Only begin to apply gravity and apply locomotion when a move input occurs.
            /// When using gravity, continues applying each frame, even if input is stopped, until touching ground.
            /// </summary>
            /// <remarks>
            /// Use this style when you don't want gravity to apply when the player physically walks away and off a ground surface.
            /// Gravity will only begin to move the player back down to the ground when they try to use input to move.
            /// </remarks>
            AttemptingMove,

            /// <summary>
            /// Apply gravity and apply locomotion every frame, even without move input.
            /// </summary>
            /// <remarks>
            /// Use this style when you want gravity to apply when the player physically walks away and off a ground surface,
            /// even when there is no input to move.
            /// </remarks>
            Immediately,
        }

        [SerializeField]
        [Tooltip("The speed, in units per second, to move forward.")]
        float m_MoveSpeed = 1f;
        /// <summary>
        /// The speed, in units per second, to move forward.
        /// </summary>
        public float moveSpeed
        {
            get => m_MoveSpeed;
            set => m_MoveSpeed = value;
        }

        [SerializeField]
        [Tooltip("Controls whether to enable strafing (sideways movement).")]
        bool m_EnableStrafe = true;
        /// <summary>
        /// Controls whether to enable strafing (sideways movement).
        /// </summary>
        public bool enableStrafe
        {
            get => m_EnableStrafe;
            set => m_EnableStrafe = value;
        }

        [SerializeField]
        [Tooltip("Controls whether gravity affects this provider when a Character Controller is used.")]
        bool m_UseGravity = true;
        /// <summary>
        /// Controls whether gravity affects this provider when a <see cref="CharacterController"/> is used.
        /// </summary>
        public bool useGravity
        {
            get => m_UseGravity;
            set => m_UseGravity = value;
        }

        [SerializeField]
        [Tooltip("Controls when gravity begins to take effect.")]
        GravityApplicationMode m_GravityApplicationMode;
        /// <summary>
        /// Controls when gravity begins to take effect.
        /// </summary>
        /// <seealso cref="GravityApplicationMode"/>
        public GravityApplicationMode gravityApplicationMode
        {
            get => m_GravityApplicationMode;
            set => m_GravityApplicationMode = value;
        }

        [SerializeField]
        [Tooltip("The source Transform to define the forward direction.")]
        Transform m_ForwardSource;
        /// <summary>
        /// The source <see cref="Transform"/> that defines the forward direction.
        /// </summary>
        public Transform forwardSource
        {
            get => m_ForwardSource;
            set => m_ForwardSource = value;
        }

        CharacterController m_CharacterController;

        bool m_AttemptedGetCharacterController;

        Vector3 m_VerticalVelocity;


        /// <summary>
        /// Determines how much to slide the rig due to <paramref name="input"/> vector.
        /// </summary>
        /// <param name="input">Input vector, such as from a thumbstick.</param>
        /// <returns>Returns the translation amount in world space to move the rig.</returns>
        protected virtual Vector3 ComputeDesiredMove(Vector2 leftInput, Vector2 rightInput)
        {
            if (leftInput == Vector2.zero && rightInput == Vector2.zero)
                return Vector3.zero;

            var xrOrigin = system.xrOrigin;
            if (xrOrigin == null)
                return Vector3.zero;

            // Assumes that the input axes are in the range [-1, 1].
            // Clamps the magnitude of the input direction to prevent faster speed when moving diagonally,
            // while still allowing for analog input to move slower (which would be lost if simply normalizing).
            var inputMove = Vector3.ClampMagnitude(new Vector3(m_EnableStrafe ? rightInput.x : 0f, 0f, rightInput.y), 1f);

            var originTransform = xrOrigin.Origin.transform;
            var originUp = originTransform.up;

            // Determine frame of reference for what the input direction is relative to
            var forwardSourceTransform = m_ForwardSource == null ? xrOrigin.Camera.transform : m_ForwardSource;
            var inputForwardInWorldSpace = forwardSourceTransform.forward;
            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(inputForwardInWorldSpace, originUp)), 1f))
            {
                // When the input forward direction is parallel with the rig normal,
                // it will probably feel better for the player to move along the same direction
                // as if they tilted forward or up some rather than moving in the rig forward direction.
                // It also will probably be a better experience to at least move in a direction
                // rather than stopping if the head/controller is oriented such that it is perpendicular with the rig.
                inputForwardInWorldSpace = -forwardSourceTransform.up;
            }

            var inputForwardProjectedInWorldSpace = Vector3.ProjectOnPlane(inputForwardInWorldSpace, originUp);
            var forwardRotation = Quaternion.FromToRotation(originTransform.forward, inputForwardProjectedInWorldSpace);

            var translationInRigSpace = forwardRotation * inputMove * (m_MoveSpeed * Time.deltaTime);
            var translationInWorldSpace = originTransform.TransformDirection(translationInRigSpace);
            translationInWorldSpace += new Vector3(0,leftInput.normalized.y * m_MoveSpeed * Time.deltaTime,0);
            return translationInWorldSpace;
        }

        /// <summary>
        /// Creates a locomotion event to move the rig by <paramref name="translationInWorldSpace"/>,
        /// and optionally applies gravity.
        /// </summary>
        /// <param name="translationInWorldSpace">The translation amount in world space to move the rig (pre-gravity).</param>
        protected virtual void MoveRig(Vector3 translationInWorldSpace)
        {
            var xrOrigin = system.xrOrigin;
            if (xrOrigin == null)
                return;

            FindCharacterController();

            var motion = translationInWorldSpace;

            if (m_CharacterController != null && m_CharacterController.enabled)
            {
                // Step vertical velocity from gravity
                if (m_CharacterController.isGrounded || !m_UseGravity)
                {
                    m_VerticalVelocity = Vector3.zero;
                }
                else
                {
                    m_VerticalVelocity += Physics.gravity * Time.deltaTime;
                }

                motion += m_VerticalVelocity * Time.deltaTime;

                if (CanBeginLocomotion() && BeginLocomotion())
                {
                    // Note that calling Move even with Vector3.zero will have an effect by causing isGrounded to update
                    m_CharacterController.Move(motion);

                    EndLocomotion();
                }
            }
            else
            {
                if (CanBeginLocomotion() && BeginLocomotion())
                {
                    xrOrigin.Origin.transform.position += motion;

                    EndLocomotion();
                }
            }
        }

        void FindCharacterController()
        {
            var xrOrigin = system.xrOrigin;
            if (xrOrigin == null)
                return;

            // Save a reference to the optional CharacterController on the rig GameObject
            // that will be used to move instead of modifying the Transform directly.
            if (m_CharacterController == null && !m_AttemptedGetCharacterController)
            {
                m_CharacterController = xrOrigin.Origin.GetComponent<CharacterController>();
                m_AttemptedGetCharacterController = true;
            }
        }
    }
}

