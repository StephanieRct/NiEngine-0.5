using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

namespace Nie.Editor
{
    /// <summary>
    /// GUI horizontal layout within a given Rect
    /// </summary>
    public struct RectLayout
    {
        public Rect OriginalRect;
        public Rect FreeRect;
        public bool IsHorizontal;
        public bool IsVertical => !IsHorizontal;
        public RectLayout(Rect rect, bool horizontal = true)
        {
            OriginalRect = rect;
            FreeRect = rect;
            IsHorizontal = horizontal;
        }
        static public RectLayout Horizontal(Rect rect) => new RectLayout(rect);
        static public RectLayout Vertical(Rect rect) => new RectLayout(rect, false);

        public RectLayout SubHorizontal(float size) => new RectLayout(Acquire(size), true);
        public RectLayout SubVertical(float size) => new RectLayout(Acquire(size), false);

        public RectLayout SubHorizontal() => new RectLayout(Acquire(FreeRect.width, FreeRect.height), true);
        public RectLayout SubVertical() => new RectLayout(Acquire(FreeRect.width, FreeRect.height), false);
        //public Rect Acquire(float size) => IsHorizontal ? Acquire(size, OriginalRect.height) : Acquire(OriginalRect.width, size);
        public static float WidthOf(string text) => GUI.skin.box.CalcSize(new GUIContent(text)).x;
        public static float MinHeight => GUI.skin.box.CalcSize(new GUIContent("Fj")).y;
        public Rect Acquire(float width, float height)
        {
            if (IsHorizontal)
            {
                if (width < 0)
                    width = FreeRect.width + width;
                Rect r = new Rect(FreeRect.xMin, OriginalRect.yMin, width, height);
                FreeRect.xMin += r.width;
                return r;
            }
            else
            {
                height = math.max(height, MinHeight);
                Rect r = new Rect(OriginalRect.xMin, FreeRect.yMin, width, height);
                FreeRect.yMin += r.height;
                return r;
            }
        }
        public Rect Acquire(float width)
        {
            if (IsHorizontal)
            {
                return Acquire(width, FreeRect.height);
            }
            else
            {
                return Acquire(FreeRect.width, MinHeight);
            }
        }
        public Rect Acquire()
        {
            if (IsHorizontal)
            {
                return Acquire(FreeRect.width, FreeRect.height);
            }
            else
            {
                return Acquire(FreeRect.width, MinHeight);
            }
        }
        public void Label(string text)
        {
            var content = new GUIContent(text);
            var size = GUI.skin.box.CalcSize(content);
            EditorGUI.LabelField(Acquire(size.x), content, new GUIContent(text));
        }
        public void Box(string text)
        {
            GUI.Box(Acquire(), text);
        }
        public void Box(string text, float width)
        {
            GUI.Box(Acquire(width), text);
        }

        public void PropertyField(SerializedProperty property, float width = 100)
        {
            EditorGUI.PropertyField(Acquire(width), property, GUIContent.none);

        }
        public void PropertyField(SerializedProperty property)
        {
            EditorGUI.PropertyField(Acquire(), property, GUIContent.none);

        }
        public bool Button(string caption)
        {
            var content = new GUIContent(caption);
            var size = GUI.skin.box.CalcSize(content);
            return GUI.Button(Acquire(size.x + 4, math.min(size.y + 4, OriginalRect.height)), content);
        }
        public void PrefixLabel(GUIContent label)
        {
            var size = GUI.skin.box.CalcSize(label);
            var r = EditorGUI.PrefixLabel(Acquire(size.x, size.y), label);
            FreeRect.xMin = r.xMin;
            OriginalRect = FreeRect;
        }
    }

}