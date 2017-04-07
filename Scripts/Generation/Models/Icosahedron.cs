using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public struct VertexPosition
{
    public int[] NAI { get; private set; }
    public int[] NAP { get; private set; }

    public VertexPosition(int[] ai, int[] ap)
    {
        NAI = ai;
        NAP = ap;
    }
}

public class Icosahedron
{
    static Dictionary<Vector3, VertexPosition> VertexMap = new Dictionary<Vector3, VertexPosition>();
    static Mesh[][] Geosphere = new Mesh[9][];
    public static VertexPosition[][] VertexInfo;
    static Vector3[] vDirections =
    {
           new Vector3(0, 1, 0),

           new Vector3(0f, 0.4472136f, -0.8944272f),
           new Vector3(0.8506508f,0.4472136f,-0.2763932f),                                                                                                                                                                                                                             
           new Vector3(0.5257311f,0.4472136f,0.7236068f),
           new Vector3(-0.5257311f,0.4472136f,0.7236068f),
           new Vector3(-0.8506508f,0.4472136f,-0.2763932f),
           new Vector3(0f, 0.4472136f, -0.8944272f),
           
           new Vector3(0.5257311f,-0.4472136f,-0.7236068f),
           new Vector3(0.8506508f,-0.4472136f,0.2763932f),
           new Vector3(0f,-0.4472136f, 0.8944272f),
           new Vector3(-0.8506508f,-0.4472136f,0.2763932f),
           new Vector3(-0.5257311f,-0.4472136f,-0.7236068f),
           new Vector3(0.5257311f,-0.4472136f,-0.7236068f),

           new Vector3(0, -1, 0)
    };

    static int[][] fNeighbours =
    {
        new int[] { 0,   16, 12,  8,  4,    5,  2,  1,    18, 17 },
        new int[] { 1,    0,  4,  5,  2,    3, 19, 18,    17, 16 },
        new int[] { 2,    1,  0,  4,  5,    6,  7,  3,    19, 18 },
        new int[] { 3,    2,  5,  6,  7,   11, 15, 19,    18,  1 },
        new int[] { 4,    0, 16, 12,  8,    9,  6,  5,     2,  1 },
        new int[] { 5,    4,  8,  9,  6,    7,  3,  2,     1,  0 },
        new int[] { 6,    5,  4,  8,  9,   10, 11,  7,     3,  2 },
        new int[] { 7,    6,  9, 10, 11,   15, 19,  3,     2,  5 },
        new int[] { 8,    4,  0, 16, 12,   13, 10,  9,     6,  5 },
        new int[] { 9,    8, 12, 13, 10,   11,  7,  6,     5,  4 },
        new int[] {10,    9,  8, 12, 13,   14, 15, 11,     7,  6 },
        new int[] {11,   10, 13, 14, 15,   19,  3,  7,     6,  9 },
        new int[] {12,    8,  4,  0, 16,   17, 14, 13,    10,  9 },
        new int[] {13,   12, 16, 17, 14,   15, 11, 10,     9,  8 },
        new int[] {14,   13, 12, 16, 17,   18, 19, 15,    11, 10 },
        new int[] {15,   14, 17, 18, 19,    3,  7, 11,    10, 13 },
        new int[] {16,   12,  8,  4,  0,    1, 18, 17,    14, 13 },
        new int[] {17,   16,  0,  1, 18,   19, 15, 14,    13, 12 },
        new int[] {18,   17, 16,  0,  1,    2,  3, 19,    15, 14 },
        new int[] {19,   18,  1,  2,  3,    7, 11, 15,    14, 17 }
    };

    public static int[] vLimits = new int[] { 3, 6, 15, 45, 153, 561, 2145, 8385, 33153 };

    public static Mesh[] CreateGeosphere(int subdivision)
    {
        Mesh[] r = new Mesh[20];
        if (Geosphere[subdivision] == null)
        {
            Geosphere[subdivision] = CreateComplexGeosphere(subdivision);
        }

        for(int i = 0; i < 20; i++)
        {
            r[i] = new Mesh();
            r[i].name = "icosahedron_triangle_" + i;
            r[i].vertices = Geosphere[subdivision][i].vertices;
            r[i].uv = Geosphere[subdivision][i].uv;
            r[i].triangles = Geosphere[subdivision][i].triangles;
        }
        return r;
    }
    static Mesh[] CreateComplexGeosphere(int subdivision)
    {
        Stopwatch sw = new Stopwatch();
        Stopwatch neighbourMapping = new Stopwatch();
        Stopwatch geometryGeneration = new Stopwatch();
        Stopwatch normalsGen = new Stopwatch();
        sw.Start();
        geometryGeneration.Start();
        int resolution = 1 << subdivision;
        int uVertices = 10 * resolution * resolution + 2;
        VertexInfo = new VertexPosition[20][];
        Mesh[] cm = new Mesh[20];

        Vector3[][] vertices = new Vector3[20][];
        Vector3[][] normals = new Vector3[20][];
        Vector2[][] uv = new Vector2[20][];
        int[][] triangles = new int[20][];
        
        int f = 0;
        ManualResetEvent[] handles = new ManualResetEvent[4];
        geometryGeneration.Start();
        for (int d = 1; d < 6; d++)
        {
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = new ManualResetEvent(false);
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                CreateUpwardTriangle(ref vertices[f], ref uv[f], ref triangles[f], subdivision, resolution, f, vDirections[0], vDirections[d], vDirections[d + 1]);
                handles[0].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                CreateDownwardTriangle(ref vertices[f + 1], ref uv[f + 1], ref triangles[f + 1], subdivision, resolution, f + 1, vDirections[d + 6], vDirections[d], vDirections[d + 1]);
                handles[1].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                CreateUpwardTriangle(ref vertices[f + 2], ref uv[f + 2], ref triangles[f + 2], subdivision, resolution, f + 2, vDirections[d + 1], vDirections[d + 6], vDirections[d + 7]);
                handles[2].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                CreateDownwardTriangle(ref vertices[f + 3], ref uv[f + 3], ref triangles[f + 3], subdivision, resolution, f + 3, vDirections[13], vDirections[d + 6], vDirections[d + 7]);
                handles[3].Set();
            });
            WaitHandle.WaitAll(handles);
            f += 4;
        }
        geometryGeneration.Stop();
        UnityEngine.Debug.Log("Geometry took " + geometryGeneration.ElapsedMilliseconds + "ms.");
        
        // Mapping the geometry
        neighbourMapping.Start();
        for (f = 0; f < 20;)
        {
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = new ManualResetEvent(false);
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                MapVertices(ref VertexInfo[f], f, resolution, vLimits[subdivision]);
                handles[0].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                MapVertices(ref VertexInfo[f + 1], f + 1, resolution, vLimits[subdivision]);
                handles[1].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                MapVertices(ref VertexInfo[f + 2], f + 2, resolution, vLimits[subdivision]);
                handles[2].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                MapVertices(ref VertexInfo[f + 3], f + 3, resolution, vLimits[subdivision]);
                handles[3].Set();
            });
            WaitHandle.WaitAll(handles);
            f += 4;
        }
        neighbourMapping.Stop();
        UnityEngine.Debug.Log("Mapping took " + neighbourMapping.ElapsedMilliseconds + "ms.");

        for (int i = 0; i < cm.Length; i++)
        {
            cm[i] = new Mesh();
            cm[i].name = "" + i;
            cm[i].vertices = vertices[i];
            cm[i].triangles = triangles[i];
            cm[i].uv = uv[i];
            cm[i].normals = normals[i];
        }

        sw.Stop();
        UnityEngine.Debug.Log("Generation took " + sw.ElapsedMilliseconds + "ms.");
        return cm;
    }
    static void CreateUpwardTriangle(ref Vector3[] vertices, ref Vector2[] uv, ref int[] triangles, int subdivision, int resolution, int faceIndex, Vector3 vd0, Vector3 vd1, Vector3 vd2)
    {
        int vi = 0, ti = 0, vTop = 0;
        vertices = new Vector3[vLimits[subdivision]];
        uv = new Vector2[vLimits[subdivision]];
        triangles = new int[resolution * resolution * 3];

        for (int i = 0; i < resolution + 1; i++)
        {
            float progress = (float)i / (resolution);
            Vector3 from, to;
            Vector2 fromUV, toUV;

            from = Vector3.Lerp(vd0, vd1, progress);
            fromUV = Vector2.Lerp(new Vector2(0.5f, 1f), new Vector2(0f, 0f), progress);
            to = Vector3.Lerp(vd0, vd2, progress);
            toUV = Vector2.Lerp(new Vector2(0.5f, 1f), new Vector2(1f, 0f), progress);
            if (i < resolution)
            {
                createUpwardStrip(ref triangles, i + 1, vi + i + 1, ref vTop, ref ti);
            }
            for (int m = 0; m <= i; m++)
            {
                float sProgress = 0;
                if (i == 0) sProgress = (float)m / 1;
                else sProgress = (float)m / i;
                uv[vi] = Vector2.Lerp(fromUV, toUV, sProgress);
                Vector3 p = Vector3.Lerp(from, to, sProgress).normalized;
                vertices[vi++] = p;
            }
        }
    }

    static void CreateDownwardTriangle(ref Vector3[] vertices, ref Vector2[] uv, ref int[] triangles, int subdivision, int resolution, int faceIndex, Vector3 vd0, Vector3 vd1, Vector3 vd2)
    {
        int vi = 0, ti = 0, vTop = 0;
        vertices = new Vector3[vLimits[subdivision]];
        uv = new Vector2[vLimits[subdivision]];
        triangles = new int[resolution * resolution * 3];

        for (int i = resolution; i >= 0; i--)
        {
            float progress = (float)i / resolution;
            Vector3 from, to;
            Vector2 fromUV, toUV;

            from = Vector3.Lerp(vd0, vd1, progress);
            fromUV = Vector2.Lerp(new Vector2(0.5f, 0f), new Vector2(0f, 1f), progress);
            to = Vector3.Lerp(vd0, vd2, progress);
            toUV = Vector2.Lerp(new Vector2(0.5f, 0f), new Vector2(1f, 1f), progress);
            for (int m = 0; m < i + 1; m++)
            {
                float sProgress = 0;
                if (i == 0) sProgress = (float)m / 1;
                else sProgress = (float)m / i;
                uv[vi] = Vector2.Lerp(fromUV, toUV, sProgress);
                Vector3 p = Vector3.Lerp(from, to, sProgress).normalized;
                vertices[vi++] = p;
            }
            if (i > 0)
            {
                createDownwardStrip(ref triangles, i, vi, ref vTop, ref ti);
            }
        }
    }

    static void createUpwardStrip(ref int[] triangles, int steps, int vBot, ref int vTop, ref int ti)
    {
        triangles[ti++] = vBot;
        triangles[ti++] = vTop;
        triangles[ti++] = ++vBot;

        for (int i = 1; i < steps; i++)
        {
            triangles[ti++] = vTop++;
            triangles[ti++] = vTop;
            triangles[ti++] = vBot;

            triangles[ti++] = vBot++;
            triangles[ti++] = vTop;
            triangles[ti++] = vBot;
        }
        vTop++; // vTop should be increased after each of this function for per polygon generation, was vTop incremented outside of here in solid generation?
    }
    static void createDownwardStrip(ref int[] triangles, int steps, int vBot, ref int vTop, ref int ti)
    {
        triangles[ti++] = vTop++;
        triangles[ti++] = vTop;
        triangles[ti++] = vBot;

        for (int i = 1; i < steps; i++)
        {
            triangles[ti++] = vBot++;
            triangles[ti++] = vTop;
            triangles[ti++] = vBot;

            triangles[ti++] = vTop++;
            triangles[ti++] = vTop;
            triangles[ti++] = vBot;
        }
        vTop++; // vTop should be increased after each of this function for per polygon generation, was vTop incremented outside of here in solid generation?
    }

    static void MapVertices(ref VertexPosition[] vInfo, int f, int r, int vl)
    {
        vInfo = new VertexPosition[vl];
        int vi = 0, rt = 0, lt = r;
        vl -= 1;

        // figuring out what surrounds the triangle to simplify the assignment code

        if(f % 2 == 0)
        {
            for (int i = 0; i <= r; i++)
            {
                for (int m = 0; m <= i; m++)
                {
                    if (i == 0)
                    {
                        if ((f + 4) % 4 == 0)
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { fNeighbours[f][1], fNeighbours[f][2], fNeighbours[f][3], fNeighbours[f][4], fNeighbours[f][0] },
                                new int[] { 2, 2, 2, 2, 2});
                        }
                        else
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { fNeighbours[f][1], fNeighbours[f][2], fNeighbours[f][3], fNeighbours[f][4], fNeighbours[f][0], fNeighbours[f][0] },
                                new int[] { r - 1, vl - r - 1, vl - r + 1, r + 1, 2, 1 });
                        }
                    }
                    else if (i == r)
                    {
                        if(m == 0)
                        {
                            if ((f + 4) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][7], fNeighbours[f][8], fNeighbours[f][9], fNeighbours[f][1] },
                                    new int[] { vi - i, 1, 2, r + r, vl - 1 });
                            }
                            else
                            {
                                vInfo[vi] = new VertexPosition(
                                new int[] { fNeighbours[f][0], fNeighbours[f][7], fNeighbours[f][8], fNeighbours[f][9], fNeighbours[f][1]  },
                                new int[] { vi - i, 1, r + r, vl - 1, vl - 2  });
                            }
                        }
                        else if(m == i)
                        {
                            if ((f + 4) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][5], fNeighbours[f][6], fNeighbours[f][7] },
                                    new int[] { vi - 1, vi - i - i, 1, 2, r + r, });
                            }
                            else
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][5], fNeighbours[f][6], fNeighbours[f][7] },
                                    new int[] { vi - 1, vi - 2, vi - i - i, 1, r + r });
                            }
                        }
                        else
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { f, f, f, f, f + 1, f + 1 },
                                new int[] { vi - 1, vi - i - 1, vi - i, vi + 1, i + i + 1 - (vl - vi), i + i - (vl - vi) });
                        }
                    }
                    else
                    {
                        if (m == 0)
                        {
                            if ((f + 4) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][1], fNeighbours[f][1] },
                                    new int[] { vi - i, vi + 1, vi + i + 2, vi + i + 1, vi + i + i + 1, vi + i - 1 });
                            }
                            else
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][1], fNeighbours[f][1] },
                                    new int[] { vi - i,vi + 1,vi + i + 2,vi + i + 1,lt + r - i,lt - 1 });
                                lt += r - i + 1;
                            }
                        }
                        else if (m == i)
                        {
                            if ((f + 4) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][4] },
                                    new int[] { vi + i + 2, vi + i + 1, vi - 1, vi - i - 1, vi - i + 1, vi + 2 });
                            }
                            else
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][4] },
                                    new int[] { vi + i + 2, vi + i + 1, vi - 1, vi - i - 1, rt + 1, rt + r - i + 3 });
                                rt += r - i + 2;
                            }
                        }
                        else
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { f, f, f, f, f, f },
                                new int[] { vi - i, vi + 1, vi + i + 2, vi + i + 1, vi - 1, vi - i - 1 });
                        }
                    }
                    vi++;
                }
            }
        }
        else if (f % 2 == 1)
        {
            for (int i = r; i >= 0; i--)
            { 
                for (int m = 0; m <= i; m++)
                {
                    if (i == 0)
                    {
                        if ((f + 1) % 4 == 0)
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][5], fNeighbours[f][6] , fNeighbours[f][7] },
                                new int[] { vi - 2, vi - 2, vi - 2, vi - 2, vi - 2 });
                        }
                        else
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][5], fNeighbours[f][6], fNeighbours[f][7] },
                                new int[] { vi - 2, vi - r - r, 1, r + r, vi - 1 });
                        }
                    }
                    else if (i == r)
                    {
                        if(m == 0)
                        {
                            if ((f + 1) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][7], fNeighbours[f][8], fNeighbours[f][9], fNeighbours[f][1] },
                                    new int[] { 1, r + r, vl - 1, vl - 2, vl - r - r });
                            }
                            else
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][7], fNeighbours[f][8], fNeighbours[f][9], fNeighbours[f][1] },
                                    new int[] { 1, 2, r + r, vl - 1, vl - r - r });
                            }
                        }
                        else if(m == i)
                        {
                            if ((f + 1) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][1], fNeighbours[f][2], fNeighbours[f][3], fNeighbours[f][4] },
                                    new int[] { vi + i, vl - 1, vl - 2, vl - r - r, 1 });
                            }
                            else
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][1], fNeighbours[f][2], fNeighbours[f][3], fNeighbours[f][4] },
                                    new int[] { vi + i, vl - 1, vl - r - r, 1, 2 });
                            }
                        }
                        else
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { f, f, f, f, f - 1, f - 1 },
                                new int[] { vi + 1, vi + i + 1, vi + i, vi - 1, vl - r - r + vi - 1, vl - r - r + vi });
                        }
                    }
                    else
                    {
                        if (m == 0)
                        {

                            if ((f + 1) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][7], fNeighbours[f][7] },
                                    new int[] { vi - i - 2, vi - i - 1, vi + 1, vi + i + 1, vi + i - 1, vi - 2 });
                            }
                            else
                            {
                                rt += r - i + 1;
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][7], fNeighbours[f][7] },
                                    new int[] { vi - i - 2, vi - i - 1, vi + 1, vi + i + 1, rt + r - i + 1, rt - 1 });
                            }
                        }
                        else if (m == i)
                        {
                            if ((f + 1) % 4 == 0)
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][4] },
                                    new int[] { vi + i, vi - 1, vi - i - 2, vi - i - 1, vi - i - i - 1, vi - i + 1 });
                            }
                            else
                            {
                                vInfo[vi] = new VertexPosition(
                                    new int[] { fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][0], fNeighbours[f][4], fNeighbours[f][4] },
                                    new int[] { vi + i, vi - 1, vi - i - 2, vi - i - 1, rt - r + i + 1, rt + 2 });
                            }
                        }
                        else
                        {
                            vInfo[vi] = new VertexPosition(
                                new int[] { f, f, f, f, f, f },
                                new int[] { vi - i - 1, vi + 1, vi + i + 1, vi + i, vi - 1, vi - i - 2 });

                        }
                    }
                    vi++;
                }
            }
        }
    }
    public static Vector3 ComputeFaceNormal(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 e0 = v1 - v0;
        Vector3 e1 = v2 - v0;
        Vector3 r;

        r.x = e0.y * e1.z - e0.z * e1.y;
        r.y = e0.z * e1.x - e0.x * e1.z;
        r.z = e0.x * e1.y - e0.y * e1.x;

        return r;
    }

    public static Vector3[][] RecalculateNormals(Mesh[] mesh)
    {
        Vector3[][] normals = new Vector3[mesh.Length][];
        Vector3[][] vertices = new Vector3[mesh.Length][];
        for(int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = mesh[i].vertices;
        }
        ManualResetEvent[] handles = new ManualResetEvent[4];

        for (int f = 0; f < 20;)
        {
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = new ManualResetEvent(false);
            }
            ThreadPool.QueueUserWorkItem((o) =>
            {
                tRecalculateNormals(vertices, ref normals[f], VertexInfo[f], f);
                handles[0].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                tRecalculateNormals(vertices, ref normals[f + 1], VertexInfo[f + 1], f + 1);
                handles[1].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                tRecalculateNormals(vertices, ref normals[f + 2], VertexInfo[f + 2], f + 2);
                handles[2].Set();
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                tRecalculateNormals(vertices, ref normals[f + 3], VertexInfo[f + 3], f + 3);
                handles[3].Set();
            });
            WaitHandle.WaitAll(handles);
            f += 4;
        }

        return normals;
    }
    static void tRecalculateNormals(Vector3[][] vertices, ref Vector3[] normals, VertexPosition[] vInfo, int f)
    {
        normals = new Vector3[vertices[f].Length];
        int vi;
        int i;
        for (vi = 0; vi < normals.Length; vi++)
        {
            Vector3 n = Vector3.zero;
            for (i = 0; i < vInfo[vi].NAI.Length - 1; i++)
            {
                Vector3 fn = ComputeFaceNormal(
                      vertices[f][vi],
                      vertices[vInfo[vi].NAI[i]][vInfo[vi].NAP[i]],
                      vertices[vInfo[vi].NAI[i + 1]][vInfo[vi].NAP[i + 1]]);
                n += fn;
            }
            Vector3 fnl = ComputeFaceNormal(vertices[f][vi], vertices[vInfo[vi].NAI[i]][vInfo[vi].NAP[i]], vertices[vInfo[vi].NAI[0]][vInfo[vi].NAP[0]]);
            n += fnl;
            normals[vi] = n;
        }
    }
}