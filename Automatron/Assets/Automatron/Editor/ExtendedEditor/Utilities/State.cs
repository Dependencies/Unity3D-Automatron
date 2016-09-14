﻿namespace TNRD.Automatron.Editor.Utilities {

    public class State<T> {

        public T Current;
        public T Previous;

        public State() { }

        public State( T current ) {
            Current = current;
        }

        public State( T current, T previous ) {
            Current = current;
            Previous = previous;
        }

        public void Update( T state ) {
            Previous = Current;
            Current = state;
        }

        public void Update() {
            Previous = Current;
        }

        public static bool operator true( State<T> v ) {
            return v.Current.Equals( true );
        }

        public static bool operator false( State<T> v ) {
            return v.Current.Equals( false );
        }

        public static bool operator !( State<T> v ) {
            return v.Current.Equals( false );
        }

        public bool IsPressed() {
            return Current.Equals( true ) && Previous.Equals( false );
        }

        public bool IsReleased() {
            return Current.Equals( false ) && Previous.Equals( true );
        }

        public bool IsDown() {
            return Current.Equals( true );
        }

        public bool IsUp() {
            return Current.Equals( false );
        }
    }
}