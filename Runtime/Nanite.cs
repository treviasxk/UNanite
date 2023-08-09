using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

public class Nanite : MonoBehaviour{
    int instanceCount;
    int subMeshIndex = 0;
    bool isNanite = true;
    int cachedInstanceCount = -1;
    int cachedSubMeshIndex = -1;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] {0, 0, 0, 0, 0 };

    List<ObjectNanite> AllObject = new List<ObjectNanite>();

    public struct ObjectNanite{
        public Mesh mesh;
        public Material material;
        public Vector3 position;
        public Vector3 rotation;
        public bool active;
    }

    void Start(){
        if(NaniteEditor.isNanite){
            DontDestroyOnLoad(gameObject);
            foreach(GameObject ob in FindObjectsOfType<GameObject>()){
                if(ob.GetComponent<MeshFilter>()){
                    if(ob.GetComponent<MeshRenderer>().material){
                        AllObject.Add(new ObjectNanite(){mesh = ob.GetComponent<MeshFilter>().mesh, material = ob.GetComponent<MeshRenderer>().material, position = ob.transform.position, rotation = ob.transform.rotation.eulerAngles, active = ob.activeSelf});
                        ob.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
            instanceCount = AllObject.Count;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            UpdateBuffers();
        }
        Debug.Log("Mesh: " + AllObject.Count);
    }


    void Update() {
        if(NaniteEditor.isNanite){
            // Update starting position buffer
            if(cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
                UpdateBuffers();


            // Render
            foreach(ObjectNanite nanite in AllObject)
                Graphics.DrawMeshInstancedIndirect(nanite.mesh, 0, nanite.material, new Bounds(nanite.position, nanite.rotation), argsBuffer);
        }
    }


    void UpdateBuffers() {
        foreach(ObjectNanite nanite in AllObject){
            if(nanite.mesh != null)
                subMeshIndex = Mathf.Clamp(subMeshIndex, 0, nanite.mesh.subMeshCount - 1);

            // Indirect args
            if(nanite.mesh != null){
                args[0] = (uint)nanite.mesh.GetIndexCount(subMeshIndex);
                args[1] = (uint)instanceCount;
                args[2] = (uint)nanite.mesh.GetIndexStart(subMeshIndex);
                args[3] = (uint)nanite.mesh.GetBaseVertex(subMeshIndex);
            }
            else{
                args[0] = args[1] = args[2] = args[3] = 0;
            }

            argsBuffer.SetData(args);
            cachedInstanceCount = instanceCount;
            cachedSubMeshIndex = subMeshIndex;
        }
    }
}