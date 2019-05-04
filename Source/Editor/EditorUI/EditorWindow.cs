using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame.Editor
{
    public class EditorWindow : DisposeBase
    {
        static List<EditorWindow> windowList_ = new List<EditorWindow>();

        public static T GetWindow<T>() where T : EditorWindow, new()
        {
            foreach(EditorWindow w in windowList_)
            {
                if(w.GetType() == typeof(T))
                {
                    return w as T;
                }
            }

            T ret = new T();
            return ret;
        }

        protected EditorWindow()
        {
            windowList_.Add(this);
        }

        protected virtual void Draw()
        {

        }

        internal static void OnGUI()
        {
            foreach(EditorWindow window in windowList_)
            {
                window.Draw();
            }
        }

        protected override void Destroy()
        {
            windowList_.Remove(this);
        }
    }
}
