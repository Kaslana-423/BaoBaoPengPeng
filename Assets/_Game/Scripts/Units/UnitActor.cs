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
        private Color livingColor;
        public event Action<UnitActor> StatsChanged;
        public event Action<UnitActor> Defeated;
        public UnitData Data => data;
        public UnitTeam Team => team;
        public Rigidbody2D Body => body != null ? body : body = GetComponent<Rigidbody2D>();
        public int MaxHealth => data != null ? Mathf.Max(1, data.maxHealth) : 0;
        public int AttackPower => data != null ? Mathf.Max(0, data.attackPower) : 0;
        public int CurrentHealth { get { EnsureHealth(); return currentHealth; } }
        public bool IsAlive => data != null && CurrentHealth > 0;
        public bool CanLaunch => isActiveAndEnabled && Body.simulated && IsAlive;

        private void Awake()
        {
            ApplyConfiguration();
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
            ResetHealth();
            SetSelected(false);
        }

        private void EnsureHealth()
        {
            if (healthInitialized) return;
            currentHealth = MaxHealth;
            healthInitialized = true;
        }

        public void ResetHealth()
        {
            currentHealth = MaxHealth;
            healthInitialized = true;
            Body.simulated = true;
            GetComponent<CircleCollider2D>().enabled = true;
            if (visual != null) visual.color = livingColor;
            StatsChanged?.Invoke(this);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            currentHealth = Mathf.Max(0, currentHealth - amount);
            if (currentHealth == 0)
            {
                StopMotion();
                Body.simulated = false;
                GetComponent<CircleCollider2D>().enabled = false;
                SetSelected(false);
                if (visual != null) visual.color = new Color(0.3f, 0.34f, 0.38f, 0.55f);
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
