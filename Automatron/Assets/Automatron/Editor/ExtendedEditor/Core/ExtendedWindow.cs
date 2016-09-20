#if UNITY_EDITOR
﻿using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Automatron.Editor.Serialization;
using TNRD.Automatron.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace TNRD.Automatron.Editor.Core {

    public class ExtendedWindow {

        private static ReflectionData rData = new ReflectionData( typeof( ExtendedControl ) );

        public GUIContent WindowContent = new GUIContent();

        public Rect WindowRect = new Rect();

        public int WindowID {
            get { return windowID; }
        }

        [RequireSerialization]
        private int windowID = 0;
        [RequireSerialization]
        private int controlID = 0;

        public Vector2 Position {
            get { return WindowRect.position; }
            set {
                var temp = value;
                temp.x = Mathf.Floor( temp.x );
                temp.y = Mathf.Floor( temp.y );
                WindowRect.position = temp;
                WindowSettings.IsFullscreen = false;
            }
        }

        public Vector2 Size {
            get { return WindowRect.size; }
            set {
                WindowRect.size = value;
                WindowSettings.IsFullscreen = false;
            }
        }

        public ExtendedWindowSettings WindowSettings;

        public EWindowStyle WindowStyle;

        public ExtendedAssets Assets {
            get { return Editor.Assets; }
        }

        public ExtendedInput Input {
            get {
                return Editor.Input;
            }
        }

        /// <summary>
        /// Setting this to true will sort the controls based on their SortingOrder in the next OnGUI run
        /// </summary>
        [IgnoreSerialization]
        public bool ShouldSortControls = false;

        [IgnoreSerialization]
        public ExtendedEditor Editor;

        [RequireSerialization]
        private List<ExtendedControl> controls = new List<ExtendedControl>();

        private Dictionary<Type, List<ExtendedControl>> controlsGrouped = new Dictionary<Type, List<ExtendedControl>>();

        private List<Action> guiActions = new List<Action>();

        [RequireSerialization]
        private bool initializedGUI = false;
        
        private GUIStyle maximizeButtonStyle;
        private GUIStyle closeButtonStyle;

        private void InternalInitialize( int id ) {
            windowID = id;

            if ( rData == null ) {
                rData = new ReflectionData( typeof( ExtendedControl ) );
            }

            if ( Position == Vector2.zero && Size == Vector2.zero ) {
                WindowRect = new Rect( Vector2.zero, Editor.Size );
            }

            if ( WindowSettings == null ) {
                WindowSettings = new ExtendedWindowSettings();
            }

            SceneView.onSceneGUIDelegate -= InternalSceneGUI;
            SceneView.onSceneGUIDelegate += InternalSceneGUI;

            OnInitialize();
        }

        private void InternalInitializeGUI() {
            CreateStyles();
            OnInitializeGUI();
            initializedGUI = true;
        }

        private void InternalBeforeSerialize() {
            foreach ( var item in controls ) {
                item.Window = this;
                rData.BeforeSerialize.Invoke( item, null );
            }

            OnBeforeSerialize();
        }

        private void InternalAfterDeserialize() {
            RunOnGUIThread( CreateStyles );

            controls = controls.Where( c => c != null ).ToList();

            foreach ( var item in controls ) {
                AddControlGrouped( item );
            }

            foreach ( var item in controls ) {
                item.Window = this;
            }

            OnAfterSerialized();

            var ctrls = new List<ExtendedControl>( controls );
            foreach ( var item in ctrls ) {
                rData.AfterDeserialize.Invoke( item, null );
            }
        }

        private void InternalDestroy() {
            SceneView.onSceneGUIDelegate -= InternalSceneGUI;

            OnDestroy();
        }

        private void InternalFocus() {
            OnFocus();

            var controlsToProcess = new List<ExtendedControl>( controls );
            for ( int i = 0; i < controlsToProcess.Count; i++ ) {
                rData.Focus.Invoke( controlsToProcess[i], null );
            }
        }

        private void InternalLostFocus() {
            OnLostFocus();

            var controlsToProcess = new List<ExtendedControl>( controls );
            for ( int i = 0; i < controlsToProcess.Count; i++ ) {
                rData.LostFocus.Invoke( controlsToProcess[i], null );
            }
        }

        private void InternalInspectorUpdate() {
            OnInspectorUpdate();

            var controlsToProcess = new List<ExtendedControl>( controls );
            for ( int i = 0; i < controlsToProcess.Count; i++ ) {
                rData.InspectorUpdate.Invoke( controlsToProcess[i], null );
            }
        }

        private void InternalUpdate() {
            OnUpdate();

            var controlsToProcess = new List<ExtendedControl>( controls );
            for ( int i = 0; i < controlsToProcess.Count; i++ ) {
                rData.Update.Invoke( controlsToProcess[i], null );
            }
        }

        private void InternalGUI() {
            if ( !initializedGUI ) {
                InternalInitializeGUI();
            }

            if ( ShouldSortControls && Event.current.type == EventType.Layout ) {
                ShouldSortControls = false;
                SortControls();
                Repaint();
            }

            if ( WindowSettings.IsFullscreen ) {
                var pos = WindowRect.position;
                if ( pos.x != 0 || pos.y != 0 ) {
                    WindowRect.position = new Vector2();
                }

                var size = WindowRect.size;
                if ( size.x != Editor.Size.x || size.y != Editor.Size.y ) {
                    WindowRect.size = Editor.Size;
                }
            }

            if ( guiActions.Count > 0 ) {
                var gActions = new List<Action>( guiActions );
                gActions.Reverse();
                guiActions.Clear();

                for ( int i = gActions.Count - 1; i >= 0; i-- ) {
                    gActions[i].Invoke();
                }
            }

            var controlsToProcess = new List<ExtendedControl>( controls );
            for ( int i = 0; i < controlsToProcess.Count; i++ ) {
                rData.GUI.Invoke( controlsToProcess[i], null );
            }

            OnGUI();

            if ( WindowSettings.Draggable ) {
                var rect = new Rect( 0, 0, Size.x - 30, 17f );
                GUI.DragWindow( rect );
                EditorGUIUtility.AddCursorRect( rect, MouseCursor.Pan );
            }

            if ( WindowSettings.DrawTitleBarButtons ) {
                if ( WindowStyle == EWindowStyle.Default || WindowStyle == EWindowStyle.DefaultUnity ) {
                    var rect = new Rect( new Vector2( Size.x - 13, 1 ), new Vector2( 13, 13 ) );
                    if ( GUI.Button( rect, "", closeButtonStyle ) ) {
                        Editor.RemoveWindow( this );
                    }

                    rect.x -= 13;
                    if ( GUI.Button( rect, "", maximizeButtonStyle ) ) {
                        WindowSettings.IsFullscreen = !WindowSettings.IsFullscreen;
                    }
                }
            }
        }

        private void InternalSceneGUI( SceneView view ) {
            OnSceneGUI( view );

            var controlsToProcess = new List<ExtendedControl>( controls );
            var param = new object[] { view };
            for ( int i = 0; i < controlsToProcess.Count; i++ ) {
                rData.SceneGUI.Invoke( controlsToProcess[i], param );
            }
        }

        protected virtual void OnInitialize() { }

        protected virtual void OnInitializeGUI() { }

        protected virtual void OnBeforeSerialize() { }

        protected virtual void OnAfterSerialized() { }

        protected virtual void OnDestroy() { }

        protected virtual void OnFocus() { }

        protected virtual void OnLostFocus() { }

        protected virtual void OnGUI() { }

        protected virtual void OnSceneGUI( SceneView view ) { }

        protected virtual void OnInspectorUpdate() { }

        protected virtual void OnUpdate() { }

        private void CreateStyles() {
            closeButtonStyle = new GUIStyle();
            closeButtonStyle.normal.background = Assets.B64( "closeButtonStyleNormal",
                @"iVBORw0KGgoAAAANSUhEUgAAAA0AAAANCAYAAABy6+R8AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA4RpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuNS1jMDIxIDc5LjE1NTc3MiwgMjAxNC8wMS8xMy0xOTo0NDowMCAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo4MWRjMDQyMi01ZGJmLTQ0NDgtODVhNC1iN2IzYzJhM2IzNmQiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6QzgzNjI2MjEyOENCMTFFNUFFNTJDNjM1REM5RUIyNzMiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6QzgzNjI2MjAyOENCMTFFNUFFNTJDNjM1REM5RUIyNzMiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENDIDIwMTQgKFdpbmRvd3MpIj4gPHhtcE1NOkRlcml2ZWRGcm9tIHN0UmVmOmluc3RhbmNlSUQ9InhtcC5paWQ6OTg1NTdmYTctNDZkMi00YzRiLWJkMmYtNjJhZTVmNzBkNjhkIiBzdFJlZjpkb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6YTc2NmFhMjQtMjhjYS0xMWU1LTg1MTgtYjk3OWUxOThhMWQ0Ii8+IDwvcmRmOkRlc2NyaXB0aW9uPiA8L3JkZjpSREY+IDwveDp4bXBtZXRhPiA8P3hwYWNrZXQgZW5kPSJyIj8+dLJBTAAAAGRJREFUeNpi/P//PwOpgImBDECWJhZkTk9PTxOUWVdSUoIiBuTXYdUEBbVQxSBFTVB+M06bQDYga0TSUIdPE7pGDA0YAQH1B7KTaqF8ggGBbkMtmguwOg9ZQx22IGcc3CkCIMAAVKIgsARntswAAAAASUVORK5CYII=" );
            closeButtonStyle.hover.background = Assets.B64( "closeButtonStyleHover",
                @"iVBORw0KGgoAAAANSUhEUgAAAA0AAAANCAYAAABy6+R8AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA4RpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuNS1jMDIxIDc5LjE1NTc3MiwgMjAxNC8wMS8xMy0xOTo0NDowMCAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo4MWRjMDQyMi01ZGJmLTQ0NDgtODVhNC1iN2IzYzJhM2IzNmQiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6QzdBQkJERjMyOENCMTFFNUJCNENBMzIyQUY5MDU1MDciIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6QzdBQkJERjIyOENCMTFFNUJCNENBMzIyQUY5MDU1MDciIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENDIDIwMTQgKFdpbmRvd3MpIj4gPHhtcE1NOkRlcml2ZWRGcm9tIHN0UmVmOmluc3RhbmNlSUQ9InhtcC5paWQ6OTg1NTdmYTctNDZkMi00YzRiLWJkMmYtNjJhZTVmNzBkNjhkIiBzdFJlZjpkb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6YTc2NmFhMjQtMjhjYS0xMWU1LTg1MTgtYjk3OWUxOThhMWQ0Ii8+IDwvcmRmOkRlc2NyaXB0aW9uPiA8L3JkZjpSREY+IDwveDp4bXBtZXRhPiA8P3hwYWNrZXQgZW5kPSJyIj8+oeLN7AAAAK1JREFUeNpi/P//PwOpgAVEFBYWegKpuUAsiUftcyBO7u/v384EFQBpCANiRjw4DKoOYhPUhiPS0tJNUH5dSUkJmNHT0wMWe/r0aR3MJSxYnFELVQxS1ATlN2P4CQnUIWtE0lCHTxO6RgwNIMCEzIH6A9lJtVA+A06boJ5Gt6EWzQVYnYesoQ5n5EIjzgYarLj8aA3EL5A1pQDxaiCWwJMinoJSBIjBSE7aAwgwACXrMflUdZGBAAAAAElFTkSuQmCC" );

            maximizeButtonStyle = new GUIStyle();
            maximizeButtonStyle.normal.background = Assets.B64( "maximizeButtonStyleNormal",
                @"iVBORw0KGgoAAAANSUhEUgAAAA0AAAANCAYAAABy6+R8AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA3ZpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuNS1jMDIxIDc5LjE1NTc3MiwgMjAxNC8wMS8xMy0xOTo0NDowMCAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo4MWRjMDQyMi01ZGJmLTQ0NDgtODVhNC1iN2IzYzJhM2IzNmQiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6QjNFN0VFOTEyOEM2MTFFNUE5MDFENTY1N0FFMzcyMjciIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6QjNFN0VFOTAyOEM2MTFFNUE5MDFENTY1N0FFMzcyMjciIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENDIDIwMTQgKFdpbmRvd3MpIj4gPHhtcE1NOkRlcml2ZWRGcm9tIHN0UmVmOmluc3RhbmNlSUQ9InhtcC5paWQ6ZTFiZmY3ZTAtNjNlNy05MDRkLTgxMmUtZTYzZTBhODhmNzQyIiBzdFJlZjpkb2N1bWVudElEPSJ4bXAuZGlkOjgxZGMwNDIyLTVkYmYtNDQ0OC04NWE0LWI3YjNjMmEzYjM2ZCIvPiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/Pubx6yMAAAA2SURBVHjaYvz//z8DqYCJgQxAP00s6AI9PT0YniwpKWGkvk3oJmOzmXo2YTMdGTAOw8gFCDAAh1MQvM4df+EAAAAASUVORK5CYII=" );
            maximizeButtonStyle.hover.background = Assets.B64( "maximizeButtonStyleHover",
                @"iVBORw0KGgoAAAANSUhEUgAAAA0AAAANCAYAAABy6+R8AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA3ZpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuNS1jMDIxIDc5LjE1NTc3MiwgMjAxNC8wMS8xMy0xOTo0NDowMCAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo4MWRjMDQyMi01ZGJmLTQ0NDgtODVhNC1iN2IzYzJhM2IzNmQiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6Nzg3RUY3QUQyOENBMTFFNUJENkJEQjZFQzlDOEIyOUUiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6Nzg3RUY3QUMyOENBMTFFNUJENkJEQjZFQzlDOEIyOUUiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENDIDIwMTQgKFdpbmRvd3MpIj4gPHhtcE1NOkRlcml2ZWRGcm9tIHN0UmVmOmluc3RhbmNlSUQ9InhtcC5paWQ6NGMxNjZhNWEtNjU4MS0xMzQzLWFlYTQtMTExMDQ4MWUyNWVkIiBzdFJlZjpkb2N1bWVudElEPSJ4bXAuZGlkOjgxZGMwNDIyLTVkYmYtNDQ0OC04NWE0LWI3YjNjMmEzYjM2ZCIvPiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/PqdYt6kAAACKSURBVHjaYvz//z8DqYAFRBQWFnoCqblALIlH7XMgTu7v79/OBBUAaQgDYkY8OAyqjgGmCWTDEQKuOgJzCQu6jLS0NIYnnz59yojMZ2IgA7BgEywpKYGb3NPTg2Ez9WzCZjpeTeiexgaYkCLOhoBaayB+gWxTChCvBmIJPJqeglIEiMFITtoDCDAAOuwhzlHH0qQAAAAASUVORK5CYII=" );
        }

        public void ShowNotification( string text ) {
            AddControl( new ExtendedNotification( text, false ) );
        }

        public void ShowNotificationError( string text ) {
            AddControl( new ExtendedNotification( text, true ) );
        }

        public void ShowPopup( ExtendedPopup popup ) {
            Editor.ShowPopup( popup );
        }

        public void RemovePopup() {
            Editor.RemovePopup();
        }

        public void AddWindow( ExtendedWindow window ) {
            Editor.AddWindow( window );
        }

        public void RemoveWindow( ExtendedWindow window ) {
            Editor.RemoveWindow( window );
        }

        public ExtendedWindow GetWindow( Type type ) {
            return Editor.GetWindowByType( type );
        }

        public T GetWindow<T>() where T : ExtendedWindow {
            return Editor.GetWindowByType<T>();
        }

        public List<ExtendedWindow> GetWindows( Type type ) {
            return Editor.GetWindowsByType( type );
        }

        public List<T> GetWindows<T>() where T : ExtendedWindow {
            return Editor.GetWindowsByType<T>();
        }

        public void AddControl( ExtendedControl control ) {
            control.Window = this;

            rData.Initialize.Invoke( control, null );
            controls.Add( control );
            AddControlGrouped( control );
            ShouldSortControls = true;
        }

        public void RemoveControl( ExtendedControl control ) {
            rData.Destroy.Invoke( control, null );
            controls.Remove( control );
            RemoveControlGrouped( control );
        }

        private void AddControlGrouped( ExtendedControl control, Type wType = null ) {
            if ( control == null ) return;

            if ( wType == null ) {
                wType = control.GetType();
            }

            if ( !controlsGrouped.ContainsKey( wType ) ) {
                controlsGrouped.Add( wType, new List<ExtendedControl>() );
            }

            controlsGrouped[wType].Add( control );

            if ( wType.BaseType != null ) {
                AddControlGrouped( control, wType.BaseType );
            }
        }

        private void RemoveControlGrouped( ExtendedControl control, Type wType = null ) {
            if ( control == null ) return;

            if ( wType == null ) {
                wType = control.GetType();
            }

            if ( !controlsGrouped.ContainsKey( wType ) ) {
                return;
            }

            controlsGrouped[wType].Remove( control );

            if ( wType.BaseType != null ) {
                RemoveControlGrouped( control, wType.BaseType );
            }
        }

        public ExtendedControl GetControl( string id ) {
            return controls.Where( c => c.ID == id ).FirstOrDefault();
        }

        public T GetControl<T>( string id ) where T : ExtendedControl {
            return (T)controls.Where( c => c.ID == id ).FirstOrDefault();
        }

        public ExtendedControl GetControl( Type type ) {
            if ( controlsGrouped.ContainsKey( type ) ) {
                return controlsGrouped[type].FirstOrDefault();
            } else {
                return null;
            }
        }

        public T GetControl<T>() where T : ExtendedControl {
            var type = typeof( T );
            if ( controlsGrouped.ContainsKey( type ) ) {
                return (T)controlsGrouped[type].FirstOrDefault();
            } else {
                return null;
            }
        }

        public List<ExtendedControl> GetControls( Type type ) {
            if ( controlsGrouped.ContainsKey( type ) ) {
                return controlsGrouped[type];
            } else {
                return new List<ExtendedControl>();
            }
        }

        public List<T> GetControls<T>() where T : ExtendedControl {
            var type = typeof( T );
            if ( controlsGrouped.ContainsKey( type ) ) {
                return controlsGrouped[type].Cast<T>().ToList();
            } else {
                return new List<T>();
            }
        }

        public int GetControlID() {
            return controlID++;
        }

        public void Repaint() {
            Editor.Repaint();
        }

        public void Remove() {
            Editor.RemoveWindow( this );
        }

        public void RunOnGUIThread( Action action ) {
            guiActions.Add( action );
        }

        public void RunOnGUIThreadImmediate( Action action ) {
            guiActions.Add( action );
            Editor.Repaint();
        }

        public static ExtendedEditor CreateEditor() {
            return CreateEditor( "" );
        }

        public static ExtendedEditor CreateEditor( string title ) {
            var stack = new System.Diagnostics.StackTrace();
            if ( stack.FrameCount >= 1 ) {
                var mBase = stack.GetFrame( 1 ).GetMethod();
                var type = mBase.DeclaringType;
                var instance = (ExtendedWindow)Activator.CreateInstance( type );
                return ExtendedEditor.CreateEditor( title, instance );
            } else {
                throw new Exception( "Unable to create editor" );
            }
        }

        public void SortControls() {
            controls = controls.OrderBy( c => c.SortingOrder ).ToList();
        }
    }
}
#endif