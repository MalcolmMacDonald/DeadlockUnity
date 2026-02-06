using System.Collections;
using UnityEngine;

public class UrnController : MonoBehaviour
{
    [SerializeField] private Rigidbody urnRigidbody;

    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private float arcTorque;
    [SerializeField] private float outOfArcDrag;
    [SerializeField] private float wallHitHopHeight;
    [SerializeField] private Quaternion targetRotationOffset;
    [SerializeField] private int torqueCorrectionIterations = 4;
    private bool inArc;

    private void FixedUpdate()
    {
        if (!inArc)
        {
            var velocity = urnRigidbody.linearVelocity;
            var horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            var dragForce = -horizontalVelocity * outOfArcDrag;
            urnRigidbody.AddForce(dragForce, ForceMode.Acceleration);
        }
    }

    private void OnEnable()
    {
        inArc = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (inArc)
        {
            Debug.DrawRay(other.contacts[0].point, other.contacts[0].normal, Color.red, 2f);
            EndArc();
            var hitNormal = other.contacts[0].normal;
            var bounceDirection = Vector3.Reflect(urnRigidbody.linearVelocity, hitNormal);
            //Mathf.Sqrt(2f * maxThrowDistance * Mathf.Abs(Physics.gravity.y))
            bounceDirection.y = Mathf.Sqrt(2f * wallHitHopHeight * Mathf.Abs(Physics.gravity.y));
            urnRigidbody.linearVelocity = bounceDirection;
        }
    }

    public void BeginArc()
    {
        inArc = true;
        trailRenderer.Clear();
        urnRigidbody.constraints = RigidbodyConstraints.None;
        trailRenderer.gameObject.SetActive(true);
        trailRenderer.emitting = true;
        StartCoroutine(HandleArc());
    }

    private void DrawRotation(Quaternion rotation)
    {
        var forward = rotation * Vector3.forward;
        var right = rotation * Vector3.right;
        var up = rotation * Vector3.up;
        Debug.DrawRay(urnRigidbody.position, forward, Color.blue);
        Debug.DrawRay(urnRigidbody.position, right, Color.red);
        Debug.DrawRay(urnRigidbody.position, up, Color.green);
    }

    private IEnumerator HandleArc()
    {
        while (inArc)
        {
            for (var i = 0; i < torqueCorrectionIterations; i++)
            {
                CorrectRotation();
            }


            yield return null;
        }
    }

    private void CorrectRotation()
    {
        var velocity = urnRigidbody.linearVelocity;
        Debug.DrawRay(urnRigidbody.position, velocity, Color.yellow);
        var currentRotation = urnRigidbody.rotation;
        var urnForward = currentRotation * Vector3.forward;


        if (Vector3.Angle(velocity, urnForward) < 0.1f || velocity.magnitude < 0.01f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(velocity, urnForward);
        targetRotation *= targetRotationOffset;
        DrawRotation(targetRotation);

        var rotationDifference = targetRotation * Quaternion.Inverse(currentRotation);


        rotationDifference.ToAngleAxis(out var angleInDegrees, out var rotationAxis);
        if (angleInDegrees > 180f)
        {
            angleInDegrees -= 360f;
        }

        rotationAxis *= angleInDegrees * Mathf.Deg2Rad;
        rotationAxis *= arcTorque;

        // rotationAxis -= urnRigidbody.angularVelocity * Time.deltaTime;

        urnRigidbody.AddTorque(rotationAxis);
    }

    public void EndArc()
    {
        inArc = false;
        StopAllCoroutines();
        //    trailRenderer.gameObject.SetActive(false);
        // 
        trailRenderer.emitting = false;
        urnRigidbody.isKinematic = false;
        urnRigidbody.angularVelocity = Vector3.zero;
        urnRigidbody.rotation = Quaternion.Euler(0f, urnRigidbody.rotation.eulerAngles.y, 0f);
        urnRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }
}