using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Nie.Editor
{
    [CustomPropertyDrawer(typeof(CollisionFXPair))]
    public class CollisionFXPairPropertyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var layout = new RectLayout(position);
            layout.Label("A:");
            layout.PropertyField(property.FindPropertyRelative("MaterialA"), 100);
            layout.Label("B:");
            layout.PropertyField(property.FindPropertyRelative("MaterialB"), 100);
            layout.Label("Sound:");
            var propSFX = property.FindPropertyRelative("Sound");
            layout.PropertyField(propSFX, 200);
            if (layout.Button("Play"))
            {
                if (propSFX.objectReferenceValue is SoundFX sfx)
                {
                    sfx.PlayInEditor();
                }
            }
            EditorGUI.EndProperty();
        }
    }


    [CustomPropertyDrawer(typeof(AnimatorStateReference))]
    public class AnimatorStateReferencePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var propAnimator = property.FindPropertyRelative("Animator");
            if (propAnimator.objectReferenceValue is not null)
                return RectLayout.MinHeight * 2 + 4;
            return RectLayout.MinHeight + 4;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var layout = RectLayout.Horizontal(position);
            layout.PrefixLabel(label);
            layout = layout.SubVertical();
            var propAnimator = property.FindPropertyRelative("Animator");
            var animator = propAnimator.objectReferenceValue as Animator;
            layout.PropertyField(propAnimator);
            if (animator != null)
            {
                layout = layout.SubHorizontal();
                layout.Label("State:");
                var propStateName = property.FindPropertyRelative("State");
                layout.PropertyField(propStateName, -RectLayout.WidthOf("Missing"));
                
                var stateHash = Animator.StringToHash(propStateName.stringValue);
                if (animator.HasState(0, stateHash))
                {
                    property.FindPropertyRelative("StateHash").intValue = stateHash;
                    layout.Label("Found");
                }
                else
                {
                    layout.Label("Missing");
                }
            }
            
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ReactionStateReference))]
    public class ReactionStateReferencePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var propObject = property.FindPropertyRelative("Object");
            if (propObject.objectReferenceValue is not null)
                return RectLayout.MinHeight * 2 + 4;
            return RectLayout.MinHeight + 4;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var layout = RectLayout.Horizontal(position);
            layout.PrefixLabel(label);
            layout = layout.SubVertical();
            var propObject = property.FindPropertyRelative("Object");
            var obj = propObject.objectReferenceValue as ReactionState;
            layout.PropertyField(propObject);
            if (obj != null)
            {
                layout = layout.SubHorizontal();
                layout.Label("State:");
                var propStateName = property.FindPropertyRelative("StateName");
                layout.PropertyField(propStateName, -RectLayout.WidthOf("Missing"));

                if (obj.gameObject.GetComponents<ReactionState>().Any(x => x.StateName == propStateName.stringValue))
                {
                    layout.Label("Found");
                }
                else
                {
                    layout.Label("Missing");
                }
            }

            EditorGUI.EndProperty();
        }
    }
}