using Oculus.Interaction.Input;
using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>
    /// Reads the joints the calibration needs from the Interaction SDK hands of the scene.
    /// </summary>
    public sealed class IsdkHandSampler
    {
        // The points that touch the desk when the hand lies flat on it.
        static readonly HandJointId[] ContactJoints =
        {
            HandJointId.HandWristRoot,
            HandJointId.HandIndex1, HandJointId.HandMiddle1, HandJointId.HandRing1, HandJointId.HandPinky1,
            HandJointId.HandIndexTip, HandJointId.HandMiddleTip, HandJointId.HandRingTip, HandJointId.HandPinkyTip,
        };

        const float SearchInterval = 1f;

        readonly Vector3[] leftContacts = new Vector3[ContactJoints.Length];
        readonly Vector3[] rightContacts = new Vector3[ContactJoints.Length];
        IHand left;
        IHand right;
        float nextSearch;

        public void Sample(out HandSample leftSample, out HandSample rightSample)
        {
            FindHands();
            leftSample = Read(left, leftContacts);
            rightSample = Read(right, rightContacts);
        }

        void FindHands()
        {
            if ((left != null && right != null) || Time.unscaledTime < nextSearch)
            {
                return;
            }

            nextSearch = Time.unscaledTime + SearchInterval;
            foreach (Hand hand in Object.FindObjectsByType<Hand>(FindObjectsSortMode.None))
            {
                // Hand subclasses (synthetic hands, filters) sit on top of the tracked ones.
                if (hand.GetType() != typeof(Hand))
                {
                    continue;
                }

                if (hand.Handedness == Handedness.Left)
                {
                    left ??= hand;
                }
                else
                {
                    right ??= hand;
                }
            }
        }

        static HandSample Read(IHand hand, Vector3[] contacts)
        {
            if (hand == null || !hand.IsTrackedDataValid || !hand.IsHighConfidence)
            {
                return default;
            }

            for (int i = 0; i < ContactJoints.Length; i++)
            {
                if (!hand.GetJointPose(ContactJoints[i], out Pose pose))
                {
                    return default;
                }

                contacts[i] = pose.position;
            }

            return new HandSample { Valid = true, Contacts = contacts };
        }
    }
}
