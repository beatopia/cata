using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AnimationClipCreator : EditorWindow
{
    private AnimatorController sourceController;
    private string newCharacterName = "";
    private string savePath = "Assets/Characters/Player/Skins/";
    private Object sourceAnimationFolder;

    [MenuItem("Tools/Animation Clip Creator")]
    public static void ShowWindow()
    {
        GetWindow<AnimationClipCreator>("Animation Clip Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Create New Character Animation Clips", EditorStyles.boldLabel);

        sourceController = (AnimatorController)EditorGUILayout.ObjectField("Source Animator Controller", sourceController, typeof(AnimatorController), false);
        newCharacterName = EditorGUILayout.TextField("New Character Name", newCharacterName);
        sourceAnimationFolder = EditorGUILayout.ObjectField("Source Animation Folder", sourceAnimationFolder, typeof(Object), false);

        if (GUILayout.Button("Create Animation Clips"))
        {
            if (sourceController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a source Animator Controller", "OK");
                return;
            }

            if (string.IsNullOrEmpty(newCharacterName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a character name", "OK");
                return;
            }

            if (sourceAnimationFolder == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select the source animation folder", "OK");
                return;
            }

            CreateAnimationClips();
        }
    }

    private void CreateAnimationClips()
    {
        // Create character folder if it doesn't exist
        string characterFolder = Path.Combine(savePath, newCharacterName);
        if (!Directory.Exists(characterFolder))
        {
            Directory.CreateDirectory(characterFolder);
        }

        // Create animation clips folder
        string animFolder = Path.Combine(characterFolder, "Animations");
        if (!Directory.Exists(animFolder))
        {
            Directory.CreateDirectory(animFolder);
        }

        // Get all animation clips from the source controller
        foreach (var layer in sourceController.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.motion is AnimationClip sourceClip)
                {
                    // Create a new animation clip
                    AnimationClip newClip = new AnimationClip();
                    newClip.name = sourceClip.name;

                    // Copy all curves from the source clip
                    foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip))
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                        AnimationUtility.SetEditorCurve(newClip, binding, curve);
                    }

                    // Copy all events from the source clip
                    AnimationEvent[] events = AnimationUtility.GetAnimationEvents(sourceClip);
                    AnimationUtility.SetAnimationEvents(newClip, events);

                    // Save the new clip
                    string clipPath = Path.Combine(animFolder, newClip.name + ".anim");
                    AssetDatabase.CreateAsset(newClip, clipPath);
                }
            }
        }

        // Create Animator Override Controller
        AnimatorOverrideController overrideController = new AnimatorOverrideController(sourceController);
        string overridePath = Path.Combine(characterFolder, newCharacterName + "Override.controller");
        AssetDatabase.CreateAsset(overrideController, overridePath);

        // Refresh the asset database
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"Created animation clips for {newCharacterName} in {animFolder}", "OK");
    }
} 