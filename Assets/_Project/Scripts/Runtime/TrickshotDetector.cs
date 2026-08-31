using System.Collections.Generic;
using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Riconosce i trickshot al momento dello sparo (Fase 1.2):
    /// - Airborne: in aria da >= 0.3s (precisione ridotta gestita dalla pistola)
    /// - Flick: rotazione orizzontale della camera >= 150° negli ultimi 0.5s
    /// - Behind-the-back: direzione di tiro opposta alla velocità (dot &lt; -0.5)
    /// Anti-spam: lo stesso trick dalla terza volta in 3s vale il 50%.
    /// </summary>
    public class TrickshotDetector : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField] Transform yawSource; // il corpo del player (solo yaw)

        [Header("Soglie (dal piano, da tarare in playtest)")]
        [SerializeField] float airborneMinTime = 0.3f;
        [SerializeField] float flickDegrees = 150f;
        [SerializeField] float flickWindow = 0.5f;
        [SerializeField] float behindDotThreshold = -0.5f;
        [SerializeField] float behindMinSpeed = 2f;

        [Header("Anti-spam")]
        [SerializeField] float spamWindow = 3f;
        [SerializeField] int spamFreeUses = 2; // dal terzo uso scatta la penalità
        [SerializeField] float spamPenalty = 0.5f;

        struct YawSample
        {
            public float Time;
            public float CumulativeYaw;
        }

        readonly Queue<YawSample> yawHistory = new Queue<YawSample>();
        readonly Dictionary<Tricks, Queue<float>> recentUses = new Dictionary<Tricks, Queue<float>>();

        CharacterController controller;
        float cumulativeYaw;
        float previousYaw;

        /// <summary>In aria in questo istante (per la precisione ridotta, senza soglia di tempo).</summary>
        public bool InAir => !motor.IsGrounded;

        void Awake()
        {
            controller = motor.GetComponent<CharacterController>();
            previousYaw = yawSource.eulerAngles.y;
        }

        void Update()
        {
            // Yaw "srotolato": DeltaAngle gestisce il wrap 360->0
            float yaw = yawSource.eulerAngles.y;
            cumulativeYaw += Mathf.DeltaAngle(previousYaw, yaw);
            previousYaw = yaw;

            yawHistory.Enqueue(new YawSample { Time = Time.time, CumulativeYaw = cumulativeYaw });
            while (yawHistory.Count > 0 && yawHistory.Peek().Time < Time.time - flickWindow)
                yawHistory.Dequeue();
        }

        /// <summary>Da chiamare al momento dello sparo.</summary>
        public TrickShotInfo Evaluate(Vector3 fireDirection)
        {
            Tricks tricks = Tricks.None;

            if (!motor.IsGrounded && motor.TimeSinceGrounded >= airborneMinTime)
                tricks |= Tricks.Airborne;

            foreach (var sample in yawHistory)
            {
                if (Mathf.Abs(cumulativeYaw - sample.CumulativeYaw) >= flickDegrees)
                {
                    tricks |= Tricks.Flick;
                    break;
                }
            }

            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            Vector3 flatDir = fireDirection;
            flatDir.y = 0f;
            if (velocity.magnitude >= behindMinSpeed && flatDir.sqrMagnitude > 1e-4f
                && Vector3.Dot(velocity.normalized, flatDir.normalized) < behindDotThreshold)
                tricks |= Tricks.BehindTheBack;

            float penalty = tricks == Tricks.None ? 1f : RegisterUseAndGetPenalty(tricks);
            return new TrickShotInfo(tricks, penalty);
        }

        /// <summary>Registra l'uso di ogni trick attivo; se uno è abusato, penalità.</summary>
        float RegisterUseAndGetPenalty(Tricks tricks)
        {
            bool spammed = false;
            foreach (Tricks single in new[] { Tricks.Airborne, Tricks.Flick, Tricks.BehindTheBack, Tricks.NoScope })
            {
                if ((tricks & single) == 0)
                    continue;

                if (!recentUses.TryGetValue(single, out var uses))
                    recentUses[single] = uses = new Queue<float>();

                while (uses.Count > 0 && uses.Peek() < Time.time - spamWindow)
                    uses.Dequeue();

                if (uses.Count >= spamFreeUses)
                    spammed = true;
                uses.Enqueue(Time.time);
            }
            return spammed ? spamPenalty : 1f;
        }
    }
}
