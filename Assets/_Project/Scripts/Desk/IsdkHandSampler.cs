using System.Collections.Generic;
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

        // The Meta rig has several hands per side (tracked hands, hands driven by the controllers). On the headset a
        // valid-looking one can sit frozen near the eyes, so of the valid ones the lowest is the user's hand.
        readonly List<Hand> lefts = new List<Hand>();
        readonly List<Hand> rights = new List<Hand>();
        float nextSearch;

        public void Sample(out HandSample leftSample, out HandSample rightSample)
        {
            FindHands();
            leftSample = Read(Lowest(lefts), leftContacts);
            rightSample = Read(Lowest(rights), rightContacts);
        }

        /// <summary>True while either hand pinches thumb and index together.</summary>
        public bool IsPinching()
        {
            FindHands();
            IHand left = Lowest(lefts);
            IHand right = Lowest(rights);
            return (left != null && left.GetIndexFingerIsPinching()) || (right != null && right.GetIndexFingerIsPinching());
        }

        static IHand Lowest(List<Hand> hands)
        {
            IHand lowest = null;
            float lowestY = float.MaxValue;
            foreach (Hand hand in hands)
            {
                if (hand != null && hand.isActiveAndEnabled && hand.IsTrackedDataValid &&
                    hand.GetJointPose(HandJointId.HandWristRoot, out Pose wrist) && wrist.position.y < lowestY)
                {
                    lowest = hand;
                    lowestY = wrist.position.y;
                }
            }

            return lowest;
        }

        void FindHands()
        {
            if ((lefts.Count > 0 && rights.Count > 0) || Time.unscaledTime < nextSearch)
            {
                return;
            }

            nextSearch = Time.unscaledTime + SearchInterval;
            lefts.Clear();
            rights.Clear();
            foreach (Hand hand in Object.FindObjectsByType<Hand>(FindObjectsInactive.Include))
            {
                // Hand subclasses (synthetic hands, filters) sit on top of the tracked ones.
                if (hand.GetType() != typeof(Hand))
                {
                    continue;
                }

                (hand.Handedness == Handedness.Left ? lefts : rights).Add(hand);
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
