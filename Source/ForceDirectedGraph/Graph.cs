// Karel Kroeze
// Graph.cs
// 2016-12-26

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace FamilyTree.ForceDirectedGraph {
    public class Graph {
        #region Fields

        public float zoomFactor = 1f;

        public static float MAX_TEMPERATURE = .05f;
        public static float REPULSIVE_CONSTANT = 5000f;
        public static float ATTRACTIVE_CONSTANT = .2f;
        public static float CENTRAL_CONSTANT = .5f;
        public static int MAX_ITERATIONS = 2000;
        public static float THRESHOLD = .02f;

        public bool done;
        private readonly List<Edge> edges = new();
        public static float idealDistance;
        private int iteration = 1;
        public List<Node> nodes = new();
        private readonly Dictionary<Node, List<Node>> connections = new();
        public Vector2 size;
        private float temperature = .1f;
        private readonly Dictionary<Pawn, Node> _pawnNodes = new();

        private Vector2 panPosition = new(0, 0);

#if DEBUG
        public static StringBuilder msg = new();
#endif

        #endregion Fields
        
        public void Restart() {
            done = false;
            iteration = 1;
        }

        public Vector2 Center => size / 2f;

        public List<Node> Connections(Node node) {
            if (!connections.ContainsKey(node)) {
                connections.Add(node, new List<Node>());
            }

            return connections[node];
        }

        #region Constructors

        public Graph(Vector2 size) {
            this.size = size;
        }

        #endregion Constructors

        #region Methods

        public void AddEdge<T>(Node nodeA, Node nodeB) where T : Edge {
            if (
                nodeA != null &&
                nodeB != null &&
                nodeA != nodeB &&
                !Connections(nodeA).Contains(nodeB)) {
                edges.Add((T) Activator.CreateInstance(typeof(T), nodeA, nodeB));
                Connections(nodeA).Add(nodeB);
                Connections(nodeB).Add(nodeA);
            }
        }

        public void AddEdge(Edge edge)
        {
            if (Connections(edge.nodeA).Contains(edge.nodeB)) return;
            
            edges.Add(edge);
            Connections(edge.nodeA).Add(edge.nodeB);
            Connections(edge.nodeB).Add(edge.nodeA);
        }

        public void Draw(Rect canvas) {
            GUI.BeginGroup(canvas);
            
            foreach (Node node in nodes)
            {
                if (!node.zoomFactor.Equals(zoomFactor))
                {
                    node.position *= 1 / node.zoomFactor;
                    node.zoomFactor = zoomFactor;
                    node.position *= zoomFactor;
                }

                
                node.Draw();
            }
            
            foreach (Edge edge in edges)
            {
                Edge.zoomFactor = zoomFactor;
                edge.Draw();
            }
            
            Interactions();

            GUI.EndGroup();
        }

        public Node Node(Pawn pawn) {
            if (pawn == null) {
                return null;
            }

            if (!_pawnNodes.TryGetValue(pawn, out Node node)) {
                node = nodes.FirstOrDefault(n => n.pawn == pawn);
                if (node == null) {
                    return node;
                }

                _pawnNodes.Add(pawn, node);
            }

            return node;
        }

        public void ClearEdges() {
            edges.Clear();
            connections.Clear();
        }

        public void Update() {
            // check if done
            if (done || iteration > MAX_ITERATIONS || !nodes.Any(node => !node.Frozen)) {
                if (!done) {
                    done = true;
                }

                return;
            }

#if DEBUG
            msg = new StringBuilder();
            msg.AppendLine("Iteration: " + iteration);
#endif

            // prepare iteration global vars, nodes and edges.
            PrepareNextIteration();

            // update node positions
            done = true;

            // tidy up
            Vector2 graphCentre = new(
                                          ((nodes.Where(n => !n.Frozen).Max(node => node.position.x) -
                                            nodes.Where(n => !n.Frozen).Min(node => node.position.x)) / 2f) +
                                          nodes.Where(n => !n.Frozen).Min(node => node.position.x),
                                          ((nodes.Where(n => !n.Frozen).Max(node => node.position.y) -
                                            nodes.Where(n => !n.Frozen).Min(node => node.position.y)) / 2f) +
                                          nodes.Where(n => !n.Frozen).Min(node => node.position.y));
            Vector2 offset = (size / 2f) - graphCentre;

#if DEBUG
            msg.AppendLine("Centre: " + graphCentre + ", offset: " + (size / 2f));
#endif
            foreach (Node node in nodes) {
                // move to true center
                if (!node.Frozen) {
                    node.position += offset;
                }

                node.position += panPosition;

#if DEBUG
                msg.AppendLine("\t" + node.pawn.LabelShort + ", velocity: " + node.velocity + ", position: " + node.position);
            }
            Log.Message(msg.ToString());
#else
            }
#endif
            iteration++;
        }

        private void PrepareNextIteration() {
            // set iteration vars
            idealDistance = Mathf.Clamp(Mathf.Sqrt(size.x * size.y) / nodes.Count, Constants.SlotSize,
                                         Constants.SlotSize * 5f);
            temperature = MAX_TEMPERATURE * (1f - (1f / MAX_ITERATIONS * iteration));

#if DEBUG
            msg.AppendLine("idealDistance: " + idealDistance + ", temperature: " + temperature);
#endif
        }
        
        public virtual void Interactions()
        {
            // hover and drag handlers
            // if (Mouse.IsOver(slot)) {

                // on left mouse drag, move node, freeze location, and restart graph so other nodes react
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDrag) {
                    panPosition += Event.current.delta;
                    Restart();
                }
                
                if (Event.current.button == 0 && Event.current.type == EventType.ScrollWheel) {
                    Log.Message($"Delta: {Event.current.delta}");
                    zoomFactor += Event.current.delta.y * -1f * .03f;

                    zoomFactor = Mathf.Clamp(zoomFactor, 0.3f, 3f);
                    Restart();
                    Update();
                }
                // }

            // clicks
            // if (Widgets.ButtonInvisible(slot) && !wasDragged) {
            //     // on right click
            //     if (Event.current.button == 1 && OnRightClick != null) {
            //         OnRightClick();
            //     }
            //
            //     // on left click
            //     if (Event.current.button == 0 && OnLeftClick != null) {
            //         OnLeftClick();
            //     }
            // }
        }

        #endregion Methods
    }
}
