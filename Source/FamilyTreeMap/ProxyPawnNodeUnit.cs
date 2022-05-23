using System.Collections.Generic;
using FamilyTree.ForceDirectedGraph;

namespace FamilyTree.FamilyTreeMap
{
    public class ProxyPawnNodeUnit: PawnNodeUnit
    {
        public ProxyPawnNodeUnit(FamilyMapNode familyMapNode) : base(familyMapNode) { }

        public override List<Node> GetNodes(Graph graph)
        {
            node ??= new ProxyNode(PawnNode.Pawn, GetPosition(), graph, secondary: true);

            return new List<Node> { node };
        }
    }
}