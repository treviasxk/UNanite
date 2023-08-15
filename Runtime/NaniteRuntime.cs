using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityMeshSimplifier;

public class NaniteRuntime : MonoBehaviour{
    
    public float MinRender = 5f;
    public float MaxRender = 50;
    public Shader NaniteShader;
    static List<Nanite> Nanites = new List<Nanite>();
    class Nanite{
        public Transform transform;
        public int quality;
        public MeshFilter meshFilter;
        public Dictionary<int, Mesh> LODs = new Dictionary<int, Mesh>();
    }

    Vector3 cameraPosition;
    void Start(){
        DontDestroyOnLoad(gameObject);
    }

    bool fisrt = true;
    float timing;
    void Update(){
        if(NaniteEditor.isNanite){
            if(fisrt){
                fisrt = false;
                foreach(GameObject nanite in FindObjectsOfType<GameObject>())
                    if(nanite.GetComponent<MeshFilter>())
                        Nanites.Add(new Nanite(){quality = 10, meshFilter = nanite.GetComponent<MeshFilter>(), transform = nanite.transform});
            }
            cameraPosition = Camera.main.gameObject.transform.position;
            foreach(Nanite nanite in Nanites.OrderBy(item => item.quality)){
                float distance = Mathf.Pow(Vector3.Distance(nanite.transform.position, cameraPosition), 1);
                float value = (1 / MaxRender) * (distance - MinRender);
                value = 1 - Mathf.Clamp(value, 0, 1);
                int quality = Convert.ToInt16(value * 10);

                if(!nanite.LODs.ContainsKey(quality)){
                    if(nanite.LODs.Count > 0){
                        nanite.LODs.Add(quality, SimplifyMeshFilter(nanite.LODs.Values.OrderBy(item => item.vertexCount).Last(), value));
                    }else
                        if(nanite.meshFilter.mesh)
                            nanite.LODs.Add(10, SimplifyMeshFilter(nanite.meshFilter.mesh, 10));
                }else
                    if(nanite.quality != quality){
                        nanite.quality = quality;
                        nanite.meshFilter.mesh = nanite.LODs[quality];
                    }
            }
        }else{
            if(!fisrt){
                fisrt = true;
                foreach(Nanite nanite in Nanites){
                    if(nanite.quality != 10){
                        nanite.quality = 10;
                        nanite.meshFilter.mesh = nanite.LODs[10];
                    }
                }
                Nanites.Clear();
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