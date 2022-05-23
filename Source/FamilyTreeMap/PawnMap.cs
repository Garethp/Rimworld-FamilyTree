using System.Collections.Generic;
using Verse;

namespace FamilyTree.FamilyTreeMap
{
    public class PawnMap
    {
        private List<GenerationGroup> generations = new ();

        public void AddPawns(List<Pawn> pawns)
        {
            foreach (var pawn in pawns)
            {
                AddPawn(pawn);
            }
        }
        
        public void AddPawn(Pawn pawn)
        {
            foreach (var generationGroup in generations)
            {
                if (!generationGroup.CanAcceptPawn(pawn)) continue;
                
                generationGroup.AddPawn(pawn);
                return;
            }
            
            generations.Add(new GenerationGroup(pawn));
            generations.SortBy(a => a.GenerationNumber);
            generations.Reverse();
        }

        public GroupNodeUnit GetPawns()
        {
            var pawns = new List<Pawn>();
            var familyNodes = new List<FamilyMapNode>();

            foreach (var generationGroup in generations)
            {
                var generationPawns = generationGroup.GetPawns();
                pawns.AddRange(generationPawns);
                
                generationPawns.ForEach(pawn =>
                {
                    familyNodes.Add(new FamilyMapNode(pawn, generationGroup.GenerationNumber));
                });
            }
            
            familyNodes.ForEach(node =>
            {
                node.CreateParentRelationships(familyNodes);
            });
            
            familyNodes.ForEach(node =>
            {
                node.CreateRelationships(familyNodes);
            });

            var pawnNodesUnits = new List<NodeUnit>();
            familyNodes.ForEach(node =>
            {
                pawnNodesUnits.Add(new PawnNodeUnit(node));
            });
            
            familyNodes.Sort((a,b) => a.Generation - b.Generation);
            
            var familyGroup = new GroupNodeUnit(pawnNodesUnits, "Family", null);
            Log.Message($"Top group has {familyGroup.children.Count} children");

            return familyGroup;
            // return pawns;
        }

        public List<GenerationGroup> GetGenerations()
        {
            return generations;
        }
    }
}