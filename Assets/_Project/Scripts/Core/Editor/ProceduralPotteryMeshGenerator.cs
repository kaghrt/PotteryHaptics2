using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PotteryHaptics.Core.Editor
{
    /// <summary>
    /// 器(Bowl)のプロシージャルメッシュ生成。断面プロファイルをCatmull-Romスプラインで補間し、
    /// Y軸周りに64分割で回転(revolve)させて滑らかな器形状を作る。
    /// プロファイル自体は差し替え可能なため、JND実験用の厚み違いバリエーション生成にも転用できる。
    /// </summary>
    public static class ProceduralPotteryMeshGenerator
    {
        private const string OutputAssetPath = "Assets/_Project/Models/Bowl_Procedural.asset";
        private const int RadialSegments = 64;
        private const float CmToUnit = 0.01f;

        // (半径cm, 高さcm) の断面制御点。高台(削り跡)の段差を含む。
        private static readonly Vector2[] DefaultProfileControlPointsCm =
        {
            new Vector2(0f, 0f),
            new Vector2(4.5f, 0f),
            new Vector2(4.5f, 0.6f), // 高台の段差
            new Vector2(3.6f, 1.2f),
            new Vector2(6.0f, 3.5f),
            new Vector2(8.5f, 8.0f),
            new Vector2(9.2f, 12.0f),
            new Vector2(8.7f, 15.0f),
            new Vector2(8.9f, 15.4f), // 縁のわずかな返り
        };

        [MenuItem("HapticResearch/Generate Bowl Mesh (Procedural)")]
        public static void GenerateBowlMeshMenuItem()
        {
            GenerateBowlMesh();
        }

        public static Mesh GenerateBowlMesh()
        {
            return GenerateBowlMesh(DefaultProfileControlPointsCm, RadialSegments);
        }

        public static Mesh GenerateBowlMesh(Vector2[] profileControlPointsCm, int radialSegments)
        {
            List<Vector2> profile = SampleCatmullRomProfile(profileControlPointsCm, 24);
            Mesh mesh = BuildRevolveMesh(profile, radialSegments);

            Directory.CreateDirectory("Assets/_Project/Models");

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(OutputAssetPath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AssetDatabase.CreateAsset(mesh, OutputAssetPath);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static List<Vector2> SampleCatmullRomProfile(Vector2[] controlPointsCm, int samplesPerSegment)
        {
            var result = new List<Vector2>();
            int count = controlPointsCm.Length;

            for (int i = 0; i < count - 1; i++)
            {
                Vector2 p0 = controlPointsCm[Mathf.Max(i - 1, 0)];
                Vector2 p1 = controlPointsCm[i];
                Vector2 p2 = controlPointsCm[i + 1];
                Vector2 p3 = controlPointsCm[Mathf.Min(i + 2, count - 1)];

                int samples = i == count - 2 ? samplesPerSegment + 1 : samplesPerSegment;
                for (int s = 0; s < samples; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    result.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            return result;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private static Mesh BuildRevolveMesh(List<Vector2> profileCm, int radialSegments)
        {
            int profileCount = profileCm.Count;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int ring = 0; ring <= radialSegments; ring++)
            {
                float angle = ring / (float)radialSegments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                for (int p = 0; p < profileCount; p++)
                {
                    Vector2 profilePoint = profileCm[p];
                    float radius = profilePoint.x * CmToUnit;
                    float height = profilePoint.y * CmToUnit;

                    vertices.Add(new Vector3(radius * cos, height, radius * sin));
                    uvs.Add(new Vector2(ring / (float)radialSegments, p / (float)(profileCount - 1)));
                }
            }

            for (int ring = 0; ring < radialSegments; ring++)
            {
                for (int p = 0; p < profileCount - 1; p++)
                {
                    int current = ring * profileCount + p;
                    int next = (ring + 1) * profileCount + p;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            var mesh = new Mesh { name = "Bowl_Procedural" };
            mesh.indexFormat = vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
