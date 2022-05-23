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
                foreach (var nodeUnit in children)
                {
                    if (nodeUnit is not GroupNodeUnit) continue;
                    ((GroupNodeUnit) nodeUnit).OrganiseSubGroups();
                }
            }
        }

        private void OrganiseSubGroups()
        {
            // Create Love Units without proxy nodes
            foreach (var nodeUnit in children.ToList())
            {
                if (GetNodeType() == "Love") continue;
                if (nodeUnit.GetNodeType() != "Pawn") continue;
                if (nodeUnit.Parent?.GetNodeType() == "Love") continue;

                var pawnNodeUnit = (PawnNodeUnit) nodeUnit;
                if (!pawnNodeUnit.FamilyMember.HasLoveInterested()) continue;

                if (!pawnNodeUnit.FamilyMember.IsAnchor) continue;

                var loveUnitNodes = new List<NodeUnit>() {pawnNodeUnit};
                foreach (var familyMapNode in pawnNodeUnit.FamilyMember.GetLoveInterests())
                {
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

                // Attempt to anchor the new love node unit
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
            
            // Create love nodes that require proxy nodes
            foreach (var nodeUnit in children.ToList())
            {
                if (GetNodeType() == "Love") continue;
                if (nodeUnit.GetNodeType() != "Pawn") continue;
                if (nodeUnit.Parent?.GetNodeType() == "Love") continue;

                var pawnNodeUnit = (PawnNodeUnit) nodeUnit;
                if (!pawnNodeUnit.FamilyMember.HasLoveInterested()) continue;

                if (!pawnNodeUnit.FamilyMember.IsAnchor) continue;

                var loveUnitNodes = new List<PawnNodeUnit>() {pawnNodeUnit};
                foreach (var familyMapNode in pawnNodeUnit.FamilyMember.GetLoveInterests())
                {
                    if (familyMapNode.Generation != pawnNodeUnit.FamilyMember.Generation)
                        loveUnitNodes.Add(familyMapNode.GetPawnNodeUnit());
                }
                
                if (loveUnitNodes.Count < 2) continue;
                
                // TODO: Adjust this for having multiple lovers from multiple generations
                var youngestGeneration = loveUnitNodes.Select(node => node.GetGeneration()).Min();
                var youngestLovers = loveUnitNodes.FindAll(node => node.GetGeneration() == youngestGeneration);
                var loversToProxy = loveUnitNodes.FindAll(node => node.GetGeneration() != youngestGeneration);

                
                List<NodeUnit> newLoveUnitNodes = new();
                newLoveUnitNodes.AddRange(youngestLovers);
                newLoveUnitNodes.AddRange(loversToProxy.Select(nodeUnit => new ProxyPawnNodeUnit(nodeUnit.FamilyMember, youngestLovers[0].FamilyMember)));

                newLoveUnitNodes = newLoveUnitNodes.FindAll(node => node.Parent?.GetNodeType() != "Love");
                
                // Log.Message("Create proxy love node for: ");
                // loveUnitNodes.ForEach(node => Log.Message(node.FamilyMember.Pawn.Name.ToStringShort));

                if (newLoveUnitNodes.Count < 2) continue;

                foreach (var unit in newLoveUnitNodes)
                {
                    ((GroupNodeUnit) unit.Parent)?.RemoveChild(unit);
                }
                
                var loveUnit = new GroupNodeUnit(newLoveUnitNodes, "Love", this);
                
                // Attempt to anchor the new love node unit
                var anchorNode =
                    loveUnitNodes.Find(node => node.FamilyMember.IsAnchor && node is not ProxyPawnNodeUnit) ??
                    loveUnitNodes.Find(node => node is not ProxyPawnNodeUnit) ??
                    loveUnitNodes.ElementAt(0);
                //
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

            // Tie children to parents?
            foreach (var nodeUnit in this.children.ToList())
            {
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
                // Because we're changing the list of children, we need to check that the list still contains the one we're looking at
                if (!children.Contains(nodeUnit)) continue;
                
                if (nodeUnit is not PawnNodeUnit pawnUnit) continue;
                
                // TODO: We need to check that this isn't the oldest parent of a child
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
                    if (childUnit.FamilyMember.IsParentProxy(pawnUnit.FamilyMember)) return;
                    
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

                nodeUnit.IsHeadOfFamily = true;
                nodeToMove.IsHeadOfFamily = true;
                
                // Don't try to accidentally move the node into itself
                var nodeToMoveTo = nodesToMove.Contains(this) ? (GroupNodeUnit) Parent! : this;
                
                // This shouldn't happen but...
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
                // nodeToMove.SetNodeToBeRelativeTo(null);
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
            this.CalculatedNodes = nodes;
            
            var currentGeneration = 1;
            var offset = new Vector2(0, 0);

            var generationalWidth = GetDictionaryWidth();
            var maxWidth = GetWidth();
            var middle = maxWidth / 2;

            var offsets = new Dictionary<int, Vector2>();
            
            // Set up the offsets for each generation that is in the graph
            foreach (var genWidth in GetDictionaryWidth())
            {
                if (offsets.ContainsKey(genWidth.Key)) continue;
                    
                var startPosition = middle - (genWidth.Value / 2);
                
                if (children.FindAll(child => child.IsInGeneration(genWidth.Key) && child.GetHeight() > 0).Count > 0)
                    startPosition = 0;
                
                offsets.Add(genWidth.Key, new Vector2(startPosition, 0));
            }

            // Setup the initial node positions
            children.ForEach(child =>
            {
                var gen = child.GetGeneration();
                var widthDict = child.GetDictionaryWidth();
                
                float offsetX = 0;
                foreach (var genWidth in widthDict)
                {
                    offsetX = Math.Max(offsetX, offsets[genWidth.Key].x);
                }

                child.Offset = new Vector2(offsetX, 0) + GetPosition();
                
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
            if (this.CalculatedEdges.Count > 0) return;
            
            var edges = new List<Edge>();

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
            // return 0;
            
            // First, Naive method
            var width = children.Sum(child => child.GetWidth());

            // Second method that just calculates the max width of the generations
            width = GetDictionaryWidth().Select(genWidth => genWidth.Value).Prepend(0).Max();

            // Third attempt (This works?!?)
            var widths = new Dictionary<int, int>();
            foreach (var child in children)
            {
                var curGen = child.GetGeneration();
                if (!widths.ContainsKey(curGen)) widths.Add(curGen, 0);

                widths[curGen] += child.GetWidth();
                
                for (var i = 1; i < child.GetHeight() + 1; i++)
                {
                    curGen = child.GetGeneration() - i;
                    if (!widths.ContainsKey(curGen)) widths.Add(curGen, 0);
                
                    widths[curGen] += child.GetWidth();
                }
            }

            width = widths.Select(genWidth => genWidth.Value).Prepend(0).Max();
            
            return width;
        }

        public override int GetHeight()
        {
            var max = GetPawns().Select(pawn => pawn.Generation).Max();
            var min = GetPawns().Select(pawn => pawn.Generation).Min();
            
            return max - min;
        }

        public override Dictionary<int, int> GetDictionaryWidth()
        {
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
        
        public Dictionary<int, int> GetChildDictionaryWidth()
        {
            var width = new Dictionary<int, int>();
            
            foreach (var nodeUnit in children)
            {
                    if (!width.ContainsKey(nodeUnit.GetGeneration())) width.Add(nodeUnit.GetGeneration(), 0);
                    width[nodeUnit.GetGeneration()] += nodeUnit.GetWidth();
            }

            return width;
        }

        public override int GetGeneration() => children.First().GetGeneration();

        public override bool IsInGeneration(int gen) => gen <= GetGeneration() && gen >= GetGeneration() - GetHeight();

        public override List<FamilyMember> GetPawns()
        {
            var list = new List<FamilyMember>();
            
            children.ForEach(child =>
            {
                list.AddRange(child.GetPawns());
            });

            return list;
        }
    }
}