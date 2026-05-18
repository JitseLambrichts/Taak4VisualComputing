using UnityEngine;

public class ArmController : MonoBehaviour
{
    // De objecten vanuit Unity
    public Transform gripperPoint;
    public Transform dropPoint;
    public Transform pickupArea;
    public Transform horizontalBar;
    private Transform targetBlock;

    // Variabelen voor de beweging
    public float moveSpeed = 2f;
    public float carryHeight = 2f;
    public float startDelay = 9f;

    // Variabelen voor ophalen / loslaten van de blokken
    private GameObject carriedBlock;
    private Rigidbody carriedRb;
    private int placedBlockCount;
    private Vector3 currentDropTarget;
    private float startTime;
    private readonly Vector3[] dropOffsets =
    {
        new Vector3(-0.3f, 0f, -0.2f),
        new Vector3(0f, 0f, -0.2f),
        new Vector3(0.3f, 0f, -0.2f),
        new Vector3(-0.3f, 0f, 0.2f),
        new Vector3(0f, 0f, 0.2f),
        new Vector3(0.3f, 0f, 0.2f),
    };

    // Verschillende states
    private enum State
    {
        FindBlock,
        MoveToBlock,
        PickUp,
        MoveUpAfterPickup,
        MoveToDrop,
        Drop
    }

    // Starting state definiëren
    private State state = State.FindBlock;

    // Start-process om de timer te starten (wachten tot de blokjes allemaal gespawnt zijn)
    void Start()
    {
        startTime = Time.time + startDelay;
    }

    void Update()
    {
        // Timer voordat deze mag beginnen (niet beginnen als er nog blokjes aan het droppen zijn)
        if (Time.time < startTime)
        {
            return;
        }

        // Switchen tussen de verschillende states
        switch (state)
        {
            case State.FindBlock:
                FindNearestBlock();
                break;

            case State.MoveToBlock:
                // Als de gripper boven het blokje hangt, dan pas overgaan naar de PickUp state
                if (MoveGripperTo(targetBlock.position + Vector3.up * 0.4f))
                    state = State.PickUp;
                break;

            case State.PickUp:
                PickUpBlock();
                break;

            case State.MoveUpAfterPickup:
                Vector3 carryPosition = gripperPoint.position;
                carryPosition.y = carryHeight;

                if (MoveGripperTo(carryPosition))
                    state = State.MoveToDrop;
                break;

            case State.MoveToDrop:
                Vector3 dropPosition = currentDropTarget;
                dropPosition.y = carryHeight;

                if (MoveGripperTo(dropPosition))
                    state = State.Drop;
                break;

            case State.Drop:
                DropBlock();
                break;
        }

        // Constant horizontale bar mee updaten
        UpdateHorizontalBarPosition();
    }

    // Verplaatsen van de horizontale bar zodat deze mee beweegt met de gripper
    void UpdateHorizontalBarPosition()
    {
        if (horizontalBar == null)
            return;

        Vector3 barPosition = horizontalBar.position;
        barPosition.x = gripperPoint.position.x;
        horizontalBar.position = barPosition;
    }

    // Zoeken van het dichtstbijzijnde blok voor pickup
    void FindNearestBlock()
    {
        // Zoeken op items met de tag "Block"
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");

        GameObject nearest = null;
        float nearestDistance = float.PositiveInfinity;

        // Dichtstbijzijnde blokje zoeken
        foreach (GameObject block in blocks)
        {
            // Controleren of deze wel enkel binnen de pickup area liggen 
            if (!IsInsidePickupArea(block.transform.position))
                continue;

            float distance = Vector3.Distance(gripperPoint.position, block.transform.position);

            if (distance < nearestDistance)
            {
                nearest = block;
                nearestDistance = distance;
            }
        }

        if (nearest == null)
            return;

        targetBlock = nearest.transform;
        state = State.MoveToBlock;
    }

    // Helper functie om te controleren of een blokje in de pickup zone ligt (anders direct terug blokje opnemen na dropoff)
    bool IsInsidePickupArea(Vector3 worldPosition)
    {
        if (pickupArea == null)
        {
            Debug.LogWarning($"{nameof(ArmController)} heeft geen pickupArea ingesteld.");
            return false;
        }

        // Grenzen berekenen zodat deze ook rekening houdt met rotatie (Feedback AI)
        Vector3 localPosition = pickupArea.InverseTransformPoint(worldPosition);
        return Mathf.Abs(localPosition.x) <= 0.5f && Mathf.Abs(localPosition.z) <= 0.5f;
    }

    // Gripper bewegen naar de juiste coordinaten
    bool MoveGripperTo(Vector3 targetPosition)
    {
        Vector3 currentPosition = gripperPoint.position;
        float step = moveSpeed * Time.deltaTime;
        float tolerance = 0.05f;

        // Verplaatsen in de x-richting
        if (Mathf.Abs(currentPosition.x - targetPosition.x) > tolerance)
        {
            currentPosition.x = Mathf.MoveTowards(currentPosition.x, targetPosition.x, step);
            gripperPoint.position = currentPosition;
            return false;
        }

        // Verplaatsen in de z-richting
        if (Mathf.Abs(currentPosition.z - targetPosition.z) > tolerance)
        {
            currentPosition.z = Mathf.MoveTowards(currentPosition.z, targetPosition.z, step);
            gripperPoint.position = currentPosition;
            return false;
        }

        // Verplaatsen in de y-richting
        if (Mathf.Abs(currentPosition.y - targetPosition.y) > tolerance)
        {
            currentPosition.y = Mathf.MoveTowards(currentPosition.y, targetPosition.y, step);
            gripperPoint.position = currentPosition;
            return false;
        }

        gripperPoint.position = targetPosition;
        return true;
    }

    // Blokje opnemen
    void PickUpBlock()
    {
        // Als er geen blokje gevonden is wat binnen de pickup area ligt, dan opnieuw blokje zoeken
        if (targetBlock == null || !IsInsidePickupArea(targetBlock.position))
        {
            targetBlock = null;
            state = State.FindBlock;
            return;
        }

        carriedBlock = targetBlock.gameObject;
        carriedRb = carriedBlock.GetComponent<Rigidbody>();

        // Als het blokje gegrepen wordt, moet deze mee bewegen en mag deze niet naar onder vallen
        if (carriedRb != null)
        {
            carriedRb.isKinematic = true;
            carriedRb.useGravity = false;
        }

        // Meebewegen met parent (gripper)
        carriedBlock.transform.SetParent(gripperPoint);
        carriedBlock.transform.localPosition = Vector3.down * 0.2f;
        currentDropTarget = GetNextDropPosition();

        state = State.MoveUpAfterPickup;
    }

    // Blokje laten vallen
    void DropBlock()
    {
        // Loskoppelen van parent
        carriedBlock.transform.SetParent(null);

        // Eigenschappen van blokje terugzetten
        if (carriedRb != null)
        {
            carriedRb.isKinematic = false;
            carriedRb.useGravity = true;
        }

        carriedBlock = null;
        carriedRb = null;
        targetBlock = null;
        placedBlockCount++;

        state = State.FindBlock;
    }

    // Dropoff voor volgende blokje berekenen
    Vector3 GetNextDropPosition()
    {
        if (dropPoint == null)
        {
            Debug.LogWarning($"{nameof(ArmController)} heeft geen dropPoint ingesteld.");
            return gripperPoint.position;
        }

        // Berekenen van locatie op basis van het hoeveelste blokje wordt geplaatst
        int index = placedBlockCount % dropOffsets.Length;
        return dropPoint.TransformPoint(dropOffsets[index]);
    }
}
