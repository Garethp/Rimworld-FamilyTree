// Karel Kroeze
// MainTabWindow_Relations.cs
// 2016-12-26

using System;
using System.Collections.Generic;
using System.Linq;
using FamilyTree.FamilyTreeMap;
using FamilyTree.ForceDirectedGraph;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static FamilyTree.Constants;

namespace FamilyTree {
    public enum GraphMode {
        ForceDirected,
        Circle
    }

    public enum Page {
        Colonists,
        Factions
    }

    public class MainTabWindow_Relations: MainTabWindow {
        #region Constructors

        public MainTabWindow_Relations() {
            forcePause = true;
        }

        #endregion Constructors

        #region Fields

        public Graph graph;
        private static Page _currentPage = Page.Colonists;
        private static GraphMode _mode = GraphMode.ForceDirected;
        private static Faction _selectedFaction;
        private static Pawn _selectedPawn;
        private static List<Pawn> pawns;
        private static List<Pawn> firstDegreePawns;
        private static bool drawFirstDegreePawns = false;
        private float _factionDetailHeight = 999f;
        private Vector2 _factionDetailScrollPosition = Vector2.zero;
        private Pawn _lastSelectedPawn;
        private Rect detailRect;
        private Rect networkRect;
        private Rect sourceButtonRect;

        #endregion Fields

        #region Properties

        public Page CurrentPage {
            get => _currentPage;
            set {
                _currentPage = value;
                CreateGraph();
            }
        }

        public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight - 35f);

        public Faction SelectedFaction {
            get => _selectedFaction;
            set {
                // unfreeze old selection
                if (_mode == GraphMode.ForceDirected && _selectedFaction != null &&
                     graph.Node(_selectedFaction.Leader()) != null) {
                    graph.Node(_selectedFaction.Leader()).Frozen = false;
                }

                // change selection and freeze it if not null
                _selectedFaction = value;
                if (value != null) {
                    graph.Node(_selectedFaction.Leader()).Frozen = true;
                }

                // clear current list of connections
                graph.ClearEdges();

                // if something selected, draw only connections for that faction
                if (value != null && graph.Node(value.Leader()) != null) {
                    Node node = graph.Node( value.Leader() );
                    foreach (Node other in graph.nodes) {
                        graph.AddEdge<FactionEdge>(node, other);
                    }
                } else
                  // if nothing selected, build full list of connections
                  {
                    foreach (Node node in graph.nodes) {
                        foreach (Node other in graph.nodes) {
                            graph.AddEdge<FactionEdge>(node, other);
                        }
                    }
                }

                // restart dynamic process
                graph.Restart();
            }
        }

        public Pawn SelectedPawn {
            get => _selectedPawn;
            set {
                // unfreeze old selection
                if (_mode == GraphMode.ForceDirected && _selectedPawn != null && graph.Node(_selectedPawn) != null) {
                    graph.Node(_selectedPawn).Frozen = false;
                }

                // change selection and freeze it if not null
                _selectedPawn = value;
                if (value != null) {
                    graph.Node(_selectedPawn).Frozen = false;
                }

                // add relations for all pawns
                // foreach (Node node in graph.nodes) {
                //     foreach (Pawn other in node.pawn.GetRelatedPawns(pawns, false)) {
                //         graph.AddEdge<PawnEdge>(node, graph.Node(other));
                //     }
                // }

                // start adaptive process
                graph.Restart();
            }
        }

        #endregion Properties

        #region Methods

        public void CreateCircle(bool freeze) {
            int count = graph.nodes.Count;
            Vector2 center = graph.Center;
            float radius = Mathf.Min( graph.size.x / 2f, graph.size.y / 2f ) - (SlotSize / 2f);

            // set nodes on the circle, and freeze them
            for (int i = 0; i < count; i++) {
                Node node = graph.nodes[i];
                node.position = Helpers.PointOnCircle(i, count, center, radius);
                node.Frozen = freeze;
            }
        }

        public void CreateGraph() {
            // Check if Ideology is installed and enabled
            bool ideologyInstalledAndActive = ModsConfig.IdeologyActive;

            // calculate positions
            graph = new Graph(networkRect.size);
            if (CurrentPage == Page.Colonists)
            {
                var map = new PawnMap();
                
                map.AddPawns(pawns);
                var nodeGroup = map.GetPawnMap();

                var nodesToAdd = nodeGroup.GetNodes(graph);
                
                nodesToAdd.ForEach(node =>
                {
                    node.position *= new Vector2(.7f, 120);
                });
                
                graph.nodes = nodesToAdd;
                nodeGroup.GetEdges(graph).ForEach(edge => graph.AddEdge(edge));
                
                if (drawFirstDegreePawns) {
                    graph.nodes.AddRange(firstDegreePawns.Select(p => new PawnNode(p, new Vector2(0, 0), graph, secondary: true) as Node));
                }

                foreach (Node node in graph.nodes) {
                    // attach event handlers to node
                    node.OnHover += () => TooltipHandler.TipRegion(node.slot, node.pawn.GetTooltip(SelectedPawn));
                    node.OnLeftClick += () => SelectedPawn = node.pawn;
                    node.PreDrawExtras += delegate {
                        if (node.pawn == SelectedPawn || Mouse.IsOver(node.slot)) {
                            GUI.DrawTexture(node.slot, Resources.Halo);
                        }
                    };
                    node.PostDrawExtras += delegate {
                        if (node.Frozen && _mode == GraphMode.ForceDirected) {
                            GUI.DrawTexture(
                                new Rect(node.slot.xMax - 16f, node.slot.yMin,
                                    16f, 16f), Resources.Pin);
                        }
                    };
                    // add ideology information
                    if (ideologyInstalledAndActive) {
                        node.PostDrawExtras += delegate {
                            if (node.pawn.Ideo is Ideo ideo && node.pawn.ideo is not null) {
                                Rect ideologyIconRect = new(node.slot.xMin, node.slot.yMin, 16f, 16f);
                                PawnSlotDrawer.DrawTextureColoured(ideologyIconRect, ideo.Icon, ideo.Color);
                                TooltipHandler.TipRegion(ideologyIconRect,
                                    $"{ideo.name.Colorize(ideo.Color)}\n{"Certainty".Translate().CapitalizeFirst().Resolve()}: {node.pawn.ideo.Certainty.ToStringPercent()}");
                            }
                        };
                    }

                    // if pawn is not a member of this colony, add faction information
                    if (node.secondary && (!node.pawn.Faction?.IsPlayer ?? false)) {
                        node.PostDrawExtras += delegate {
                            Rect factionIconRect = new(node.slot.xMin,
                                    // Only add offset if Ideology is installed and hence there is another icon
                                    node.slot.yMin + (ideologyInstalledAndActive ?  20f : 0f), 16f, 16f);
                            PawnSlotDrawer.DrawTextureColoured(factionIconRect, node.pawn.Faction.def.FactionIcon, node.pawn.Faction.Color);
                            TooltipHandler.TipRegion(factionIconRect, node.pawn.Faction.GetFactionRelationString(Faction.OfPlayer));
                        };
                    }

                    // add edges - assign SelectedPawn to null to trigger Set method and reset selected
                    SelectedPawn = null;
                }
            } else {
                graph.nodes = Find.FactionManager
                    .AllFactionsInViewOrder
                    .Select(f => new FactionNode(f, networkRect.RandomPoint(), graph) as Node)
                    .Where(n => n.pawn != null)
                    .ToList();

                foreach (Node node in graph.nodes) {
                    // attach event handlers to node
                    if (node is not FactionNode fnode) {
                        Log.Warning("Non-faction node in node list for faction tab. ");
                        continue;
                    }

                    fnode.OnHover +=
                        () => TooltipHandler.TipRegion(fnode.slot, fnode.faction.GetTooltip(SelectedFaction));
                    fnode.OnLeftClick += () => SelectedFaction = fnode.faction;
                    fnode.PreDrawExtras += delegate {
                        if (fnode.faction == SelectedFaction || Mouse.IsOver(fnode.slot)) {
                            GUI.DrawTexture(fnode.slot, Resources.Halo);
                        }
                    };
                    node.PostDrawExtras += delegate {
                        if (node.Frozen && _mode == GraphMode.ForceDirected) {
                            GUI.DrawTexture(new Rect(node.slot.xMax - 16f, node.slot.yMin, 16f, 16f),
                                Resources.Pin);
                        }
                    };
                    node.PostDrawExtras += delegate {
                        Rect factionIconRect = new( node.slot.xMin, node.slot.yMin, 16f, 16f );
                        PawnSlotDrawer.DrawTextureColoured(factionIconRect, fnode.faction.def.FactionIcon, fnode.faction.Color);
                    };

                    // If Ideology is enabled add corresponding icons and information about the faction
                    if (ideologyInstalledAndActive) {
                        node.PostDrawExtras += delegate {
                            Rect ideologyIconRect = new(node.slot.xMin, node.slot.yMin + 20f, 16f, 16f);
                            PawnSlotDrawer.DrawTextureColoured(ideologyIconRect, fnode.faction.ideos.PrimaryIdeo.Icon, fnode.faction.ideos.PrimaryIdeo.Color);
                            TooltipHandler.TipRegion(ideologyIconRect, fnode.faction.NameColored + " mainly believes in " + fnode.faction.ideos.PrimaryIdeo.name.Colorize(fnode.faction.ideos.PrimaryIdeo.Color) + ".");
                        };
                    }

                    // attach edges - assign selected to itself to trigger Set method.
                    SelectedFaction = null;
                }
            }

            // force circle positions if mode is circle
            // CreateCircle(_mode == GraphMode.Circle);
        }

        public override void DoWindowContents(Rect canvas) {
            // update the graph
            graph.Update();

            // set size and draw background
            //base.DoWindowContents( canvas );
            
            // graph reset and mode selection icons
            DrawGraphOptions(canvas);

            // draw relevant page
            if (CurrentPage == Page.Colonists) {
                DrawPawnRelations();
            }

            // see if we can catch clicks in the main rect to reset selections
            if (Widgets.ButtonInvisible(networkRect)) {
                if (CurrentPage == Page.Colonists) {
                    SelectedPawn = null;
                }

                if (CurrentPage == Page.Factions) {
                    SelectedFaction = null;
                }
            }
        }

        public void DrawDetails(Rect canvas, Pawn pawn) {
            GUI.BeginGroup(canvas);

            int numSections = 3;
            float titleHeight = 30f;
            float margin = 6f;
            float availableHeight = canvas.height - (( titleHeight + margin ) * numSections);

            // set up rects
            Rect pawnInfoTitleRect = new( 0f, 0f, canvas.width, titleHeight );
            Rect pawnInfoRect = new( 0f, titleHeight + margin, canvas.width, availableHeight / 5f );
            Rect relationsTitleRect = new( 0f, pawnInfoRect.yMax, canvas.width, titleHeight );
            Rect relationsRect = new( 0f, relationsTitleRect.yMax + margin, canvas.width,
                availableHeight / 5f * 2f );
            Rect interactionsTitleRect = new( 0f, relationsRect.yMax + margin, canvas.width, titleHeight );
            Rect interactionsRect = new( 0f, interactionsTitleRect.yMax + margin, canvas.width,
                availableHeight / 5f * 2f );

            // titles
            Text.Font = GameFont.Medium;
            Widgets.Label(pawnInfoTitleRect, pawn.Name.ToStringFull);
            Widgets.Label(relationsTitleRect,
                "Fluffy_Relations.Possesive".Translate(pawn.LabelShort) +
                "Fluffy_Relations.Relations".Translate());
            Widgets.Label(interactionsTitleRect,
                "Fluffy_Relations.Possesive".Translate(pawn.LabelShort) +
                "Fluffy_Relations.Interactions".Translate());
            Text.Font = GameFont.Small;

            // draw overview of traits and status effects relevant to social relations
            RelationsHelper.DrawSocialStatusEffectsSummary(pawnInfoRect, pawn);
            SocialCardUtility.DrawRelationsAndOpinions(relationsRect, pawn);
            InteractionCardUtility.DrawInteractionsLog(interactionsRect, pawn, Find.PlayLog.AllEntries, 24);
            GUI.EndGroup();
        }

        public static List<Func<Faction, Vector2, float, float>> ExtraFactionDetailDrawers = new();
        public void DrawDetails(Rect canvas, Faction faction) {
            Vector2 pos = canvas.min;
            Rect viewRect = new(
                pos.x,
                pos.y,
                canvas.width,
                _factionDetailHeight );
            if (viewRect.height > canvas.height) {
                viewRect.width -= 18f;
            }

            Widgets.BeginScrollView(canvas, ref _factionDetailScrollPosition, viewRect);

            // extra drawers
            foreach (Func<Faction, Vector2, float, float> drawer in ExtraFactionDetailDrawers) {
                pos.y += drawer(faction, pos, canvas.width) + StandardMargin;
            }

            // faction details
            pos.y += DrawFactionInformation(faction, pos, canvas.width) + StandardMargin;

            // ideology
            if (ModsConfig.IdeologyActive && faction.ideos?.PrimaryIdeo is not null) {
                pos.y += DrawFactionIdeology(faction, pos, canvas.width) + StandardMargin;
            }

            // kidnappees, if any
            pos.y += DrawFactionKidnappees(faction, pos, canvas.width) + StandardMargin;

            // relations
            pos.y += DrawFactionRelations(faction, pos, canvas.width) + StandardMargin;



            Widgets.EndScrollView();
            _factionDetailHeight = pos.y - canvas.yMin + 30;
        }

        public float DrawFactionInformation(Faction faction, Vector2 pos, float width) {
            Vector2 startPos = pos;

            // draw faction icons
            Rect informationIconRect = new(pos.x, pos.y, 30f, 30f);
            PawnSlotDrawer.DrawTextureColoured(informationIconRect, faction.def.FactionIcon, faction.Color);
            informationIconRect.x += 36f;
            pos.x += 36f;
            if (Resources.baseTextures.TryGetValue(faction.def, out Texture2D baseTexture)) {
                PawnSlotDrawer.DrawTextureColoured(informationIconRect, baseTexture, faction.Color);
                pos.x += 36f;
            }

            // faction name
            Utilities.Label(ref pos, width - (pos.x - startPos.x), faction.GetFactionLabel(), Color.white, GameFont.Medium);
            pos.x = startPos.x;

            // faction leader
            Utilities.Label(ref pos, width, faction.def.leaderTitle.CapitalizeFirst() + ": "
                + (faction.Leader()?.NameFullColored.Resolve() ?? "Noone".Translate()));
            DoLeaderSelectionButton(new Vector2(pos.x + width - SmallIconSize, pos.y - SmallIconSize), faction);

            // faction type (tech)
            Utilities.Label(ref pos, width, faction.def.LabelCap.Colorize(faction.def.DefaultColor) + " (" + faction.def.techLevel.ToStringHuman() + ")");

            // faction description
            Utilities.Label(ref pos, width, faction.def.description);

            return pos.y - startPos.y;
        }

        public float DrawFactionKidnappees(Faction faction, Vector2 pos, float width) {
            Vector2 startPos = pos;

            if (faction.kidnapped?.KidnappedPawnsListForReading.Count > 0) {
                Utilities.Label(ref pos, width, "Fluffy_Relations.KidnappedColonists".Translate(), font: GameFont.Medium);
                foreach (Pawn kidnappee in faction.kidnapped.KidnappedPawnsListForReading) {
                    Utilities.Label(ref pos, width, "\t" + kidnappee.Name);
                }
            }
            return pos.y - startPos.y;
        }

        public float DrawFactionRelations(Faction faction, Vector2 pos, float width) {
            Vector2 startPos = pos;

            Utilities.Label(ref pos, width, "Fluffy_Relations.Relations".Translate(), font: GameFont.Medium);
            IOrderedEnumerable<Faction> otherFactions = Find.FactionManager
                .AllFactionsVisible
                .Where( other => other != faction &&
                                 other.RelationWith( faction, true ) != null )
                .OrderByDescending( other => other.GoodwillWith( faction ) );
            foreach (Faction other in otherFactions) {
                int opinion = Mathf.RoundToInt( other.GoodwillWith( faction ) );
                Color color = RelationsHelper.GetRelationColor( opinion );
                string label = faction.HostileTo(other) ?  "HostileTo".Translate(other.GetFactionLabel()) : other.GetFactionLabel();
                label += ": " + opinion;
                if (Utilities.ButtonLabel(ref pos, width, label, color)) {
                    SelectedFaction = other;
                }
            }

            return pos.y - startPos.y;
        }

        public float DrawFactionIdeology(Faction faction, Vector2 pos, float width) {
            Vector2 start = pos;
            if (faction.ideos?.PrimaryIdeo is not Ideo primary) {
                return 0;
            }

            Rect iconRect = new( pos.x, pos.y, 30, 30);
            PawnSlotDrawer.DrawTextureColoured(iconRect, primary.Icon, primary.Color);
            pos.x += 36;
            Utilities.Label(ref pos, width - 36, "Fluffy_Relations.Ideoligion".Translate().CapitalizeFirst(), font: GameFont.Medium);
            pos.x -= 36;
            Utilities.Label(ref pos, width, primary.name.Colorize(primary.Color));
            Utilities.Label(ref pos, width, primary.description);

            if (faction.ideos.IdeosMinorListForReading.Any()) {
                Utilities.Label(ref pos, width, "Fluffy_Relations.SecondaryIdeoligions".Translate(faction.ideos.IdeosMinorListForReading.Select(i => i.name.Colorize(i.Color)).ToCommaList()).CapitalizeFirst().Resolve());
            }

            return pos.y - start.y;
        }

        private void DoLeaderSelectionButton(Vector2 pos, Faction faction) {
            if (faction == Faction.OfPlayer && RelationsHelper.GetMayor() == null) {
                Rect factionLeaderSelectRect = new( pos.x, pos.y, SmallIconSize, SmallIconSize );
                TooltipHandler.TipRegion(factionLeaderSelectRect, "Fluffy_Relations.SelectLeaderTip".Translate());
                if (Widgets.ButtonImage(factionLeaderSelectRect, Resources.Edit)) {
                    // do leader selection dropdown.
                    List<FloatMenuOption> options = new();

                    // pawns on maps
                    foreach (Pawn pawn in Find.Maps.SelectMany(m => m.mapPawns.FreeColonists)) {
                        // todo; draw portrait extra.
                        options.Add(new FloatMenuOption(pawn.Name.ToStringShort, () => {
                            GameComponent_Leader.Leader = pawn;
                            BuildPawnList(); // restarts graph
                        },
                            extraPartWidth: 24f,
                            extraPartOnGUI: (rect) => {
                                GUI.DrawTexture(rect, PortraitsCache.Get(pawn, new Vector2(rect.width, rect.height), Rot4.South));
                                return Widgets.ButtonInvisible(rect);
                            }));
                    }

                    // pawns in caravans
                    foreach (Pawn pawn in Find.WorldObjects.Caravans
                        .Where(c => c.IsPlayerControlled)
                        .SelectMany(c => c.PawnsListForReading)
                        .Where(p => p.IsColonist)) {
                        // todo; draw portrait extra.
                        options.Add(new FloatMenuOption(pawn.Name.ToStringShort, () => {
                            GameComponent_Leader.Leader = pawn;
                            BuildPawnList(); // restarts graph
                        },
                            extraPartWidth: 24f,
                            extraPartOnGUI: (rect) => {
                                GUI.DrawTexture(rect, PortraitsCache.Get(pawn, new Vector2(rect.width, rect.height), Rot4.South));
                                return Widgets.ButtonInvisible(rect);
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
        }

        public void DrawLegend(Rect canvas) {
            // TODO: Draw legend.
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.grey;
            Widgets.Label(canvas, "Fluffy_Relations.NothingSelected".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void DrawPawnRelations() {
            // catch selected pawn changes ( clicking on relations and/or log entries will move selector )
            UpdateSelectedPawn();

            // draw pawn graph
            graph.Draw(networkRect);

            // draw legend or details in the detail rect
            if (SelectedPawn != null) {
                DrawDetails(detailRect, SelectedPawn);
            } else {
                DrawLegend(detailRect);
            }
        }

        public override void PostClose() {
            base.PostClose();

            _lastSelectedPawn = null;
        }

        public override void PreOpen() {
            base.PreOpen();
            BuildPawnList();
            _selectedFaction = Faction.OfPlayer;
            _selectedPawn = pawns.FirstOrDefault();
        }

        public void UpdateSelectedPawn() {
            // Clicking on the opinion overviews selects a pawn in the game's selector).
            // which is what we're trying to catch and reflect here.

            // do we currently have a pawn selected, and is it different from our previous selection?
            if (Find.Selector.SingleSelectedThing is Pawn selectorPawn && selectorPawn != _lastSelectedPawn) {
                // is the pawn we currently have selected a valid target for selection in the relations tab?
                // (i.e. is it a colonist?)
                if (RelationsHelper.Colonists.Contains(selectorPawn)) {
                    SelectedPawn = selectorPawn;
                }

                // stop this check happening again until we select something else.
                _lastSelectedPawn = selectorPawn;
            }
        }

        /// <summary>
        ///     Builds pawn list + slot positions
        ///     called from base.PreOpen(), and various methods that want to reset the graph.
        /// </summary>
        protected void BuildPawnList()
        {
            pawns = new List<Pawn>();
            pawns.AddRange(PawnsFinder.AllMaps_FreeColonistsSpawned);
            pawns.AddRange(Find.WorldPawns.AllPawnsDead.Where(pawn => pawn.IsColonist));
            
            var caravans = Find.WorldObjects.Caravans;
            foreach (var caravan in caravans)
            {
                if (!caravan.IsPlayerControlled) continue;
                
                var pawnsListForReading = caravan.PawnsListForReading;
                pawns.AddRange(pawnsListForReading.Where(pawn => pawn.IsColonist));
            }
            
            // Colonists on Temp Map?
            // Colonists on help missions?
            // Kidnapped?
            
            // rebuild pawn list
            // pawns = Find.CurrentMap.mapPawns.FreeColonists.ToList();
            firstDegreePawns = pawns.SelectMany(p => p.relations.RelatedPawns).Distinct().Except(pawns).ToList();
            RelationsHelper.ResetOpinionCache();

            // recalculate positions
            CreateAreas();
            CreateGraph();

            // create list of social thoughts to pawns
            RelationsHelper.CreateThoughtList(pawns.Concat(firstDegreePawns).ToList());
        }

        // split the screen into two areas
        private void CreateAreas() {
            // social network on the right, always square, try to fill the whole height - but limited by width.
            float desiredNetworkSize = Mathf.Min( UI.screenHeight - 35f, UI.screenWidth - MinDetailWidth ) -
                                     (2 * Margin);

            // detail view on the left, full height (minus what is needed for faction/colonists selection) - fill available width, but don't exceed 1/3 of the screen
            float detailRectWidth = Mathf.Min( UI.screenWidth - desiredNetworkSize - (Margin * 2), Screen.width / 3f );
            detailRect = new Rect(0f, 36f, detailRectWidth, UI.screenHeight - 35f - (Margin * 2));

            // finalize the network rect
            networkRect = new Rect(detailRectWidth + (Margin * 2), 0f, desiredNetworkSize, desiredNetworkSize);

            // selection button rect
            sourceButtonRect = new Rect(0f, 0f, 200f, 30f);
        }

        private Rect GetIconRect(Rect canvas, int index) {
            return new Rect(canvas.xMax - ((IconSize + Inset) * index), canvas.yMin + Inset, IconSize, IconSize);
        }

        private void DrawGraphOptions(Rect canvas) {
            int iconIndex = 1;
            if (_mode == GraphMode.ForceDirected) {
                // tooltips
                Rect modeIconRect = GetIconRect(canvas, iconIndex++);
                Rect resetIconRect = GetIconRect(canvas, iconIndex++);
                TooltipHandler.TipRegion(modeIconRect, "Fluffy_Relations.ModeCircleTip".Translate());
                TooltipHandler.TipRegion(resetIconRect, "Fluffy_Relations.GraphResetTip".Translate());

                if (Widgets.ButtonImage(modeIconRect, Resources.DotsCircle)) {
                    _mode = GraphMode.Circle;
                    BuildPawnList(); // restarts graph
                }

                if (Widgets.ButtonImage(resetIconRect, TexUI.RotLeftTex)) {
                    BuildPawnList();
                }
            }
            if (_mode == GraphMode.Circle) {
                Rect modeIconRect = GetIconRect(canvas, iconIndex++);
                TooltipHandler.TipRegion(modeIconRect, "Fluffy_Relations.ModeGraphTip".Translate());

                if (Widgets.ButtonImage(modeIconRect, Resources.DotsDynamic)) {
                    _mode = GraphMode.ForceDirected;
                    BuildPawnList(); // restarts graph
                }
            }
            if (_currentPage == Page.Colonists) {
                Rect firstDegreeRect = GetIconRect( canvas, iconIndex++ );
                Color baseColor = drawFirstDegreePawns ? Color.white : Color.grey;
                TaggedString tipString = drawFirstDegreePawns
                    ? "Fluffy_Relations.FirstDegreeTip_Off".Translate()
                    : "Fluffy_Relations.FirstDegreeTip_On".Translate();

                TooltipHandler.TipRegion(firstDegreeRect, tipString);
                if (Widgets.ButtonImage(firstDegreeRect, Resources.FamilyTree, baseColor)) {
                    drawFirstDegreePawns = !drawFirstDegreePawns;
                    BuildPawnList();
                }

                // because Widgets.BI() doesn't reset it...
                GUI.color = Color.white;
            }
        }

        #endregion Methods
    }
}
