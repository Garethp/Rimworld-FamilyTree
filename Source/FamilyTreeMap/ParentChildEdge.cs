using FamilyTree.ForceDirectedGraph;
using RimWorld;
using UnityEngine;
using Verse;

namespace FamilyTree.FamilyTreeMap
{
    public class ParentChildEdge: Edge {
        #region Constructors

        private Node parentA;
        private Node parentB;
        private Node child;

        public ParentChildEdge(Node parentA, Node parentB, Node child) : base(parentA, child)
        {
            this.parentA = parentA;
            this.parentB = parentB;
            this.child = child;
        }

        #endregion Constructors

        #region Methods

        public override void Draw() {
            PawnRelationDef relation = nodeA.pawn.GetMostImportantVisibleRelation( nodeB.pawn );

            var halfwayDown = (parentA.position.y + child.position.y) / 2;

            if (parentB == null)
            {
                DrawLine(parentA.position, new Vector2(parentA.position.x, halfwayDown),
                    RelationsHelper.GetRelationColor(relation, parentA.pawn.OpinionOfCached(child.pawn)),
                    startShort: true);
                DrawLine(new Vector2(parentA.position.x, halfwayDown), new Vector2(child.position.x, halfwayDown),
                    RelationsHelper.GetRelationColor(relation, parentA.pawn.OpinionOfCached(child.pawn)));
                DrawLine(new Vector2(child.position.x, halfwayDown), child.position,
                    RelationsHelper.GetRelationColor(relation, parentA.pawn.OpinionOfCached(child.pawn)),
                    stopShort: true);
            }
            else
            {
                var halfwayBetweenParents = (parentA.position.x + parentB.position.x) / 2;
                DrawLine(new Vector2(halfwayBetweenParents, parentA.position.y), new Vector2(halfwayBetweenParents, halfwayDown),
                    RelationsHelper.GetRelationColor(relation, parentA.pawn.OpinionOfCached(child.pawn)));
                DrawLine(new Vector2(halfwayBetweenParents, halfwayDown), new Vector2(child.position.x, halfwayDown),
                    RelationsHelper.GetRelationColor(relation, parentA.pawn.OpinionOfCached(child.pawn)));
                DrawLine(new Vector2(child.position.x, halfwayDown), child.position,
                    RelationsHelper.GetRelationColor(relation, parentA.pawn.OpinionOfCached(child.pawn)),
                    stopShort: true);
            }
        }

        #endregion Methods
        
        public static void DrawLine(Vector2 from, Vector2 to, Color color, bool startShort = false, bool stopShort = false) {
            // get the normalized direction of the line, offset for parallel lines, and directions of arrow head lines
            Vector2 direction = from.DirectionTo( to );
            Vector2 lineOffset = direction.RotatedBy( 90f ) * 2f;
            Vector2 arrowDirectionA = direction.RotatedBy( 145f );
            Vector2 arrowDirectionB = direction.RotatedBy( 215f );

            // start a little away from 'real' start, and offset to avoid overlapping
            if (startShort) from += direction * 40f;
            // from += lineOffset;

            // end 40 px away from 'real' end
            if (stopShort) to -= direction * 30f;
            // to += lineOffset;

            // arrow end points
            Vector2 arrowA = to + (arrowDirectionA * 6f);
            Vector2 arrowB = to + (arrowDirectionB * 6f);

            // draw the lines
            Widgets.DrawLine(from, to, color, 1f);
            // Widgets.DrawLine(to, arrowA, color, 1f);
            // Widgets.DrawLine(to, arrowB, color, 1f);
        }
    }
}