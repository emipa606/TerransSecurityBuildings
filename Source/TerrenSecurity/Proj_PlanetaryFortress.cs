using RimWorld;
using Verse;

namespace TerrenSecurity;

public class Proj_PlanetaryFortress : Projectile
{
    private const int ExtraExplosionCount = 1;

    private const int ExtraExplosionRadius = 5;

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        var map = Map;
        base.Impact(hitThing, blockedByShield);
        GenExplosion.DoExplosion(Position, map, def.projectile.explosionRadius, DamageDefOf.Bomb, launcher,
            base.DamageAmount, base.ArmorPenetration,
            def.projectile.soundExplode, equipmentDef, def, intendedTarget.Thing);
        for (var i = 0; i < ExtraExplosionCount; i++)
        {
            var randomCell = CellRect.CenteredOn(Position, ExtraExplosionRadius).RandomCell.ClampInsideMap(map);
            explode(randomCell, map, 5f);
        }
    }

    private void explode(IntVec3 pos, Map map, float radius)
    {
        GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Bomb, launcher, base.DamageAmount, base.ArmorPenetration,
            null, equipmentDef, def, intendedTarget.Thing);
    }
}