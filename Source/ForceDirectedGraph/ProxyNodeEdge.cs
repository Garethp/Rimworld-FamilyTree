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
            DrawLine(nodeA.position, new Vector2(nodeA.position.x, nodeA.position.y - (40f * zoomFactor)), Color.yellow);
            DrawLine(new Vector2(nodeA.position.x, nodeA.position.y - (40f * zoomFactor)), new Vector2(nodeB.position.x, nodeA.position.y - (40f * zoomFactor)), Color.yellow);
            DrawLine(new Vector2(nodeB.position.x, nodeA.position.y - (40f * zoomFactor)), nodeB.position, Color.yellow);
            
            // draw lines
            // Helpers.DrawArrow(nodeA.position, nodeB.position, Color.yellow);
        }
        
        public static void DrawLine(Vector2 from, Vector2 to, Color color, bool startShort = false, bool stopShort = false) {
            // get the normalized direction of the line, offset for parallel lines, and directions of arrow head lines
            Vector2 direction = from.DirectionTo( to );

            if (startShort) from += direction * 40f;
            if (stopShort) to -= direction * 30f;
            
            // draw the lines
            Widgets.DrawLine(from, to, color, 1f);
        }

        #endregion Methods
    }
}