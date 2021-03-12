namespace TilingBot
{
	using Meta.Numerics.Functions;
	using R3.Geometry;
	using R3.Math;
	using System.Linq;
	using System.Numerics;
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

		public static double Interp( double frac, double start, double end )
		{
			return start + ( end - start ) * frac;
		}

		public static double Smoothed( double frac, double start, double end )
		{
			double newFrac = Smoothed( frac, 1.0 );
			return Interp( newFrac, start, end );
		}

		public static double Smoothed( double frac, double max )
		{
			return (max / 2.0) * (-Math.Cos( Math.PI * frac ) + 1);
		}

		/// <summary>
		/// https://paramanands.blogspot.com/2011/01/elliptic-functions-complex-variables.html#.W14TVtJKiUl
		/// equation 14.
		/// </summary>
		public static Complex JacobiCn( Complex u, double k )
		{
			// k prime
			double k_ = Math.Sqrt( 1 - k * k );

			double cnx = AdvancedMath.JacobiCn( u.Real, k );
			double snx = AdvancedMath.JacobiSn( u.Real, k );
			double dnx = AdvancedMath.JacobiDn( u.Real, k );
			double cny = AdvancedMath.JacobiCn( u.Imaginary, k_ );
			double sny = AdvancedMath.JacobiSn( u.Imaginary, k_ );
			double dny = AdvancedMath.JacobiDn( u.Imaginary, k_ );

			double denom = cny * cny + k * k * snx * snx * sny * sny;
			double real = cnx * cny / denom;
			double imag = -snx * dnx * sny * dny / denom;

			return new Complex( real, imag );
		}

		/// <summary>
		/// See this paper: http://archive.bridgesmathart.org/2016/bridges2016-179.pdf
		/// This logically belongs in HyperbolicModels.cs, but I didn't want the dependency on Meta.Numerics in R3.Core.
		/// </summary>
		public static Vector3D SquareToPoincare( Vector3D s )
		{
			double K_e = AdvancedMath.EllipticF( Math.PI / 2, 1.0 / Math.Sqrt( 2 ) );
			Complex a = new Complex( 1, -1 ) / Math.Sqrt( 2 );
			Complex b = new Complex( 1, 1 ) / 2;
			Complex z = s;

			Complex result = a * JacobiCn( K_e * ( b * z - 1 ), 1 / Math.Sqrt( 2 ) );
			return Vector3D.FromComplex( result );
		}

		public static Vector3D PeirceToStereo( Vector3D p )
		{
			return SquareToPoincare( p );
		}

		/// <summary>
		/// https://www.physicsforums.com/threads/conformal-map-for-regular-polygon-in-circle.89759/
		/// https://math.stackexchange.com/questions/1528270/numerical-libraries-and-special-function-of-complex-parameters/1528299#1528299
		/// </summary>
		public static Complex PolygonToPoincare( Vector3D p )
		{
			//z( 1 - z ^ m ) ^ ( 2 / m )( -1 + z ^ m ) ^ ( -2 / m ) Hypergeometric2F1[1 / m, 2 / m, 1 + 1 / m, z ^ m]
			//AdvancedMath.Hypergeometric2F1( )
			throw new System.NotImplementedException();
		}

		//
		// Hyperbolic utility functions.
		// ZZZ - Move to shared location.
		//

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

		public static Vector3D EdgeToPlane( Geometry g, H3.Cell.Edge edge )
		{
			if( g != Geometry.Hyperbolic )
				throw new System.NotImplementedException();

			Vector3D b1, b2;
			H3Models.Ball.GeodesicIdealEndpoints( edge.Start, edge.End, out b1, out b2 );
			if( ( ( b2 + b1 ) / 2 ).IsOrigin )
			{
				Vector3D lineNormal = b2 - b1;
				lineNormal.RotateXY( Math.PI / 2 );
				lineNormal.Normalize();
				return new Vector3D( lineNormal.X, lineNormal.Y, lineNormal.Z, 0 );
			}

			Vector3D center, normal;
			double radius, angleTot;
			H3Models.Ball.Geodesic( edge.Start, edge.End, out center, out radius, out normal, out angleTot );

			Vector3D closest = H3Models.Ball.ClosestToOrigin( new Circle3D() { Center = center, Radius = radius, Normal = normal } );
			Vector3D closestKlein = HyperbolicModels.PoincareToKlein( closest );
			center.Normalize();
			return new Vector3D( center.X, center.Y, center.Z, closestKlein.Abs() );
		}

		// In hyperboloid model
		public static Vector3D PlaneDualPoint( Geometry g, Vector3D planeKlein )
		{
			if( g != Geometry.Hyperbolic )
				throw new System.NotImplementedException();

			if( Math.Abs( planeKlein.W ) < 1e-7 )
			{
				return new Vector3D( planeKlein.X, planeKlein.Y, 0.0 );
			}

			double inv = 1.0 / planeKlein.W;
			//Vector3D dual = new Vector3D( planeKlein.X * inv, planeKlein.Y * inv, planeKlein.Z * inv, 1.0 );
			Vector3D dual = new Vector3D( planeKlein.X * inv, planeKlein.Y * inv, 1.0 );    // Turn into 2D, would be nice to be more general.

			//NormalizeInGeometry( g, ref dual );
			// Ugh, sign convention of function above messing me up.
			System.Func<Vector3D, Vector3D> lorentzNormalize = v =>
			{
				double normSquared = Math.Abs( LorentzDot( v, v ) );
				double norm = Math.Sqrt( normSquared );
				v /= norm;
				return v;
			};

			dual = lorentzNormalize( dual );

			return dual;
		}

		// In hyperboloid model
		public static double GeodesicPlaneHSDF( Vector3D samplePoint, Vector3D dualPoint, double offset = 0 )
		{
			double dot = -DotInGeometry( Geometry.Hyperbolic, samplePoint, dualPoint );
			return DonHatch.asinh( dot ) - offset;
		}
	}
}
