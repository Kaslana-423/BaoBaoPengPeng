using UnityEngine;

namespace TuanziArena
{
    public readonly struct DamageHit
    {
        public readonly UnitActor Attacker;
        public readonly UnitActor Defender;
        public readonly int Amount;
        public readonly int ActionId;
        public readonly Vector3 Position;

        public DamageHit(UnitActor attacker, UnitActor defender, int amount, int actionId, Vector3 position)
        {
            Attacker = attacker; Defender = defender; Amount = amount; ActionId = actionId; Position = position;
        }
    }
}
