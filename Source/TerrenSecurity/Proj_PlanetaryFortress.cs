using RimWorld;
using Verse;

namespace TerrenSecurity;

public class Proj_PlanetaryFortress : Projectile
{
    private const int ExtraExplosionCount = 1;

    private const int ExtraExplosionRadius = 5;

    protected virtual void Impact(Thing hitThing)
    {
        var map = Map;
        base.Impact(hitThing);
        var position = Position;
        var explosionRadius = def.projectile.explosionRadius;
        var bomb = DamageDefOf.Bomb;
        var thing = launcher;
        var damageAmount = base.DamageAmount;
        var armorPenetration = base.ArmorPenetration;
        var soundExplode = def.projectile.soundExplode;
        var thingDef = equipmentDef;
        var thingDef2 = def;
        GenExplosion.DoExplosion(position, map, explosionRadius, bomb, thing, damageAmount, armorPenetration,
            soundExplode, thingDef, thingDef2, intendedTarget.Thing);
        var cellRect = CellRect.CenteredOn(Position, ExtraExplosionRadius);
        cellRect.ClipInsideMap(map);
        for (var i = 0; i < ExtraExplosionCount; i++)
        {
            var randomCell = cellRect.RandomCell;
            Explode(randomCell, map, 5f);
        }
    }

    protected void Explode(IntVec3 pos, Map map, float radius)
    {
        GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Bomb, launcher, base.DamageAmount, base.ArmorPenetration,
            null, equipmentDef, def, intendedTarget.Thing);
    }
}