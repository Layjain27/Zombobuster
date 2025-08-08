// Filename: Missile.cs
using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private GameObject _explosionPrefab;

    [Header("MOVEMENT")]
    [SerializeField] private float _speed = 15;
    [SerializeField] private float _rotateSpeed = 95;

    [Header("PREDICTION")]
    [SerializeField] private float _maxDistancePredict = 100;
    [SerializeField] private float _minDistancePredict = 5;
    [SerializeField] private float _maxTimePrediction = 5;

    [Header("DEVIATION")]
    [SerializeField] private float _deviationAmount = 50;
    [SerializeField] private float _deviationSpeed = 2;

    [Header("COMBAT")]
    [Tooltip("The amount of damage the missile deals on impact.")]
    [SerializeField] private float _damage = 50f;
    [Tooltip("The radius in which the missile will search for a new target if current is lost.")]
    [SerializeField] private float _retargetRadius = 25f;
    [Tooltip("Layer for enemies.")]
    [SerializeField] private LayerMask _enemyLayer;

    private Transform _target;
    private Vector3 _standardPrediction, _deviatedPrediction;
    private Rigidbody _targetRb;
    private CharacterController _targetCc;

    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
        if (_target != null)
        {
            _targetRb = _target.GetComponent<Rigidbody>();
            _targetCc = _target.GetComponent<CharacterController>();
            Debug.Log($"Missile assigned to: {_target.name}");
        }
    }

    private void FixedUpdate()
    {
        if (_target == null)
        {
            TryFindNewTarget();

            if (_target == null)
            {
                _rb.linearVelocity = transform.forward * _speed;
                Destroy(gameObject, _maxTimePrediction);
                return;
            }
        }

        _rb.linearVelocity = transform.forward * _speed;

        float leadTimePercentage = Mathf.InverseLerp(_minDistancePredict, _maxDistancePredict,
                                                     Vector3.Distance(transform.position, _target.position));

        PredictMovement(leadTimePercentage);
        AddDeviation(leadTimePercentage);
        RotateRocket();
    }

    private void PredictMovement(float leadTimePercentage)
    {
        float predictionTime = Mathf.Lerp(0, _maxTimePrediction, leadTimePercentage);
        Vector3 targetVelocity = _targetRb != null ? _targetRb.linearVelocity :
                                  (_targetCc != null ? _targetCc.velocity : Vector3.zero);

        _standardPrediction = _target.position + targetVelocity * predictionTime;
    }

    private void AddDeviation(float leadTimePercentage)
    {
        Vector3 deviation = new Vector3(Mathf.Cos(Time.time * _deviationSpeed),
                                        Mathf.Sin(Time.time * _deviationSpeed), 0);
        Vector3 predictionOffset = transform.TransformDirection(deviation) *
                                   _deviationAmount * leadTimePercentage;
        _deviatedPrediction = _standardPrediction + predictionOffset;
    }

    private void RotateRocket()
    {
        Vector3 heading = _deviatedPrediction - transform.position;
        Quaternion rotation = Quaternion.LookRotation(heading);
        _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, rotation, _rotateSpeed * Time.deltaTime));
    }

    private void TryFindNewTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _retargetRadius, _enemyLayer);
        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var col in colliders)
        {
            if (col.transform == null) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = col.transform;
            }
        }

        if (bestTarget != null)
        {
            SetTarget(bestTarget);
            Debug.Log("Missile re-targeted to: " + bestTarget.name);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_explosionPrefab) Instantiate(_explosionPrefab, transform.position, Quaternion.identity);

        if (collision.transform.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_damage);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (_target == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, _standardPrediction);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(_standardPrediction, _deviatedPrediction);
    }
}
