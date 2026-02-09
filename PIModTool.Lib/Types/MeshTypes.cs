namespace PIModTool.Lib.Types
{
    public struct MeshVertex
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public MeshVertex(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct MeshFace
    {
        public int[] Indices { get; set; }

        public MeshFace(int[] indices)
        {
            Indices = indices;
        }
    }

    public class DRPMesh
    {
        public IReadOnlyList<MeshVertex> Vertices { get; set; }
        public IReadOnlyList<MeshFace> Faces { get; set; }

        public DRPMesh(MeshVertex[] vertices, IReadOnlyList<MeshFace>? faces = null)
        {
            Vertices = vertices;
            if(faces != null)
            {
                Faces = faces;
            }
            else
            {
                Faces = Array.Empty<MeshFace>();
            }
        }
    }
}
