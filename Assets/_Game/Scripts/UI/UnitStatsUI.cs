using UnityEngine;
using UnityEngine.UI;

namespace TuanziArena
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitActor))]
    public sealed class UnitStatsUI : MonoBehaviour
    {
        [SerializeField] private UnitActor owner;
        [SerializeField] private Canvas statsCanvas;
        [SerializeField] private Text attackText;
        [SerializeField] private Text healthText;
        [SerializeField] private Vector2 localOffset = new Vector2(0f, -0.65f);
        [SerializeField] private ActionRingGraphic actionRing;
        private UnitActor subscribedOwner;
        private TurnManager turns;
        private TurnManager subscribedTurns;
        private GameFlowController flow;
        private GameFlowController subscribedFlow;

        public Text AttackText => attackText;
        public Text HealthText => healthText;
        public bool IsActionIndicatorVisible => actionRing != null && actionRing.gameObject.activeSelf;
        public ActionRingGraphic ActionRing => actionRing;

        private void Awake() => BuildView();
        private void OnEnable()
        {
            BuildView();
            Bind();
            BindTurns(turns);
            BindFlowEvents();
        }

        public void BuildView()
        {
            if (owner == null) owner = GetComponent<UnitActor>();
            if (statsCanvas == null)
            {
                GameObject root = new GameObject("UnitStatsUI", typeof(RectTransform), typeof(Canvas));
                root.transform.SetParent(transform, false);
                root.transform.localPosition = localOffset;
                root.transform.localScale = Vector3.one * 0.01f;
                statsCanvas = root.GetComponent<Canvas>();
                statsCanvas.renderMode = RenderMode.WorldSpace;
                statsCanvas.sortingOrder = 40;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(156f, 36f);
                // These badges have no GraphicRaycaster and cannot capture aiming input.
                attackText = Badge("Attack", root.transform, -40f, 70f,
                    StatIconGraphic.Shape.Attack, new Color(1f, 0.78f, 0.12f));
                healthText = Badge("Health", root.transform, 37f, 78f,
                    StatIconGraphic.Shape.Heart, new Color(1f, 0.28f, 0.4f));
            }
            if (actionRing == null)
            {
                Transform existing = statsCanvas.transform.Find("UnusedActionRing");
                if (existing != null) actionRing = existing.GetComponent<ActionRingGraphic>();
                if (actionRing == null)
                {
                    RectTransform ring = Rect("UnusedActionRing", statsCanvas.transform, new Vector2(0f, 47f), new Vector2(196f, 196f));
                    ring.SetAsFirstSibling();
                    actionRing = ring.gameObject.AddComponent<ActionRingGraphic>();
                    actionRing.color = new Color(1f, 0.83f, 0.28f, 0.85f);
                    actionRing.raycastTarget = false;
                }
            }
            Refresh(owner);
        }

        public void BindTurns(TurnManager manager)
        {
            turns = manager;
            if (actionRing == null) BuildView();
            UnbindTurns();
            if (manager != null && isActiveAndEnabled)
            {
                subscribedTurns = manager;
                subscribedTurns.EligibilityChanged += RefreshActionIndicator;
            }
            RefreshActionIndicator();
        }

        public void BindFlow(GameFlowController controller)s
        {
            flow = controller;
            BindTurns(controller != null ? controller.Turns : null);
            BindFlowEvents();
        }

        private void BindFlowEvents()
        {
            UnbindFlow();
            if (flow != null && isActiveAndEnabled)
            {
                subscribedFlow = flow;
                subscribedFlow.StateChanged += OnFlowStateChanged;
            }
            RefreshActionIndicator();
        }

        private void OnFlowStateChanged(BattleState state) => RefreshActionIndicator();

        private void RefreshActionIndicator()
        {
            if (actionRing != null)
                actionRing.gameObject.SetActive(owner != null && owner.Team == UnitTeam.Player &&
                    flow != null && flow.State == BattleState.PlayerSelect && turns != null && !turns.HasActed(owner));
        }

        private void Bind()
        {
            if (subscribedOwner == owner) return;
            Unbind();
            subscribedOwner = owner;
            if (subscribedOwner != null) subscribedOwner.StatsChanged += Refresh;
            Refresh(owner);
        }

        private void Refresh(UnitActor unit)
        {
            if (unit == null) return;
            if (attackText != null) attackText.text = unit.AttackPower.ToString();
            if (healthText != null)
            {
                healthText.text = unit.CurrentHealth.ToString();
                healthText.color = !unit.IsAlive ? new Color(1f, 0.38f, 0.45f)
                    : unit.CurrentHealth <= unit.MaxHealth * 0.3f ? new Color(1f, 0.77f, 0.35f) : Color.white;
            }
            RefreshActionIndicator();
        }

        private static Text Badge(string name, Transform parent, float x, float width,
            StatIconGraphic.Shape icon, Color iconColor)
        {
            RectTransform panel = Rect(name + "Badge", parent, new Vector2(x, 0f), new Vector2(width, 36f));
            panel.gameObject.AddComponent<StatIconGraphic>().Configure(StatIconGraphic.Shape.Badge,
                new Color(0.06f, 0.19f, 0.31f));
            RectTransform image = Rect(name + "Icon", panel, new Vector2(-width / 2f + 17f, 1f), new Vector2(24f, 25f));
            image.gameObject.AddComponent<StatIconGraphic>().Configure(icon, iconColor);
            RectTransform label = Rect(name + "Value", panel, new Vector2(14f, 0f), new Vector2(width - 32f, 34f));
            Text text = label.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private void Unbind()
        {
            if (subscribedOwner != null) subscribedOwner.StatsChanged -= Refresh;
            subscribedOwner = null;
        }

        private void UnbindTurns()
        {
            if (subscribedTurns != null) subscribedTurns.EligibilityChanged -= RefreshActionIndicator;
            subscribedTurns = null;
        }

        private void UnbindFlow()
        {
            if (subscribedFlow != null) subscribedFlow.StateChanged -= OnFlowStateChanged;
            subscribedFlow = null;
        }

        private void OnDisable() { Unbind(); UnbindTurns(); UnbindFlow(); }
        private void OnDestroy() { Unbind(); UnbindTurns(); UnbindFlow(); }
    }
}
