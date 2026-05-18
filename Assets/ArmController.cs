using UnityEngine;

public class ArmController : MonoBehaviour
{
    public Transform gripperPoint;
    public Transform dropPoint;
    public Transform pickupArea;
    public Transform horizontalBar;

    public float moveSpeed = 2f;
    public float pickupRadius = 0.4f;
    public float carryHeight = 2f;

    private GameObject carriedBlock;
    private Rigidbody carriedRb;
    private int placedBlockCount;
    private Vector3 currentDropTarget;
    private readonly Vector3[] dropOffsets =
    {
        new Vector3(-0.3f, 0f, -0.2f),
        new Vector3(0f, 0f, -0.2f),
        new Vector3(0.3f, 0f, -0.2f),
        new Vector3(-0.3f, 0f, 0.2f),
        new Vector3(0f, 0f, 0.2f),
        new Vector3(0.3f, 0f, 0.2f),
    };

    private enum State
    {
        FindBlock,
        MoveToBlock,
        PickUp,
        MoveUpAfterPickup,
        MoveToDrop,
        Drop
    }

    private State state = State.FindBlock;
    private Transform targetBlock;

    void Update()
    {
        switch (state)
        {
            case State.FindBlock:
                FindNearestBlock();
                break;

            case State.MoveToBlock:
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

        UpdateHorizontalBarPosition();
    }

    void UpdateHorizontalBarPosition()
    {
        if (horizontalBar == null)
            return;

        Vector3 barPosition = horizontalBar.position;
        barPosition.x = gripperPoint.position.x;
        horizontalBar.position = barPosition;
    }

    void FindNearestBlock()
    {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");

        GameObject nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (GameObject block in blocks)
        {
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

    bool IsInsidePickupArea(Vector3 worldPosition)
    {
        if (pickupArea == null)
        {
            Debug.LogWarning($"{nameof(ArmController)} heeft geen pickupArea ingesteld.");
            return false;
        }

        Vector3 localPosition = pickupArea.InverseTransformPoint(worldPosition);
        return Mathf.Abs(localPosition.x) <= 0.5f && Mathf.Abs(localPosition.z) <= 0.5f;
    }

    bool MoveGripperTo(Vector3 targetPosition)
    {
        Vector3 currentPosition = gripperPoint.position;
        float step = moveSpeed * Time.deltaTime;
        float tolerance = 0.05f;

        if (Mathf.Abs(currentPosition.x - targetPosition.x) > tolerance)
        {
            currentPosition.x = Mathf.MoveTowards(currentPosition.x, targetPosition.x, step);
            gripperPoint.position = currentPosition;
            return false;
        }

        if (Mathf.Abs(currentPosition.z - targetPosition.z) > tolerance)
        {
            currentPosition.z = Mathf.MoveTowards(currentPosition.z, targetPosition.z, step);
            gripperPoint.position = currentPosition;
            return false;
        }

        if (Mathf.Abs(currentPosition.y - targetPosition.y) > tolerance)
        {
            currentPosition.y = Mathf.MoveTowards(currentPosition.y, targetPosition.y, step);
            gripperPoint.position = currentPosition;
            return false;
        }

        gripperPoint.position = targetPosition;
        return true;
    }

    void PickUpBlock()
    {
        if (targetBlock == null || !IsInsidePickupArea(targetBlock.position))
        {
            targetBlock = null;
            state = State.FindBlock;
            return;
        }

        carriedBlock = targetBlock.gameObject;
        carriedRb = carriedBlock.GetComponent<Rigidbody>();

        if (carriedRb != null)
        {
            carriedRb.isKinematic = true;
            carriedRb.useGravity = false;
        }

        carriedBlock.transform.SetParent(gripperPoint);
        carriedBlock.transform.localPosition = Vector3.down * 0.2f;
        currentDropTarget = GetNextDropPosition();

        state = State.MoveUpAfterPickup;
    }

    void DropBlock()
    {
        carriedBlock.transform.SetParent(null);

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

    Vector3 GetNextDropPosition()
    {
        if (dropPoint == null)
        {
            Debug.LogWarning($"{nameof(ArmController)} heeft geen dropPoint ingesteld.");
            return gripperPoint.position;
        }

        int index = placedBlockCount % dropOffsets.Length;
        return dropPoint.TransformPoint(dropOffsets[index]);
    }
}
