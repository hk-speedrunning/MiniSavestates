using System.Collections;
using System.Reflection;
using UnityEngine;
using GlobalEnums;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using MonoMod;
using System.IO;
using System;
#pragma warning disable CS0626

namespace Patches
{
    [Serializable]
    public class Keybinds
    {
        public string LoadStateButton = "f1";
        public string SaveStateButton = "f2";
    }

    public static class SaveStateManager
    {
        private static string saveScene;
        private static PlayerData savedPd;
        private static object lockArea;
        private static SceneData savedSd;
        private static Vector3 savePos;
        private static readonly FieldInfo cameraGameplayScene = typeof(CameraController)
            .GetField("isGameplayScene", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo cameraLockArea = typeof(CameraController)
            .GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic);

        public static Keybinds Keybinds = new Keybinds();

        public static void LoadState()
        {
            GameManager.instance.StartCoroutine(LoadStateCoro());
        }

        public static void SaveState()
        {
            savedPd = JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(PlayerData.instance));
            savedSd = JsonUtility.FromJson<SceneData>(JsonUtility.ToJson(SceneData.instance));
            savePos = HeroController.instance.gameObject.transform.position;
            saveScene = GameManager.instance.GetSceneNameString();
            lockArea = cameraLockArea.GetValue(GameManager.instance.cameraCtrl);
        }

        public static void LoadKeybinds()
        {
            try
            {
                Keybinds = JsonUtility.FromJson<Keybinds>(
                    File.ReadAllText(Application.persistentDataPath + "/minisavestates.json")
                );
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static IEnumerator LoadStateCoro()
        {
            if (savedPd == null || string.IsNullOrEmpty(saveScene))
            {
                yield break;
            }
            GameManager.instance.entryGateName = "dreamGate";
            GameManager.instance.startedOnThisScene = true;
            USceneManager.LoadScene("Room_Sly_Storeroom");
            yield return new WaitUntil(() => USceneManager.GetActiveScene().name == "Room_Sly_Storeroom");
            GameManager.instance.sceneData = (SceneData.instance = JsonUtility.FromJson<SceneData>(JsonUtility.ToJson(savedSd)));
            GameManager.instance.ResetSemiPersistentItems();
            yield return null;
            PlayerData.instance = (GameManager.instance.playerData = (HeroController.instance.playerData = JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(savedPd))));
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = saveScene,
                HeroLeaveDirection = GatePosition.unknown,
                EntryGateName = "dreamGate",
                EntryDelay = 0f,
                WaitForSceneTransitionCameraFade = false,
                Visualization = 0,
                AlwaysUnloadUnusedAssets = true
            });
            cameraGameplayScene.SetValue(GameManager.instance.cameraCtrl, true);
            GameManager.instance.cameraCtrl.PositionToHero(false);
            bool flag2 = lockArea != null;
            if (flag2)
            {
                GameManager.instance.cameraCtrl.LockToArea(lockArea as CameraLockArea);
            }
            yield return new WaitUntil(() => USceneManager.GetActiveScene().name == saveScene);
            GameManager.instance.cameraCtrl.FadeSceneIn();
            HeroController.instance.TakeMP(1);
            HeroController.instance.AddMPChargeSpa(1);
            HeroController.instance.TakeHealth(1);
            HeroController.instance.AddHealth(1);
            HeroController.instance.geoCounter.geoTextMesh.text = savedPd.geo.ToString();
            GameCameras.instance.hudCanvas.gameObject.SetActive(true);
            cameraGameplayScene.SetValue(GameManager.instance.cameraCtrl, true);
            yield return null;
            HeroController.instance.gameObject.transform.position = savePos;
            HeroController.instance.transitionState = 0;
            MethodInfo method = typeof(HeroController).GetMethod("FinishedEnteringScene", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(HeroController.instance, new object[]
                {
                    true,
                    false
                });
            }
            yield break;
        }
    }

    [MonoModPatch("global::GameManager")]
    public class GameManagerPatch : global::GameManager
    {
        public extern void orig_Update();

        public new void Update()
        {
            orig_Update();
            if (Input.GetKeyDown(SaveStateManager.Keybinds.SaveStateButton))
            {
                SaveStateManager.SaveState();
            }
            else if (Input.GetKeyDown(SaveStateManager.Keybinds.LoadStateButton))
            {
                SaveStateManager.LoadState();
            }
        }

        public extern void orig_Start();

        public void Start()
        {
            orig_Start();
            SaveStateManager.LoadKeybinds();
        }
    }
}