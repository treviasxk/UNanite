using System.Collections.Generic;
using UnityEngine;

public class Nanite : MonoBehaviour{
    public Material MaterialNanite;
    int instanceCount;
    int subMeshIndex = 0;
    int cachedInstanceCount = -1;
    int cachedSubMeshIndex = -1;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] {0, 0, 0, 0, 0 };

    List<ObjectNanite> AllObject = new List<ObjectNanite>();

    public struct ObjectNanite{
        public Transform coordinates;
        public Mesh mesh;
        public Color[] viewTriangles;
        public Material material;
    }

    void Start(){//
        if(NaniteEditor.isNanite){
            DontDestroyOnLoad(gameObject);
            foreach(GameObject nanite in FindObjectsOfType<GameObject>()){
                if(nanite.GetComponent<MeshFilter>()){
                    if(nanite.GetComponent<MeshRenderer>().material){
                        // Generate color triangles
                        Vector3[] vertices = nanite.GetComponent<MeshFilter>().mesh.vertices;
                        Color[] colors = new Color[vertices.Length];
                        for(int i = 0; i < vertices.Length; i++)
                            colors[i] = new Color(Random.Range(0.0f, 1.0f),Random.Range(0.0f, 1.0f),Random.Range(0.0f, 1.0f),1.0f);


                        MeshProperties[] properties = new MeshProperties[]{new MeshProperties(){mat = Matrix4x4.TRS(nanite.transform.position, Quaternion.Euler(nanite.transform.rotation.eulerAngles), nanite.transform.localScale)}};
                        meshPropertiesBuffer = new ComputeBuffer(1, MeshProperties.Size());
                        meshPropertiesBuffer.SetData(properties);
                        MaterialNanite.SetBuffer("_Properties", meshPropertiesBuffer);

                        AllObject.Add(new ObjectNanite(){mesh = nanite.GetComponent<MeshFilter>().mesh, material = nanite.GetComponent<MeshRenderer>().material, viewTriangles = colors, coordinates = nanite.transform});
                        nanite.GetComponent<MeshRenderer>().enabled = false;
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
            foreach(ObjectNanite nanite in AllObject){
                if(NaniteEditor.isViewTriangle)
                    nanite.mesh.colors = nanite.viewTriangles;
                Graphics.DrawMeshInstancedIndirect(nanite.mesh, 0, NaniteEditor.isViewTriangle ? MaterialNanite : nanite.material, new Bounds(nanite.coordinates.position, new Vector3(0,0,0)), argsBuffer);
            }
        }
    }

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties {
        public Matrix4x4 mat;
        public Vector4 color;

        public static int Size() {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }
    private ComputeBuffer meshPropertiesBuffer;
    void UpdateBuffers(){

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

    private void OnDisable() {
        if(argsBuffer != null) 
            argsBuffer.Release();
        argsBuffer = null;
    }
}