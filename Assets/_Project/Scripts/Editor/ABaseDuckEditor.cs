using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using Unity.Netcode;

// [CustomEditor(typeof(ABaseDuck))]
// public class ABaseDuckEditor : Editor {
//     public override void OnInspectorGUI() {
//         ABaseDuck duck = (ABaseDuck)target;

//         EditorGUILayout.LabelField("NetworkVariable Debugger", EditorStyles.boldLabel);
//         if (Application.isPlaying && target.GetComponent<NetworkObject>().IsSpawned)
//         {
//             EditorGUILayout.LabelField("HP", duck.CurrentHP.Value.ToString());
//             EditorGUILayout.LabelField("Mane", duck.CurrentStamina.Value.ToString());
//             EditorGUILayout.LabelField("IsFlying", duck.IsFlying.Value.ToString());
//             EditorGUILayout.LabelField("CanJump", duck.CanJump.Value.ToString());
//         }
//         else
//         {
//             EditorGUILayout.HelpBox("NetworkObject is not spawned yet", MessageType.Info);
//         }
//         base.OnInspectorGUI();
//     }
// }