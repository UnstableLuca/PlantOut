using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;
using System.Collections;
using Unity.Mathematics;

[RequireComponent(typeof(CinemachineCamera))]
public class DollyTransitionController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineSplineDolly splineDolly;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] protected CinemachineCamera vcamPause;

    [Header("Ajustes")]
    [SerializeField] private float waitTime = 0.3f;
    [SerializeField] private float travelDuration = 4.0f;

    private CinemachineCamera vcamDolly;

    private void Awake()
    {
        vcamDolly = GetComponent<CinemachineCamera>();

        if (splineDolly == null) splineDolly = GetComponent<CinemachineSplineDolly>();
        if (splineContainer == null) splineContainer = splineDolly.Spline;
    }

    public void StartTransition()
    {
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        UpdateAndSmoothSpline();

        splineDolly.CameraPosition = 0f;

        yield return new WaitForSeconds(waitTime);

        vcamDolly.Priority = 25;

        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            splineDolly.CameraPosition = smoothT;

            yield return null;
        }

        splineDolly.CameraPosition = 1f;

        if (vcamPause != null)
        {
            vcamPause.Priority = 30;
        }

        vcamDolly.Priority = 10;
    }

    private void UpdateAndSmoothSpline()
    {
        if (splineContainer == null || vcamPause == null) return;

        Spline spline = splineContainer.Spline;

        Vector3 localPos = splineContainer.transform.InverseTransformPoint(vcamPause.transform.position);

        Quaternion localRotation = Quaternion.Inverse(splineContainer.transform.rotation) * vcamPause.transform.rotation;

        BezierKnot targetKnot = new BezierKnot((float3)localPos)
        {
            Rotation = (quaternion)localRotation
        };

        float tangentLength = 4.0f;

        if (math.lengthsq(targetKnot.TangentIn) > 0.001f)
        {
            targetKnot.TangentIn = math.normalize(targetKnot.TangentIn) * tangentLength;
        }
        else
        {
            targetKnot.TangentIn = new float3(0, 0, -tangentLength);
        }

        if (math.lengthsq(targetKnot.TangentOut) > 0.001f)
        {
            targetKnot.TangentOut = math.normalize(targetKnot.TangentOut) * tangentLength;
        }
        else
        {
            targetKnot.TangentOut = new float3(0, 0, tangentLength);
        }

        if (spline.Count <= 2)
        {
            spline.Add(targetKnot);
        }
        else
        {
            spline[2] = targetKnot;
        }

        for (int i = 0; i < spline.Count - 1; i++)
        {
            spline.SetTangentMode(i, TangentMode.AutoSmooth);
        }

        spline.SetTangentMode(spline.Count - 1, TangentMode.Continuous);
    }
}
