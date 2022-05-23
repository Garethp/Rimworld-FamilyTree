using System.Collections.Generic;
using System.Linq;
using FamilyTree.ForceDirectedGraph;
using UnityEngine;
using Verse;

namespace FamilyTree.FamilyTreeMap
{
    public class PawnNodeUnit: NodeUnit
    {
        public FamilyMapNode PawnNode;

        private List<Edge> edges = new();
        
        public PawnNodeUnit(FamilyMapNode familyMapNode)
        {
            PawnNode = familyMapNode;
            PawnNode.AddedToNodeUnit(this);
        }

        protected Node node;

        public Node GetNode() => node;
        
        public override List<Node> GetNodes(Graph graph)
        {
            node ??= new PawnNode(PawnNode.Pawn, GetPosition(), graph, secondary: true);

            return new List<Node> { node };
        }

        private void CalculateEdges(Graph graph)
        {
            if (GetNode() == null) return;
            if (this.edges.Count > 0) return;
            
            var edges = new List<Edge>();

            if (PawnNode.parents.Count > 0)
            {
                var parents = PawnNode.parents;
                var parentANode = parents.ElementAtOrDefault(0)?.GetPawnNodeUnit()?.GetNode();
                var parentBNode = parents.ElementAtOrDefault(1)?.GetPawnNodeUnit()?.GetNode();
                

                edges.Add(new ParentChildEdge(parentANode, parentBNode, GetNode()));
            }

            this.edges = edges;
        }
        
        public override List<Edge> GetEdges(Graph graph)
        {
            CalculateEdges(graph);
            return edges;
        }

        public override Vector2 GetDesiredPosition()
        {
            return new Vector2(0, 0 - PawnNode.Generation);
        }

        public override NodeUnit GetRelativeUnit()
        {
            return null;
        }

        public override string GetNodeType() => "Pawn";

        public override int GetWidth() => 50;

        public override int GetHeight() => 0;

        public override Dictionary<int, int> GetDictionaryWidth()
        {
            return new Dictionary<int, int> { { GetGeneration(), GetWidth() } };
        }

        public override int GetGeneration() => PawnNode.Generation;

        public override bool IsInGeneration(int gen) => gen == GetGeneration();

        public override List<FamilyMapNode> GetPawns() => new() {PawnNode};
    }
}