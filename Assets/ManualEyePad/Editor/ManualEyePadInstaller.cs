using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDKBase;
using Object = UnityEngine.Object;
using Control = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;

namespace ManualEyePad
{
    internal static class Installer
    {
        internal const string RigName = "Manual Eye Pad";
        internal const string On = "ManualEyePad/On";
        internal const string Reset = "ManualEyePad/Reset";
        internal const string StareeeOn = "STAREEE_On";
        internal const string StareeeOwned = "STAREEE_GazeOwned";
        internal static readonly string[] Directions = { "Up", "Right", "Down", "Left" };
        internal static string Axis(string direction) => "ManualEyePad/" + direction;
        private const string Output = "Assets/ManualEyePadGenerated";

        [MenuItem("Tools/Manual Eye Pad/Install on Selected Avatar (Synced)")]
        private static void InstallSynced() => InstallSelection(true);

        [MenuItem("Tools/Manual Eye Pad/Install on Selected Avatar (Local Only)")]
        private static void InstallLocal() => InstallSelection(false);

        private static void InstallSelection(bool synced)
        {
            try
            {
                var avatar = Selection.activeGameObject?.GetComponentInParent<VRCAvatarDescriptor>();
                if (!avatar || EditorUtility.IsPersistent(avatar))
                    throw new InvalidOperationException("Hierarchyでシーン上のアバターを選択してください。");
                if (synced && avatar.expressionParameters &&
                    avatar.expressionParameters.CalcTotalCost() + 33 > VRCExpressionParameters.MAX_PARAMETER_COST)
                    throw new InvalidOperationException("同期パラメータに33ビットの空きがありません。Local Only版を選んでください。");
                var installed = Install(avatar, synced);
                Selection.activeGameObject = installed;
            }
            catch (Exception error)
            {
                EditorUtility.DisplayDialog(RigName, error.Message, "OK");
            }
        }

        internal static GameObject Install(VRCAvatarDescriptor avatar, bool synced)
        {
            var root = avatar.transform;
            if (root.Find(RigName)) throw new InvalidOperationException("Manual Eye Padは導入済みです。");
            var animator = avatar.GetComponent<Animator>();
            if (!animator || !animator.isHuman)
                throw new InvalidOperationException("Humanoidアバターが必要です。");
            var eyes = avatar.customEyeLookSettings;
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var left = eyes.leftEye ? eyes.leftEye : animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var right = eyes.rightEye ? eyes.rightEye : animator.GetBoneTransform(HumanBodyBones.RightEye);
            if (!head || !left || !right || left == right || !left.IsChildOf(head) || !right.IsChildOf(head))
                throw new InvalidOperationException("Headの下にある左右の目ボーンを、Avatar DescriptorのEye LookかHumanoid設定に指定してください。");

            var stareee = root.Find("STAREEE");
            var existing = stareee ? stareee.GetComponent<ModularAvatarMergeAnimator>() : null;
            var stareeeFx = existing ? existing.animator as AnimatorController : null;
            string[] blockedBy = new[] { StareeeOn, StareeeOwned }.Where(name =>
                stareeeFx && stareeeFx.parameters.Any(p => p.name == name && p.type == AnimatorControllerParameterType.Bool)).ToArray();
            var networkOff = Enumerable.Range(0, 6).Select(bit => "STAREEE_LYawBit" + bit).ToArray();
            var stareeeParameters = stareee ? stareee.GetComponent<ModularAvatarParameters>() : null;
            if (!stareeeParameters || !networkOff.All(name => stareeeParameters.parameters.Any(p => p.nameOrPrefix == name && !p.localOnly)))
                networkOff = Array.Empty<string>();

            if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets", "ManualEyePadGenerated");
            string folder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(Output, Guid.NewGuid().ToString("N")));
            if (string.IsNullOrEmpty(folder)) throw new InvalidOperationException("導入用フォルダーを作成できませんでした。");
            var rig = Child(root, RigName);
            try
            {
                var fx = AnimatorController.CreateAnimatorControllerAtPath(folder + "/EyePad.controller");
                var config = rig.gameObject.AddComponent<ModularAvatarParameters>();
                var merge = rig.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                merge.animator = fx;
                merge.pathMode = MergeAnimatorPathMode.Relative;
                merge.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                merge.layerPriority = 50;

                Define(On, false, synced);
                Define(Reset, false, false);
                foreach (var direction in Directions) Define(Axis(direction), true, synced);
                foreach (var name in blockedBy) fx.AddParameter(name, AnimatorControllerParameterType.Bool);
                foreach (var name in networkOff)
                    fx.AddParameter(new AnimatorControllerParameter
                    {
                        name = name, type = AnimatorControllerParameterType.Bool, defaultBool = true
                    });

                var frame = Child(rig, "Head Frame");
                frame.rotation = root.rotation;
                frame.position = head.position;
                var follow = frame.gameObject.AddComponent<VRCParentConstraint>();
                follow.Sources.Add(new VRCConstraintSource(head, 1f)
                {
                    ParentRotationOffset = (Quaternion.Inverse(head.rotation) * frame.rotation).eulerAngles
                });
                follow.IsActive = follow.Locked = true;
                var yaw = Child(frame, "Yaw");
                var pitch = Child(yaw, "Pitch");
                var straight = eyes.eyesLookingStraight;
                var outputs = new[] { AddEye(left, eyes.leftEye == left, straight?.left ?? default, "Left"),
                    AddEye(right, eyes.rightEye == right, straight?.right ?? default, "Right") };

                AxisLayer(yaw, "y", "Left", "Right", 25);
                AxisLayer(pitch, "x", "Down", "Up", -18);
                var ownership = Layer("Manual Eye Pad Ownership", true);
                var idle = ownership.AddState("Natural gaze");
                var manual = ownership.AddState("Manual gaze");
                var restore = ownership.AddState("Restore eye tracking");
                idle.motion = OwnershipClip(false);
                manual.motion = OwnershipClip(true);
                restore.motion = idle.motion;
                idle.writeDefaultValues = manual.writeDefaultValues = restore.writeDefaultValues = false;
                ownership.defaultState = idle;
                manual.AddStateMachineBehaviour<VRCAnimatorTrackingControl>().trackingEyes =
                    VRCAnimatorTrackingControl.TrackingType.Animation;
                restore.AddStateMachineBehaviour<VRCAnimatorTrackingControl>().trackingEyes =
                    VRCAnimatorTrackingControl.TrackingType.Tracking;
                var start = Transition(idle, manual);
                Condition(start, On, true);
                Condition(start, Reset, false);
                foreach (string name in blockedBy) Condition(start, name, false);
                foreach (string name in networkOff) Condition(start, name, true);
                Condition(Transition(manual, restore), On, false);
                foreach (string name in blockedBy) Condition(Transition(manual, restore), name, true);
                foreach (string name in networkOff) Condition(Transition(manual, restore), name, false);
                var done = Transition(restore, idle);
                done.hasExitTime = true;
                done.exitTime = 1;

                var resetLayer = Layer("Manual Eye Pad Center", false);
                var ready = resetLayer.AddState("Ready");
                var center = resetLayer.AddState("Center");
                ready.writeDefaultValues = center.writeDefaultValues = false;
                resetLayer.defaultState = ready;
                var driver = center.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                driver.localOnly = true;
                driver.parameters = Directions.Select(direction => new VRC_AvatarParameterDriver.Parameter
                {
                    name = Axis(direction), type = VRC_AvatarParameterDriver.ChangeType.Set, value = 0
                }).ToList();
                Condition(Transition(ready, center), Reset, true);
                Condition(Transition(center, ready), Reset, false);

                var inner = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                inner.controls.Add(Item("手動目線", Control.ControlType.Toggle, On));
                var pad = Item("上下左右パッド", Control.ControlType.FourAxisPuppet);
                pad.subParameters = Directions.Select(direction => new Control.Parameter { name = Axis(direction) }).ToArray();
                pad.labels = new[] { "上", "右", "下", "左" }.Select(label => new Control.Label { name = label }).ToArray();
                inner.controls.Add(pad);
                inner.controls.Add(Item("正面に戻す", Control.ControlType.Button, Reset));
                var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                var entry = Item("目線パッド", Control.ControlType.SubMenu);
                entry.subMenu = inner;
                menu.controls.Add(entry);
                AssetDatabase.CreateAsset(inner, folder + "/Controls.asset");
                AssetDatabase.CreateAsset(menu, folder + "/Menu.asset");
                var menuInstaller = rig.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                menuInstaller.menuToAppend = menu;
                var stareeeInstaller = stareee ? stareee.GetComponent<ModularAvatarMenuInstaller>() : null;
                if (stareeeInstaller) menuInstaller.installTargetMenu = stareeeInstaller.installTargetMenu;
                EditorUtility.SetDirty(fx);
                AssetDatabase.SaveAssets();
                Undo.RegisterCreatedObjectUndo(rig.gameObject, "Install Manual Eye Pad");
                EditorSceneManager.MarkSceneDirty(avatar.gameObject.scene);
                return rig.gameObject;

                void Define(string name, bool axis, bool networked)
                {
                    fx.AddParameter(name, axis ? AnimatorControllerParameterType.Float : AnimatorControllerParameterType.Bool);
                    config.parameters.Add(new ParameterConfig
                    {
                        nameOrPrefix = name, syncType = axis ? ParameterSyncType.Float : ParameterSyncType.Bool,
                        localOnly = !networked, saved = false, defaultValue = 0, hasExplicitDefaultValue = true
                    });
                }

                VRCRotationConstraint AddEye(Transform eye, bool hasConfiguredEye, Quaternion straight, string side)
                {
                    var output = Child(pitch, side + " neutral");
                    bool hasNeutral = hasConfiguredEye && Quaternion.Dot(straight, straight) > .5f;
                    output.rotation = hasNeutral ? eye.parent.rotation * straight : eye.rotation;
                    var holder = Child(rig, side + " eye output");
                    var constraint = holder.gameObject.AddComponent<VRCRotationConstraint>();
                    constraint.Sources.Add(new VRCConstraintSource(output, 1));
                    constraint.TargetTransform = eye;
                    constraint.Locked = true;
                    constraint.IsActive = constraint.enabled = false;
                    return constraint;
                }

                AnimationClip OwnershipClip(bool active)
                {
                    var clip = Clip(active ? "Eye Pad Active" : "Eye Pad Inactive");
                    foreach (var output in outputs)
                    {
                        Curve(clip, output.transform, typeof(VRCRotationConstraint), "m_Enabled", active ? 1 : 0);
                        Curve(clip, output.transform, typeof(VRCRotationConstraint), "IsActive", active ? 1 : 0);
                    }
                    return clip;
                }

                void AxisLayer(Transform target, string coordinate, string negative, string positive, float limit)
                {
                    var zero = Pose("center", 0);
                    var neg = Pose(negative, -limit);
                    var pos = Pose(positive, limit);
                    var opposite = Tree(target.name + " negative", Axis(negative), zero, neg);
                    var tree = Tree(target.name + " directions", Axis(positive), opposite, pos);
                    var machine = Layer("Manual Eye Pad " + target.name, false);
                    var state = machine.AddState("Aim");
                    state.writeDefaultValues = false;
                    state.motion = tree;
                    machine.defaultState = state;

                    AnimationClip Pose(string name, float degrees)
                    {
                        var clip = Clip(target.name + " " + name);
                        Curve(clip, target, typeof(Transform), "localEulerAnglesRaw." + coordinate, degrees);
                        return clip;
                    }
                }

                AnimatorStateMachine Layer(string name, bool first)
                {
                    if (!first) fx.AddLayer(name);
                    var layers = fx.layers;
                    var layer = layers[first ? 0 : layers.Length - 1];
                    layer.name = name;
                    layer.defaultWeight = 1;
                    layer.stateMachine.name = name;
                    fx.layers = layers;
                    return layer.stateMachine;
                }

                AnimationClip Clip(string name)
                {
                    var clip = new AnimationClip { name = name, frameRate = 60 };
                    AssetDatabase.AddObjectToAsset(clip, fx);
                    return clip;
                }

                void Curve(AnimationClip clip, Transform target, Type type, string property, float value) =>
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(
                        AnimationUtility.CalculateTransformPath(target, rig), type, property),
                        AnimationCurve.Constant(0, 1f / 60f, value));

                BlendTree Tree(string name, string parameter, Motion zero, Motion one)
                {
                    var tree = new BlendTree { name = name, blendType = BlendTreeType.Simple1D,
                        blendParameter = parameter, useAutomaticThresholds = false };
                    AssetDatabase.AddObjectToAsset(tree, fx);
                    tree.AddChild(zero, 0);
                    tree.AddChild(one, 1);
                    return tree;
                }
            }
            catch
            {
                Object.DestroyImmediate(rig.gameObject);
                AssetDatabase.DeleteAsset(folder);
                throw;
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Control Item(string label, Control.ControlType type, string parameter = null) =>
            new Control { name = label, type = type, value = 1,
                parameter = new Control.Parameter { name = parameter ?? "" } };

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0;
            return transition;
        }

        private static void Condition(AnimatorStateTransition transition, string name, bool value) =>
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, name);

    }
}
