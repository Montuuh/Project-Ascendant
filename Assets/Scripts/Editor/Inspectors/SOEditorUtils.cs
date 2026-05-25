using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ProjectAscendant.Editor
{
    // Per Task 3.5.3 — Shared utilities for Project Ascendant custom SO inspectors.
    // Provides the GDD Reference footer used by all editors (specific + generic).
    public static class SOEditorUtils
    {
        private static readonly Color SeparatorColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        // ── GDD Reference footer ───────────────────────────────────────────
        // Call at the bottom of any OnInspectorGUI after DrawDefaultInspector().
        // Locates the GDDReference string field by reflection so callers don't
        // need to know the field name explicitly.
        public static void DrawGDDFooter(UnityEngine.Object target)
        {
            FieldInfo field = target.GetType().GetField("GDDReference",
                BindingFlags.Public | BindingFlags.Instance);

            if (field == null) return;

            DrawSeparator();

            string value  = field.GetValue(target) as string;
            bool   isEmpty = string.IsNullOrWhiteSpace(value);

            if (isEmpty)
            {
                EditorGUILayout.HelpBox(
                    "⚠ GDDReference is empty. Add the §N.N.N section that specifies " +
                    "this asset (per data-assets.md — bidirectional link rule).",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"GDD: {value}", MessageType.None);
                if (GUILayout.Button("Copy", GUILayout.Width(50), GUILayout.Height(36)))
                {
                    GUIUtility.systemCopyBuffer = value;
                    Debug.Log($"Copied GDD reference: {value}");
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ── Generic helpers ────────────────────────────────────────────────
        public static void DrawSeparator()
        {
            EditorGUILayout.Space(2);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, SeparatorColor);
            EditorGUILayout.Space(2);
        }
    }
}
