namespace TilingBot
{
	using R3.Geometry;
	using System.Linq;
	using Math = System.Math;

	public class Util
	{
		// Normalizing barycentric coords amounts to making sure the 4 coords add to 1.
		public static Vector3D BaryNormalize( Vector3D b )
		{
			return b / (b.X + b.Y + b.Z);
		}

		// Bary Coords to Euclidean
		public static Vector3D BaryToEuclidean( Vector3D[] kv, Vector3D b )
		{
			Vector3D result =
				kv[0] * b.X + kv[1] * b.Y + kv[2] * b.Z;
			return result;
		}

		// So that we can leverage Euclidean barycentric coordinates, we will first convert our simplex to the Klein model.
		// We will need to take care to properly convert back to the Ball as needed.
		public static Vector3D[] KleinVerts( Geometry g, Vector3D[] conformalVerts )
		{
			return conformalVerts.Select( v =>
			{
				switch( g )
				{
				case Geometry.Spherical:
					return SphericalModels.StereoToGnomonic( v );
				case Geometry.Euclidean:
					return v;
				case Geometry.Hyperbolic:
					return HyperbolicModels.PoincareToKlein( v );
				}

				throw new System.ArgumentException();
			} ).ToArray();
		}

		public static Vector3D ToConformal( Geometry g, Vector3D[] kv, Vector3D b )
		{
			Vector3D klein = Util.BaryToEuclidean( kv, b );
			switch( g )
			{
			case Geometry.Spherical:
				return SphericalModels.GnomonicToStereo( klein );
			case Geometry.Euclidean:
				return klein;
			case Geometry.Hyperbolic:
				return HyperbolicModels.KleinToPoincare( klein );
			}

			throw new System.ArgumentException();
		}

		public static double LorentzDot( Vector3D v1, Vector3D v2 )
		{
			return -( v1.X * v2.X + v1.Y * v2.Y - v1.Z * v2.Z );
		}

		public static double DotInGeometry( Geometry g, Vector3D v1, Vector3D v2 )
		{
			if( g == Geometry.Hyperbolic )
				return LorentzDot( v1, v2 );

			return v1.Dot( v2 );
		}

		public static void NormalizeInGeometry( Geometry g, ref Vector3D v )
		{
			switch( g )
			{
			case Geometry.Spherical:
				v.Normalize();
				break;
			case Geometry.Euclidean:
				throw new System.NotImplementedException();
			case Geometry.Hyperbolic:
				Sterographic.NormalizeToHyperboloid( ref v );
				break;
			}
		}

		public static Vector3D Centroid( Geometry g, Vector3D[] conformalVerts )
		{
			if( g == Geometry.Euclidean )
			{
				Vector3D result = new Vector3D();
				foreach( Vector3D v in conformalVerts )
					result += v;
				return result / conformalVerts.Length;
			}

			Vector3D[] verts = conformalVerts.Select( v =>
			{
				switch( g )
				{
				case Geometry.Spherical:
					return Sterographic.PlaneToSphereSafe( v );
				case Geometry.Hyperbolic:
					return Sterographic.PlaneToHyperboloid( v );
				}

				throw new System.ArgumentException();
			} ).ToArray();

			// https://math.stackexchange.com/a/2173370/300001
			Vector3D sum = new Vector3D();
			for( int i = 0; i < verts.Length; i++ )
				sum += verts[i];
			Vector3D centroid = sum / Math.Sqrt( DotInGeometry( g, sum, sum ) );
			NormalizeInGeometry( g, ref centroid );

			switch( g )
			{
			case Geometry.Spherical:
				return Sterographic.SphereToPlane( centroid );
			case Geometry.Hyperbolic:
				return Sterographic.HyperboloidToPlane( centroid );
			}

			throw new System.ArgumentException();
		}

		public static double Smoothed( double input, double max )
		{
			return (max / 2.0) * (-Math.Cos( Math.PI * input / max ) + 1);
		}
	}
}
