using UnityEngine;
using Verse;

namespace FamilyTree.ForceDirectedGraph {
    public class Edge {
        #region Fields

        public Node nodeA;
        public Node nodeB;

        #endregion Fields

        #region Constructors

        public Edge(Node nodeA, Node nodeB) {
            this.nodeA = nodeA;
            this.nodeB = nodeB;
        }

        #endregion Constructors

        #region Methods

        public virtual void Draw() {
            Widgets.DrawLine(nodeA.position, nodeB.position, Color.white, 1f);
        }

        #endregion Methods
    }
}
