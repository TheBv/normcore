using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Normal.Realtime;
using System.Linq;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(RealtimeTransform))]
public class SphereBehaviour : MonoBehaviour
{
    [SerializeField] private Realtime _realtime = null;

    [SerializeField] private RealtimeTransform _realtimeTransform;

    [SerializeField] private SphereCollider _collider;

    private enum Hand { LeftHand, RightHand };

    [SerializeField] private Hand _hand = Hand.LeftHand;

    [SerializeField] private Rigidbody body;

    private static int VELOCITY_LENGTH = 5;
    
    private Vector3[] releaseVelocities = new Vector3[VELOCITY_LENGTH];

    private int updateCount = 0;

    private Vector3 localGrabPoint;

    private Quaternion localGrabRotation;

    private Vector3 _handPosition;

    private Quaternion _handRotation;

    bool isFollowingHand = false;

    bool released = false;

    bool previousTriggerState = false;

    // Update is called once per frame
    void Update()
    {

        if (!_realtime.connected)
            return;
        XRNode node = _hand == Hand.LeftHand ? XRNode.LeftHand : XRNode.RightHand;
        string trigger = _hand == Hand.LeftHand ? "Left Trigger" : "Right Trigger";

        // Get the position & rotation of the hand
        bool handIsTracking = UpdatePose(node, ref _handPosition, ref _handRotation);

        // Figure out if the trigger is pressed or not
        bool triggerPressed = Input.GetAxisRaw(trigger) > 0.1f;

        if (!handIsTracking)
            triggerPressed = false;

        if (TriggerPressed(triggerPressed, previousTriggerState) && !isFollowingHand)
        {
            if (!_collider.bounds.Contains(_handPosition))
                return;
            Debug.Log("Grabbed");
            isFollowingHand = true;
            //TODO: 
            localGrabPoint = _collider.ClosestPointOnBounds(_handPosition);
            localGrabRotation = Quaternion.Inverse(_handRotation) * transform.rotation;
            _realtimeTransform.RequestOwnership();
        }
        else if (TriggerReleased(triggerPressed, previousTriggerState) && isFollowingHand)
        {
            Debug.Log("Released");
            isFollowingHand = false;
            released = true;
        }

        previousTriggerState = triggerPressed;
    }

    private static bool TriggerPressed(bool currentState, bool previousState)
    {
        return !previousState && currentState;
    }

    private static bool TriggerReleased(bool currentState, bool previousState)
    {
        return previousState && !currentState;
    }

    private void FixedUpdate()
    {
        if (isFollowingHand)
        {
            var prevPosition = transform.position;
            transform.rotation = _handRotation;
            
            transform.position = _handPosition;
            releaseVelocities[updateCount % VELOCITY_LENGTH] = (transform.position - prevPosition) / Time.fixedDeltaTime;
            updateCount++;
            updateCount = updateCount % VELOCITY_LENGTH;
        }
        if (released)
        {
            Vector3 releaseVelocity = new Vector3(
            releaseVelocities.Average(x => x.x),
            releaseVelocities.Average(x => x.y),
            releaseVelocities.Average(x => x.z));

            body.AddForce(releaseVelocity, ForceMode.Impulse);
            released = false;
        }

    }

    private static bool UpdatePose(XRNode node, ref Vector3 position, ref Quaternion rotation)
    {
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);

        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == node)
            {
                Vector3 nodePosition;
                Quaternion nodeRotation;
                bool gotPosition = nodeState.TryGetPosition(out nodePosition);
                bool gotRotation = nodeState.TryGetRotation(out nodeRotation);

                if (gotPosition)
                    position = nodePosition;
                if (gotRotation)
                    rotation = nodeRotation;

                return gotPosition;
            }
        }

        return false;
    }
}
