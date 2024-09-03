using System.Collections.Generic;
using System.Numerics;

namespace NiflySharp.Structs
{
    public partial struct Triangle
	{
		public static readonly Triangle Zero = new(0, 0, 0);

		public Triangle(ushort v1, ushort v2, ushort v3)
		{
			V1 = v1;
			V2 = v2;
			V3 = v3;
		}

		public void Set(ushort v1, ushort v2, ushort v3)
		{
			V1 = v1;
			V2 = v2;
			V3 = v3;
		}

		public Vector3 Trinormal(IList<Vector3> vertexList)
		{
			return Vector3.Cross(vertexList[V2] - vertexList[V1], vertexList[V3] - vertexList[V1]);
		}

		public void Rotate()
		{
			if (V2 < V1 && V2 < V3)
			{
				Set(V2, V3, V1);
			}
			else if (V3 < V1)
			{
				Set(V3, V1, V2);
			}
		}

		public ushort this[ushort ind]
        {
            readonly get => ind != 0 ? (ind == 2 ? V3 : V2) : V1;

            set
            {
                switch (ind)
                {
                    case 0:
                        V1 = value;
                        break;
                    case 1:
                        V2 = value;
                        break;
                    case 2:
                        V3 = value;
                        break;
                }
            }
        }
    }
}
