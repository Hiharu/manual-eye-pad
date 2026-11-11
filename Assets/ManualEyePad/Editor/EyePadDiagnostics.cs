using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
namespace ManualEyePad
{
    public sealed class EyePadDiagnostics : EditorWindow
    {
        private VRCAvatarDescriptor avatar;
        private bool synced = true;
        private string report;
        private Vector2 scroll;
        [MenuItem("Tools/Manual Eye Pad/環境チェック・診断")]
        public static void Open()
        {
            var window = GetWindow<EyePadDiagnostics>("Eye Pad 診断");
            window.minSize = new Vector2(480, 330);
            window.avatar = Selection.activeGameObject ? Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>(true) : null;
        }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("① アバター指定 → ② 環境チェック → ③ Hierarchyで対象を選択し、Tools / Manual Eye PadのSyncedまたはLocal Onlyで導入 → ④ SDKでBuild & Test。", MessageType.Info);
            avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("診断対象", avatar, typeof(VRCAvatarDescriptor), true);
            synced = EditorGUILayout.Toggle("同期版を導入予定", synced);
            SupportPanel.Draw(() => Build(avatar, synced), "https://github.com/Hiharu/manual-eye-pad#readme", ref report, ref scroll);
        }
        public static string Build(VRCAvatarDescriptor avatar, bool synced)
        {
            var b = SupportPanel.Header("Manual Eye Pad 1.1.0", "com.vrchat.avatars", "com.vrchat.base", "nadena.dev.modular-avatar", "nadena.dev.ndmf");
            b.AppendLine("[参考] 検証: Unity 2022.3.22f1 / SDK 3.10.5 / MA 1.18.7 / NDMF 1.14.8");
            b.AppendLine("導入予定: " + (synced ? "Synced / 33bit" : "Local Only / 0bit"));
            if (!avatar || EditorUtility.IsPersistent(avatar) || !avatar.gameObject.scene.IsValid())
            { b.AppendLine("[要対応] HierarchyのAvatar Descriptor付きアバターを指定してください。"); return SupportPanel.Finish(b); }
            b.AppendLine(avatar.gameObject.activeInHierarchy ? "[OK] アバターは有効。" : "[要対応] アバターまたは親を有効にしてください。");
            var animator = avatar.GetComponent<Animator>();
            if (!animator || !animator.isHuman) b.AppendLine("[要対応] Humanoid Animatorが必要です。Genericリグには対応しません。");
            else
            {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var eyes = avatar.customEyeLookSettings;
                var left = eyes.leftEye ? eyes.leftEye : animator.GetBoneTransform(HumanBodyBones.LeftEye);
                var right = eyes.rightEye ? eyes.rightEye : animator.GetBoneTransform(HumanBodyBones.RightEye);
                b.AppendLine("Head: " + (head ? "あり" : "なし") + " / 左目: " + (left ? "あり" : "なし") + " / 右目: " + (right ? "あり" : "なし"));
                b.AppendLine(head && left && right && left != right && left.IsChildOf(head) && right.IsChildOf(head) ? "[OK] 左右の目ボーンは別々でHead配下。" : "[要対応] Eye LookまたはHumanoidにHead配下の左右別の目ボーンを指定してください。");
            }
            var root = avatar.transform.Find(Installer.RigName);
            if (avatar.expressionParameters)
            {
                int cost = avatar.expressionParameters.CalcTotalCost();
                b.AppendLine("Descriptor直接設定の同期コスト: " + cost + "/256");
                if (!root && synced && cost + 33 > 256) b.AppendLine("[要対応] 同期枠が不足しています。Local Onlyまたは他の同期設定を見直してください。");
            }
            b.AppendLine("[注意] 同期コストはMA統合前。統合後の合計はSDKビルドで確認してください。");
            if (!root) b.AppendLine("[未導入] Manual Eye Pad生成ルートなし。問題解消後に導入メニューを使ってください。");
            else
            {
                b.AppendLine("[導入済み] 重複導入はできません。同期設定や生成内容を変える場合はREADMEの再導入手順を確認。");
                b.AppendLine(root.gameObject.activeInHierarchy ? "[OK] 生成ルート有効。" : "[要対応] 生成ルートまたは親を有効にしてください。");
                var menu = root.GetComponent<ModularAvatarMenuInstaller>();
                b.AppendLine(menu && menu.enabled ? "[OK] MA Menu Installer有効。" : "[要対応] MA Menu Installerがないか無効です。");
                if (menu) b.AppendLine("導入先: " + (menu.installTargetMenu ? "指定サブメニュー / Select Menuで確認" : "ルート（既定）"));
                var merge = root.GetComponent<ModularAvatarMergeAnimator>();
                b.AppendLine(merge && merge.enabled && merge.animator ? "[OK] MA Merge AnimatorとControllerあり。" : "[要対応] MA Merge AnimatorとControllerを確認してください。");
                var parameters = root.GetComponent<ModularAvatarParameters>();
                b.AppendLine(parameters ? "生成ルートのパラメータ定義数: " + parameters.parameters.Count : "[要対応] MA Parametersがありません。");
                if (parameters) b.AppendLine("生成ルートの同期コスト概算: " + parameters.parameters.Where(p => !p.localOnly).Sum(p => p.syncType == ParameterSyncType.Bool ? 1 : p.syncType == ParameterSyncType.Int || p.syncType == ParameterSyncType.Float ? 8 : 0));
                if (!root.Find("Head Frame/Yaw/Pitch")) b.AppendLine("[要対応] 目線制御の階層が不足しています。");
            }
            b.AppendLine("STAREEEルート: " + (avatar.transform.Find("STAREEE") ? "あり / 目線の競合・解除後の復帰は別途確認" : "なし"));
            b.AppendLine("次の操作: 警告解消 → 導入 → MAの導入先確認 → シーン保存 → SDK Build & Test。");
            return SupportPanel.Finish(b);
        }
    }
}
