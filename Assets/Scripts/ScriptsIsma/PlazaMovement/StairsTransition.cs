using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StairsTransition : MonoBehaviour
{
    [Header("Puntos de Entrada y Salida")]
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;

    [Header("Ajustes")]
    [SerializeField] private float approachSpeed = 6.0f;
    [SerializeField] private float climbSpeed = 10.0f;

    [Header("Salida")]
    [SerializeField] private Transform topExit;
    [SerializeField] private Transform botExit;

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            float distToBottom = Vector3.Distance(other.transform.position, bottomPoint.position);
            float distToTop = Vector3.Distance(other.transform.position, topPoint.position);

            bool isBottom = distToBottom < distToTop;

            Transform startPos = isBottom ? bottomPoint : topPoint;
            Transform endPos = isBottom ? topPoint : bottomPoint;
            Transform exitPos = isBottom ? topExit : botExit;

            StartCoroutine(StairSequenceRoutine(playerMovement, startPos.position, endPos.position, exitPos.position));
        }
    }

    private IEnumerator StairSequenceRoutine(PlayerMovement player, Vector3 start, Vector3 end, Vector3 exit)
    {
        isTransitioning = true;

        player.DisableMovement();

        // FASE 1 Acercar a escalera
        Vector3 initialPos = player.transform.position;
        float approachDistance = Vector3.Distance(initialPos, start);
        float approachJourney = 0f;

        while (approachJourney < approachDistance)
        {
            approachJourney += approachSpeed * Time.deltaTime;
            float percent = Mathf.Clamp01(approachJourney / approachDistance);

            player.SetExternalPosition(Vector3.Lerp(initialPos, start, percent));
            yield return null;
        }

        // FASE 2 Subir escalera
        float climbDistance = Vector3.Distance(start, end);
        float climbJourney = 0f;

        while (climbJourney < climbDistance)
        {
            climbJourney += climbSpeed * Time.deltaTime;
            float percent = Mathf.Clamp01(climbJourney / climbDistance);

            player.SetExternalPosition(Vector3.Lerp(start, end, percent));
            yield return null;
        }

        // FASE 3 Alejarse de la escalera
        float exitDistance = Vector3.Distance(end, exit);
        float exitJourney = 0f;

        while (exitJourney < exitDistance)
        {
            exitJourney += approachSpeed * Time.deltaTime;
            float percent = Mathf.Clamp01(exitJourney / exitDistance);

            player.SetExternalPosition(Vector3.Lerp(end, exit, percent));
            yield return null;
        }

        player.SetExternalPosition(exit);
        player.EnableMovement();

        yield return new WaitForSeconds(0.2f);
        isTransitioning = false;
    }
}
