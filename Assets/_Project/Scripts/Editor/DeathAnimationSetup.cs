#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DeathAnimationSetup
{
    const string ControllerPath = "Assets/_Project/Animations/PlayerController.controller";
    const string ZombieDyingPath = "Assets/_Project/Models/character animation ver 2/char_1@Zombie Dying.fbx";
    const string FallingBackDeathPath = "Assets/_Project/Models/character animation ver 2/char_1@Falling Back Death.fbx";

    [InitializeOnLoadMethod]
    static void AutoApplyOnce()
    {
        EditorApplication.delayCall += delegate
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimationClip zombieClip = LoadClip(ZombieDyingPath);
            if (ctrl == null) return;

            AnimatorState deathState = FindState(ctrl.layers[0].stateMachine, "Death");
            AnimatorState killedState = FindState(ctrl.layers[0].stateMachine, "KilledByDino");
            ModelImporter importer = AssetImporter.GetAtPath(ZombieDyingPath) as ModelImporter;
            bool needClipFix = importer != null && (importer.clipAnimations == null || importer.clipAnimations.Length == 0
                || !importer.clipAnimations[0].keepOriginalPositionY || !importer.clipAnimations[0].lockRootHeightY);
            bool hasAutoExitFromDeath = false;
            if (deathState != null)
            {
                foreach (AnimatorStateTransition tr in deathState.transitions)
                {
                    if (tr != null && tr.hasExitTime && tr.conditions.Length == 0)
                    {
                        hasAutoExitFromDeath = true;
                        break;
                    }
                }
            }
            if (deathState != null && (killedState == null || deathState.motion != zombieClip || needClipFix || hasAutoExitFromDeath))
                ApplyDeathAnimationSwap();
            EnsureSceneCutsceneDirector();
        };
    }

    [MenuItem("Tools/Ride a Dino/Setup Dino Kill Cutscene & Species Defaults")]
    public static void ApplySpeciesKillCutsceneDefaults()
    {
        string[] guids = AssetDatabase.FindAssets("t:DinoData");
        int updated = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            DinoData data = AssetDatabase.LoadAssetAtPath<DinoData>(path);
            if (data == null) continue;

            if (data.diet == DinoDiet.Herbivore)
            {
                data.allowKillPounceBite = false;
                data.allowKillTossSwallow = false;
                data.allowKillStomp = true;
            }
            else if (data.size <= DinoSpeciesSize.SmallMedium)
            {
                // Thú ăn thịt nhỏ/vừa (như Dilophosaurus): chuyên vồ & cạp khi nằm ngửa
                data.allowKillPounceBite = true;
                data.allowKillTossSwallow = false;
                data.allowKillStomp = false;
            }
            else
            {
                // Thú ăn thịt lớn (như Allosaurus, Tyrannosaurus): vồ cạp, hất lên nuốt chửng, hoặc giẫm
                data.allowKillPounceBite = true;
                data.allowKillTossSwallow = true;
                data.allowKillStomp = data.size >= DinoSpeciesSize.Large;
            }
            EditorUtility.SetDirty(data);
            updated++;
        }
        EnsureSceneCutsceneDirector();
        AssetDatabase.SaveAssets();
        Debug.Log("[Ride a Dino] Đã gán mặc định kiểu chết theo loài cho " + updated + " DinoData và gắn DinoKillCutsceneDirector lên Player.");
    }

    static void EnsureSceneCutsceneDirector()
    {
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        if (player != null && player.GetComponent<DinoKillCutsceneDirector>() == null)
        {
            Undo.AddComponent<DinoKillCutsceneDirector>(player.gameObject);
            EditorUtility.SetDirty(player.gameObject);
        }
    }

    [MenuItem("Tools/Ride a Dino/Update Death Animation (Zombie Dying)")]
    public static void ApplyDeathAnimationSwap()
    {
        // 1. Đảm bảo FBX mới dùng Rig Humanoid và Bake Into Pose Y (Original) để không bị lơ lửng
        ModelImporter importer = AssetImporter.GetAtPath(ZombieDyingPath) as ModelImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    ModelImporterClipAnimation c = clips[i];
                    if (c.loopTime || !c.lockRootHeightY || !c.keepOriginalPositionY || !c.lockRootRotation || !c.keepOriginalOrientation || c.heightFromFeet)
                    {
                        c.loopTime = false;
                        c.loopPose = false;
                        c.lockRootRotation = true;
                        c.keepOriginalOrientation = true;
                        c.lockRootHeightY = true;
                        c.keepOriginalPositionY = true;
                        c.heightFromFeet = false;
                        clips[i] = c;
                        changed = true;
                    }
                }
                if (changed) importer.clipAnimations = clips;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        AnimationClip newDeathClip = LoadClip(ZombieDyingPath);
        AnimationClip oldDeathClip = LoadClip(FallingBackDeathPath);
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null || newDeathClip == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy PlayerController.controller hoặc clip trong char_1@Zombie Dying.fbx.");
            return;
        }

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // 2. Đổi state "Death" sang animation mới (Zombie Dying) và bỏ transition tự thoát về Idle
        AnimatorState deathState = FindState(sm, "Death");
        if (deathState != null)
        {
            deathState.motion = newDeathClip;
            AnimatorStateTransition[] trs = deathState.transitions;
            for (int i = trs.Length - 1; i >= 0; i--)
            {
                if (trs[i] != null && trs[i].hasExitTime && trs[i].conditions.Length == 0)
                    deathState.RemoveTransition(trs[i]);
            }
        }

        // 3. Giữ animation cũ (Falling Back Death) cho state "KilledByDino" để dùng cho cutscene bị Dino giết
        AnimatorState killedState = FindState(sm, "KilledByDino");
        if (killedState == null)
        {
            killedState = sm.AddState("KilledByDino", new Vector3(410f, 300f, 0f));
            bool hasParam = false;
            for (int i = 0; i < ctrl.parameters.Length; i++)
                if (ctrl.parameters[i].name == "KilledByDino") { hasParam = true; break; }
            if (!hasParam) ctrl.AddParameter("KilledByDino", AnimatorControllerParameterType.Trigger);

            AnimatorStateTransition t = sm.AddAnyStateTransition(killedState);
            t.AddCondition(AnimatorConditionMode.If, 0f, "KilledByDino");
            t.duration = 0.12f;
            t.hasExitTime = false;
            t.canTransitionToSelf = false;
        }
        if (oldDeathClip != null)
            killedState.motion = oldDeathClip;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[Ride a Dino] Đã đổi animation Death sang char_1@Zombie Dying và giữ lại Falling Back Death trong state KilledByDino.");
    }

    static AnimationClip LoadClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null) return null;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        if (sm == null) return null;
        for (int i = 0; i < sm.states.Length; i++)
            if (sm.states[i].state != null && sm.states[i].state.name == name)
                return sm.states[i].state;
        return null;
    }
}
#endif
