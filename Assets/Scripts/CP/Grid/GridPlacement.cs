using System.Collections.Generic;
using UnityEngine;

public class GridPlacement : MonoBehaviour
{
    [SerializeField] private List<GameObject> buildingList;
    [SerializeField] private Grid grid;
    [SerializeField] private GridInput gridInput;
    [SerializeField] private GameObject cellIndicator;

    private int selectedBuildingIndex = -1;
    private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

    private void Start()
    {
        StopPlacement();
    }

    private void OnEnable()
    {
        gridInput.OnClicked += PlaceStructure;
        gridInput.OnExit += StopPlacement;
    }

    private void OnDisable()
    {
        gridInput.OnClicked -= PlaceStructure;
        gridInput.OnExit -= StopPlacement;
    }

    private void Update()
    {
        if (selectedBuildingIndex < 0) return;

        Vector3 selectedPosition = gridInput.GetSelectedPosition();
        Vector3Int cellPosition = grid.WorldToCell(selectedPosition);

        if (cellIndicator != null)
        {
            cellIndicator.transform.position = grid.CellToWorld(cellPosition);
        }
    }

    public void StartPlacement(int buildingIndex)
    {
        if (buildingIndex < 0 || buildingIndex >= buildingList.Count) return;

        selectedBuildingIndex = buildingIndex;
        if (cellIndicator != null) cellIndicator.SetActive(true);
    }

    public void StopPlacement()
    {
        selectedBuildingIndex = -1;
        if (cellIndicator != null) cellIndicator.SetActive(false);
    }

    private void PlaceStructure()
    {
        if (selectedBuildingIndex < 0 || gridInput.IsPointerOverUI()) return;

        Vector3 selectedPosition = gridInput.GetSelectedPosition();
        Vector3Int cellPosition = grid.WorldToCell(selectedPosition);

        if (occupiedCells.Contains(cellPosition))
        {
            Debug.Log("Esta celda ya está ocupada.");
            return;
        }

        Vector3 spawnPosition = grid.CellToWorld(cellPosition);
        Instantiate(buildingList[selectedBuildingIndex], spawnPosition, Quaternion.identity);

        occupiedCells.Add(cellPosition);
    }
}