using UnityEngine;
using Verse;

namespace FamilyTree.ForceDirectedGraph
{
    public class ProxyNode: PawnNode
    {
        public ProxyNode(Pawn pawn, Vector2 position, Graph graph, bool frozen = false, bool secondary = false):
            base(pawn, position, graph, frozen, secondary) { }

        public override void Draw()
        {
            Rect bgRect = slot.ContractedBy(Constants.Inset * zoomFactor);
            GUI.DrawTexture(bgRect, TexUI.GrayBg);
            Widgets.DrawBox(bgRect);
            
            var label = pawn.Name.ToStringShort;
            Rect labelRect = PawnSlotDrawer.LabelRect(label, slot);

                Text.Font = GameFont.Tiny;
                // if (drawLabelBG) {
                //     GUI.DrawTexture(labelRect, Resources.SlightlyDarkBG);
                // }

                if (secondary) {
                    GUI.color = Color.grey;
                }
                Widgets.Label(labelRect, label);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
        }
    }
}