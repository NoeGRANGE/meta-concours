using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Data;
using Deskmates.Desk;
using UnityEngine;
using UnityEngine.UI;

namespace Deskmates.UI
{
    /// <summary>
    /// The first-run walkthrough (PRD 6.1): hand on the desk, the mail creature drops in, worries, is looked at and
    /// pinched, sighs, then the rest of the family joins. Afterwards it only shows the hand prompt whenever a
    /// calibration waits. Instructions sit on a small card in front of the user.
    /// shortcut: text card only, no animated ghost hand nor hatching egg yet (PRD 6.1), add them if time allows.
    /// </summary>
    public sealed class Onboarding : MonoBehaviour
    {
        const string DoneKey = "onboarding.done";
        const string CalibratePrompt = "Place your hand flat on your desk\n<size=22>and hold it still for a second</size>";

        /// <summary>Test hook: forces the walkthrough done (true) or pending (false) instead of the saved state.</summary>
        public static bool? DoneOverride;

        [SerializeField] AppBootstrap app;
        [SerializeField] DeskSession desk;
        [SerializeField] Transform head;
        [SerializeField] Canvas card;
        [SerializeField] Text cardText;

        [Tooltip("Where the card floats: metres in front of the eyes, and below them.")]
        [SerializeField] float distance = 0.6f;
        [SerializeField] float below = 0.12f;

        [SerializeField] float introSeconds = 5f;
        [SerializeField] float farewellSeconds = 6f;

        enum Step { Desk, Intro, Wait, Look, Clear, Farewell, Done }

        Step step;
        float stepTime;
        float demoSpeed = 1f;
        Creature mail;
        bool relieved;
        bool shown;

        public static bool IsDone => DoneOverride ?? PlayerPrefs.GetInt(DoneKey, 0) == 1;

        public bool IsRunning => step != Step.Done;

        void Start()
        {
            if (IsDone)
            {
                step = Step.Done;
                return;
            }

            // Nothing on the desk and the demo on hold until the user has shown where the desk is.
            app.HideAll();
            if (DataHub.Instance != null)
            {
                demoSpeed = DataHub.Instance.Speed;
                DataHub.Instance.Speed = 0f;
            }

            Go(Step.Desk);
        }

        void OnDestroy()
        {
            if (mail != null)
            {
                mail.Relieved -= OnRelieved;
            }
        }

        void Update()
        {
            stepTime += Time.deltaTime;
            string prompt = Advance();
            card.gameObject.SetActive(prompt != null);
            if (prompt == null)
            {
                shown = false;
                return;
            }

            cardText.text = prompt;
            Follow();
        }

        string Advance()
        {
            if (step != Step.Done && step != Step.Desk && step != Step.Farewell && relieved)
            {
                // The sigh is the reward: now the rest of the family drops in.
                app.Reveal(int.MaxValue);
                MarkDone();
                Go(Step.Farewell);
            }

            switch (step)
            {
                case Step.Desk:
                    if (desk.HasDesk)
                    {
                        app.Reveal(1);
                        mail = app.MailCreature;
                        mail.Relieved += OnRelieved;
                        if (DataHub.Instance != null)
                        {
                            DataHub.Instance.Speed = demoSpeed;
                        }

                        Go(Step.Intro);
                    }

                    return CalibratePrompt;
                case Step.Intro:
                    if (stepTime >= introSeconds)
                    {
                        Go(Step.Wait);
                    }

                    return "This is your Mail Fretling\n<size=22>It worries about your inbox so you don't have to</size>";
                case Step.Wait:
                    if (mail.Expression >= CreatureExpression.Stressed)
                    {
                        Go(Step.Look);
                    }

                    return "Keep working\n<size=22>It will let you know when mail needs you</size>";
                case Step.Look:
                    if (DetailPanel.Instance != null && DetailPanel.Instance.Creature != null)
                    {
                        Go(Step.Clear);
                    }

                    return "It's worried!\n<size=22>Look at it, then pinch</size>";
                case Step.Clear:
                    return "Pinch each line to mark it read\n<size=22>Watch it calm down</size>";
                case Step.Farewell:
                    if (stepTime >= farewellSeconds)
                    {
                        Go(Step.Done);
                    }

                    return "Meet your Fretlings!\n<size=22>Calendar, Tasks and Messages worry for you too</size>";
                default:
                    return desk.IsCalibrating ? CalibratePrompt : null;
            }
        }

        void Go(Step next)
        {
            step = next;
            stepTime = 0f;
        }

        void OnRelieved() => relieved = true;

        static void MarkDone()
        {
            if (DoneOverride == null)
            {
                PlayerPrefs.SetInt(DoneKey, 1);
                PlayerPrefs.Save();
            }
        }

        // Floats a little below the eyes, in front of the user, and catches up gently when the head turns.
        void Follow()
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            Vector3 target = head.position + forward * distance + Vector3.down * below;
            float follow = shown ? 1f - Mathf.Exp(-3f * Time.deltaTime) : 1f;
            card.transform.position = Vector3.Lerp(card.transform.position, target, follow);
            Billboard.Face(card.transform);
            shown = true;
        }
    }
}
