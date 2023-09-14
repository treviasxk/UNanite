using UnityEditor;
using UnityEngine;

[InitializeOnLoadAttribute]
public class UnaniteEditor {

        const string MenuRootPath = "Unanite/";
        const string MenuItem1Path = MenuRootPath + "Enabled";

        public static bool isNanite = true;
        [MenuItem(MenuItem1Path)]
        static void EnableDisableNanite() {
                if(Menu.GetChecked(MenuItem1Path))
                        Menu.SetChecked(MenuItem1Path, false);
                else
                        Menu.SetChecked(MenuItem1Path, true);

                isNanite = Menu.GetChecked(MenuItem1Path);
        }

        static UnaniteEditor(){
                isNanite = Menu.GetChecked(MenuItem1Path);
                EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        static GameObject Unanite;
        private static void LogPlayModeState(PlayModeStateChange state){
                if(state == PlayModeStateChange.ExitingPlayMode){
                        if(Unanite)
                                Unanite.GetComponent<UnaniteRuntime>().UnaniteThread.Abort();
                }
        }
}
