using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private float mouseSensitivity = 0.5f;

    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private CapsuleCollider playerCollider;

    [SerializeField] private Transform handRoot;
    [SerializeField] private LineRenderer trajectoryLineRenderer;

    [SerializeField] private float initialChargeDuration;
    [SerializeField] private float maxChargeDuration;
    [SerializeField] private float maxThrowDistance;
    [SerializeField] private float throwPredictionDuration;
    [SerializeField] private float throwPredictionTimeStep = 0.1f;
    [SerializeField] private AnimationCurve throwForceCurve;
    [SerializeField] private float throwSpinSpeed = 360f;


    [SerializeField] private Rigidbody urnRigidbody;
    [SerializeField] private Transform urnBackPosition;
    [SerializeField] private Collider urnCollider;
    [SerializeField] private UrnController urnController;
    private float pitch;

    private float yaw;

    private float MaxThrowForce => Mathf.Sqrt(2f * maxThrowDistance * Mathf.Abs(Physics.gravity.y));

    private void Start()
    {
        yaw = playerRigidbody.rotation.eulerAngles.y;
        WebGLInput.stickyCursorLock = false;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        //handle mouse lock
        if (Input.GetMouseButtonDown(0))
        {
            StopAllCoroutines();
            StartCoroutine(HandleThrowingUrn());
        }


        HandleLooking();
        HandleMovement();
    }

    private void HandleLooking()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        playerRigidbody.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private Vector3 GetJumpForce()
    {
        return Vector3.up * Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics.gravity.y));
    }

    private bool CastCollider(Vector3 direction, out RaycastHit hitInfo, float distance)
    {
        var start = playerRigidbody.position + Vector3.up * playerCollider.height / 2f;
        var end = playerRigidbody.position + Vector3.up * playerCollider.height / 2f;
        return Physics.CapsuleCast(start, end, playerCollider.radius, direction, out hitInfo, distance);
    }

    private void HandleMovement()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");


        var inputDirection = new Vector2(horizontal, vertical);
        inputDirection = Vector2.ClampMagnitude(inputDirection, 1f);

        var forward = playerRigidbody.rotation * Vector3.forward * inputDirection.y;
        var right = playerRigidbody.rotation * Vector3.right * inputDirection.x;
        var movement = forward + right;
        movement *= movementSpeed;

        if (CastCollider(movement, out var hitInfo, movement.magnitude * Time.deltaTime * playerCollider.radius))
        {
            Debug.DrawLine(playerRigidbody.position, hitInfo.point, Color.red, 0.1f);
        }

        var verticalVelocity = playerRigidbody.linearVelocity.y;
        if (Input.GetButtonDown("Jump"))
        {
            verticalVelocity = GetJumpForce().y;
        }

        var movementWithVertical = new Vector3(movement.x, verticalVelocity, movement.z);
        playerRigidbody.linearVelocity = movementWithVertical;
    }

    private IEnumerator HandleThrowingUrn()
    {
        urnController.EndArc();
        trajectoryLineRenderer.gameObject.SetActive(true);
        urnRigidbody.interpolation = RigidbodyInterpolation.None;
        urnRigidbody.transform.parent = urnBackPosition;
        urnRigidbody.transform.localPosition = Vector3.zero;
        urnRigidbody.transform.localRotation = Quaternion.identity;
        urnRigidbody.isKinematic = true;
        urnCollider.enabled = false;
        var chargeTime = initialChargeDuration;
        var throwVelocity = Vector3.zero;
        while (Input.GetMouseButton(0))
        {
            chargeTime += Time.deltaTime;
            var chargePercent = Mathf.Clamp01(chargeTime / maxChargeDuration);
            chargePercent = throwForceCurve.Evaluate(chargePercent);
            var throwForce = chargePercent * MaxThrowForce;

            var throwDirection = cameraPivot.rotation * handRoot.localRotation * Vector3.forward;
            throwVelocity = throwDirection * throwForce;


            var throwPoints = GetThrowPoints(handRoot.position, throwVelocity, throwPredictionDuration, throwPredictionTimeStep);
            trajectoryLineRenderer.positionCount = throwPoints.Length;
            trajectoryLineRenderer.SetPositions(throwPoints);
            yield return null;
        }

        var throwBinormal = transform.right;
        var throwTangent = Vector3.Cross(throwBinormal, throwVelocity.normalized);
        Debug.DrawLine(handRoot.position, handRoot.position + throwVelocity.normalized * 2f, Color.green, 2f);
        Debug.DrawLine(handRoot.position, handRoot.position + throwTangent * 2f, Color.blue, 2f);
        Debug.DrawLine(handRoot.position, handRoot.position + throwBinormal * 2f, Color.red, 2f);
        var urnRotation = Quaternion.LookRotation(throwTangent, throwVelocity);
        urnController.BeginArc();
        urnRigidbody.transform.parent = null;
        urnRigidbody.position = handRoot.position;
        urnRigidbody.rotation = urnRotation;
        urnRigidbody.PublishTransform();
        urnRigidbody.isKinematic = false;
        urnRigidbody.linearVelocity = throwVelocity;
        urnRigidbody.angularVelocity = throwVelocity.normalized * throwSpinSpeed;
        urnRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        urnCollider.enabled = true;

        trajectoryLineRenderer.gameObject.SetActive(false);
        trajectoryLineRenderer.positionCount = 0;
    }

    public Vector3[] GetThrowPoints(Vector3 startPosition, Vector3 initialVelocity, float predictionDuration, float timeStep)
    {
        var steps = Mathf.CeilToInt(predictionDuration / timeStep);
        var points = new List<Vector3>();
        var currentPosition = startPosition;
        for (var i = 0; i < steps; i++)
        {
            var t = i * timeStep;
            var point = startPosition + initialVelocity * t + 0.5f * Physics.gravity * t * t;
            var sphereRadius = playerCollider.radius * .75f;
            var direction = point - currentPosition;
            if (Physics.SphereCast(currentPosition, sphereRadius, point - currentPosition, out var hitInfo, (point - currentPosition).magnitude))
            {
                var hitPoint = hitInfo.point;
                var hitPointOnDirection = currentPosition + Vector3.Project(hitPoint - currentPosition, direction);
                points.Add(hitPointOnDirection);
                break;
            }

            points.Add(point);
            currentPosition = point;
        }


        return points.ToArray();
    }
}