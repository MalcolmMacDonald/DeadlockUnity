using UnityEngine;

public partial class DoorDistanceMapper
{
    private void DrawValidatedDoor(EdgePoint doorSeedPoint)
    {
        if (TestDoorPlacement(doorSeedPoint, out var doorPosition))
        {
            DrawDoor(doorPosition, Color.blue);
        }
        else
        {
            DrawDoor(doorPosition, Color.red);
        }
    }

    private void DrawDoor(DoorPosition doorPosition, Color color)
    {
        var previousMatrix = Gizmos.matrix;
        Gizmos.color = color;
        Gizmos.matrix = Matrix4x4.TRS(doorPosition.center, doorPosition.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(doorWidth, doorHeight, doorDepth));
        Gizmos.matrix = previousMatrix;
    }


    private bool TestDoorPlacement(EdgePoint doorPoint, out DoorPosition doorPosition)
    {
        var rayDirection = doorPoint.wallDirection * Vector3.forward;

        bool RayTest(Vector3 position, out RaycastHit hitInfo)
        {
            return Physics.Raycast(position, rayDirection, out hitInfo, testDepth);
        }

        var corners = RaycastLocations(doorPoint);
        var isValid = true;
        var minDepth = testDepth;
        var averageNormal = Vector3.zero;
        var averageHitPoint = Vector3.zero;
        for (var i = 0; i < corners.Length; i++)
        {
            var corner = corners[i];
            if (!RayTest(corner, out var hitInfo))
            {
                isValid = false;
                //Debug.DrawRay(corner, rayDirection * testDepth, Color.red);
                //Debug.DrawRay(hitInfo.point, hitInfo.normal * 0.1f, Color.red);
            }
            else
            {
                averageHitPoint += hitInfo.point;
                averageNormal += hitInfo.normal;
                minDepth = Mathf.Min(minDepth, hitInfo.distance);
                //Debug.DrawRay(corner, rayDirection * testDepth, Color.green);
                //Debug.DrawRay(hitInfo.point, hitInfo.normal * 0.1f, Color.blue);
            }
        }

        averageHitPoint /= corners.Length;

        if (!isValid)
        {
            averageNormal = rayDirection;
            averageHitPoint = doorPoint.position + Vector3.up * (testHeight + doorHeight / 2f) +
                              rayDirection * testZOffset;
        }

        averageNormal.y = 0;
        averageNormal.Normalize();

        doorPosition = new DoorPosition
        {
            center = averageHitPoint + averageNormal * (doorWallOffset + doorDepth / 2f),
            rotation = Quaternion.LookRotation(-averageNormal, Vector3.up)
        };
        if (DoorIntersectsGeometry(doorPosition))
        {
            isValid = false;
        }

        return isValid;
    }

    private bool DoorIntersectsGeometry(DoorPosition doorPosition)
    {
        var halfExtents = new Vector3(doorWidth / 2f, doorHeight / 2f, doorDepth / 2f);
        return Physics.CheckBox(doorPosition.center, halfExtents, doorPosition.rotation);
    }

    private Vector3[] RaycastLocations(EdgePoint doorPoint)
    {
        var position = doorPoint.position;
        var rotation = doorPoint.wallDirection;


        var halfExtents = new Vector3(doorWidth / 2f, doorHeight / 2f, 0.1f);
        var center = position + Vector3.up * (testHeight + doorHeight / 2f) +
                     rotation * (Vector3.forward * testZOffset);
        var corners = new[]
        {
            center + rotation * new Vector3(-halfExtents.x, -halfExtents.y, 0),
            center + rotation * new Vector3(-halfExtents.x, 0, 0),
            center + rotation * new Vector3(halfExtents.x, -halfExtents.y, 0),
            center + rotation * new Vector3(halfExtents.x, 0, 0),
            center + rotation * new Vector3(halfExtents.x, halfExtents.y, 0),
            center + rotation * new Vector3(-halfExtents.x, halfExtents.y, 0),
            center
        };


        return corners;
    }
}