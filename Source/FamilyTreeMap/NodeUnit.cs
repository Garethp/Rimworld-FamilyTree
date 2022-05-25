using System.Collections.Generic;
using FamilyTree.ForceDirectedGraph;
using JetBrains.Annotations;
using UnityEngine;

namespace FamilyTree.FamilyTreeMap
{
    public abstract class NodeUnit
    {
        public int X;

        public int Y;

        private List<NodeUnit> unitsRelativeToThis = new();

        private NodeUnit relativeToParentNode;

        public Vector2 Offset = new Vector2(0, 0);

        [CanBeNull]
        public NodeUnit Parent;

        public bool IsHeadOfFamily = false;

        public abstract List<Node> GetNodes(Graph graph);

        public abstract List<Edge> GetEdges(Graph graph);

        public abstract Vector2 GetDesiredPosition();

        [CanBeNull]
        public abstract NodeUnit GetRelativeUnit();

        public Vector2 GetPosition()
        {
            var desiredPosition = GetDesiredPosition();
            var relativeTo = GetRelativeUnit();

            if (relativeTo != null) desiredPosition += relativeTo.GetPosition();

            return desiredPosition + Offset;
        }

        [CanBeNull]
        public NodeUnit GetParentRelativeTo() => relativeToParentNode;
        
        public void SetNodeToBeRelativeTo([CanBeNull] NodeUnit node)
        {
            relativeToParentNode = node;
            node?.AddNodeRelativeToThis(this);
        }

        public void AddNodeRelativeToThis(NodeUnit node)
        {
            unitsRelativeToThis.Add(node);
        }

        public List<NodeUnit> NodesRelativeToThis() => unitsRelativeToThis;

        public abstract string GetNodeType();

        public abstract int GetWidth();

        public abstract int GetHeight();

        public abstract Dictionary<int, int> GetDictionaryWidth();

        public abstract int GetGeneration();

        public abstract bool IsInGeneration(int gen);

        public abstract List<NodeUnit> GetAllNodeUnits();
    }
}