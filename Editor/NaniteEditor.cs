using UnityEditor;

[InitializeOnLoadAttribute]
public class NaniteEditor {

        const string MenuRootPath = "Nanite/";
        const string MenuItem1Path = MenuRootPath + "Enabled";
        const string MenuItem2Path = MenuRootPath + "View Triangles";

        public static bool isNanite = true;
        [MenuItem(MenuItem1Path)]
        static void EnableDisableNanite() {
                if(Menu.GetChecked(MenuItem1Path))
                        Menu.SetChecked(MenuItem1Path, false);
                else
                        Menu.SetChecked(MenuItem1Path, true);

                isNanite = Menu.GetChecked(MenuItem1Path);
        }
        public static bool isViewTriangle = false;
        [MenuItem(MenuItem2Path)]
        static void ViewTrianglesNanite() {
                if(Menu.GetChecked(MenuItem2Path))
                        Menu.SetChecked(MenuItem2Path, false);
                else
                        Menu.SetChecked(MenuItem2Path, true);

                isViewTriangle = Menu.GetChecked(MenuItem2Path);
        }


        static NaniteEditor(){
                Menu.SetChecked(MenuItem1Path, isNanite);
                isNanite = Menu.GetChecked(MenuItem1Path);
                isViewTriangle = Menu.GetChecked(MenuItem2Path);
                EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        void LoadConfig(){

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
