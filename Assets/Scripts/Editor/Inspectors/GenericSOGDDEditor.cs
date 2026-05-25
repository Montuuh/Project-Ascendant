using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ProjectAscendant.Editor
{
    // Per Task 3.5.3 — Generic ScriptableObject inspector that appends a GDD Reference
    // validation footer to every SO that does NOT have a more-specific custom editor.
    //
    // Unity editor priority: a [CustomEditor(typeof(ConcreteType))] always wins over
    // this fallback (editorForChildClasses: true). So PokemonSpeciesSOEditor and
    // MoveSOEditor handle their types; this covers everything else automatically.
    //
    // All SOs that inherit from ScriptableObject and are NOT already handled by a
    // specific editor will show: default fields + GDD reference footer.
    [CustomEditor(typeof(ScriptableObject), editorForChildClasses: true)]
    public class GenericSOGDDEditor : UnityEditor.Editor
    {
        // Cache: only search for the field once per editor lifetime.
        private bool _searched;
        private bool _hasGDDField;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!_searched)
            {
                _hasGDDField = target.GetType()
                    .GetField("GDDReference", BindingFlags.Public | BindingFlags.Instance) != null;
                _searched = true;
            }

            if (_hasGDDField)
                SOEditorUtils.DrawGDDFooter(target);
        }
    }
}
