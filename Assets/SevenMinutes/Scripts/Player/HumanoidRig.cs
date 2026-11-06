using UnityEngine;

namespace SevenMinutes.Player
{
    public enum BodyMode
    {
        Gameplay,
        Lying,
        Sitting
    }

    /// <summary>
    /// Procedural animation for the human character (built from primitives by the scene builder):
    /// walk / run cycle, crouch, jump, reach (interact), lying in bed, sitting, eyelids and head look.
    /// </summary>
    public class HumanoidRig : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private Transform model;
        [SerializeField] private Transform pelvis;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform head;
        [SerializeField] private Transform shoulderL, elbowL, shoulderR, elbowR;
        [SerializeField] private Transform hipL, kneeL, hipR, kneeR;
        [SerializeField] private Transform eyelidL, eyelidR;
        [SerializeField] private Vector3 eyelidClosedScale = new Vector3(0.05f, 0.05f, 0.03f);
        [SerializeField] private Vector3 eyelidOpenScale = new Vector3(0.05f, 0.012f, 0.03f);

        public BodyMode Mode = BodyMode.Gameplay;
        [Range(0f, 1f)] public float EyesOpen = 1f;
        public float HeadYaw;

        private Vector3 pelvisBase;
        private float phase;
        private float reachTimer;

        private float thighL, thighR, kneeLA, kneeRA, armL, armR, elbL, elbR, spineX, headYaw, headPitch, pelvisY, bodyPitch;
        private Vector3 modelOffset;

        public void Configure(PlayerController owner, Transform modelRoot, Transform pelvisT, Transform spineT, Transform headT,
            Transform shL, Transform elL, Transform shR, Transform elR, Transform hL, Transform kL, Transform hR, Transform kR,
            Transform lidL, Transform lidR, Vector3 lidClosed, Vector3 lidOpen)
        {
            controller = owner; model = modelRoot; pelvis = pelvisT; spine = spineT; head = headT;
            shoulderL = shL; elbowL = elL; shoulderR = shR; elbowR = elR;
            hipL = hL; kneeL = kL; hipR = hR; kneeR = kR;
            eyelidL = lidL; eyelidR = lidR; eyelidClosedScale = lidClosed; eyelidOpenScale = lidOpen;
        }

        private void Awake()
        {
            if (pelvis != null) pelvisBase = pelvis.localPosition;
        }

        public void Reach() => reachTimer = 0.45f;

        /// <summary>Instantly apply the current mode (no blending), e.g. when the scene starts in bed.</summary>
        public void SnapPose()
        {
            Evaluate(0f, true);
        }

        private void LateUpdate()
        {
            Evaluate(Time.deltaTime, false);
        }

        private void Evaluate(float dt, bool snap)
        {
            float tThighL = 0, tThighR = 0, tKneeL = 0, tKneeR = 0, tArmL = 0, tArmR = 0, tElbL = -10, tElbR = -10;
            float tSpine = 0, tHeadPitch = 0, tPelvisY = 0, tBodyPitch = 0;
            Vector3 tModelOffset = Vector3.zero;

            switch (Mode)
            {
                case BodyMode.Lying:
                    tBodyPitch = -90f;
                    tArmL = 4f; tArmR = 4f; tElbL = -5f; tElbR = -5f;
                    tHeadPitch = -5f;
                    break;

                case BodyMode.Sitting:
                    tThighL = -88f; tThighR = -84f; tKneeL = 86f; tKneeR = 82f;
                    tArmL = -28f; tArmR = -28f; tElbL = -35f; tElbR = -35f;
                    tSpine = 6f; tHeadPitch = 6f;
                    tModelOffset = new Vector3(0f, -0.42f, -0.18f);
                    break;

                default:
                {
                    float speed = controller != null ? controller.PlanarSpeed : 0f;
                    float runSpeed = controller != null ? controller.RunSpeed : 5.8f;
                    bool grounded = controller == null || controller.IsGrounded;
                    bool crouch = controller != null && controller.IsCrouching;
                    float k = Mathf.Clamp01(speed / runSpeed);

                    phase += dt * speed * 3.9f;
                    float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
                    float amp = speed > 0.15f ? 14f + 34f * k : 0f;

                    tThighL = -s * amp; tThighR = s * amp;
                    tKneeL = Mathf.Max(0f, c) * amp * 1.25f + 4f;
                    tKneeR = Mathf.Max(0f, -c) * amp * 1.25f + 4f;
                    tArmL = s * amp * 0.75f; tArmR = -s * amp * 0.75f;
                    float elbow = -12f - 60f * k;
                    tElbL = elbow; tElbR = elbow;
                    tSpine = 4f + 8f * k;
                    tPelvisY = -Mathf.Abs(c) * 0.035f * k;

                    if (crouch)
                    {
                        tPelvisY -= 0.5f;
                        tThighL -= 62f; tThighR -= 62f;
                        tKneeL += 100f; tKneeR += 100f;
                        tSpine += 30f; tHeadPitch -= 18f;
                        tArmL -= 15f; tArmR -= 15f;
                    }
                    if (!grounded)
                    {
                        tThighL = -32f; tThighR = -12f;
                        tKneeL = 60f; tKneeR = 35f;
                        tArmL = -35f; tArmR = -25f;
                    }
                    break;
                }
            }

            if (reachTimer > 0f)
            {
                reachTimer -= dt;
                tArmR = -78f;
                tElbR = -8f;
            }

            float l = snap ? 1f : 1f - Mathf.Exp(-14f * dt);
            float lb = snap ? 1f : 1f - Mathf.Exp(-4f * dt);
            thighL = Mathf.Lerp(thighL, tThighL, l);
            thighR = Mathf.Lerp(thighR, tThighR, l);
            kneeLA = Mathf.Lerp(kneeLA, tKneeL, l);
            kneeRA = Mathf.Lerp(kneeRA, tKneeR, l);
            armL = Mathf.Lerp(armL, tArmL, l);
            armR = Mathf.Lerp(armR, tArmR, l);
            elbL = Mathf.Lerp(elbL, tElbL, l);
            elbR = Mathf.Lerp(elbR, tElbR, l);
            spineX = Mathf.Lerp(spineX, tSpine, l);
            headPitch = Mathf.Lerp(headPitch, tHeadPitch, l);
            headYaw = Mathf.Lerp(headYaw, HeadYaw, snap ? 1f : 1f - Mathf.Exp(-5f * dt));
            pelvisY = Mathf.Lerp(pelvisY, tPelvisY, l);
            bodyPitch = Mathf.Lerp(bodyPitch, tBodyPitch, lb);
            modelOffset = Vector3.Lerp(modelOffset, tModelOffset, lb);

            if (hipL != null) hipL.localRotation = Quaternion.Euler(thighL, 0f, 0f);
            if (hipR != null) hipR.localRotation = Quaternion.Euler(thighR, 0f, 0f);
            if (kneeL != null) kneeL.localRotation = Quaternion.Euler(kneeLA, 0f, 0f);
            if (kneeR != null) kneeR.localRotation = Quaternion.Euler(kneeRA, 0f, 0f);
            if (shoulderL != null) shoulderL.localRotation = Quaternion.Euler(armL, 0f, -6f);
            if (shoulderR != null) shoulderR.localRotation = Quaternion.Euler(armR, 0f, 6f);
            if (elbowL != null) elbowL.localRotation = Quaternion.Euler(elbL, 0f, 0f);
            if (elbowR != null) elbowR.localRotation = Quaternion.Euler(elbR, 0f, 0f);
            if (spine != null) spine.localRotation = Quaternion.Euler(spineX, 0f, 0f);
            if (head != null) head.localRotation = Quaternion.Euler(headPitch, headYaw, 0f);
            if (pelvis != null) pelvis.localPosition = pelvisBase + Vector3.up * pelvisY;
            if (model != null)
            {
                model.localRotation = Quaternion.Euler(bodyPitch, 0f, 0f);
                model.localPosition = modelOffset;
            }

            var lid = Vector3.Lerp(eyelidClosedScale, eyelidOpenScale, EyesOpen);
            if (eyelidL != null) eyelidL.localScale = lid;
            if (eyelidR != null) eyelidR.localScale = lid;
        }
    }
}
