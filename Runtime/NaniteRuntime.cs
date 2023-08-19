using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;

public class NaniteRuntime : MonoBehaviour{
    
    public float MinRender = 5f;
    public float MaxRender = 50;
    Dictionary<int, Nanite> Nanites = new();
    readonly ConcurrentQueue<UnaniteObject> ListRenderUnanites = new();
    readonly ConcurrentQueue<UnaniteObject> Unanites = new();
    public Thread UnaniteThread;
    GameObject Unanite;
    class Nanite{
        public Transform transform;
        public int quality;
        public MeshFilter meshFilter;
        public List<MeshFilter> childrenMeshFilter;
        public Dictionary<int, Mesh> LODs;
    }

    class UnaniteObject {
        public int instanceID;
        public float value;
        public int quality;
        public Vector3[] vertices;
        public Vector2[] uv;
        public int[] triangles;
        public Vector3[] normals;
        public Vector4[] tangents;
    }
    
    Vector3 cameraPosition;
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
                meshSimplifier.SimplifyMesh(unanite.value);
                Unanites.Enqueue(new UnaniteObject(){instanceID = unanite.instanceID, vertices = meshSimplifier.Vertices, normals = meshSimplifier.Normals, tangents = meshSimplifier.Tangents, uv = meshSimplifier.UV1, triangles = meshSimplifier.GetSubMeshTriangles(0), value = unanite.value, quality = unanite.quality});
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
                if(!Nanites[unanite.instanceID].LODs.ContainsKey(unanite.quality)){
                    Mesh mesh = new(){vertices = unanite.vertices, tangents = unanite.tangents, normals = unanite.normals, triangles = unanite.triangles, uv = unanite.uv, name = "Unanite"};            
                    Nanites[unanite.instanceID].LODs.Add(unanite.quality, mesh);
                }
            }
        }

        if(NaniteEditor.isNanite){
            if(fisrt){
                fisrt = false;
                if(!Unanite){
                    Unanite = new GameObject("Unanite");
                    Unanite.transform.SetAsFirstSibling();
                    foreach(GameObject nanite in FindObjectsOfType<GameObject>())
                        if(nanite.GetComponent<MeshFilter>()){
                            int code = nanite.GetComponent<MeshFilter>().sharedMesh.GetInstanceID();
                            if(!Nanites.ContainsKey(code)){
                                Nanites.Add(code, new Nanite(){quality = 10, meshFilter = nanite.GetComponent<MeshFilter>(), transform = nanite.transform, LODs = new Dictionary<int, Mesh>(), childrenMeshFilter = new List<MeshFilter>(){}});
                            }else{
                                int code2 = nanite.GetComponent<MeshRenderer>().sharedMaterial.GetInstanceID();
                                if(Nanites[code].meshFilter.GetComponent<MeshRenderer>().sharedMaterial.GetInstanceID() == code2)
                                    Nanites[code].childrenMeshFilter.Add(nanite.GetComponent<MeshFilter>());
                                else{
                                    if(!Nanites.ContainsKey(code2)){
                                        Nanites.Add(code2, new Nanite(){quality = 10, meshFilter = nanite.GetComponent<MeshFilter>(), transform = nanite.transform, LODs = new Dictionary<int, Mesh>(), childrenMeshFilter = new List<MeshFilter>(){}});
                                    }else{
                                        Nanites[code2].childrenMeshFilter.Add(nanite.GetComponent<MeshFilter>());
                                    }
                                }
                            }
                        }
                }

            }
            cameraPosition = Camera.main.gameObject.transform.position;
            foreach(var nanite in Nanites.OrderBy(item => item.Value.quality)){
                float distance = Mathf.Pow(Vector3.Distance(nanite.Value.transform.position, cameraPosition), 1);
                float value = (1 / MaxRender) * (distance - MinRender);
                value = 1 - Mathf.Clamp(value, 0, 1);
                int quality = Convert.ToInt16(value * 10);

                if(!nanite.Value.LODs.ContainsKey(quality)){
                    CreateQuality(nanite.Key, nanite.Value, quality, value);
                }else
                    ChangeQuality(nanite.Key, nanite.Value, quality);
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

    void CreateQuality(int instanceID, Nanite nanite, int quality, float value){
        if(!ListRenderUnanites.Any(item => item.instanceID == instanceID && item.quality == quality) && !Unanites.Any(item => item.instanceID == instanceID && item.quality == quality) && !nanite.LODs.ContainsKey(quality))
            if(nanite.meshFilter.mesh){
                ListRenderUnanites.Enqueue(new UnaniteObject{instanceID = instanceID, vertices = nanite.meshFilter.mesh.vertices,  normals = nanite.meshFilter.mesh.normals, tangents = nanite.meshFilter.mesh.tangents, uv = nanite.meshFilter.mesh.uv, triangles = nanite.meshFilter.mesh.triangles, quality = quality, value = value});        
            }
    }

    void ChangeQuality(int id, Nanite nanite, int quality){
        if(Unanite)
        if(Unanite.activeSelf){
            if(nanite.quality != quality){
                nanite.quality = quality;
                var unanite = GameObject.Find(id.ToString());
                if(!unanite){
                    unanite = new GameObject(id.ToString());
                    unanite.AddComponent<MeshFilter>();
                    unanite.AddComponent<MeshRenderer>();
                    unanite.transform.position = Vector3.zero;
                    unanite.transform.rotation = Quaternion.identity;
                    unanite.transform.parent = Unanite.transform;
                }

                List<CombineInstance> combine = new();          

                for(int i = 0; i < nanite.childrenMeshFilter.Count; i++){
                    float distance = Mathf.Pow(Vector3.Distance(nanite.childrenMeshFilter[i].transform.position, cameraPosition), 1);
                    float value = (1 / MaxRender) * (distance - MinRender);
                    value = 1 - Mathf.Clamp(value, 0, 1);
                    quality = Convert.ToInt16(value * 10);

                    if(!nanite.LODs.ContainsKey(quality)){
                        CreateQuality(id, nanite, quality, value);
                    }else{
                        combine.Add(new CombineInstance(){mesh = nanite.LODs[quality], transform = nanite.childrenMeshFilter[i].transform.localToWorldMatrix});
                        nanite.childrenMeshFilter[i].GetComponent<MeshRenderer>().enabled = false;
                    }
                }

                
                if(nanite.LODs.ContainsKey(quality)){
                    combine.Add(new CombineInstance(){mesh = nanite.LODs[quality], transform = nanite.meshFilter.transform.localToWorldMatrix});
                    nanite.meshFilter.gameObject.GetComponent<MeshRenderer>().enabled = false;
                }
                Mesh mesh = new(){name = "Unanite",indexFormat = UnityEngine.Rendering.IndexFormat.UInt32};
                mesh.CombineMeshes(combine.ToArray());
                unanite.GetComponent<MeshFilter>().sharedMesh = mesh;
                unanite.GetComponent<MeshRenderer>().material = nanite.meshFilter.GetComponent<MeshRenderer>().material;
            }
        }
    }
}