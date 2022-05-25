using System.Collections.Generic;
using System.Linq;
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

        public GroupNodeUnit GetPawnMap()
        {
            var pawns = new List<Pawn>();
            var familyNodes = new List<FamilyMember>();

            foreach (var generationGroup in generations)
            {
                var generationPawns = generationGroup.GetPawns();
                pawns.AddRange(generationPawns);
                
                generationPawns.ForEach(pawn =>
                {
                    familyNodes.Add(new FamilyMember(pawn, generationGroup.GenerationNumber));
                });
            }

            familyNodes.ForEach(node =>
            {
                node.CreateParentRelationships(familyNodes);
            });
            
            familyNodes.ForEach(node =>
            {
                node.CreateLoveRelationships(familyNodes);
            });

            var pawnNodesUnits = new List<NodeUnit>();
            pawnNodesUnits.AddRange(familyNodes.Select(node => new PawnNodeUnit(node)));
            
            familyNodes.Sort((a,b) => a.Generation - b.Generation);
            
            // For long-lived races let's try to close generational gaps between members. For exmaple, if we've got people
            // who are usually between 20-100 years old, then a 600 year old person and a 3000 year old person there'll be a 
            // lot of gaps between the regular aged people and the older people. So we go through, detect those gaps and remove them
            var previousGen = familyNodes.Select(node => node.Generation).Min();
            for (var i = 0; i < familyNodes.Count; i++)
            {
                var node = familyNodes[i];
                if (node.Generation > previousGen + 2)
                {
                    var diff = node.Generation - previousGen - 2;
                    familyNodes
                        .FindAll(node => node.Generation > previousGen + 2)
                        .ForEach(node => node.Generation -= diff);
                }

                previousGen = node.Generation;
            }
            
            var familyGroup = new GroupNodeUnit(pawnNodesUnits, "Family", null);

            return familyGroup;
        }
    }
}