using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AOT;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Linq;

using EigenCore.Core.Sparse;
using EigenCore.Core.Dense;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Storage;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Collections.Concurrent;

public class RenderEndpointsObjectSpace : MonoBehaviour
{
    public bool Verbose = false;
    public List<Vector3> EndpointCoords;
    public Vector3 RootpointCoords;
    public int ThinningStepOfInterest = 3;

    public GameObject EndpointObject;

    void OnDrawGizmos()
    {
        return;
        Matrix4x4 localToWorld = transform.localToWorldMatrix;
        if (EndpointCoords != null)
        {
            Gizmos.color = Color.red;
            foreach (var endpointCoord in EndpointCoords)
            {
                Gizmos.DrawSphere(localToWorld.MultiplyPoint3x4(endpointCoord), 3f);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(localToWorld.MultiplyPoint3x4(RootpointCoords), 4f);
        }
    }

    async void Start()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;


        // ConfigureAwait(true) makes sure that the rest of the method runs on the main thread again
        (Vector3[] newVertices, int[] newFaces, List<int> endpoints, int rootPoint) = await ExtractSkeleton(mesh).ConfigureAwait(true);
        var newMesh = new Mesh();
        newMesh.SetVertices(newVertices);
        newMesh.SetTriangles(newFaces, 0);
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();
        newMesh.RecalculateTangents();

        GameObject skeleton = new();
        skeleton.name = "Skeleton";
        skeleton.AddComponent<MeshFilter>().mesh = newMesh;
        skeleton.AddComponent<MeshRenderer>();
        skeleton.transform.SetParent(transform, false);



        var vertices = GetComponent<MeshFilter>().sharedMesh.vertices;

        EndpointCoords = endpoints.Where(i => i != rootPoint).Select(i => vertices[i]).ToList();
        RootpointCoords = vertices[rootPoint];


        foreach (var endpoint in EndpointCoords)
        {
            GameObject endpointGameObject = Instantiate(EndpointObject);
            endpointGameObject.name = "Endpoint";
            endpointGameObject.transform.SetParent(transform, false);
            endpointGameObject.transform.localPosition = endpoint;
        }
        GameObject rootPointGameObject = new();
        rootPointGameObject.name = "Rootpoint";
        rootPointGameObject.transform.SetParent(transform, false);
        rootPointGameObject.transform.localPosition = RootpointCoords;
    }

    async Task<(Vector3[] vertices, int[] faces, List<int> endpoints, int rootPoint)> ExtractSkeleton(Mesh mesh)
    {
        var meshEmpty = mesh.vertices.Length == 0;
        if (meshEmpty)
        {
            throw new Exception("The Mesh appears to be empty or not loaded. Consider using OnBecameVisible.");
        }

        Debug.Log("Started - First Calcualation may take some time");
        ConvertMesh(mesh, out DenseMatrix vertices, out int[,] faces);
        //LoadMeshFromObjFile("Assets/Models/vesselTree.obj", out DenseMatrix vertices, out int[,] faces);

        // Using Task.Factory.StartNew with TaskCreationOptions.LongRunning seems to force the use of background threads better than Task.Run
        // This prevents freezing the UI
        (var skeletonVertices, var thinnedMesh) = await Task.Factory.StartNew(() => 
            SkeletonExtraction(vertices, faces, 5f, 10), TaskCreationOptions.LongRunning);

        List<int> endpoints = await Task.Factory.StartNew(() => 
            FindEndpoints(skeletonVertices, faces), TaskCreationOptions.LongRunning);

        int rootPoint = await Task.Factory.StartNew(() =>
            FindRootPoint(vertices, faces, endpoints), TaskCreationOptions.LongRunning);


        Vector3[] newVerts = new Vector3[thinnedMesh.RowCount];
        for (int i = 0; i < thinnedMesh.RowCount; i++)
        {
            newVerts[i] = new Vector3((float)thinnedMesh[i, 0], (float)thinnedMesh[i, 1], (float)thinnedMesh[i, 2]);
        }

        int[] newFaces = new int[faces.Length];
        Buffer.BlockCopy(faces, 0, newFaces, 0, faces.Length * sizeof(int));
        Debug.Log("Finished - Endpoints may be saved to increase Perfromance for next run");
        return (newVerts, newFaces, endpoints, rootPoint);
    }

    int FindRootPoint(DenseMatrix originalVertices, int[,] faces, List<int> endpoints)
    {
        if (endpoints.Count == 0)
            return -1;

        SparseMatrix laplaceCotanMatrix = (SparseMatrix)(LaplaceCotanMatrix(originalVertices, faces) / 2);
        SparseMatrix M = VoronoiMassMatrix(originalVertices, faces);
        InvertDiagonalEntries(M);
        // laplace beltrami
        DenseMatrix HN = (DenseMatrix)(-M * (laplaceCotanMatrix * originalVertices));
        // mean curvature
        Vector<double> curvature = HN.RowNorms(2);

        int minIdx = endpoints[0];
        double minCurvature = curvature[minIdx];
        foreach (var endpointIndex in endpoints)
        {
            if (curvature[endpointIndex] < minCurvature)
            {
                minIdx = endpointIndex;
                minCurvature = curvature[endpointIndex];
            }
        }

        return minIdx;
    }

    private void InvertDiagonalEntries(SparseMatrix m)
    {
        m.MapIndexedInplace((i, j, v) =>
        {
            if (i == j)
                return 1 / v;
            return v;
        }, Zeros.AllowSkip);
    }

    private SparseMatrix VoronoiMassMatrix(DenseMatrix originalVertices, int[,] faces)
    {
        // this can be optimized a lot!!

        DenseMatrix edgeLengths = EdgeLengths(originalVertices, faces);

        int n = faces.Cast<int>().Max() + 1;
        int m = faces.GetLength(0);

        var doubleArea = GetDoubleArea(edgeLengths, 0).Column(0);
        int[] coordinate = new int[m * 3];
        for (int i = 0; i < 3; i++)
        {
            int offset = i * m;
            for (int j = 0; j < m; j++)
            {
                coordinate[offset + j] = faces[j, i];
            }
        }

        Vector<double> l0 = edgeLengths.Column(0),
            l1 = edgeLengths.Column(1),
            l2 = edgeLengths.Column(2),
            l0Squared = l0.PointwisePower(2),
            l1Squared = l1.PointwisePower(2),
            l2Squared = l2.PointwisePower(2);

        DenseMatrix cosines = (DenseMatrix)DenseMatrix.Build.DenseOfColumnVectors(
            (l2Squared + l1Squared - l0Squared).PointwiseDivide(l1.PointwiseMultiply(l2) * 2),
            (l0Squared + l2Squared - l1Squared).PointwiseDivide(l2.PointwiseMultiply(l0) * 2),
            (l1Squared + l0Squared - l2Squared).PointwiseDivide(l0.PointwiseMultiply(l1) * 2));

        DenseMatrix barycentric = (DenseMatrix)cosines.PointwiseMultiply(edgeLengths);
        barycentric = (DenseMatrix)barycentric.NormalizeRows(1);

        DenseMatrix partial = (DenseMatrix)DenseMatrix.Build.DenseOfColumnVectors(
            barycentric.EnumerateColumns().Select(v => v.PointwiseMultiply(doubleArea) * 0.5));

        Vector<double>[] quads = new[] {  
            (partial.Column(1) + partial.Column(2)) * 0.5,
            (partial.Column(2) + partial.Column(0)) * 0.5,
            (partial.Column(0) + partial.Column(1)) * 0.5 };

        double[] multipliers0 = new[] { 0.25, 0.125, 0.125 },
            multipliers1 = new[] { 0.125, 0.25, 0.125 },
            multipliers2 = new[] { 0.125, 0.125, 0.25 };

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < quads[i].Count; j++)
            {
                if (cosines[j, 0] < 0)
                    quads[i][j] = doubleArea[j] * multipliers0[i];

                if (cosines[j, 1] < 0)
                    quads[i][j] = doubleArea[j] * multipliers1[i];

                if (cosines[j, 2] < 0)
                    quads[i][j] = doubleArea[j] * multipliers2[i];
            }
        }

        DenseMatrix values = (DenseMatrix)DenseMatrix.Build.DenseOfMatrixArray(new[,]
        {
            { quads[0].ToColumnMatrix() },
            { quads[1].ToColumnMatrix() },
            { quads[2].ToColumnMatrix() }
        });

        SparseMatrix massMatrix = new(n, n);

        for (int i = 0; i < 3 * m; i++)
        {
            massMatrix[coordinate[i], coordinate[i]] += values[i, 0];
        }

        return massMatrix;
    }

    List<int> FindEndpoints(DenseMatrix skeletonVertices, int[,] faces)
    {
        int vertCount = skeletonVertices.RowCount;
        int faceCount = faces.GetLength(0);

        double averageEdgeLength = 0;
        for (int i = 0; i < faceCount; i++)
        {
            var a = skeletonVertices.Row(faces[i, 0]);
            var b = skeletonVertices.Row(faces[i, 1]);
            var c = skeletonVertices.Row(faces[i, 2]);
            averageEdgeLength += (b - a).L2Norm() + (c - b).L2Norm() + (a - c).L2Norm();
        }
        averageEdgeLength /= 3 * faceCount;

        double radius = averageEdgeLength * 3;



        ConcurrentBag<int> endpoints = new();

        Parallel.For(0, vertCount, iVertex =>
        {
            var center = skeletonVertices.Row(iVertex);

            int furthestVertexIndex = iVertex;
            double furthestDistance = 0;

            DenseMatrix centerRepeated = (DenseMatrix)DenseMatrix.Build.DenseOfRowVectors(Enumerable.Repeat(center, vertCount));
            DenseMatrix vertOffsetToCenter = skeletonVertices - centerRepeated;
            Vector<double> distToCenter = vertOffsetToCenter.RowNorms(2);

            List<int> inRadiusVertIndices = new();
            for (int i = 0; i < vertCount; i++)
            {
                var dist = distToCenter[i];
                if (dist < radius)
                {
                    inRadiusVertIndices.Add(i);
                    if (dist > furthestDistance)
                    {
                        furthestDistance = dist;
                        furthestVertexIndex = i;
                    }
                }
            }

            if (furthestVertexIndex != iVertex)
            {
                var furthestOffset = vertOffsetToCenter.Row(furthestVertexIndex);

                // is an endpoint if all points inside the radius are on the same hemisphere as the furthest vertex (found above)
                bool isEndpoint = inRadiusVertIndices.All(idx => vertOffsetToCenter.Row(idx).DotProduct(furthestOffset) >= 0);
                if (isEndpoint)
                    endpoints.Add(iVertex);
            }
        });

        return endpoints.ToList();
    }

    void ConvertMesh(Mesh mesh, out DenseMatrix vertices, out int[,] faces)
    {
        var originalVertices = mesh.vertices;
        var originalFaces = mesh.triangles;

        vertices = (DenseMatrix)DenseMatrix.Build.Dense(mesh.vertexCount, 3);
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vertices[i, 0] = originalVertices[i].x;
            vertices[i, 1] = originalVertices[i].y;
            vertices[i, 2] = originalVertices[i].z;
        }

        faces = new int[originalFaces.Length / 3, 3];
        Buffer.BlockCopy(originalFaces, 0, faces, 0, faces.Length * sizeof(int));
    }

    void LoadMeshFromObjFile(string path, out DenseMatrix vertices, out int[,] faces)
    {
        var lines = File.ReadAllLines(path);
        int vertexCount = lines.Count(v => v.StartsWith("v "));
        int faceCount = lines.Count(v => v.StartsWith("f "));

        vertices = (DenseMatrix)DenseMatrix.Build.Dense(vertexCount, 3);
        foreach ((int index, var vertex) in lines.Where(l => l.StartsWith("v ")).Select((l, i) => (i, l.Split(" ").Skip(1).Select(n => float.Parse(n, CultureInfo.InvariantCulture)).ToList())))
        {
            vertices[index, 0] = vertex[0];
            vertices[index, 1] = vertex[1];
            vertices[index, 2] = vertex[2];
        }

        faces = new int[faceCount, 3];

        foreach ((int index, var face) in lines
            .Where(l => l.StartsWith("f "))
            .Select((l, i) => (i, l.Split(" ")
            .Skip(1)
            .Select(n => int.Parse(n.Split("/").First()))
            .ToList())))
        {
            faces[index, 0] = face[0] - 1;
            faces[index, 1] = face[1] - 1;
            faces[index, 2] = face[2] - 1;
        }
    }

    void CleanAndConvertMesh(Mesh mesh, out DenseMatrix vertices, out int[,] faces)
    {
        var originalVertices = mesh.vertices;
        var originalFaces = mesh.triangles;

        int faceCount = originalFaces.Length / 3;

        Dictionary<Vector3, int> verticesLookup = new();
        int getVertexId(Vector3 vertex)
        {
            if (verticesLookup.TryGetValue(vertex, out int vertexId))
                return vertexId;

            vertexId = verticesLookup.Count;
            verticesLookup[vertex] = vertexId;
            return vertexId;
        }


        faces = new int[faceCount, 3];
        for (int i = 0; i < faceCount; i++)
        {
            faces[i, 0] = getVertexId(originalVertices[originalFaces[i * 3 + 0]]);
            faces[i, 1] = getVertexId(originalVertices[originalFaces[i * 3 + 1]]);
            faces[i, 2] = getVertexId(originalVertices[originalFaces[i * 3 + 2]]);
        }

        vertices = (DenseMatrix)DenseMatrix.Build.Dense(verticesLookup.Count, 3);
        foreach (var kv in verticesLookup)
        {
            vertices[kv.Value, 0] = kv.Key.x;
            vertices[kv.Value, 1] = kv.Key.y;
            vertices[kv.Value, 2] = kv.Key.z;
        }
    }

    double Average(Matrix m)
    {
        return m.ColumnSums().Sum() / (m.RowCount * m.ColumnCount);
    }

    void PrintBounds(Matrix m, string label)
    {
        if (!Verbose)
            return;

        Debug.Log(label + Environment.NewLine 
            + string.Join(' ', m.EnumerateColumns().Take(5).Select(c => $"[{c.Min():N4} : {c.Max():N4} | {c.Average():N4}]")) + Environment.NewLine
            + $"Average: {Average(m):N4}");
    }

    (DenseMatrix thinnestVersion, DenseMatrix thinnedVersion) SkeletonExtraction(DenseMatrix vertices, int[,] faces, double contractionFactor, int maxIterations)
    {
        PrintBounds(vertices, "Vertices");
        SparseMatrix laplaceCotMatrix = LaplaceCotanMatrix(vertices, faces);

        DenseMatrix oneRingAreas = OneRingAreas(vertices, faces);

        PrintBounds(oneRingAreas, "areas");

        SparseMatrix WH = SparseMatrix.CreateIdentity(vertices.RowCount),
            WL = SparseMatrix.CreateIdentity(vertices.RowCount);
        WL *= Math.Sqrt(Average(oneRingAreas)) * 1e-3;

        DenseMatrix initialOneRingAreas = (DenseMatrix)oneRingAreas.Clone();
        SparseMatrix bZero = new(vertices.RowCount, 3);
        SparseMatrix initialWH = (SparseMatrix)WH.Clone();

        double initialVolume = GetVolume(vertices, faces);

        LinkedList<DenseMatrix> verticesHistory = new();
        for (int iter = 0; iter < maxIterations; iter++)
        {
            verticesHistory.AddLast(vertices);
            if (verticesHistory.Count > ThinningStepOfInterest)
                verticesHistory.RemoveFirst();

            Debug.Log($"Skeletonization iter {iter}, volume: {GetVolume(vertices, faces)}");

            SparseMatrix WLL = WL * laplaceCotMatrix;
            SparseMatrix WHV = (SparseMatrix)SparseMatrix.Build.SparseOfMatrix(WH * vertices);

            var M = (SparseMatrix)Matrix.Build.SparseOfMatrixArray(new[,] { { WLL }, { WH } });
            var B = (SparseMatrix)Matrix.Build.SparseOfMatrixArray(new[,] { { bZero } , { WHV } });


            // the following 3 statements are taken from Treelike
            SparseMatrix MtM = (SparseMatrix)M.Transpose().Multiply(M);
            SparseMatrix MtB = (SparseMatrix)M.Transpose().Multiply(B);

            // MathNet does not provide direct solvers (better for sparse matrices)
            // So we use Eigen via EigenCore
            vertices = SolveUsingLDLT(MtM, MtB);

            double relError = (SparseMatrix.Build.SparseOfMatrix(MtM * vertices) - MtB).L2Norm() / MtB.L2Norm();
            Debug.Log($"Relative Error: {relError:N6}");
            if (double.IsNaN(relError) || relError > 0.001)
            {
                // apparently not in paper, also from Treelike
                Debug.Log("Relative error too large, stopping.");
                return (verticesHistory.Last.Value, verticesHistory.First.Value);
            }

            double volume = GetVolume(vertices, faces);
            if ((volume / initialVolume) < 1e-8)
            {
                Debug.Log($"Volume difference too big at iteration {iter}.");
                return (verticesHistory.Last.Value, verticesHistory.First.Value);
            }

            if (double.IsNaN(volume))
            {
                Debug.Log($"Volume is NaN at iter {iter}.");
                return (verticesHistory.Last.Value, verticesHistory.First.Value);
            }

            laplaceCotMatrix = LaplaceCotanMatrix(vertices, faces);
            oneRingAreas = OneRingAreas(vertices, faces);
            
            WL *= contractionFactor;
            DenseMatrix AFactors = (DenseMatrix)initialOneRingAreas.PointwiseDivide(oneRingAreas).PointwiseSqrt();
            SparseMatrix diag = (SparseMatrix)DiagonalMatrix.Build.SparseOfDiagonalArray(AFactors.Column(0).AsArray());

            WH = (SparseMatrix)initialWH.PointwiseMultiply(diag);
        }
        return (verticesHistory.Last.Value, verticesHistory.First.Value);
    }

    DenseMatrix SolveUsingLDLT(SparseMatrix a, SparseMatrix b)
    {
        var sparseStorage = (SparseCompressedRowMatrixStorage<double>)a.Storage;
        // MathNet uses the CSR sparse format, Eigen uses SCS.
        // Converting between the two can be done by simply transposing the matrix.
        var eigenA = new SparseMatrixD(sparseStorage.Values, sparseStorage.ColumnIndices, sparseStorage.RowPointers, a.ColumnCount, a.RowCount)
            .Transpose();
        try
        {
        // for each column b of B we solve Ax=b seperately and then concat the x columns to matrix X 
        return (DenseMatrix)DenseMatrix.Build.DenseOfColumnArrays(
            b.EnumerateColumns()
            .Select(v => new VectorXD(v.ToArray()))
            .Select(v => eigenA.DirectSolve(v, EigenCore.Core.Sparse.LinearAlgebra.DirectSolverType.SimplicialLDLT).Values));
        }
        catch (Exception)
        {
            return null;
        }
    }

    double GetVolume(DenseMatrix vertices, int[,] faces)
    {
        int faceCount = faces.GetLength(0);

        double sum = 0f;
        for (int iTri = 0; iTri < faceCount; iTri++)
        {
            int i = faces[iTri, 0];
            int j = faces[iTri, 1];
            int k = faces[iTri, 2];

            var A = (DenseVector)vertices.Row(i);
            var B = (DenseVector)vertices.Row(j);
            var C = (DenseVector)vertices.Row(k);
            sum += A.DotProduct(Cross(B, C));
        }
        sum /= 6;
        return Math.Abs(sum);
    }

    DenseMatrix OneRingAreas(DenseMatrix vertices, int[,] faces)
    {
        var oneRingAreas = new DenseMatrix(vertices.RowCount, 1);
        var faceCount = faces.GetLength(0);

        for (int iTri = 0; iTri < faceCount; iTri++)
        {
            int i = faces[iTri, 0];
            int j = faces[iTri, 1];
            int k = faces[iTri, 2];

            DenseVector b = (DenseVector)(vertices.Row(i) - vertices.Row(k));
            // DenseVector c = (DenseVector)(vertices.Row(j) - vertices.Row(i));
            DenseVector a = (DenseVector)(vertices.Row(k) - vertices.Row(j));

            var area = Cross(b, a).L2Norm() * 0.5;
            oneRingAreas[i, 0] += area;
            oneRingAreas[j, 0] += area;
            oneRingAreas[k, 0] += area;
        }

        oneRingAreas = (DenseMatrix)(oneRingAreas / 3.0);
        return oneRingAreas;
    }

    DenseVector Cross(DenseVector left, DenseVector right)
    {
        // from https://stackoverflow.com/a/20015626/8512719
        if (left.Count != 3 || right.Count != 3)
        {
            string message = "Vectors must have a length of 3.";
            throw new Exception(message);
        }

        DenseVector result = new(3)
        {
            [0] = left[1] * right[2] - left[2] * right[1],
            [1] = -left[0] * right[2] + left[2] * right[0],
            [2] = left[0] * right[1] - left[1] * right[0]
        };

        return result;
    }

    SparseMatrix LaplaceCotanMatrix(DenseMatrix vertices, int[,] faces)
    {
        var laplaceMatrix = new SparseMatrix(vertices.RowCount, vertices.RowCount);

        var edges = new int[,] {
            {1,2},
            {2,0},
            {0,1}};

        var faceCount = faces.GetLength(0);

        var cotMatrix = CotMatrix(vertices, faces);
        PrintBounds(cotMatrix, "cotmat");
        var edgeCount = edges.GetLength(0);
        for (int i = 0; i < faceCount; i++)
        {
            for (int e = 0; e < edgeCount; e++)
            {
                int source = faces[i, edges[e, 0]];
                int dest = faces[i, edges[e, 1]];

                laplaceMatrix[source, dest] += cotMatrix[i, e];
                laplaceMatrix[dest, source] += cotMatrix[i, e];
                laplaceMatrix[source, source] += -cotMatrix[i, e];
                laplaceMatrix[dest, dest] += -cotMatrix[i, e];
            }
        }

        PrintBounds(laplaceMatrix, "Laplace");

        return laplaceMatrix * 2;
    }

    DenseMatrix CotMatrix(DenseMatrix vertices, int[,] faces)
    {
        int faceCount = faces.GetLength(0);
        DenseMatrix lengths = EdgeLengths(vertices, faces);
        DenseMatrix squaredLengths = (DenseMatrix)lengths.PointwisePower(2);

        PrintBounds(squaredLengths, "squared lenghts");

        DenseMatrix doubleArea = GetDoubleArea(lengths, 0);

        PrintBounds(doubleArea, "double area");

        DenseMatrix cotMatrix = new(faceCount, 3);
        for (int i = 0; i < faceCount; i++)
        {
            cotMatrix[i, 0] = (squaredLengths[i, 1] + squaredLengths[i, 2] - squaredLengths[i, 0]) / doubleArea[i, 0] / 4;
            cotMatrix[i, 1] = (squaredLengths[i, 2] + squaredLengths[i, 0] - squaredLengths[i, 1]) / doubleArea[i, 0] / 4;
            cotMatrix[i, 2] = (squaredLengths[i, 0] + squaredLengths[i, 1] - squaredLengths[i, 2]) / doubleArea[i, 0] / 4;
        }

        return cotMatrix;
    }

    private static DenseMatrix GetDoubleArea(DenseMatrix lengths, double nanReplacement)
    {
        int faceCount = lengths.RowCount;

        lengths = (DenseMatrix)Matrix.Build.DenseOfRows(lengths.EnumerateRows().Select(r => r.OrderByDescending(r => r)));

        var doubleArea = new DenseMatrix(lengths.RowCount, 1);

        for (int i = 0; i < faceCount; i++)
        {
            var x =
                (lengths[i, 0] + (lengths[i, 1] + lengths[i, 2])) *
                (lengths[i, 2] - (lengths[i, 0] - lengths[i, 1])) *
                (lengths[i, 2] + (lengths[i, 0] - lengths[i, 1])) *
                (lengths[i, 0] + (lengths[i, 1] - lengths[i, 2]));
            doubleArea[i, 0] = 2f * 0.25f * Math.Sqrt(x);
            if (double.IsNaN(doubleArea[i, 0]))
                doubleArea[i, 0] = nanReplacement;
            Debug.Assert(lengths[i, 2] - (lengths[i, 0] - lengths[i, 1]) >= 0);
        }

        return doubleArea;
    }

    private static DenseMatrix EdgeLengths(DenseMatrix vertices, int[,] faces)
    {
        int faceCount = faces.GetLength(0);

        var lengths = new DenseMatrix(faceCount, 3);
        for (int i = 0; i < faceCount; i++)
        {
            lengths[i, 0] = (vertices.Row(faces[i, 1]) - vertices.Row(faces[i, 2])).L2Norm();
            lengths[i, 1] = (vertices.Row(faces[i, 2]) - vertices.Row(faces[i, 0])).L2Norm();
            lengths[i, 2] = (vertices.Row(faces[i, 0]) - vertices.Row(faces[i, 1])).L2Norm();
        }

        return lengths;
    }
}
