using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace FamilyTree.FamilyTreeMap
{
    public class FamilyMember
    {
        public FamilyMember(Pawn pawn, int generation)
        {
            this.Pawn = pawn;
            Generation = generation;
        }
        
        private bool relationshipsCreated = false;
        
        public Pawn Pawn;
        
        private PawnNodeUnit nodeUnit;

        private Dictionary<FamilyMember, ProxyPawnNodeUnit> proxyNodes = new();

        public List<FamilyMember> loveInterests = new();

        public List<FamilyMember> parents = new();

        public List<FamilyMember> children = new();

        public bool IsAnchor = false;
        
        public int Generation;

        public void CreateLoveRelationships(List<FamilyMember> allNodes)
        {
            relationshipsCreated = true;
            
            // Find all nodes that have a relationship with us. Instead of working from **this member's** relationships
            // and spreading outwards we check all the others who are in the graph and see if they're in a relationship with
            // us, that way we can avoid looking at people who aren't part of the colony and therefore aren't part of the graph
            
            // ... Now that I think about it, I can probably just perform a check like allNodes.FindAll(node => node.Pawn == relationship.otherPawn).Any()
            // to be more efficient... Let's throw that in a @TODO. The current commit isn't about efficiency yet, just bug squashing. I don't want to create
            // more bugs in this commit
            allNodes.ForEach(node =>
            {
                node.Pawn.GetLoveRelations(true).ForEach(relationship =>
                {
                    if (relationship.otherPawn != Pawn) return;
                    
                    // We're only going to assign an anchor if one of them isn't already the anchor
                    if (!IsAnchor && !node.IsAnchor)
                    {
                        // If we're going to end up using proxy nodes, just make them both anchors
                        if (Generation != node.Generation)
                        {
                            IsAnchor = true;
                            node.IsAnchor = true;
                        }
                     
                        // If we don't have parents in the graph but they do, they get to be the anchor. Otherwise we do
                        if (parents.Count == 0 && node.parents.Count > 0)
                            node.IsAnchor = true;
                        else
                            IsAnchor = true;
                    }
                    
                    // In the future we want to check if someone changed their last name and they get to be the anchor, but for now if neither of them have
                    // parents or they both have parents it really doesn't matter who gets to be the anchor. The only reason it matters **who** the anchor is
                    // is because that's who's parents they go under.
                        
                    loveInterests.Add(node);
                });
            });

            // If you have no love interests that aren't proxied (of an other generation), you're an Anchor! Well done!
            if (!loveInterests.FindAll(love => love.Generation == Generation).Any()) IsAnchor = true;
        }

        public void CreateParentRelationships(List<FamilyMember> allNodes)
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

        public List<FamilyMember> GetLoveInterests() => loveInterests;

        public void AddedToNodeUnit(PawnNodeUnit unit) => nodeUnit = unit;

        public void AddProxyFor(FamilyMember familyMember, ProxyPawnNodeUnit proxyNodeUnit) =>
            proxyNodes[familyMember] = proxyNodeUnit;

        [CanBeNull]
        public PawnNodeUnit GetPawnNodeUnit() => nodeUnit;
        
        public bool IsParentProxy(FamilyMember parentToCheck)
        {
            var youngestGeneration = parents.Select(parent => parent.Generation).Min();

            return parentToCheck.Generation != youngestGeneration;
        }

        [CanBeNull]

        public PawnNodeUnit GetPawnNodeUnitFor(FamilyMember familyMember) =>
            proxyNodes.ContainsKey(familyMember) ? proxyNodes[familyMember] : GetPawnNodeUnit();
        
        [CanBeNull]
        public NodeUnit GetParentNodeUnit() => nodeUnit?.Parent;
    }
}