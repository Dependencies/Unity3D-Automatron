#if UNITY_EDITOR
﻿using UnityEditor;
using UnityEngine;

namespace TNRD.Automatron.Drawers {

    [CustomDrawer( typeof( Quaternion ) )]
    public class QuaternionDrawer : AutomationDrawer {

        public override void OnGUI( Rect rect, string name, ref object value ) {
            base.OnGUI( rect, name, ref value );

            EditorGUI.BeginDisabledGroup( IsReadOnly );

            var quat = (Quaternion)value;
            var vec = new Vector4( quat.x, quat.y, quat.z, quat.w );

            EditorGUI.HandlePrefixLabel( rect, GetControlRect(), new GUIContent( name ) );
            vec = EditorGUI.Vector4Field( GetControlRect(), "", vec );

            value = new Quaternion( vec.x, vec.y, vec.z, vec.w );
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif