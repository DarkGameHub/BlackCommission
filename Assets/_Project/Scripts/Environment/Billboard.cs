using UnityEngine;

namespace Project.Environment
{
    [ExecuteInEditMode]
    public class Billboard : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera != null)
            {
                Vector3 targetPosition = _mainCamera.transform.position;
                targetPosition.y = transform.position.y;
                transform.LookAt(targetPosition);
                transform.Rotate(0, 180, 0);
            }
        }
    }
}