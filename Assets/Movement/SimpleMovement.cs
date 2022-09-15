using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Normal.Realtime;
using UnityEngine.InputSystem;
using System.Linq;
public class SimpleMovement : MonoBehaviour
{
    [SerializeField] private RealtimeAvatarManager _realtime = null;
    [SerializeField] public Transform cameraOffset;
    [SerializeField] private Camera _camera;
    private enum Hand { LeftHand, RightHand };

    [SerializeField] private Hand _hand = Hand.LeftHand;
    void FixedUpdate()
    {

        XRNode node = _hand == Hand.LeftHand ? XRNode.LeftHand : XRNode.RightHand;

        var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, leftHandDevices);

        if (leftHandDevices.Count == 1)
        {
            UnityEngine.XR.InputDevice device = leftHandDevices[0];

            Vector2 inputMovement;

            // Get the direction the camera is looking parallel to the ground plane.
            Vector3 cameraLookForwardVector = ProjectVectorOntoGroundPlane(_camera.transform.forward);
            Quaternion cameraLookForward = Quaternion.LookRotation(cameraLookForwardVector);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out inputMovement);
            // Use the camera look direction to convert the input movement from camera space to world space
            Vector3 _targetMovement = cameraLookForward * new Vector3(inputMovement.x, 0, inputMovement.y);
            
            Vector3 _movement = new Vector3();
            _movement = Vector3.Lerp(_movement, _targetMovement, Time.fixedDeltaTime * 5.0f);

            if (cameraOffset)
                cameraOffset.transform.position += _movement;
        }

        if (_realtime && _realtime.localAvatar) {
            _realtime.localAvatar.transform.position = cameraOffset.position;
        }

    }

    private static Vector3 ProjectVectorOntoGroundPlane(Vector3 vector)
    {
        Vector3 planeNormal = Vector3.up;
        Vector3.OrthoNormalize(ref planeNormal, ref vector);
        return vector;
    }
}