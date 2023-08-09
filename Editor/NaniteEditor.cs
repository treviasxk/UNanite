using UnityEditor;
[InitializeOnLoadAttribute]
public class NaniteEditor {

        const string MenuRootPath = "Nanite/";
        const string MenuItem1Path = MenuRootPath + "Enabled";
        public static bool isNanite = true;
        [MenuItem(MenuItem1Path)]
        static void Item1Validate() {
                if(Menu.GetChecked(MenuItem1Path))
                        Menu.SetChecked(MenuItem1Path, false);
                else
                        Menu.SetChecked(MenuItem1Path, true);

                isNanite = Menu.GetChecked(MenuItem1Path);
        }



        static NaniteEditor(){
                Menu.SetChecked(MenuItem1Path, isNanite);
                EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private static void LogPlayModeState(PlayModeStateChange state){
                if(state == PlayModeStateChange.EnteredPlayMode){
                        if(!UnityEngine.GameObject.Find("Nanite")){
                                UnityEngine.GameObject runThreadUnity = new UnityEngine.GameObject("Nanite");
                                runThreadUnity.AddComponent<Nanite>();
                        }
                }
        }
}
