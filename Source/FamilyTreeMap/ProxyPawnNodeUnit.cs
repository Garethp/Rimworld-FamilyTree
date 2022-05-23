using System.Collections.Generic;
using FamilyTree.ForceDirectedGraph;

namespace FamilyTree.FamilyTreeMap
{
    public class ProxyPawnNodeUnit: PawnNodeUnit
    {
        protected FamilyMember relationship;
        
        public ProxyPawnNodeUnit(FamilyMember familyMember, FamilyMember relationship)
        {
            FamilyMember = familyMember;
            FamilyMember.AddProxyFor(relationship, this);
            this.relationship = relationship;
            relationship.IsAnchor = true;
        }

        public override List<Node> GetNodes(Graph graph)
        {
            node ??= new ProxyNode(FamilyMember.Pawn, GetPosition(), graph, secondary: true);

            return new List<Node> { node };
        }

        public override string GetNodeType() => "Proxy";

        public override int GetGeneration() => relationship.Generation;

        public override List<Edge> GetEdges(Graph graph)
        {
            var edges = base.GetEdges(graph);

            edges.Add(new ProxyNodeEdge(FamilyMember.GetPawnNodeUnit()?.GetNode(), GetNode()));
            
            return edges;
        }
    }
}