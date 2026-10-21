using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Player
{
    /// <summary>
    /// Oyuncunun WASD ile hareketini yönetir.
    /// Diyalog sırasında hareketi kapatmak için SetInputEnabled kullanılır.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController controller;
        private float verticalVelocity;
        private bool inputEnabled = true;

        public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 direction = inputEnabled ? ReadInput() : Vector3.zero;
            Move(direction);
        }

        private Vector3 ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector3.zero;

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            return new Vector3(x, 0f, z).normalized;
        }

        private void Move(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * moveSpeed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
