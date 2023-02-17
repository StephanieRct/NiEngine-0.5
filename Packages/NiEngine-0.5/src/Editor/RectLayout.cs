using UnityEngine;
using UnityEditor;

namespace Nie.Editor
{
    /// <summary>
    /// GUI horizontal layout within a given Rect
    /// </summary>
    public struct RectLayout
    {
        public Rect OriginalRect;
        public Rect FreeRect;

        public RectLayout(Rect rect)
        {
            OriginalRect = rect;
            FreeRect = rect;
        }

        public void Label(string text)
        {
            var content = new GUIContent(text);
            var size = GUI.skin.box.CalcSize(content);

            Rect r = new Rect(FreeRect.xMin, OriginalRect.yMin, size.x, size.y);

            EditorGUI.LabelField(r, content, new GUIContent(text));
            FreeRect.xMin += r.width;
        }

        public void PropertyField(SerializedProperty property, float width)
        {
            Rect r = new Rect(FreeRect.xMin, OriginalRect.yMin, width, OriginalRect.height);

            EditorGUI.PropertyField(r, property, GUIContent.none);
            FreeRect.xMin += r.width;

        }
        public bool Button(string caption)
        {

            var content = new GUIContent(caption);
            var size = GUI.skin.box.CalcSize(content);

            Rect r = new Rect(FreeRect.xMin, OriginalRect.yMin, size.x + 4, size.y);

            return GUI.Button(r, content);
        }
    }
}