﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TNRD.Editor;
using TNRD.Editor.Core;
using TNRD.Editor.Serialization;
using UnityEditor;
using UnityEngine;

namespace TNRD.Automatron {

    public class Automation : ExtendedControl {

        private static Automation dragger = null;

        private string name;
        private GUIStyle headerStyle;

        private List<AutomationField> fields = new List<AutomationField>();
        private Dictionary<string, AutomationField> sortedFields = new Dictionary<string, AutomationField>();

        [RequireSerialization]
        protected bool showCloseButton = true;
        [RequireSerialization]
        protected bool showOutArrow = true;
        [RequireSerialization]
        protected bool showInArrow = true;

        public float Progress;

        public AutomationLine LineIn;
        public AutomationLine LineOut;

        protected override void OnInitialize() {
            Size = new Vector2( 250, 300 );

            AnchorPoint = EAnchor.TopLeft;
            SortingOrder = ESortingOrder.Automation;

            name = ( GetType().GetCustomAttributes( typeof( AutomationAttribute ), false )[0] as AutomationAttribute ).Name;
            GetFields();
            UpdateSize();
            RunOnGUIThread( CreateStyles );
        }

        protected override void OnAfterSerialize() {
            name = ( GetType().GetCustomAttributes( typeof( AutomationAttribute ), false )[0] as AutomationAttribute ).Name;
            GetFields();
            UpdateSize();
            RunOnGUIThread( CreateStyles );
        }

        protected override void OnDestroy() {
            foreach ( var item in fields ) {
                if ( item.LineIn != null ) {
                    item.LineIn.Remove();
                }

                for ( int i = item.LinesOut.Count - 1; i >= 0; i-- ) {
                    item.LinesOut[i].Remove();
                }
            }
        }

        private void GetFields() {
            var type = GetType();
            var infos = type.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly );
            for ( int i = 0; i < infos.Length; i++ ) {
                var field = infos[i];
                var id = string.Format( "{0}_{1}_{2}", ID, field.Name, i );
                var instance = new AutomationField( this, field, id );
                fields.Add( instance );
                sortedFields.Add( id, instance );
            }
        }

        private void UpdateSize() {
            float height = 24;
            foreach ( var item in fields ) {
                height += item.GetHeight();
            }
            Size.y = Mathf.Max( height, 34 );
        }

        private void CreateStyles() {
            headerStyle = new GUIStyle( EditorStyles.label );
            headerStyle.alignment = TextAnchor.MiddleCenter;
        }

        protected override void OnGUI() {
            var rect = Rectangle;

            GUI.Box( rect, "", ExtendedGUI.DefaultWindowStyle );
            GUI.Label( new Rect( rect.x, rect.y, rect.width, 16 ), name, headerStyle );

            if ( showCloseButton ) {
                if ( GUI.Button( new Rect( rect.x + rect.width - 20, rect.y, 20, 16 ), "X", EditorStyles.toolbar ) ) {
                    Remove();
                }
            }

            var lArrow = new Rect( rect.x - 16, rect.y, 16, 17 );
            var rArrow = new Rect( rect.x + rect.width, rect.y, 16, 17 );

            if ( showInArrow ) {
                GUI.DrawTexture( lArrow, Assets["toparrowleft"] );

                if ( Input.ButtonReleased( EMouseButton.Left ) && lArrow.Contains( Input.MousePosition ) ) {
                    if ( LineIn != null ) {
                        LineIn.Remove();
                        LineIn = null;
                    }

                    LineIn = AutomationLine.HookLineIn( this );
                    Window.AddControl( LineIn );
                    Input.Use();
                }
            }

            if ( showOutArrow ) {
                GUI.DrawTexture( rArrow, Assets["toparrowright"] );

                if ( Input.ButtonReleased( EMouseButton.Left ) && rArrow.Contains( Input.MousePosition ) ) {
                    if ( LineOut != null ) {
                        LineOut.Remove();
                        LineOut = null;
                    }

                    LineOut = AutomationLine.HookLineOut( this );
                    Window.AddControl( LineOut );
                    Input.Use();
                }
            }

            if ( Input.ButtonPressed( EMouseButton.Left ) ) {
                dragger = null;

                switch ( SortingOrder ) {
                    case ESortingOrder.Automation:
                        if ( rect.Contains( Input.MousePosition ) ) {
                            SortingOrder = ESortingOrder.AutomationSelected;
                        }
                        break;
                    case ESortingOrder.AutomationSelected:
                        if ( !rect.Contains( Input.MousePosition ) ) {
                            SortingOrder = ESortingOrder.Automation;
                        }
                        break;
                }
            }

            if ( Input.ButtonDown( EMouseButton.Left ) ) {
                if ( dragger == null ) {
                    var dragRect = new Rect( rect.x, rect.y, rect.width, 16 );
                    if ( dragRect.Contains( Input.MousePosition ) ) {
                        dragger = this;
                        GUIUtility.keyboardControl = 0;
                    }
                }
            }

            if ( dragger == this ) {
                Position += Input.DragDelta;
            }

            var fieldRect = new Rect( rect.x, rect.y + 18, rect.width, rect.height );
            foreach ( var item in fields ) {
                var height = item.GetHeight();
                fieldRect.height = height;
                item.OnGUI( fieldRect );
                fieldRect.y += height;
            }

            UpdateSize();
        }

        public bool HasField( string id ) {
            return sortedFields.ContainsKey( id );
        }

        public AutomationField GetField( string id ) {
            return sortedFields[id];
        }

        public virtual IEnumerator Execute() {
            yield break;
        }
    }
}