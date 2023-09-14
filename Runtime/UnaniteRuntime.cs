using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;

public class UNaniteRuntime : MonoBehaviour{
    
    public float MinRender = 5f;
    public float MaxRender = 50;
    Dictionary<int, Nanite> Nanites = new();
    readonly ConcurrentQueue<UnaniteObject> ListRenderUnanites = new();
    readonly ConcurrentQueue<UnaniteObject> Unanites = new();
    public Thread UnaniteThread;
    GameObject Unanite;
    class Nanite{
        public Transform transform;
        public MeshFilter meshFilter;
        public List<MeshFilter> childrenMeshFilter;
        public Mesh meshLow;
        public bool oward;
    }

    class UnaniteObject {
        public int instanceID;
        public float value;
        public Vector3[] vertices;
        public Vector2[] uv;
        public int[] triangles;
        public Vector3[] normals;
        public Vector4[] tangents;
    }

    [RuntimeInitializeOnLoadMethod]
    static void Init() {
        GameObject Unanite = new GameObject("Nanite");
        Unanite.AddComponent<UNaniteRuntime>();
    }

    void Start(){
        DontDestroyOnLoad(gameObject);
        Nanites.Clear();
        UnaniteThread = new Thread(new ThreadStart(RenderUnanite)){IsBackground = true};
        UnaniteThread.Start();
    }

    void RenderUnanite(){
        Parallel.ForEach(ListRenderUnanites, unanite => {
            if(ListRenderUnanites.TryDequeue(out _)){
                var meshSimplifier = new MeshSimplifier
                {
                    Vertices = unanite.vertices,
                    UV1 = unanite.uv,
                    Tangents = unanite.tangents,
                    Normals = unanite.normals
                };
                meshSimplifier.AddSubMeshTriangles(unanite.triangles);
                meshSimplifier.PreserveUVSeamEdges = true;
                meshSimplifier.SimplifyMesh(0.2f);
                Unanites.Enqueue(new UnaniteObject(){instanceID = unanite.instanceID, vertices = meshSimplifier.Vertices, normals = meshSimplifier.Normals, tangents = meshSimplifier.Tangents, uv = meshSimplifier.UV1, triangles = meshSimplifier.GetSubMeshTriangles(0), value = unanite.value});
            }
        });
    }

    bool fisrt = true;
    void Update(){
        if(!UnaniteThread.IsAlive){
            UnaniteThread = new Thread(new ThreadStart(RenderUnanite)){IsBackground = true};
            UnaniteThread.Start();
        }

        foreach(var unanite in Unanites){
            if(Unanites.TryDequeue(out var _)){
                if(Nanites[unanite.instanceID].meshLow == null){
                    Mesh mesh = new(){vertices = unanite.vertices, tangents = unanite.tangents, normals = unanite.normals, triangles = unanite.triangles, uv = unanite.uv, name = "Unanite"};
                    Nanites[unanite.instanceID].meshLow = mesh;
                }
            }
        }

        if(UNaniteEditor.isNanite){
            if(fisrt){
                fisrt = false;
                if(!Unanite){
                    Unanite = new GameObject("Unanite");
                    Unanite.transform.SetAsFirstSibling();
                }
                foreach(GameObject nanite in FindObjectsOfType<GameObject>())
                    if(nanite.GetComponent<MeshFilter>()){
                        MeshFilter meshFilter = nanite.GetComponent<MeshFilter>();
                        int code = meshFilter.sharedMesh.GetInstanceID();
                        if(!Nanites.ContainsKey(code)){
                            Nanites.Add(code, new Nanite(){meshFilter = meshFilter, transform = nanite.transform, childrenMeshFilter = new List<MeshFilter>(){}, oward = true});
                        }else{
                            MeshRenderer meshRenderer = Nanites[code].meshFilter.GetComponent<MeshRenderer>();
                            int code2 = meshRenderer.sharedMaterial.GetInstanceID();
                            if(meshRenderer.sharedMaterial.GetInstanceID() == code2)
                                Nanites[code].childrenMeshFilter.Add(meshFilter);
                            else{
                                if(!Nanites.ContainsKey(code2)){
                                    Nanites.Add(code2, new Nanite(){meshFilter = meshFilter, transform = nanite.transform, childrenMeshFilter = new List<MeshFilter>(){}});
                                }else{
                                    Nanites[code2].childrenMeshFilter.Add(meshFilter);
                                }
                            }
                        }
                    }
                }

            
            foreach(var nanite in Nanites){;
                ChangeQuality(nanite.Key, nanite.Value);
            }
        }else{
            if(!fisrt){
                fisrt = true;
                foreach(Nanite nanite in Nanites.Values){
                    nanite.meshFilter.gameObject.GetComponent<MeshRenderer>().enabled = true;
                    foreach(MeshFilter nanite1 in nanite.childrenMeshFilter){
                        nanite1.gameObject.GetComponent<MeshRenderer>().enabled = true;
                    }
                }
                if(Unanite)
                    Destroy(Unanite);
                ListRenderUnanites.Clear();
                Unanites.Clear();
                Nanites.Clear();
            }
        }
    }

    void CreateQuality(int instanceID, Nanite nanite){
        if(!ListRenderUnanites.Any(item => item.instanceID == instanceID) && !Unanites.Any(item => item.instanceID == instanceID) && nanite.meshLow == null)
            if(nanite.meshFilter.mesh){
                ListRenderUnanites.Enqueue(new UnaniteObject{instanceID = instanceID, vertices = nanite.meshFilter.mesh.vertices,  normals = nanite.meshFilter.mesh.normals, tangents = nanite.meshFilter.mesh.tangents, uv = nanite.meshFilter.mesh.uv, triangles = nanite.meshFilter.mesh.triangles, value = 0.1f});
                nanite.meshFilter.sharedMesh ??= nanite.childrenMeshFilter[0].sharedMesh;
            }
    }

    void ChangeQuality(int id, Nanite nanite){
        if(Unanite)
        if(Unanite.activeSelf){
            var unanite = GameObject.Find(id.ToString());
            if(!unanite && nanite.meshLow){
                unanite = new GameObject(id.ToString());
                unanite.AddComponent<MeshFilter>();
                unanite.AddComponent<MeshRenderer>();
                unanite.transform.position = Vector3.zero;
                unanite.transform.rotation = Quaternion.identity;
                unanite.transform.parent = Unanite.transform;

                List<CombineInstance> combine = new();

                for (int i = 0; i < nanite.childrenMeshFilter.Count; i++){
                    combine.Add(new CombineInstance() { mesh = nanite.meshLow, transform = nanite.childrenMeshFilter[i].transform.localToWorldMatrix });
                    nanite.childrenMeshFilter[i].GetComponent<MeshRenderer>().enabled = false;
                }

                combine.Add(new CombineInstance() { mesh = nanite.meshLow, transform = nanite.meshFilter.transform.localToWorldMatrix });
                nanite.meshFilter.gameObject.GetComponent<MeshRenderer>().enabled = false;

                Mesh mesh = new() { name = "Unanite", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(combine.ToArray());
                unanite.GetComponent<MeshFilter>().sharedMesh = mesh;
                unanite.GetComponent<MeshRenderer>().material = nanite.meshFilter.GetComponent<MeshRenderer>().material;
            }else{
                CreateQuality(id, nanite);
            }
        }
    }
}