
using HelixToolkit.Wpf;
using PIModTool.Lib.Types;
using System.Windows.Media.Media3D;

namespace PIModTool.Wpf.Utilities
{
    public static class MeshUtilities
    {
        public static GeometryModel3D BuildGeometry(DRPMesh meshData)
        {
            MeshGeometry3D model = new MeshGeometry3D();
            for (int i = 0; i < meshData.Vertices.Count; i++)
            {
                model.Positions.Add(new Point3D(meshData.Vertices[i].X, meshData.Vertices[i].Y, meshData.Vertices[i].Z));
            }
            for (int i = 0; i < meshData.Faces.Count; i++)
            {
                if (i % 2 == 0)
                {
                    model.TriangleIndices.Add(meshData.Faces[i].Indices[0]);
                    model.TriangleIndices.Add(meshData.Faces[i].Indices[1]);
                    model.TriangleIndices.Add(meshData.Faces[i].Indices[2]);
                }
                else
                {
                    model.TriangleIndices.Add(meshData.Faces[i].Indices[0]);
                    model.TriangleIndices.Add(meshData.Faces[i].Indices[2]);
                    model.TriangleIndices.Add(meshData.Faces[i].Indices[1]);
                }
            }

            return new GeometryModel3D { Geometry = model, Material = Materials.Gray, BackMaterial = Materials.Gray };
        }
    }
}
