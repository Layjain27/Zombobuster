// Filename: TowerDefenseSystem.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TowerDefenseSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The missile prefab to be fired.")]
    [SerializeField] private GameObject _missilePrefab;
    [Tooltip("The point(s) where missiles will be spawned.")]
    [SerializeField] private Transform[] _firePoints;

    [Header("Targeting Settings")]
    [Tooltip("The maximum range to detect enemies.")]
    [SerializeField] private float _detectionRadius = 20f;
    [Tooltip("How fast the tower fires (launches per second).")]
    [SerializeField] private float _fireRate = 1f;
    [Tooltip("The layer(s) that enemies are on.")]
    [SerializeField] private LayerMask _enemyLayer;
    [Tooltip("The maximum number of enemies to target at once.")]
    [SerializeField] private int _maxTargets = 3;

    [Header("Firing Settings")]
    [Tooltip("How many missiles to fire per enemy in one volley.")]
    [SerializeField] private int _missilesPerEnemy = 2;
    [Tooltip("Delay between missiles in a salvo (seconds).")]
    [SerializeField] private float _salvoDelay = 0.1f;

    private List<Transform> _targets = new List<Transform>();
    private float _nextFireTime;

    private void Update()
    {
        if (Time.time >= _nextFireTime)
        {
            FindTargets();

            if (_targets.Count > 0)
            {
                StartCoroutine(FireVolley());
                _nextFireTime = Time.time + 1f / _fireRate;
            }
        }
    }

    private void FindTargets()
    {
        _targets.Clear();

        Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRadius, _enemyLayer);

        var validTargets = new List<Transform>();
        foreach (Collider col in colliders)
        {
            Debug.Log("Found: " + col.name);

            if (col.TryGetComponent<Target>(out _))
            {
                validTargets.Add(col.transform);
                Debug.Log("Added as valid target: " + col.name);
            }
        }

        _targets = validTargets.OrderBy(t => Vector3.Distance(transform.position, t.position))
                               .Take(_maxTargets)
                               .ToList();

        Debug.Log("Total targets selected: " + _targets.Count);
    }


    private System.Collections.IEnumerator FireVolley()
    {
        if (_firePoints.Length == 0)
        {
            Debug.LogError("No fire points assigned! Please assign at least one in the Inspector.");
            yield break;
        }

        for (int m = 0; m < _missilesPerEnemy; m++)
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                Transform target = _targets[i];
                Transform firePoint = _firePoints[(i + m) % _firePoints.Length];

                GameObject missileGO = Instantiate(_missilePrefab, firePoint.position, firePoint.rotation);
                Missile missile = missileGO.GetComponent<Missile>();

                if (missile != null)
                {
                    missile.SetTarget(target);
                }
            }
            yield return new WaitForSeconds(_salvoDelay);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}
