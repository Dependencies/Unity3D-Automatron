#if UNITY_EDITOR
﻿using System.Collections.Generic;
using System.IO;
using TNRD.Automatron.Editor.Serialization;
using UnityEngine;

namespace TNRD.Automatron.Editor.Core {

    public class ExtendedAssets {

        [IgnoreSerialization]
        private Dictionary<string, Texture2D> textures;

        public string Path;

        public Texture2D this[string key] {
            get {
                if ( textures.ContainsKey( key ) ) {
                    return textures[key];
                } else {
                    return Load( key );
                }
            }
        }

        public ExtendedAssets() {
            textures = new Dictionary<string, Texture2D>();
        }

        public void Initialize( string path ) {
            if ( !string.IsNullOrEmpty( path ) ) {
                Path = path;
            } else {
                var stack = new System.Diagnostics.StackTrace( true );
                if ( stack.FrameCount > 0 ) {
                    var frame = stack.GetFrame( stack.FrameCount - 1 );
                    var fname = System.IO.Path.GetFileName( frame.GetFileName() );

                    Path = frame.GetFileName().Replace( '\\', '/' );
                    Path = Path.Replace( fname, "Assets/" );
                }
            }
        }

        public Texture2D Load( string key ) {
            if ( textures.ContainsKey( key ) ) {
                return textures[key];
            }

            var fpath = System.IO.Path.Combine( Path, key + ".png" );
            if ( !File.Exists( fpath ) ) {
                return null;
            }

            var tex = new Texture2D( 1, 1 );
            tex.hideFlags = HideFlags.HideAndDontSave;

            var bytes = File.ReadAllBytes( fpath );
            tex.LoadImage( bytes );

            textures.Add( key, tex );
            return textures[key];
        }

        public Texture2D Load( string key, string b64 ) {
            if ( textures.ContainsKey( key ) ) {
                return textures[key];
            }

            var tex = new Texture2D( 1, 1 );
            tex.hideFlags = HideFlags.HideAndDontSave;

            var bytes = System.Convert.FromBase64String( b64 );
            tex.LoadImage( bytes );

            textures.Add( key, tex );
            return textures[key];
        }

        public bool HasKey( string key ) {
            return textures.ContainsKey( key );
        }
    }
}
#endif