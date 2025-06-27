using System.Collections.Generic;
using Verse;

namespace TerrenSecurity;

public class Verb_TerranBunker : Verb_Shoot
{
    private Building_TerranBunker bunker;

    private List<Verb> verbss;

    public override void Reset()
    {
        base.Reset();
        bunker = (Building_TerranBunker)caster;
    }

    public void ResetVerb()
    {
        bunker ??= (Building_TerranBunker)caster;

        foreach (var item in bunker.GetInner().InnerListForReading)
        {
            if (item.TryGetAttackVerb(currentTarget.Thing) != null)
            {
                item.TryGetAttackVerb(currentTarget.Thing).caster = item;
            }
        }
    }

    protected override bool TryCastShot()
    {
        verbss = [];
        bunker ??= (Building_TerranBunker)caster;

        foreach (var item in bunker.GetInner().InnerListForReading)
        {
            if (item.TryGetAttackVerb(currentTarget.Thing) != null)
            {
                verbss.Add(item.TryGetAttackVerb(currentTarget.Thing));
            }
        }

        foreach (var item2 in verbss)
        {
            item2.caster = caster;
            item2.TryStartCastOn(currentTarget);
        }

        return true;
    }
}