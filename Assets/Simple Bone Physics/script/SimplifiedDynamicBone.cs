using UnityEngine;
namespace AtivusStudio.SimplifiedDynamicBone
{
    public class SimplifiedDynamicBone : MonoBehaviour
    {
        public enum Preset
        {
            Custom,
            Hair,
            Tail,
            Cloth,
            Backpack
        }

        [Header("Bones")]
        [Tooltip("First bone of the chain. Assign it manually.")]
        public Transform tail1;

        [HideInInspector]
        public Transform tail2;

        [HideInInspector]
        public Transform tail3;

        [HideInInspector]
        public bool autoFindChildren = true;

        [HideInInspector]
        public Preset preset = Preset.Custom;

        [Header("Main Controls")]
        [Range(0f, 1f)]
        public float inertia = 0.2f;

        [Range(0f, 1f)]
        public float stiffness = 0.8f;

        [Range(0f, 1f)]
        public float stretch = 0.1f;

        [Range(0f, 1f)]
        public float velocity = 0.2f;

        [Header("Cone Limit / Fake Collision")]
        public bool useConeLimit = false;

        [Tooltip("Local cone rotation offset for Tail 1. This rotates the cone direction from the initial Tail 1 direction.")]
        public Vector3 tail1ConeOffset = Vector3.zero;

        [Tooltip("Local cone rotation offset for Tail 2. This rotates the cone direction from the initial Tail 2 direction.")]
        public Vector3 tail2ConeOffset = Vector3.zero;

        [Range(0f, 180f)]
        public float tail1ConeAngle = 70f;

        [Range(0f, 180f)]
        public float tail2ConeAngle = 90f;

        [Header("Runtime")]
        public bool resetToInitialPose = true;
        public float maxDeltaTime = 0.033f;
        public bool liveUpdateValues = true;

        [Header("Debug")]
        public bool drawGizmos = true;

        [HideInInspector][SerializeField] private float returnForce;
        [HideInInspector][SerializeField] private float damping;
        [HideInInspector][SerializeField] private float velocityPreservation;
        [HideInInspector][SerializeField] private float inertiaForce;
        [HideInInspector][SerializeField] private float maxVelocity;

        [HideInInspector][SerializeField] private float p2Motion;
        [HideInInspector][SerializeField] private float p3Motion;

        [HideInInspector][SerializeField] private float maxStretch;

        private Quaternion tail1InitialLocalRotation;
        private Quaternion tail2InitialLocalRotation;

        private Vector3 tail2InitialLocalPosition;
        private Vector3 tail3InitialLocalPosition;

        private Vector3 tail2LocalAxis;

        private Vector3 initialTail1ConeDirection;
        private Vector3 initialTail2ConeDirection;

        private float length12;
        private float length23;

        private Vector3 p2;
        private Vector3 p3;

        private Vector3 p2Velocity;
        private Vector3 p3Velocity;

        private Vector3 previousDefaultP2;
        private Vector3 previousDefaultP3;

        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnValidate()
        {
            autoFindChildren = true;

            if (autoFindChildren)
                AutoFindChildren();

            UpdateGeneratedValues();
        }

        private void AutoFindChildren()
        {
            if (tail1 == null)
                return;

            if (tail1.childCount > 0)
            {
                tail2 = tail1.GetChild(0);

                if (tail2.childCount > 0)
                    tail3 = tail2.GetChild(0);
            }
        }

        public void ApplyPreset(Preset selectedPreset)
        {
            switch (selectedPreset)
            {
                case Preset.Custom:
                    break;

                case Preset.Hair:
                    inertia = 0.38f;
                    stiffness = 0.55f;
                    stretch = 0.3f;
                    velocity = 0.4f;

                    useConeLimit = false;
                    tail1ConeOffset = Vector3.zero;
                    tail2ConeOffset = Vector3.zero;
                    tail1ConeAngle = 70f;
                    tail2ConeAngle = 90f;
                    break;

                case Preset.Tail:
                    inertia = 0.2f;
                    stiffness = 0.55f;
                    stretch = 0.18f;
                    velocity = 0.45f;

                    useConeLimit = false;
                    tail1ConeOffset = Vector3.zero;
                    tail2ConeOffset = Vector3.zero;
                    tail1ConeAngle = 80f;
                    tail2ConeAngle = 110f;
                    break;

                case Preset.Cloth:
                    inertia = 0.32f;
                    stiffness = 0.48f;
                    stretch = 0.06f;
                    velocity = 0.32f;

                    useConeLimit = false;
                    tail1ConeOffset = Vector3.zero;
                    tail2ConeOffset = Vector3.zero;
                    tail1ConeAngle = 45f;
                    tail2ConeAngle = 70f;
                    break;

                case Preset.Backpack:
                    inertia = 0.1f;
                    stiffness = 1f;
                    stretch = 0.1f;
                    velocity = 0.1f;

                    useConeLimit = false;
                    tail1ConeOffset = Vector3.zero;
                    tail2ConeOffset = Vector3.zero;
                    tail1ConeAngle = 20f;
                    tail2ConeAngle = 30f;
                    break;
            }

            preset = Preset.Custom;
            UpdateGeneratedValues();
            ResetSimulationPose();
        }

        public void ResetSimulationPose()
        {
            if (tail1 == null || tail2 == null || tail3 == null)
                return;

            if (initialized)
            {
                tail1.localRotation = tail1InitialLocalRotation;
                tail2.localRotation = tail2InitialLocalRotation;

                tail2.localPosition = tail2InitialLocalPosition;
                tail3.localPosition = tail3InitialLocalPosition;
            }

            p2 = tail2.position;
            p3 = tail3.position;

            p2Velocity = Vector3.zero;
            p3Velocity = Vector3.zero;

            previousDefaultP2 = tail2.position;
            previousDefaultP3 = tail3.position;
        }

        private void Initialize()
        {
            autoFindChildren = true;
            AutoFindChildren();

            if (tail1 == null || tail2 == null || tail3 == null)
            {
                initialized = false;
                return;
            }

            UpdateGeneratedValues();

            tail1InitialLocalRotation = tail1.localRotation;
            tail2InitialLocalRotation = tail2.localRotation;

            tail2InitialLocalPosition = tail2.localPosition;
            tail3InitialLocalPosition = tail3.localPosition;

            length12 = tail2InitialLocalPosition.magnitude;
            length23 = tail3InitialLocalPosition.magnitude;

            if (length12 <= 0.0001f || length23 <= 0.0001f)
            {
                initialized = false;
                return;
            }

            tail2LocalAxis = tail2InitialLocalPosition.normalized;

            initialTail1ConeDirection = tail1.TransformDirection(tail2InitialLocalPosition.normalized);
            initialTail2ConeDirection = tail2.TransformDirection(tail3InitialLocalPosition.normalized);

            p2 = tail2.position;
            p3 = tail3.position;

            p2Velocity = Vector3.zero;
            p3Velocity = Vector3.zero;

            previousDefaultP2 = tail2.position;
            previousDefaultP3 = tail3.position;

            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            if (liveUpdateValues)
                UpdateGeneratedValues();

            Simulate(Time.deltaTime);
        }

        private void UpdateGeneratedValues()
        {
            returnForce = Mathf.Lerp(5f, 90f, stiffness);
            damping = Mathf.Lerp(14f, 1.5f, velocity);
            velocityPreservation = Mathf.Lerp(0.1f, 0.95f, velocity);

            inertiaForce = Mathf.Lerp(0f, 1.15f, inertia);

            p2Motion = Mathf.Lerp(0.25f, 1.15f, inertia);
            p3Motion = Mathf.Lerp(0.45f, 2.15f, inertia);

            maxVelocity = Mathf.Lerp(5f, 28f, velocity);
            maxStretch = Mathf.Lerp(0f, 0.35f, stretch);
        }

        private void Simulate(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            deltaTime = Mathf.Min(deltaTime, maxDeltaTime);

            if (resetToInitialPose)
            {
                tail1.localRotation = tail1InitialLocalRotation;
                tail2.localRotation = tail2InitialLocalRotation;

                tail2.localPosition = tail2InitialLocalPosition;
                tail3.localPosition = tail3InitialLocalPosition;
            }

            Vector3 root = tail1.position;

            Vector3 defaultP2 = tail2.position;
            Vector3 defaultP3 = tail3.position;

            Vector3 defaultP2Motion = defaultP2 - previousDefaultP2;
            Vector3 defaultP3Motion = defaultP3 - previousDefaultP3;

            previousDefaultP2 = defaultP2;
            previousDefaultP3 = defaultP3;

            float safeDeltaTime = Mathf.Max(deltaTime, 0.0001f);

            p2Velocity += -defaultP2Motion / safeDeltaTime * inertiaForce * p2Motion;
            p3Velocity += -defaultP3Motion / safeDeltaTime * inertiaForce * p3Motion;

            SimulatePoint(ref p2, ref p2Velocity, defaultP2, deltaTime);
            SimulatePoint(ref p3, ref p3Velocity, defaultP3, deltaTime);

            float currentLength12 = length12;

            if (stretch > 0.001f)
            {
                float stretchAmount = Vector3.Distance(root, p2) - length12;

                stretchAmount = Mathf.Clamp(
                    stretchAmount,
                    -maxStretch,
                    maxStretch
                );

                currentLength12 = length12 + stretchAmount;
            }

            // Real stretch is always active.
            if (stretch > 0.001f)
            {
                tail2.localPosition = tail2LocalAxis * currentLength12;

                defaultP2 = tail2.position;
                defaultP3 = tail3.position;
            }

            ConstrainPoint(
                ref p2,
                ref p2Velocity,
                root,
                currentLength12,
                defaultP2 - root
            );

            if (useConeLimit)
            {
                Vector3 tail1ConeCenter = GetConeCenterDirection(
                    initialTail1ConeDirection,
                    tail1ConeOffset
                );

                ApplyConeLimit(
                    ref p2,
                    ref p2Velocity,
                    root,
                    currentLength12,
                    tail1ConeCenter,
                    tail1ConeAngle
                );
            }

            ConstrainPoint(
                ref p3,
                ref p3Velocity,
                p2,
                length23,
                defaultP3 - defaultP2
            );

            if (useConeLimit)
            {
                Vector3 tail2ConeCenter = GetConeCenterDirection(
                    initialTail2ConeDirection,
                    tail2ConeOffset
                );

                ApplyConeLimit(
                    ref p3,
                    ref p3Velocity,
                    p2,
                    length23,
                    tail2ConeCenter,
                    tail2ConeAngle
                );
            }

            RotateBoneToPoint(
                tail1,
                defaultP2,
                p2
            );

            RotateBoneToPoint(
                tail2,
                tail3.position,
                p3
            );
        }

        private Vector3 GetConeCenterDirection(Vector3 baseDirection, Vector3 eulerOffset)
        {
            if (baseDirection.sqrMagnitude <= 0.000001f)
                baseDirection = Vector3.forward;

            baseDirection.Normalize();

            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.forward, baseDirection);
            Quaternion offsetRotation = Quaternion.Euler(eulerOffset);

            return (baseRotation * offsetRotation * Vector3.forward).normalized;
        }

        private void ApplyConeLimit(
            ref Vector3 point,
            ref Vector3 pointVelocity,
            Vector3 anchor,
            float length,
            Vector3 coneCenterDirection,
            float coneAngle
        )
        {
            Vector3 direction = point - anchor;

            if (direction.sqrMagnitude <= 0.000001f)
                return;

            direction.Normalize();

            if (coneCenterDirection.sqrMagnitude <= 0.000001f)
                return;

            coneCenterDirection.Normalize();

            float angle = Vector3.Angle(coneCenterDirection, direction);

            if (angle <= coneAngle)
                return;

            Vector3 limitedDirection = Vector3.RotateTowards(
                coneCenterDirection,
                direction,
                coneAngle * Mathf.Deg2Rad,
                0f
            );

            point = anchor + limitedDirection * length;

            pointVelocity *= 0.6f;
        }

        private void SimulatePoint(
            ref Vector3 position,
            ref Vector3 pointVelocity,
            Vector3 target,
            float deltaTime
        )
        {
            Vector3 force = (target - position) * returnForce;

            pointVelocity += force * deltaTime;

            float dampingFactor = Mathf.Exp(-damping * deltaTime);

            float finalVelocityFactor = Mathf.Lerp(
                dampingFactor,
                1f,
                velocityPreservation
            );

            pointVelocity *= finalVelocityFactor;

            if (maxVelocity > 0f)
                pointVelocity = Vector3.ClampMagnitude(pointVelocity, maxVelocity);

            position += pointVelocity * deltaTime;
        }

        private void ConstrainPoint(
            ref Vector3 point,
            ref Vector3 pointVelocity,
            Vector3 anchor,
            float length,
            Vector3 fallbackDirection
        )
        {
            Vector3 direction = point - anchor;

            if (direction.sqrMagnitude <= 0.000001f)
                direction = fallbackDirection;

            if (direction.sqrMagnitude <= 0.000001f)
                return;

            Vector3 normal = direction.normalized;

            point = anchor + normal * length;

            float outwardVelocity = Vector3.Dot(pointVelocity, normal);

            if (outwardVelocity > 0f)
                pointVelocity -= normal * outwardVelocity;
        }

        private void RotateBoneToPoint(
            Transform bone,
            Vector3 defaultTargetPosition,
            Vector3 wantedTargetPosition
        )
        {
            Vector3 bonePosition = bone.position;

            Vector3 defaultDirection = defaultTargetPosition - bonePosition;
            Vector3 wantedDirection = wantedTargetPosition - bonePosition;

            if (defaultDirection.sqrMagnitude <= 0.000001f)
                return;

            if (wantedDirection.sqrMagnitude <= 0.000001f)
                return;

            defaultDirection.Normalize();
            wantedDirection.Normalize();

            Quaternion offset = Quaternion.FromToRotation(
                defaultDirection,
                wantedDirection
            );

            bone.rotation = offset * bone.rotation;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            if (tail1 == null || tail2 == null || tail3 == null)
                return;

            Color oldColor = Gizmos.color;

            const float gizmoScale = 0.5f;
            const float gizmoAlpha = 0.35f;

            float boneSphereSize = 0.025f * gizmoScale;
            float virtualSphereSize = 0.045f * gizmoScale;

            Gizmos.color = new Color(1f, 1f, 1f, gizmoAlpha);

            Gizmos.DrawLine(tail1.position, tail2.position);
            Gizmos.DrawLine(tail2.position, tail3.position);

            Gizmos.DrawSphere(tail1.position, boneSphereSize);
            Gizmos.DrawSphere(tail2.position, boneSphereSize);
            Gizmos.DrawSphere(tail3.position, boneSphereSize);

            if (Application.isPlaying && initialized)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, gizmoAlpha);

                Gizmos.DrawLine(tail1.position, p2);
                Gizmos.DrawLine(p2, p3);

                Gizmos.DrawSphere(p2, virtualSphereSize);
                Gizmos.DrawSphere(p3, virtualSphereSize);
            }

            if (useConeLimit)
            {
                Gizmos.color = new Color(1f, 0.8f, 0.2f, gizmoAlpha);

                Vector3 tail1BaseDirection = Application.isPlaying && initialized
                    ? initialTail1ConeDirection
                    : tail2.position - tail1.position;

                Vector3 tail2BaseDirection = Application.isPlaying && initialized
                    ? initialTail2ConeDirection
                    : tail3.position - tail2.position;

                DrawConeGizmo(
                    tail1.position,
                    GetConeCenterDirection(tail1BaseDirection, tail1ConeOffset),
                    (length12 > 0f ? length12 : Vector3.Distance(tail1.position, tail2.position)) * gizmoScale,
                    tail1ConeAngle
                );

                DrawConeGizmo(
                    tail2.position,
                    GetConeCenterDirection(tail2BaseDirection, tail2ConeOffset),
                    (length23 > 0f ? length23 : Vector3.Distance(tail2.position, tail3.position)) * gizmoScale,
                    tail2ConeAngle
                );
            }

            Gizmos.color = oldColor;
        }

        private void DrawConeGizmo(Vector3 origin, Vector3 direction, float length, float angle)
        {
            if (direction.sqrMagnitude <= 0.000001f)
                return;

            direction.Normalize();

            Gizmos.DrawLine(origin, origin + direction * length);

            Vector3 right = Vector3.Cross(direction, Vector3.up);

            if (right.sqrMagnitude <= 0.000001f)
                right = Vector3.Cross(direction, Vector3.right);

            right.Normalize();

            Vector3 up = Vector3.Cross(right, direction).normalized;

            const int ringSegments = 24;

            Vector3 previousPoint = Vector3.zero;

            for (int i = 0; i <= ringSegments; i++)
            {
                float t = (float)i / ringSegments;
                float radians = t * Mathf.PI * 2f;

                Vector3 circleDirection =
                    Mathf.Cos(radians) * right +
                    Mathf.Sin(radians) * up;

                Vector3 coneEdgeDirection = Vector3.RotateTowards(
                    direction,
                    circleDirection,
                    angle * Mathf.Deg2Rad,
                    0f
                );

                Vector3 point = origin + coneEdgeDirection.normalized * length;

                if (i > 0)
                    Gizmos.DrawLine(previousPoint, point);

                previousPoint = point;
            }

            DrawConeEdge(origin, direction, right, length, angle);
            DrawConeEdge(origin, direction, -right, length, angle);
            DrawConeEdge(origin, direction, up, length, angle);
            DrawConeEdge(origin, direction, -up, length, angle);
        }

        private void DrawConeEdge(
            Vector3 origin,
            Vector3 direction,
            Vector3 sideDirection,
            float length,
            float angle
        )
        {
            Vector3 edgeDirection = Vector3.RotateTowards(
                direction,
                sideDirection,
                angle * Mathf.Deg2Rad,
                0f
            );

            Gizmos.DrawLine(origin, origin + edgeDirection.normalized * length);
        }
    }
}