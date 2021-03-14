using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class Waves : MonoBehaviour
{
    [Serializable]
    public struct Octave{
        public Vector2 Speed;
        public Vector2 Scale;
        public float Height;
        public bool Alternate;
    }

    public int Dimension = 10;
    public Octave[] Octaves;
    public float UVScale = 1;

    private MeshFilter _meshFilter;
    private Mesh _mesh;

    // Start is called before the first frame update
    void Start()
    {
        _mesh = new Mesh();
        _mesh.name = gameObject.name;
        _mesh.vertices = GenerateVertices();
        _mesh.triangles = GenerateTriangles();
        _mesh.uv = GenerateUVs();
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();//??
        
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshFilter.mesh = _mesh;
    }

     public float GetHeight(Vector3 position)
    {
        //scale factor and position in local space
        var scale = new Vector3(1 / transform.lossyScale.x, 0, 1 / transform.lossyScale.z);
        var localPos = Vector3.Scale((position - transform.position), scale);

        //get edge points
        var p1 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Floor(localPos.z));
        var p2 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Ceil(localPos.z));
        var p3 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Floor(localPos.z));
        var p4 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Ceil(localPos.z));

        //clamp if the position is outside the plane
        p1.x = Mathf.Clamp(p1.x, 0, Dimension);
        p1.z = Mathf.Clamp(p1.z, 0, Dimension);
        p2.x = Mathf.Clamp(p2.x, 0, Dimension);
        p2.z = Mathf.Clamp(p2.z, 0, Dimension);
        p3.x = Mathf.Clamp(p3.x, 0, Dimension);
        p3.z = Mathf.Clamp(p3.z, 0, Dimension);
        p4.x = Mathf.Clamp(p4.x, 0, Dimension);
        p4.z = Mathf.Clamp(p4.z, 0, Dimension);

        //get the max distance to one of the edges and take that to compute max - dist
        var max = Mathf.Max(Vector3.Distance(p1, localPos), Vector3.Distance(p2, localPos), Vector3.Distance(p3, localPos), Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        var dist = (max - Vector3.Distance(p1, localPos))
                 + (max - Vector3.Distance(p2, localPos))
                 + (max - Vector3.Distance(p3, localPos))
                 + (max - Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        //weighted sum
        var height = _mesh.vertices[Index((int)p1.x, (int)p1.z)].y * (max - Vector3.Distance(p1, localPos))
                   + _mesh.vertices[Index((int)p2.x, (int)p2.z)].y * (max - Vector3.Distance(p2, localPos))
                   + _mesh.vertices[Index((int)p3.x, (int)p3.z)].y * (max - Vector3.Distance(p3, localPos))
                   + _mesh.vertices[Index((int)p4.x, (int)p4.z)].y * (max - Vector3.Distance(p4, localPos));

        //scale
        return height * transform.lossyScale.y / dist;

    }

    private Vector2[] GenerateUVs()
    {
        var uvs = new Vector2[_mesh.vertices.Length];

        for (var x = 0; x <= Dimension; x++)
        {
            for (var z = 0; z <= Dimension; z++)
            {
                var vec = new Vector2((x / UVScale) % 2, (z / UVScale) % 2);
                uvs[Index(x, z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x,
                                                 vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }

        return uvs;
    }

    private const int QuadVerticesCount = 6;
    private int[] GenerateTriangles()
    {
        var triangles = new int[_mesh.vertices.Length * QuadVerticesCount];

        for (var x = 0; x < Dimension; x++)
        {
            for (var z = 0; z < Dimension; z++)
            {
                triangles[Index(x, z) * QuadVerticesCount + 0] = Index(x,z);
                triangles[Index(x, z) * QuadVerticesCount + 1] = Index(x + 1, z + 1);
                triangles[Index(x, z) * QuadVerticesCount + 2] = Index(x+1, z);
                triangles[Index(x, z) * QuadVerticesCount + 3] = Index(x, z);
                triangles[Index(x, z) * QuadVerticesCount + 4] = Index(x, z+ 1);
                triangles[Index(x, z) * QuadVerticesCount + 5] = Index(x + 1, z + 1);
            }
        }

        return triangles;
    }

    private Vector3[] GenerateVertices()
    {
        var verts = new Vector3[(Dimension + 1) * (Dimension + 1)];

        for (var x = 0; x <= Dimension; x++)
        {
            for (var z = 0; z <= Dimension; z++)
            {
                verts[Index(x, z)] = new Vector3(x, 0, z);
            }
        }

        return verts;
    }

    private int Index(int x, int z){
        return x * (Dimension + 1) + z;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVertices();
        _mesh.RecalculateNormals();
    }

    private void UpdateVertices(){
        var verts = _mesh.vertices;

        for (var x = 0; x <= Dimension; x++)
        {
            for (var z = 0; z <= Dimension; z++)
            {
                var y = CalculateYPosition(x, z);
                verts[Index(x,z)] = new Vector3(x, y, z);
            }
        }

        _mesh.vertices = verts;
    }

    private float CalculateYPosition(int x, int z){
        float y = 0f;

        for (var o = 0; o < Octaves.Length; o++)
        {
            var octave = Octaves[o];

            if(octave.Alternate){
                var perl = Mathf.PerlinNoise((x * octave.Scale.x)/Dimension, (z * octave.Scale.y) / Dimension) * Mathf.PI * 2f;
                y += Mathf.Cos(perl + octave.Speed.magnitude * Time.time) * octave.Height;
            }
            else
            {
                var perl = Mathf.PerlinNoise((x * octave.Scale.x + Time.time * octave.Speed.x)/Dimension, 
                                                (z * octave.Scale.y + Time.time * octave.Speed.y) / Dimension) - 0.5f;
                y += perl * octave.Height;
            }
            
        }

        return y;
    }
}
