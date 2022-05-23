using UnityEngine;
using Verse;

namespace FamilyTree.ForceDirectedGraph
{
    public class ProxyNodeEdge: Edge {
        #region Constructors

        public ProxyNodeEdge(Node nodeA, Node nodeB) : base(nodeA, nodeB) {
        }

        #endregion Constructors

        #region Methods

        public override void Draw() {
            DrawLine(nodeA.position, new Vector2(nodeA.position.x, nodeA.position.y - 40), Color.yellow);
            DrawLine(new Vector2(nodeA.position.x, nodeA.position.y - 40), new Vector2(nodeB.position.x, nodeA.position.y - 40), Color.yellow);
            DrawLine(new Vector2(nodeB.position.x, nodeA.position.y - 40), nodeB.position, Color.yellow);
            
            // draw lines
            // Helpers.DrawArrow(nodeA.position, nodeB.position, Color.yellow);
        }
        
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

        #endregion Methods
    }
}