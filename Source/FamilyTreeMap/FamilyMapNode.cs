using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace FamilyTree.FamilyTreeMap
{
    public class FamilyMapNode
    {
        public FamilyMapNode(Pawn pawn, int generation)
        {
            this.Pawn = pawn;
            Generation = generation;
        }
        
        private bool relationshipsCreated = false;
        
        public Pawn Pawn;

        private List<FamilyMapNode> loveInterests = new();

        public List<FamilyMapNode> parents = new();

        public List<FamilyMapNode> children = new();

        public bool IsAnchor = false;
        
        public int Generation;

        public void CreateRelationships(List<FamilyMapNode> allNodes)
        {
            relationshipsCreated = true;
            
            allNodes.ForEach(node =>
            {
                node.Pawn.GetLoveRelations(true).ForEach(relationship =>
                {
                    if (relationship.otherPawn != Pawn) return;
                    if (!IsAnchor && !node.IsAnchor)
                    {
                        if (parents.Count == 0 && node.parents.Count > 0)
                            node.IsAnchor = true;
                        else
                            IsAnchor = true;
                    }
                        
                    loveInterests.Add(node);
                });
            });

            if (loveInterests.Count == 0) IsAnchor = true;
        }

        public void CreateParentRelationships(List<FamilyMapNode> allNodes)
        {
            allNodes.ForEach(node =>
            {
                node.Pawn.relations.DirectRelations.ForEach(relationship =>
                {
                    if (relationship.def.defName == "Parent" && relationship.otherPawn == Pawn)
                    {
                        node.parents.Add(this);
                        children.Add(node);
                    }
                });
            });
        }

        public bool HasLoveInterested() => loveInterests.Count > 0;

        public List<FamilyMapNode> GetLoveInterests() => loveInterests;

        public void AddedToNodeUnit(PawnNodeUnit unit) => this.nodeUnit = unit;

        [CanBeNull]
        public PawnNodeUnit GetPawnNodeUnit() => this.nodeUnit;
        
        [CanBeNull]
        public NodeUnit GetParentNodeUnit() => this.nodeUnit?.Parent;
        
        private PawnNodeUnit nodeUnit;
    }
}