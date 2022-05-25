using System;
using System.Collections.Generic;
using System.Linq;
using FamilyTree.ForceDirectedGraph;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace FamilyTree.FamilyTreeMap
{
    public class GroupNodeUnit: NodeUnit
    {
        private readonly string nodeType;

        private List<Node> CalculatedNodes = new();

        private List<Edge> CalculatedEdges = new();
        
        public GroupNodeUnit(List<NodeUnit> children, string nodeType, [CanBeNull] NodeUnit parent)
        {
            Parent = parent;
            this.children = children;
            this.nodeType = nodeType;

            foreach (var nodeUnit in children)
            {
                nodeUnit.Parent = this;
            }
            
            if (Parent is GroupNodeUnit parentGroup) parentGroup.AddChild(this);
            
            OrganiseSubGroups();

            if (parent == null && nodeType == "Family")
            {
                foreach (var nodeUnit in children.Where(nodeUnit => nodeUnit is GroupNodeUnit))
                {
                    ((GroupNodeUnit) nodeUnit).OrganiseSubGroups();
                }
            }
        }

        private void OrganiseSubGroups()
        {
            // Create Love Units without proxy nodes
            foreach (var nodeUnit in children.ToList())
            {
                // Skip proxy units always, we don't want to calculate them.
                // If we're in a Love node already, skip it so we don't end up in a recursive loop
                // Let's only bother looking at individual pawns
                // If the pawn is already in a love node, don't bother
                if (nodeUnit.GetNodeType() == "Proxy") continue;
                if (GetNodeType() == "Love") continue;
                if (nodeUnit.GetNodeType() != "Pawn") continue;
                if (nodeUnit.Parent?.GetNodeType() == "Love") continue;

                var pawnNodeUnit = (PawnNodeUnit) nodeUnit;
                if (!pawnNodeUnit.FamilyMember.HasLoveInterested()) continue;

                if (!pawnNodeUnit.FamilyMember.IsAnchor) continue;

                var loveUnitNodes = new List<NodeUnit>() {pawnNodeUnit};
                foreach (var familyMapNode in pawnNodeUnit.FamilyMember.GetLoveInterests())
                {
                    // Lovers who are in different generations get proxy nodes instead
                    if (familyMapNode.Generation == pawnNodeUnit.FamilyMember.Generation)
                        loveUnitNodes.Add(familyMapNode.GetPawnNodeUnit());
                }
                
                if (loveUnitNodes.Count < 2) continue;

                foreach (var unit in loveUnitNodes)
                {
                    var parentGroup = (GroupNodeUnit) unit.Parent;
                    if (parentGroup.GetNodeType() == "Love") continue;

                    ((GroupNodeUnit) unit.Parent).RemoveChild(unit);
                }

                var loveUnit = new GroupNodeUnit(loveUnitNodes, "Love", this);

                // If one of the people in the love nodes are anchored to a parent, let's find that and then anchor the node
                // to that parent (so that the love node essentially tries to be in the same place as the main pawn)
                var anchorNode =
                    (PawnNodeUnit) loveUnitNodes.Find(node => ((PawnNodeUnit) node).FamilyMember.IsAnchor) ??
                    (PawnNodeUnit) loveUnitNodes.ElementAt(0);

                if (anchorNode.FamilyMember.parents.Count > 0)
                {
                    var anchorParent = anchorNode.FamilyMember.parents.Find(parent => parent.IsAnchor) ??
                                       anchorNode.FamilyMember.parents.ElementAt(0);

                    NodeUnit nodeToAnchorTo = anchorParent.GetPawnNodeUnit();
                    if (nodeToAnchorTo != null)
                    {
                        if (nodeToAnchorTo.Parent?.GetNodeType() == "Love") nodeToAnchorTo = nodeToAnchorTo.Parent;
                        if (loveUnit.Parent?.GetParentRelativeTo()?.Equals(nodeToAnchorTo) != true) 
                            loveUnit.SetNodeToBeRelativeTo(nodeToAnchorTo);
                    }
                }
            }

            // Connect nodes to their parents for positioning later
            foreach (var nodeUnit in this.children.ToList())
            {
                if (nodeUnit.GetNodeType() == "Proxy") continue;
                if (nodeUnit.GetNodeType() != "Pawn") continue;
                if (nodeUnit.GetParentRelativeTo() != null) continue;

                var pawnNodeUnit = (PawnNodeUnit) nodeUnit;
                if (pawnNodeUnit.FamilyMember.parents.Count == 0) continue;

                var anchorParent = pawnNodeUnit.FamilyMember.parents.Find(parent => parent.IsAnchor) ??
                                   pawnNodeUnit.FamilyMember.parents.ElementAt(0);

                NodeUnit nodeToAnchorTo = anchorParent.GetPawnNodeUnit();
                if (nodeToAnchorTo == null) continue;
                
                if (nodeToAnchorTo.Parent?.GetNodeType() == "Love") nodeToAnchorTo = nodeToAnchorTo.Parent;

                if (GetNodeType() == "Love")
                {
                    if (GetParentRelativeTo() == null && Parent?.GetParentRelativeTo()?.Equals(nodeToAnchorTo) != true) 
                        SetNodeToBeRelativeTo(nodeToAnchorTo);
                }
                else
                {
                    nodeUnit.SetNodeToBeRelativeTo(nodeToAnchorTo);
                }
            }

            // Group parents and children into a "Family" unit
            foreach (var nodeUnit in this.children.ToList())
            {
                if (nodeUnit.GetNodeType() == "Proxy") continue;
                // Because we're changing the list of children, we need to check that the list still contains the one we're looking at
                if (!children.Contains(nodeUnit)) continue;
                
                if (nodeUnit is not PawnNodeUnit pawnUnit) continue;
                
                // TODO: We need to check that this isn't the oldest parent of a child, because they'll be a proxy node instead of the non-proxy parent
                if (!pawnUnit.FamilyMember.IsAnchor) continue;
                
                if (pawnUnit.FamilyMember.children.Count == 0) continue;
                
                // If we're inside a love group, let's move the entire group. Otherwise it's just a single parent, move them instead
                NodeUnit nodeToMove = GetNodeType() == "Love" ? this : pawnUnit;
                var nodeToBeRelativeTo = nodeToMove.GetParentRelativeTo() ?? pawnUnit.GetParentRelativeTo();
                
                // If we're looking at a head of family then we've already processed them
                if (nodeUnit.IsHeadOfFamily || nodeToMove.IsHeadOfFamily) continue;

                var nodesToMove = new List<NodeUnit> {nodeToMove};
                
                pawnUnit.FamilyMember.children.ForEach(child =>
                {
                    var childUnit = child.GetPawnNodeUnit();
                    if (childUnit == null) return;
                    if (!childUnit.FamilyMember.IsAnchor) return;
                    
                    // Let's not move the child if the pawnUnit we're looking at will be their parent proxy
                    if (childUnit.FamilyMember.IsParentProxy(pawnUnit.FamilyMember)) return;
                    
                    // If the child is in a love node, move the love node instead
                    var childNodeToMove = childUnit.Parent?.GetNodeType() == "Love" ? childUnit.Parent : childUnit;
                    childNodeToMove ??= childUnit;

                    // If the child is the head of the family, move the entire family
                    if (childNodeToMove.IsHeadOfFamily && childNodeToMove.Parent?.GetNodeType() == "Family")
                    {
                        childNodeToMove = childNodeToMove.Parent;
                    }
                    
                    nodesToMove.Add(childNodeToMove);
                });
                
                // If the parent is a proxy, for example, we might not want to move the children
                if (nodesToMove.Count == 1) continue;

                // We use IsHeadOfFamily as a way to check whether we've moved this node before to stop us from recurring
                // infinitely because when we create the family node the first thing it will do is run this Organisation
                // function and loop through pawns to try and create families inside that family
                nodeUnit.IsHeadOfFamily = true;
                nodeToMove.IsHeadOfFamily = true;
                
                // Don't try to accidentally move the node into itself
                var nodeToMoveTo = nodesToMove.Contains(this) ? (GroupNodeUnit) Parent! : this;
                
                // I don't think this has happened yet, but I'm gonna throw it in anyway
                if (nodeToMove == null) throw new Exception("Cannot move nodes");

                foreach (var toMove in nodesToMove)
                {
                    ((GroupNodeUnit) toMove.Parent)?.RemoveChild(toMove);

                    if (toMove.GetParentRelativeTo()?.Equals(nodeToBeRelativeTo) == true)
                    {
                        toMove.GetParentRelativeTo()?.NodesRelativeToThis().Remove(toMove);
                        toMove.SetNodeToBeRelativeTo(null);
                    }
                }
                
                var familyUnit = new GroupNodeUnit(nodesToMove, "Family", nodeToMoveTo);

                familyUnit.SetNodeToBeRelativeTo(nodeToBeRelativeTo);
            }

            // Create love nodes that require proxy nodes
            // It looks like we need a way to do this after all the other organising is done
            foreach (var nodeUnit in children.ToList())
            {
                if (nodeUnit.GetNodeType() == "Proxy") continue;
                
                // TODO: Do we need to remove this? I think we might need to?
                if (GetNodeType() == "Love") continue;
                if (nodeUnit.GetNodeType() != "Pawn") continue;
                if (nodeUnit.Parent?.GetNodeType() == "Love") continue;

                var pawnNodeUnit = (PawnNodeUnit) nodeUnit;
                if (!pawnNodeUnit.FamilyMember.HasLoveInterested()) continue;

                if (!pawnNodeUnit.FamilyMember.IsAnchor) continue;

                var loveUnitNodes = new List<PawnNodeUnit>() {pawnNodeUnit};
                
                // Just get lovers who are different generations
                foreach (var familyMapNode in pawnNodeUnit.FamilyMember.GetLoveInterests())
                {
                    if (familyMapNode.Generation != pawnNodeUnit.FamilyMember.Generation)
                        loveUnitNodes.Add(familyMapNode.GetPawnNodeUnit());
                }
                
                if (loveUnitNodes.Count < 2) continue;
                
                var oldestGeneration = loveUnitNodes.Select(node => node.GetGeneration()).Max();
                var nonProxyLovers = loveUnitNodes.FindAll(node => node.GetGeneration() != oldestGeneration);
                List<PawnNodeUnit> loversToProxy = loveUnitNodes.FindAll(node => node.GetGeneration() == oldestGeneration);
                
                // We go through each non-proxy node and add their proxies next to them rather than starting with the proxies and figuring out where to go
                nonProxyLovers.ForEach(lover =>
                {
                    // If the non-proxied lover is already in a love node, let's go ahead and add the proxy into that existing node
                    if (lover.Parent?.GetNodeType() == "Love")
                    {
                        GroupNodeUnit loveGroup = (GroupNodeUnit) lover.Parent;

                        var loversToAdd = loversToProxy
                            .FindAll(loverToProxy =>
                                lover.FamilyMember.loveInterests.Contains(loverToProxy.FamilyMember)
                                && !loveGroup.children.Contains(loverToProxy.FamilyMember.GetPawnNodeUnitFor(lover.FamilyMember))
                            )
                            .Select(nodeUnit => new ProxyPawnNodeUnit(nodeUnit.FamilyMember, lover.FamilyMember))
                            .ToList();

                        if (!loversToAdd.Any()) return;
                        
                        foreach (var proxyPawnNodeUnit in loversToAdd.ToList())
                        {
                            loveGroup.AddChild(proxyPawnNodeUnit);
                        }
                        
                        return;
                    }
                    
                    // This is gonna look a lot like creating a new love node between normal people. Because it is, we're
                    // just gonna be adding proxies instead
                    List<NodeUnit> newLoveUnitNodes = new();
                    newLoveUnitNodes.Add(lover);
                    newLoveUnitNodes.AddRange(
                        loversToProxy
                            .FindAll(loverToProxy => lover.FamilyMember.loveInterests.Contains(loverToProxy.FamilyMember))
                            .Select(nodeUnit => new ProxyPawnNodeUnit(nodeUnit.FamilyMember, lover.FamilyMember))
                    );
                    
                    if (newLoveUnitNodes.Count < 2) return;

                    var groupToMoveTo = lover.Parent;
                    foreach (var unit in newLoveUnitNodes)
                    {
                        ((GroupNodeUnit) unit.Parent)?.RemoveChild(unit);
                    }
                
                    var loveUnit = new GroupNodeUnit(newLoveUnitNodes, "Love", groupToMoveTo);
                    
                    var anchorNode =
                        loveUnitNodes.Find(node => node.FamilyMember.IsAnchor && node is not ProxyPawnNodeUnit) ??
                        loveUnitNodes.Find(node => node is not ProxyPawnNodeUnit) ??
                        loveUnitNodes.ElementAt(0);
                    
                    // Same as what we did in adding normal love nodes, we need to see if we want to be anchoring the love
                    // node to either of the existing nodes
                    if (anchorNode.FamilyMember.parents.Count > 0)
                    {
                        var anchorParent = anchorNode.FamilyMember.parents.Find(parent => parent.IsAnchor) ??
                                           anchorNode.FamilyMember.parents.ElementAt(0);
                
                        NodeUnit nodeToAnchorTo = anchorParent.GetPawnNodeUnit();
                        if (nodeToAnchorTo != null)
                        {
                            if (nodeToAnchorTo.Parent?.GetNodeType() == "Love") nodeToAnchorTo = nodeToAnchorTo.Parent;
                            if (loveUnit.Parent?.GetParentRelativeTo()?.Equals(nodeToAnchorTo) != true) 
                                loveUnit.SetNodeToBeRelativeTo(nodeToAnchorTo);
                        }
                    }
                });
            }
        }
        
        public List<NodeUnit> children;

        public void RemoveChild(NodeUnit unit)
        {
            unit.Parent = null;
            this.children.Remove(unit);
        }

        public void AddChild(NodeUnit unit)
        {
            unit.Parent = this;
            children.Add(unit);
        }

        private void CalculateNodes(Graph graph)
        {
            if (CalculatedNodes.Count > 0) return;
            
            var nodes = new List<Node>();
            CalculatedNodes = nodes;

            var maxWidth = GetWidth();
            var middle = maxWidth / 2;

            var offsets = new Dictionary<int, Vector2>();
            
            // Set up the offsets for each generation that is in the graph
            foreach (var genWidth in GetDictionaryWidth())
            {
                if (offsets.ContainsKey(genWidth.Key)) continue;
                    
                var startPosition = middle - (genWidth.Value / 2);
                
                // Setting the start position to the middle only works if all the nodes in this row exist **only** in this row
                // If they span multiple rows, we can't really center them well
                if (children.FindAll(child => child.IsInGeneration(genWidth.Key) && child.GetHeight() > 0).Any())
                    startPosition = 0;

                offsets.Add(genWidth.Key, new Vector2(startPosition, 0));
            }

            // Setup the initial node positions
            children.ForEach(child =>
            {
                // Get the generations that this child spawns and find the highest offset for all of them.
                var widthDict = child.GetDictionaryWidth();
                
                float offsetX = 0;
                foreach (var genWidth in widthDict)
                {
                    offsetX = Math.Max(offsetX, offsets[genWidth.Key].x);
                }

                child.Offset = new Vector2(offsetX, 0) + GetPosition();
                
                // For each generation that this child is in we need to increment the offsets by it's width
                foreach (var genWidth in widthDict)
                {
                    if (offsets[genWidth.Key].x < offsetX) offsets[genWidth.Key] = new Vector2(offsetX, 0);
                    
                    offsets[genWidth.Key] += new Vector2(child.GetWidth(), 0);
                }
            });

            // Add the nodes to the graph
            children.ForEach(child =>
            {
                var childNodes = child.GetNodes(graph);
                childNodes.ForEach(node =>
                {
                    nodes.Add(node);
                });
            });
            
            CalculatedNodes = nodes;
        }

        private void CalculateEdges(Graph graph)
        {
            CalculateNodes(graph);
            if (CalculatedEdges.Count > 0) return;
            
            var edges = new List<Edge>();

            // I really gotta make Love and Family nodes polymorphic types from Group...
            // Until then, this is just a bit where we connect all units in a love group with each other
            if (GetNodeType() == "Love")
            {
                foreach (var lover1 in CalculatedNodes)
                {
                    foreach (var lover2 in CalculatedNodes)
                    {
                        if (lover1 == lover2) continue;
                        
                        edges.Add(new PawnEdge(lover1, lover2));
                    }
                }
            }

            foreach (var child in children)
            {
                edges.AddRange(child.GetEdges(graph));
            }
            
            CalculatedEdges = edges;
        }
        
        public override List<Node> GetNodes(Graph graph)
        {
            CalculateNodes(graph);
            return CalculatedNodes;
        }

        public override List<Edge> GetEdges(Graph graph)
        {
            CalculateEdges(graph);
            return CalculatedEdges;
        }

        public override Vector2 GetDesiredPosition()
        {
            return new Vector2(0, 0);
        }

        public override NodeUnit GetRelativeUnit()
        {
            return null;
        }

        public override string GetNodeType() => this.nodeType;

        public override int GetWidth()
        {
            // Create a dictionary of generations that this consists of and how wide it is
            // at that generation then get the max of them all
            var widths = new Dictionary<int, int>();
            foreach (var child in children)
            {
                var curGen = child.GetGeneration();
                if (!widths.ContainsKey(curGen)) widths.Add(curGen, 0);

                widths[curGen] += child.GetWidth();
                
                // A group can span multiple generations (family is the main example)
                // so we get their "height" (always going downwards) and add the width to
                // downward generations
                for (var i = 1; i < child.GetHeight() + 1; i++)
                {
                    curGen = child.GetGeneration() - i;
                    if (!widths.ContainsKey(curGen)) widths.Add(curGen, 0);
                
                    widths[curGen] += child.GetWidth();
                }
            }

            return widths.Select(genWidth => genWidth.Value).Prepend(0).Max();
        }

        public override int GetHeight()
        {
            var max = GetAllNodeUnits().Select(child => child.GetGeneration()).Max();
            var min = GetAllNodeUnits().Select(child => child.GetGeneration()).Min();
            
            return max - min;
        }

        public override Dictionary<int, int> GetDictionaryWidth()
        {
            // Just a dictionary of what generations we have and what widths they have
            var width = new Dictionary<int, int>();
            
            foreach (var nodeUnit in children)
            {
                var childWidth = nodeUnit.GetDictionaryWidth();
                foreach (var genWidth in childWidth)
                {
                    if (!width.ContainsKey(genWidth.Key)) width.Add(genWidth.Key, 0);
                    width[genWidth.Key] += genWidth.Value;
                }
            }

            return width;
        }
        
        public override int GetGeneration() => GetAllNodeUnits().Select(child => child.GetGeneration()).Max();

        public override bool IsInGeneration(int gen) => gen <= GetGeneration() && gen >= GetGeneration() - GetHeight();

        public override List<NodeUnit> GetAllNodeUnits()
        {
            var list = new List<NodeUnit>();
            
            children.ForEach(child =>
            {
                list.AddRange(child.GetAllNodeUnits());
            });

            return list;
        }
    }
}