using System.Collections;
using UnityEngine;

public class StairsTransition : MonoBehaviour
{
    [Header("Puntos de Entrada y Salida")]
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;

    [Header("Ajustes")]
    [SerializeField] private float approachSpeed = 6.0f;
    [SerializeField] private float climbSpeed = 10.0f;

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            float distToBottom = Vector3.Distance(other.transform.position, bottomPoint.position);
            float distToTop = Vector3.Distance(other.transform.position, topPoint.position);

            Transform startPos = (distToBottom < distToTop) ? bottomPoint : topPoint;
            Transform endPos = (distToBottom < distToTop) ? topPoint : bottomPoint;

            StartCoroutine(StairSequenceRoutine(playerMovement, startPos.position, endPos.position));
        }
    }

    private IEnumerator StairSequenceRoutine(PlayerMovement player, Vector3 start, Vector3 end)
    {
        isTransitioning = true;

        player.DisableMovement();

        Vector3 initialPlayerPos = player.transform.position;
        float approachDistance = Vector3.Distance(initialPlayerPos, start);
        float approachJourney = 0.0f;

        while(approachJourney < approachDistance)
        {
            approachJourney += approachSpeed * Time.deltaTime;
            float percent = Mathf.Clamp01(approachJourney / approachDistance);

            Vector3 nextPos = Vector3.Lerp(initialPlayerPos, start, percent);
            player.SetExternalPosition(nextPos);

            yield return null;
        }

        float totalDistance = Vector3.Distance(start, end);
        float journey = 0f;

        while (journey < totalDistance)
        {
            journey += climbSpeed * Time.deltaTime;
            float percent = Mathf.Clamp01(journey / totalDistance);

            player.transform.position = Vector3.Lerp(start, end, percent);
            yield return null;
        }

        player.SetExternalPosition(end);

        player.EnableMovement();

        isTransitioning = false;
    }
}
