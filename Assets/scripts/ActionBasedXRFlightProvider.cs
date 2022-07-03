using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace UnityEngine.XR.Interaction.Toolkit
{

    public class ActionBasedXRFlightProvider : XRFlightProvider
    {

        [SerializeField]
        [Tooltip("The Input System Action that will be used to read stick data from the left hand controller. Must be a Value Vector2 Control.")]
        InputActionProperty m_LeftHandStickAction;
        /// <summary>
        /// The Input System Action that Unity uses to read stick data from the left hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty leftHandStickAction
        {
            get => m_LeftHandStickAction;
            set => SetInputActionProperty(ref m_LeftHandStickAction, value);
        }

        [SerializeField]
        [Tooltip("The Input System Action that will be used to read stick data from the right hand controller. Must be a Value Vector2 Control.")]
        InputActionProperty m_RightHandStickAction;
        /// <summary>
        /// The Input System Action that Unity uses to read stick data from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty rightHandStickAction
        {
            get => m_RightHandStickAction;
            set => SetInputActionProperty(ref m_RightHandStickAction, value);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnEnable()
        {
            m_LeftHandStickAction.EnableDirectAction();
            m_RightHandStickAction.EnableDirectAction();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
            m_LeftHandStickAction.DisableDirectAction();
            m_RightHandStickAction.DisableDirectAction();
        }

        protected override Vector2 ReadLeftInput()
        {

            var leftHandValue = m_LeftHandStickAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

            return leftHandValue;
        }

        protected override Vector2 ReadRightInput()
        {

            var rightHandValue = m_RightHandStickAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
            return rightHandValue;
        }

        void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value)
        {
            if (Application.isPlaying)
                property.DisableDirectAction();

            property = value;

            if (Application.isPlaying && isActiveAndEnabled)
                property.EnableDirectAction();
        }
    }
}
