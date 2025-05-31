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

    [Serializable]
    public struct SavedState
    {
        public string saveScene;
        public PlayerData savedPlayerData;
        public SceneData savedSceneData;
        public Vector3 savePos;
    }

    public static class SaveStateManager
    {
        private static object lockArea;
        private static readonly FieldInfo cameraGameplayScene = typeof(CameraController)
            .GetField("isGameplayScene", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo cameraLockArea = typeof(CameraController)
            .GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Keybinds Keybinds = new Keybinds();
        public static int slots = 1;

        public static void LoadState()
        {
            SavedState savedState = new SavedState();
            try
            {
                switch (slots)
                {
                    case 1:
                        savedState = JsonUtility.FromJson<SavedState>(File.ReadAllText(Application.persistentDataPath + "/minisavestates-saved1.json"));
                        break;
                    case 2:
                        savedState = JsonUtility.FromJson<SavedState>(File.ReadAllText(Application.persistentDataPath + "/minisavestates-saved2.json"));
                        break;
                    case 3:
                        savedState = JsonUtility.FromJson<SavedState>(File.ReadAllText(Application.persistentDataPath + "/minisavestates-saved3.json"));
                        break;
                    case 4:
                        savedState = JsonUtility.FromJson<SavedState>(File.ReadAllText(Application.persistentDataPath + "/minisavestates-saved4.json"));
                        break;
                    case 5:
                        savedState = JsonUtility.FromJson<SavedState>(File.ReadAllText(Application.persistentDataPath + "/minisavestates-saved5.json"));
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            GameManager.instance.StartCoroutine(LoadStateCoro(savedState));
        }

        public static void SaveState()
        {
            var savedState = new SavedState
            {
                saveScene = GameManager.instance.GetSceneNameString(),
                savedPlayerData = PlayerData.instance,
                savedSceneData = SceneData.instance,
                savePos = HeroController.instance.gameObject.transform.position
            };
            try
            {
                switch (slots)
                {
                    case 1:
                        File.WriteAllText(Application.persistentDataPath + "/minisavestates-saved1.json", JsonUtility.ToJson(savedState));
                        break;
                    case 2:
                        File.WriteAllText(Application.persistentDataPath + "/minisavestates-saved2.json", JsonUtility.ToJson(savedState));
                        break;
                    case 3:
                        File.WriteAllText(Application.persistentDataPath + "/minisavestates-saved3.json", JsonUtility.ToJson(savedState));
                        break;
                    case 4:
                        File.WriteAllText(Application.persistentDataPath + "/minisavestates-saved4.json", JsonUtility.ToJson(savedState));
                        break;
                    case 5:
                        File.WriteAllText(Application.persistentDataPath + "/minisavestates-saved5.json", JsonUtility.ToJson(savedState));
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            lockArea = cameraLockArea.GetValue(GameManager.instance.cameraCtrl);
        }

        public static void LoadKeybinds()
        {
            try
            {
                Keybinds = JsonUtility.FromJson<Keybinds>(
                    File.ReadAllText(Application.persistentDataPath + "/minisavestates1.json")
                );
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static IEnumerator LoadStateCoro(SavedState savedState)
        {
            var savedPd = savedState.savedPlayerData;
            var savedSd = savedState.savedSceneData;
            var saveScene = savedState.saveScene;
            var savePos = savedState.savePos;

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
            if (lockArea != null)
            {
                GameManager.instance.cameraCtrl.LockToArea(lockArea as CameraLockArea);
            }
            yield return new WaitUntil(() => USceneManager.GetActiveScene().name == saveScene);
            GameManager.instance.cameraCtrl.FadeSceneIn();
            HeroController.instance.TakeMP(1);
            HeroController.instance.AddMPChargeSpa(1);
            HeroController.instance.playerData.health -= 1;
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-HeroDamaged");
            HeroController.instance.playerData.health += 1;
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-Healed");
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
        private void OnGUI()
        {
            if (this.GetSceneNameString() == Constants.MENU_SCENE)
            {
                var oldBackgroundColor = GUI.backgroundColor;
                var oldContentColor = GUI.contentColor;
                var oldColor = GUI.color;
                var oldMatrix = GUI.matrix;

                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
                GUI.color = Color.white;
                GUI.matrix = Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f)
                );

                GUI.Label(
                    new Rect(20f, 20f, 200f, 200f),
                    "MultiSaveStates Active",
                    new GUIStyle
                    {
                        fontSize = 30,
                        normal = new GUIStyleState
                        {
                            textColor = Color.white,
                        }
                    }
                );

                GUI.backgroundColor = oldBackgroundColor;
                GUI.contentColor = oldContentColor;
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        public extern void orig_Update();

        public new void Update()
        {
            orig_Update();

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SaveStateManager.slots = 1;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SaveStateManager.slots = 2;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SaveStateManager.slots = 3;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SaveStateManager.slots = 4;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SaveStateManager.slots = 5;
            }

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