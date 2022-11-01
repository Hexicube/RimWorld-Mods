using System;
using System.Collections.Generic;
using ColourPicker;
using RimWorld;
using UnityEngine;
using Verse;

namespace TintApparel
{
    public class CompAutoColor : ThingComp
    {
        public bool HasPref;
        public Color Col;

        public override void CompTickRare()
        {
            base.CompTickRare();
            
            if (!HasPref) return;
            if (!(this.parent is Pawn p)) return;
            if (p.apparel == null) return;

            bool notify = false;
            Col.a = 1; // sanity - had a pawn with A=0
            foreach (var item in p.apparel.WornApparel)
            {
                if (!(item is Apparel ap)) continue; // should always pass
                if (ap.def.apparel.layers.Any(it => it.defName == "Belt")) continue; // utilities (shield belt, tornado gen, etc.)
                foreach (var colComp in item.GetComps<CompColorable>())
                {
                    if (colComp.Active && colComp.Color == Col) continue;
                    //Log.Message("[TintApparel] Attempting to apply " + Col + " to pawn " + p.Label + " for item " + item.Label);
                    colComp.SetColor(Col);
                    notify = true;
                }
            }
            if (notify) p.apparel.Notify_ApparelChanged();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref HasPref, "active", false);
            Scribe_Values.Look<Color>(ref Col, "color", Color.white);
        }
    }

    public class ITabAutoColor : ITab
    {
        private RGBHSVHolder Holder;
        
        public ITabAutoColor()
        {
            size = new Vector2(407, 249);
            labelKey = "TabAutoColor";
        }
        
        private Pawn ThePawn
        {
            get
            {
                if (SelPawn != null) return SelPawn;
                if (SelThing is Corpse c) return c.InnerPawn;
                return null;
            }
        }
        
        protected override void FillTab()
        {
            var p = ThePawn;
            if (p == null)
            {
                Widgets.Label(new Rect(12, 6, size.x-14, 28), "NoPawn".Translate());
                return;
            }
            if (!p.IsColonist)
            {
                Widgets.Label(new Rect(12, 6, size.x-14, 28), "NotPawn".Translate());
                return;
            }
            if (p.apparel == null)
            {
                Widgets.Label(new Rect(12, 6, size.x-14, 28), "CantWear".Translate());
                return;
            }
            if (!p.IsFreeNonSlaveColonist || (p.HomeFaction != null && !p.HomeFaction.IsPlayer))
            {
                Widgets.Label(new Rect(12, 6, size.x-14, 28), "NotYours".Translate());
                return;
            }
            if (p.Dead)
            {
                Widgets.Label(new Rect(12, 6, size.x-14, 28), "Dead".Translate());
                return;
            }
            var c = p.GetComp<CompAutoColor>();

            if (Widgets.RadioButton(4, 4, c.HasPref))
            {
                c.HasPref = !c.HasPref;
                c.CompTickRare();
            }
            Widgets.Label(new Rect(32, 6, 100, 28), c.HasPref ? "Active".Translate() : "Inactive".Translate());
            
            if (Holder == null)
            {
                Holder = new RGBHSVHolder
                {
                    BarSize = new Vector2(350, 20)
                };
            }
            
            var existing = new List<Color>();
            Find.ColonistBar.GetColonistsInOrder().ForEach(pawn =>
            {
                //if (!pawn.Equals(p))
                {
                    var c2 = pawn.GetComp<CompAutoColor>();
                    if (c2.HasPref)
                    {
                        if (!existing.Contains(c2.Col)) existing.Add(c2.Col);
                    }
                }
            });

            Holder.SetCol(c.Col);
            var newColOrNull = Holder.RenderAndCheck(5f, 4, 30, existing);
            if (newColOrNull is Color newCol)
            {
                c.Col = newCol;
                c.CompTickRare();
            }

            if (Widgets.ButtonText(new Rect(300, 2, 80, 24), "OldPick".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(c.Col, (newColour) =>
                {
                    c.Col = newColour;
                    c.CompTickRare();
                }));
            }
        }
    }
}