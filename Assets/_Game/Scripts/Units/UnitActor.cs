using System;
using UnityEngine;

namespace TuanziArena
{
    public enum UnitTeam { Player, Enemy }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class UnitActor : MonoBehaviour
    {
        [SerializeField] private UnitData data;
        [SerializeField] private UnitTeam team;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SpriteRenderer selectionIndicator;

        private Rigidbody2D body;
        [SerializeField, Tooltip("Runtime health. Reset from UnitData when a battle starts.")]
        private int currentHealth;
        private bool healthInitialized;
        private int pendingRespawnHealth = -1;
        private Color livingColor;
        private Vector2 spawnPosition;
        private bool spawnRecorded;
        public event Action<UnitActor> StatsChanged;
        public event Action<UnitActor> Defeated;
        public event Action<UnitActor> Respawned;
        public UnitData Data => data;
        public SpriteRenderer BodyVisual => visual;
        public Vector2 LastHitDirection { get; private set; } = Vector2.up;
        public UnitTeam Team => team;
        public Rigidbody2D Body => body != null ? body : body = GetComponent<Rigidbody2D>();
        public int MaxHealth => data != null ? Mathf.Max(1, data.maxHealth) : 0;
        public int AttackPower => data != null ? Mathf.Max(0, data.attackPower) : 0;
        public int CurrentHealth { get { EnsureHealth(); return currentHealth; } }
        public bool IsAlive => data != null && CurrentHealth > 0;
        public bool CanLaunch => isActiveAndEnabled && Body.simulated && IsAlive;
        public Vector2 SpawnPosition { get { RecordSpawn(); return spawnPosition; } }

        private void Awake()
        {
            ApplyConfiguration();
            RecordSpawn();
            EnsureHealth();
            SetSelected(false);
        }

        public void Configure(UnitData configuration, UnitTeam unitTeam,
            SpriteRenderer bodyVisual, SpriteRenderer indicator)
        {
            data = configuration;
            team = unitTeam;
            visual = bodyVisual;
            selectionIndicator = indicator;
            ApplyConfiguration();
            RecordSpawn();
            pendingRespawnHealth = -1;
            ResetHealth();
            SetSelected(false);
        }

        private void EnsureHealth()
        {
            if (healthInitialized) return;
            currentHealth = MaxHealth;
            healthInitialized = true;
        }

        private void RecordSpawn()
        {
            if (spawnRecorded) return;
            spawnPosition = transform.position;
            spawnRecorded = true;
        }

        public void RespawnAtSpawn() => RespawnAt(SpawnPosition);

        // A temporary safe placement must never change the recorded initial spawn.
        public void RespawnAt(Vector2 position)
        {
            RecordSpawn();
            Body.simulated = false;
            StopMotion();
            RigidbodyInterpolation2D interpolation = Body.interpolation;
            Body.interpolation = RigidbodyInterpolation2D.None;
            Body.position = position;
            Body.rotation = 0f;
            // A disabled body's transform may still hold its old interpolated pose. Synchronize
            // both while simulation is off, so re-enabling cannot overwrite the spawn position.
            transform.SetPositionAndRotation(new Vector3(position.x, position.y, transform.position.z), Quaternion.identity);
            SetSelected(false);
            ResetHealth();
            Body.position = position;
            StopMotion();
            Body.interpolation = interpolation;
            Respawned?.Invoke(this);
        }

        public void ResetHealth()
        {
            // A self-inflicted hazard death restores the health the unit had before the fatal hit,
            // so suicide cannot be used as a free full heal.
            currentHealth = pendingRespawnHealth >= 0
                ? Mathf.Clamp(pendingRespawnHealth, 1, Mathf.Max(1, MaxHealth))
                : MaxHealth;
            pendingRespawnHealth = -1;
            healthInitialized = true;
            Body.simulated = true;
            GetComponent<CircleCollider2D>().enabled = true;
            if (visual != null) visual.color = livingColor;
            StatsChanged?.Invoke(this);
        }

        // Only used by self-inflicted hazard damage: the next respawn restores this value.
        public void SetRespawnHealth(int health) =>
            pendingRespawnHealth = Mathf.Clamp(health, 1, Mathf.Max(1, MaxHealth));

        public void ClearRespawnHealth() => pendingRespawnHealth = -1;

        public void TakeDamage(int amount) => TakeDamage(amount, Vector2.up);

        public void TakeDamage(int amount, Vector2 hitDirection)
        {
            if (amount <= 0 || !IsAlive) return;
            LastHitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector2.up;
            currentHealth = Mathf.Max(0, currentHealth - amount);
            if (currentHealth == 0)
            {
                StopMotion();
                Body.simulated = false;
                GetComponent<CircleCollider2D>().enabled = false;
                SetSelected(false);
            }
            StatsChanged?.Invoke(this);
            if (currentHealth == 0) Defeated?.Invoke(this);
        }

        private void ApplyConfiguration()
        {
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 0f;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (data == null) return;

            Body.mass = Mathf.Max(0.01f, data.mass);
#if UNITY_6000_0_OR_NEWER
            Body.linearDamping = Mathf.Max(0.01f, data.linearDamping);
#else
            Body.drag = Mathf.Max(0.01f, data.linearDamping);
#endif
            GetComponent<CircleCollider2D>().sharedMaterial = data.physicsMaterial;
            if (visual == null) return;
            visual.sprite = data.sprite;
            livingColor = team == UnitTeam.Player ? data.playerColor : data.enemyColor;
            visual.color = livingColor;
        }

        public bool TryLaunch(Vector2 impulse)
        {
            if (!CanLaunch || impulse.sqrMagnitude < 0.0001f) return false;
            Body.AddForce(impulse * data.launchMultiplier, ForceMode2D.Impulse);
            return true;
        }

        // Scene hazards drive the body directly: they overwrite the velocity instead of queueing a force,
        // so the result is exact and can be followed by a speed cap in the same call.
        public void SetVelocity(Vector2 velocity)
        {
            if (!Body.simulated) return;
#if UNITY_6000_0_OR_NEWER
            Body.linearVelocity = velocity;
#else
            Body.velocity = velocity;
#endif
        }

        public Vector2 Velocity
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return Body.linearVelocity;
#else
                return Body.velocity;
#endif
            }
        }

        public float SpeedSquared
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return Body.linearVelocity.sqrMagnitude;
#else
                return Body.velocity.sqrMagnitude;
#endif
            }
        }

        public void StopMotion()
        {
#if UNITY_6000_0_OR_NEWER
            Body.linearVelocity = Vector2.zero;
#else
            Body.velocity = Vector2.zero;
#endif
            Body.angularVelocity = 0f;
        }

        public void SetSelected(bool selected)
        {
            if (selectionIndicator != null) selectionIndicator.enabled = selected;
        }

        private void OnDisable() => SetSelected(false);
    }
}
