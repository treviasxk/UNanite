using System.Collections.Generic;
using UnityEngine;

public class NaniteRuntime : MonoBehaviour{
    
    public float MinRender = 5f;
    public float MaxRender = 50;
    public Shader NaniteShader;
    static List<Nanite> Nanites = new List<Nanite>();
    class Nanite{
        public Transform coordinates;
        public LOD lod;
        public string quality;
        public Mesh mesh;
        public Shader shader;
        public MeshFilter meshFilter;
        public Color[] viewTriangles;
        public Material material;
    }

    Dictionary<string, Mesh> LODs = new Dictionary<string, Mesh>();

    void Start(){//
        if(NaniteEditor.isNanite){
            Nanites.Clear();
            LODs.Clear();
            DontDestroyOnLoad(gameObject);
            foreach(GameObject nanite in FindObjectsOfType<GameObject>()){
                if(nanite.GetComponent<MeshFilter>()){
                    if(nanite.GetComponent<MeshRenderer>().material){
                        // Generate color triangles
                        Vector3[] vertices = nanite.GetComponent<MeshFilter>().mesh.vertices;
                        Color[] colors = new Color[vertices.Length];
                        for(int i = 0; i < vertices.Length; i++)
                            colors[i] = new Color(Random.Range(0.0f, 1.0f),Random.Range(0.0f, 1.0f),Random.Range(0.0f, 1.0f),1.0f);
                        
                        Nanites.Add(new Nanite(){mesh = nanite.GetComponent<MeshFilter>().mesh, quality = "1", material = nanite.GetComponent<MeshRenderer>().material, meshFilter = nanite.GetComponent<MeshFilter>(), viewTriangles = colors, shader = nanite.GetComponent<MeshRenderer>().material.shader, coordinates = nanite.transform});
                    }
                }
            }
        }
    }

    bool clear = false;
    void Update() {
        if(NaniteEditor.isNanite){
            clear = true;
            foreach(Nanite nanite in Nanites){           
                foreach(Camera camera in Camera.allCameras){
                    float distance = Mathf.Pow(Vector3.Distance(nanite.coordinates.position, camera.gameObject.transform.position), 1);
                    float value = (1f / MaxRender) * (distance - MinRender);
                    value = 1f - Mathf.Clamp(value, 0f, 1f);
                    string quality = value.ToString("0.0");

                    if(!LODs.ContainsKey(quality))
                        LODs.Add(quality, SimplifyMeshFilter(nanite.mesh, value));

                    if(nanite.quality != quality){
                        nanite.quality = quality;
                        nanite.meshFilter.mesh = LODs[quality];
                        if(NaniteEditor.isViewTriangle){
                            if(nanite.material.shader != NaniteShader)
                                nanite.material.shader = NaniteShader;
                            nanite.mesh.colors = nanite.viewTriangles;
                        }else
                            if(nanite.material.shader != nanite.shader)
                                nanite.material.shader = nanite.shader;
                    }
                }
            }
        }else{
            if(clear){
                clear = false;
                foreach(Nanite nanite in Nanites){
                    nanite.meshFilter.mesh = nanite.mesh;
                    nanite.material.shader = nanite.shader;
                    nanite.quality = "1";
                }
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