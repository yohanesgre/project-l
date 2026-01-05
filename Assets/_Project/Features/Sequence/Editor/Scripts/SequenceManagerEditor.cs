using UnityEditor;
using UnityEngine;
using MyGame.Features.Sequence;

namespace MyGame.Features.Sequence.Editor
{
    [CustomEditor(typeof(SequenceManager))]
    public class SequenceManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var manager = (SequenceManager)target;

            GUILayout.Space(10);
            GUILayout.Label("Runtime Controls", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                if (manager.Sequences != null && manager.Sequences.Count > 0)
                {
                    for (int i = 0; i < manager.Sequences.Count; i++)
                    {
                        var seq = manager.Sequences[i];
                        if (seq == null) continue;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"[{i}] {seq.name}", GUILayout.Width(200));
                        if (GUILayout.Button("Play Now"))
                        {
                            manager.PlaySequenceAtIndex(i);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.Label("No sequences assigned in list.");
                }

                GUILayout.Space(5);
                if (GUILayout.Button("Stop Sequence", GUILayout.Height(30)))
                {
                    manager.StopSequence();
                }

                GUILayout.Space(5);
                if (GUILayout.Button("Trigger Next Action", GUILayout.Height(30)))
                {
                    manager.TriggerNextAction();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run sequences.", MessageType.Info);
            }
        }
    }
}
