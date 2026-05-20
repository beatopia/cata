using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AnimatorControllerCopier : EditorWindow
{
    private AnimatorController sourceController;
    private AnimatorController targetController;
    private string newControllerName = "NewController";
    private string savePath = "Assets/Characters/Player/Skins/";

    [MenuItem("Tools/Animator Controller Copier")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorControllerCopier>("Animator Controller Copier");
    }

    void OnGUI()
    {
        GUILayout.Label("Animator Controller Copier", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        sourceController = (AnimatorController)EditorGUILayout.ObjectField("Source Controller", sourceController, typeof(AnimatorController), false);
        newControllerName = EditorGUILayout.TextField("New Controller Name", newControllerName);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create New Controller"))
        {
            if (sourceController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a source controller", "OK");
                return;
            }

            if (string.IsNullOrEmpty(newControllerName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a name for the new controller", "OK");
                return;
            }

            CreateNewController();
        }
    }

    private void CreateNewController()
    {
        // Create the new controller
        string fullPath = Path.Combine(savePath, newControllerName + ".controller");
        targetController = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        // Create a folder for the new animations
        string animFolderPath = Path.Combine(savePath, newControllerName + "_Animations");
        if (!Directory.Exists(animFolderPath))
        {
            Directory.CreateDirectory(animFolderPath);
        }

        // Copy parameters
        foreach (AnimatorControllerParameter param in sourceController.parameters)
        {
            targetController.AddParameter(param.name, param.type);
        }

        // Copy layers
        for (int i = 0; i < sourceController.layers.Length; i++)
        {
            AnimatorControllerLayer sourceLayer = sourceController.layers[i];
            AnimatorControllerLayer newLayer = new AnimatorControllerLayer
            {
                name = sourceLayer.name,
                defaultWeight = sourceLayer.defaultWeight,
                avatarMask = sourceLayer.avatarMask,
                iKPass = sourceLayer.iKPass,
                syncedLayerIndex = sourceLayer.syncedLayerIndex,
                syncedLayerAffectsTiming = sourceLayer.syncedLayerAffectsTiming,
                stateMachine = new AnimatorStateMachine()
            };

            // Copy state machine
            CopyStateMachine(sourceLayer.stateMachine, newLayer.stateMachine, animFolderPath);

            // Add the layer to the controller
            targetController.AddLayer(newLayer);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", "New controller created successfully!", "OK");
    }

    private void CopyStateMachine(AnimatorStateMachine source, AnimatorStateMachine target, string animFolderPath)
    {
        // Copy states
        foreach (ChildAnimatorState sourceState in source.states)
        {
            AnimatorState newState = target.AddState(sourceState.state.name, sourceState.position);
            
            // Create a new animation clip for this state
            if (sourceState.state.motion != null)
            {
                AnimationClip sourceClip = sourceState.state.motion as AnimationClip;
                if (sourceClip != null)
                {
                    string newClipPath = Path.Combine(animFolderPath, newControllerName + "_" + sourceState.state.name + ".anim");
                    AnimationClip newClip = new AnimationClip();
                    
                    // Copy all curves from the source clip
                    foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                        AnimationUtility.SetEditorCurve(newClip, binding, curve);
                    }
                    
                    // Copy all events
                    AnimationEvent[] events = AnimationUtility.GetAnimationEvents(sourceClip);
                    AnimationUtility.SetAnimationEvents(newClip, events);
                    
                    // Save the new clip
                    AssetDatabase.CreateAsset(newClip, newClipPath);
                    newState.motion = newClip;
                }
            }
            
            newState.speed = sourceState.state.speed;
            newState.mirror = sourceState.state.mirror;
            newState.tag = sourceState.state.tag;
            newState.writeDefaultValues = sourceState.state.writeDefaultValues;
        }

        // Copy transitions
        foreach (AnimatorStateTransition transition in source.anyStateTransitions)
        {
            AnimatorStateTransition newTransition = target.AddAnyStateTransition(GetStateByName(target, transition.destinationState.name));
            CopyTransition(transition, newTransition);
        }

        foreach (ChildAnimatorState sourceState in source.states)
        {
            foreach (AnimatorStateTransition transition in sourceState.state.transitions)
            {
                AnimatorStateTransition newTransition = GetStateByName(target, sourceState.state.name).AddTransition(GetStateByName(target, transition.destinationState.name));
                CopyTransition(transition, newTransition);
            }
        }

        // Copy state machines
        foreach (ChildAnimatorStateMachine sourceStateMachine in source.stateMachines)
        {
            AnimatorStateMachine newStateMachine = target.AddStateMachine(sourceStateMachine.stateMachine.name, sourceStateMachine.position);
            CopyStateMachine(sourceStateMachine.stateMachine, newStateMachine, animFolderPath);
        }
    }

    private void CopyTransition(AnimatorStateTransition source, AnimatorStateTransition target)
    {
        target.duration = source.duration;
        target.offset = source.offset;
        target.hasExitTime = source.hasExitTime;
        target.exitTime = source.exitTime;
        target.hasFixedDuration = source.hasFixedDuration;
        target.interruptionSource = source.interruptionSource;
        target.orderedInterruption = source.orderedInterruption;
        target.canTransitionToSelf = source.canTransitionToSelf;

        // Copy conditions
        foreach (AnimatorCondition condition in source.conditions)
        {
            target.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }
    }

    private AnimatorState GetStateByName(AnimatorStateMachine stateMachine, string name)
    {
        foreach (ChildAnimatorState state in stateMachine.states)
        {
            if (state.state.name == name)
                return state.state;
        }
        return null;
    }
} 