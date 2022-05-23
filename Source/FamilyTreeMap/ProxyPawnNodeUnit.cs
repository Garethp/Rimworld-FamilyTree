using System.Collections.Generic;
using FamilyTree.ForceDirectedGraph;

namespace FamilyTree.FamilyTreeMap
{
    public class ProxyPawnNodeUnit: PawnNodeUnit
    {
        public ProxyPawnNodeUnit(FamilyMember familyMember) : base(familyMember) { }

        public override List<Node> GetNodes(Graph graph)
        {
            node ??= new ProxyNode(FamilyMember.Pawn, GetPosition(), graph, secondary: true);

            return new List<Node> { node };
        }
    }
}