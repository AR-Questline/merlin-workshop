using System.Collections.Generic;
using System.Globalization;
using Awaken.TG.Graphics.UI;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Debugging {
    [NoPrefab]
    public class VDebugLocation : View<DebugLocation> {

        MainWindow _mainWindow;
        SelectLocationTypeSelectionWindow _selectLocationTypeSelectionWindow;
        SelectLocationWindow _selectLocationWindow;

        protected override void OnInitialize() {
            _mainWindow = new MainWindow(this);
            _selectLocationTypeSelectionWindow = new SelectLocationTypeSelectionWindow(this);
            _selectLocationWindow = new SelectLocationWindow(this);
            
            _mainWindow.Show();
        }

        void OnGUI() {
            _mainWindow.OnGUI();
            _selectLocationTypeSelectionWindow.OnGUI();
            _selectLocationWindow.OnGUI();
        }

        void SelectFromNearbyLocations(float range, float dot) {
            float rangeSq = range * range;
            _selectLocationWindow.locations.Clear();
            var heroCoords = Hero.Current.Coords;
            var heroForward = Hero.Current.Forward();
            foreach (var location in World.All<Location>()) {
                var direction = location.Coords - heroCoords;
                if (direction.sqrMagnitude < rangeSq && Vector3.Dot(direction.normalized, heroForward) > dot) {
                    _selectLocationWindow.locations.Add(location);
                }
            }
            _selectLocationWindow.Show();
        }

        [UnityEngine.Scripting.Preserve]
        static void Labeled(string label, string value1, string value2) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            GUILayout.TextField(value1);
            GUILayout.TextField(value2);
            GUILayout.EndHorizontal();
        }
        static void Labeled(string label, string value) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            GUILayout.TextField(value);
            GUILayout.EndHorizontal();
        }
        static void Labeled(float labelWidth, string label, string value) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            GUILayout.TextField(value);
            GUILayout.EndHorizontal();
        }
        static void Labeled(float labelWidth, string label, float valueWidth, string value) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            GUILayout.TextField(value, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }
        static void Labeled(float labelWidth, string label, float value) => Labeled(labelWidth, label, value.ToString(CultureInfo.InvariantCulture));
        
        class MainWindow : Window {
            public Location location;
            
            Vector2 _scroll;

            NpcDrawer _npcDrawer;
                
            public MainWindow(VDebugLocation view) : base(view, "Location Debug") { }

            protected override void DrawWindow() {
                DrawLocationHeader();
                
                if (location == null) {
                    return;
                }
                _scroll = GUILayout.BeginScrollView(_scroll);
                if (location.TryGetElement(out NpcElement npc)) {
                    _npcDrawer.Draw(npc);
                } else {
                    DrawGeneric();
                }
                GUILayout.EndScrollView();
            }

            void DrawLocationHeader() {
                GUILayout.BeginHorizontal();
                if (location) {
                    GUILayout.Label("Location:", GUILayout.Width(60));
                    GUILayout.TextField(location.DisplayName, GUILayout.MinWidth(120));
                    GUILayout.TextField(location.ID, GUILayout.MinWidth(300));
                    if (GUILayout.Button("0", GUILayout.Width(40))) {
                        SelectLocation();
                    }
                } else {
                    GUILayout.Label("No Location selected", GUILayout.MinWidth(200));
                    if (GUILayout.Button("0", GUILayout.Width(40))) {
                        SelectLocation();
                    }
                }
                GUILayout.EndHorizontal();
            }

            void DrawGeneric() {
                GUILayout.Label("Generic Location");
                Labeled("Type", location.IsStatic ? "static" : location.IsNonMovable ? "non-movable" : "movable");
            }

            protected override void OnClose() {
                _view.Target.Discard();
            }
            
            void SelectLocation() {
                _view._selectLocationTypeSelectionWindow.Show();
            }

            struct NpcDrawer {
                const float SectionSpace = 10;
                
                bool _showStats;
                bool _showState;
                bool _showAnimations;

                bool _showHistorian;
                bool _showHistorianAll;
                bool _showHistorianLocomotion;
                bool _showHistorianLocomotionAll;
                bool _showHistorianMovement;
                bool _showHistorianRotation;
                bool _showHistorianStates;
                bool _showHistorianInteractions;
                bool _showHistorianAnimation;
                bool _showHistorianAnimationAll;
                bool _showHistorianAnimationGeneral;
                bool _showHistorianAnimationAdditive;
                bool _showHistorianAnimationCustom;
                bool _showHistorianAnimationTopBody;

                public void Draw(NpcElement npc) {
                    GUILayout.Label("NPC");
                    DrawToolbar();

                    if (_showStats) {
                        GUILayout.Space(SectionSpace);
                        Labeled(100, "HP", npc.Health);
                        Labeled(100, "Stamina", npc.CharacterStats.Stamina);
                        Labeled(100, "Alert", npc.NpcAI.AlertValue);
                        Labeled(100, "HeroVisibility", npc.NpcAI.HeroVisibility);
                    }

                    if (_showState) {
                        GUILayout.Space(SectionSpace);
                        Labeled(150, "Distance Band", npc.CurrentDistanceBand.ToString());
                        var state = npc.NpcAI.Behaviour.CurrentState;
                        while (state != null) {
                            Labeled(150, "State", state.GetType().Name);
                            state = state is StateMachine stateMachine ? stateMachine.CurrentState : null;
                        }
                        Labeled(150, "Interaction", npc.Behaviours.CurrentInteraction.ToString());

                        var combatBehaviourMessage = "No EnemyBaseClass";
                        if (npc.ParentModel.TryGetElement(out EnemyBaseClass enemy)) {
                            if (enemy.CurrentBehaviour.TryGet(out var behaviour)) {
                                combatBehaviourMessage = behaviour.GetType().Name;
                            } else {
                                combatBehaviourMessage = "No Behaviour";
                            }
                        } 
                        Labeled(150, "Combat Behaviour", combatBehaviourMessage);
                    }

                    if (_showAnimations) {
                        GUILayout.Space(SectionSpace);
                        DrawAnimatorStateMachineState("General", npc.Element<NpcGeneralFSM>());
                        DrawAnimatorStateMachineState("Additive", npc.Element<NpcAdditiveFSM>());
                        DrawAnimatorStateMachineState("Custom", npc.Element<NpcCustomActionsFSM>());
                        DrawAnimatorStateMachineState("TopBody", npc.Element<NpcTopBodyFSM>());
                        
                        GUILayout.Space(SectionSpace);
                    }

                    if (_showHistorian) {
                        var historian = NpcHistorian.GetHistorian(npc);
                        if (historian != null) {
                            var previousColor = GUI.color;
                            GUI.color = NpcHistorian.Enabled ? Color.green : GUI.color;
                            if (GUILayout.Button("Toggle Historian")) {
                                NpcHistorian.Enabled = !NpcHistorian.Enabled;
                            }
                            GUI.color = previousColor;
                            
                            if (_showHistorianAll) {
                                DrawHistorianNotifications("Historian - All", historian.all);
                            }
                            if (_showHistorianLocomotion) {
                                if (_showHistorianLocomotionAll) {
                                    DrawHistorianNotifications("Historian - Locomotion", historian.locomotion);
                                }
                                if (_showHistorianMovement) {
                                    DrawHistorianNotifications("Historian Locomotion - Movement", historian.movement);
                                }
                                if (_showHistorianRotation) {
                                    DrawHistorianNotifications("Historian Locomotion - Rotation", historian.rotation);
                                }   
                            }
                            if (_showHistorianStates) {
                                DrawHistorianNotifications("Historian - States", historian.states);
                            }
                            if (_showHistorianInteractions) {
                                DrawHistorianNotifications("Historian - Interactions", historian.interactions);
                            }
                            if (_showHistorianAnimation) {
                                if (_showHistorianAnimationAll) {
                                    DrawHistorianNotifications("Historian Animations - All", historian.animations);
                                }
                                if (_showHistorianAnimationGeneral) {
                                    DrawHistorianNotifications("Historian Animations - General", historian.animationsGeneral);
                                }
                                if (_showHistorianAnimationAdditive) {
                                    DrawHistorianNotifications("Historian Animations - Additive", historian.animationsAdditive);
                                }
                                if (_showHistorianAnimationCustom) {
                                    DrawHistorianNotifications("Historian Animations - Custom", historian.animationsCustom);
                                }
                                if (_showHistorianAnimationTopBody) {
                                    DrawHistorianNotifications("Historian Animations - TopBody", historian.animationsTopBody);
                                }
                            }
                        } else {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("No Historian");
                            if (GUILayout.Button("Create Historian")) {
                                NpcHistorian.Create(npc);
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }

                void DrawToolbar() {
                    GUILayout.BeginHorizontal();
                    Toggle("Stats", ref _showStats);
                    Toggle("State", ref _showState);
                    Toggle("Animations", ref _showAnimations);
                    Toggle("Historian", ref _showHistorian);
                    GUILayout.EndHorizontal();

                    if (_showHistorian) {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Historian");
                        Toggle("All", ref _showHistorianAll);
                        Toggle("Locomotion", ref _showHistorianLocomotion);
                        Toggle("States", ref _showHistorianStates);
                        Toggle("Interactions", ref _showHistorianInteractions);
                        Toggle("Animations", ref _showHistorianAnimation);
                        GUILayout.EndHorizontal();
                    }

                    if (_showHistorianLocomotion) {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Historian Locomotion");
                        Toggle("All", ref _showHistorianLocomotionAll);
                        Toggle("Movement", ref _showHistorianMovement);
                        Toggle("Rotation", ref _showHistorianRotation);
                        GUILayout.EndHorizontal();
                    }
                    
                    if (_showHistorianAnimation) {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Historian Animation");
                        Toggle("All", ref _showHistorianAnimationAll);
                        Toggle("General", ref _showHistorianAnimationGeneral);
                        Toggle("Additive", ref _showHistorianAnimationAdditive);
                        Toggle("Custom", ref _showHistorianAnimationCustom);
                        Toggle("TopBody", ref _showHistorianAnimationTopBody);
                        GUILayout.EndHorizontal();
                    }
                    
                    static void Toggle(string name, ref bool value) {
                        var color = GUI.color;
                        GUI.color = value ? Color.green : Color.gray;
                        if (GUILayout.Button(name)) {
                            value = !value;
                        }
                        GUI.color = color;
                    }
                }

                static void DrawAnimatorStateMachineState(string name, NpcAnimatorSubstateMachine machine) {
                    GUILayout.BeginHorizontal();
                    Labeled(80, "Animator", 80, name);
                    Labeled(50, "State", 100, machine.CurrentAnimatorState.Type.ToString());
                    Labeled(50, "Clip", 300, machine.AnimationClipName);
                    GUILayout.EndHorizontal();
                }

                static void DrawHistorianNotifications(string name, List<NpcHistorian.Notification> notifications) {
                    GUILayout.Space(SectionSpace);
                    GUILayout.Label(name);
                    int start = Mathf.Max(notifications.Count - 50, 0);
                    for (int i = start; i < notifications.Count; i++) {
                        GUILayout.BeginHorizontal();
                        Labeled(50, notifications[i].frames.ToString(), notifications[i].message);
                        if (GUILayout.Button("Log", GUILayout.Width(50))) {
                            LogNotification(name, notifications[i]);
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("Log All")) {
                        for(int i=start; i < notifications.Count; i++) {
                            LogNotification(name, notifications[i]);
                        }
                    }
                }

                static void LogNotification(string name, in NpcHistorian.Notification notification) {
                    var frames = notification.frames.ToString();
                    var time = notification.time.ToString(CultureInfo.InvariantCulture);
                    Log.Important?.Error(
                        $"Npc Historian {name}\n[{frames};{time}s] {notification.message}\n" +
                        $"=== BEGIN HISTORIAN STACK TRACE\n{notification.stackTrace}\n=== END HISTORIAN STACK TRACE\n"
                    );
                }
            }
        }

        class SelectLocationWindow : Window {
            public List<Location> locations = new(64);

            Vector2 _scroll;
            
            public SelectLocationWindow(VDebugLocation view) : base(view, "Select Location") { }

            protected override void DrawWindow() {
                _scroll = GUILayout.BeginScrollView(_scroll);
                foreach (var location in locations) {
                    if (GUILayout.Button($"{location.DisplayName}  {location.ID}")) {
                        _view._mainWindow.location = location;
                        Close();
                        return;
                    }
                }
                GUILayout.EndScrollView();
            }

            protected override void OnClose() {
                locations.Clear();
            }
        }

        class SelectLocationTypeSelectionWindow : Window {
            public SelectLocationTypeSelectionWindow(VDebugLocation view) : base(view, "Select Location") { }

            string _inFrontRange;
            string _inFrontDot;
            string _byIdId;

            protected override void OnShow() {
                _inFrontRange = 20.ToString();
                _inFrontDot = 0.5f.ToString(CultureInfo.CurrentCulture);
                _byIdId = "";
            }

            protected override void DrawWindow() {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("In front")) {
                    SelectInFront();
                    Close();
                    return;
                }
                GUILayout.Label("Range");
                _inFrontRange = GUILayout.TextField(_inFrontRange);
                GUILayout.Label("Dot");
                _inFrontDot = GUILayout.TextField(_inFrontDot);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("By ID")) {
                    SelectById();
                    Close();
                    return;
                }
                _byIdId = GUILayout.TextField(_byIdId);
                GUILayout.EndHorizontal();
            }

            void SelectInFront() {
                if (float.TryParse(_inFrontRange, out var range) && float.TryParse(_inFrontDot, out var dot)) {
                    _view.SelectFromNearbyLocations(range, dot);
                }
            }

            void SelectById() {
                _view._mainWindow.location = World.ByID(_byIdId).GetModelInParent<Location>();
            }
        }

        abstract class Window {
            bool _show;
            UGUIWindow _window;
            
            protected VDebugLocation _view;

            protected Window(VDebugLocation view, string name) {
                _show = false;
                var width = Screen.width / 5f;
                var height = Screen.height * 0.45f;
                var posX = 20;
                var posY = Screen.height / 2f - height / 2f;
                var position = new Rect(posX, posY, width, height);
                _window = new UGUIWindow(position, name, DrawWindow, Close, DrawToolbar);
                _view = view;
            }

            public void OnGUI() {
                if (_show) {
                    _window.OnGUI();
                }
            }

            public void Show() {
                _show = true;
                OnShow();
            }
            public void Close() {
                _show = false;
                OnClose();
            }

            protected virtual void DrawToolbar() { }
            protected virtual void DrawWindow() { }

            protected virtual void OnShow() { }
            protected virtual void OnClose() { }
        }
    }
}