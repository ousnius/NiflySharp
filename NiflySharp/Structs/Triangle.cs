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
			if (_v2 < _v1 && _v2 < _v3)
			{
				Set(_v2, _v3, _v1);
			}
			else if (_v3 < _v1)
			{
				Set(_v3, _v1, _v2);
			}
		}

		public ushort this[ushort ind]
        {
            readonly get => ind != 0 ? (ind == 2 ? _v3 : _v2) : _v1;

            set
            {
                switch (ind)
                {
                    case 0:
                        _v1 = value;
                        break;
                    case 1:
                        _v2 = value;
                        break;
                    case 2:
                        _v3 = value;
                        break;
                }
            }
        }
    }
}
