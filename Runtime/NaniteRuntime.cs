using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NaniteRuntime : MonoBehaviour{
    
    public float MinRender = 5f;
    public float MaxRender = 50;
    public Shader NaniteShader;
    static Dictionary<int, Nanite> Nanites = new Dictionary<int, Nanite>();

    static GameObject Unanite;
    class Nanite{
        public Transform transform;
        public int quality;
        public MeshFilter meshFilter;
        public List<MeshFilter> childrenMeshFilter;
        public Dictionary<int, Mesh> LODs;
    }

    Vector3 cameraPosition;
    void Start(){
        DontDestroyOnLoad(gameObject);
        Nanites.Clear();
    }

    bool fisrt = true;
    float timing;
    void Update(){
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
                    CreateQuality(nanite.Value, quality, value);
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
                Nanites.Clear();
            }
        }
    }

    void CreateQuality(Nanite nanite, int quality, float value){
        if(nanite.LODs.Count > 0){
            nanite.LODs.Add(quality, SimplifyMeshFilter(nanite.LODs.Values.OrderBy(item => item.vertexCount).Last(), value));
        }else
            if(nanite.meshFilter.mesh){
                nanite.LODs.Add(10, SimplifyMeshFilter(nanite.meshFilter.mesh, 1));
            }
    }

    void ChangeQuality(int instanceID, Nanite nanite, int quality){
        if(Unanite)
        if(Unanite.activeSelf){
            if(nanite.quality != quality){
                nanite.quality = quality;
                var unanite = GameObject.Find(instanceID.ToString());
                if(!unanite){
                    unanite = new GameObject(instanceID.ToString());
                    unanite.AddComponent<MeshFilter>();
                    unanite.AddComponent<MeshRenderer>();
                    unanite.transform.position = Vector3.zero;
                    unanite.transform.rotation = Quaternion.identity;
                    unanite.transform.parent = Unanite.transform;
                }

                CombineInstance[] combine = new CombineInstance[nanite.childrenMeshFilter.Count + 1];
                for(int i = 0; i < nanite.childrenMeshFilter.Count; i++){
                    float distance = Mathf.Pow(Vector3.Distance(nanite.childrenMeshFilter[i].transform.position, cameraPosition), 1);
                    float value = (1 / MaxRender) * (distance - MinRender);
                    value = 1 - Mathf.Clamp(value, 0, 1);
                    quality = Convert.ToInt16(value * 10);

                    if(!nanite.LODs.ContainsKey(quality))
                        CreateQuality(nanite, quality, value);

                    combine[i+1].mesh = nanite.LODs[quality];
                    combine[i+1].transform = nanite.childrenMeshFilter[i].transform.localToWorldMatrix;
                    nanite.childrenMeshFilter[i].GetComponent<MeshRenderer>().enabled = false;
                }

                combine[0].mesh = nanite.LODs[quality];
                combine[0].transform = nanite.meshFilter.transform.localToWorldMatrix;
                nanite.meshFilter.gameObject.GetComponent<MeshRenderer>().enabled = false;


                Mesh mesh = new Mesh();
                mesh.name = "Unanite";
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(combine);

                unanite.GetComponent<MeshFilter>().sharedMesh = mesh;
                unanite.GetComponent<MeshRenderer>().material = nanite.meshFilter.GetComponent<MeshRenderer>().material;
            }
        }
    }


    Mesh SimplifyMeshFilter(Mesh mesh, float quality){
        var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
        meshSimplifier.Initialize(mesh);
        meshSimplifier.SimplifyMesh(quality);
        return meshSimplifier.ToMesh();
    }
}