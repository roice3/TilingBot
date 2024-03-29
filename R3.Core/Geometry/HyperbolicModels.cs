﻿namespace R3.Geometry
{
	using R3.Math;
	using System;
	using System.Numerics;

	public enum HyperbolicModel
	{
		Poincare,
		Klein,
		Pseudosphere,
		Hyperboloid,
		Band,
		UpperHalfPlane,
		Orthographic,
		Square,
		InvertedPoincare,
		Joukowsky,
		Ring,
		Azimuthal_Equidistant,
		Azimuthal_EqualArea,
		Schwarz_Christoffel
	}

	public class HyperbolicModels
	{
		public static Vector3D PoincareToKlein( Vector3D p )
		{
			double mag = 2 / (1 + p.Dot( p ));
			return p * mag;
		}

		public static double KleinToPoincare( double magSquared )
		{
			double dot = magSquared;
			if (dot > 1)    // This avoids some NaN problems I saw.
				dot = 1;
			return (1 - Math.Sqrt( 1 - dot )) / dot;
		}

		public static Vector3D KleinToPoincare( Vector3D k )
		{
			double dot = k.Dot( k );
			return k * KleinToPoincare( dot );
		}

		public static Mobius Upper
		{
			get
			{
				Cache();
				return m_upper;
			}
		}
		public static Mobius UpperInv
		{
			get
			{
				Cache();
				return m_upperInv;
			}
		}

		/// <summary>
		/// This was needed for performance.  We don't want this Mobius transform calculated repeatedly.
		/// </summary>
		private static void Cache()
		{
			if( m_cached )
				return;

			Mobius m1 = new Mobius(), m2 = new Mobius();
			m2.Isometry( Geometry.Euclidean, 0, new Complex( 0, -1 ) );
			m1.UpperHalfPlane();
			m_upper = m2 * m1;
			m_upperInv = m_upper.Inverse();

			m_cached = true;
		}
		private static bool m_cached = false;
		private static Mobius m_upper, m_upperInv;


		public static Vector3D PoincareToUpper( Vector3D v )
		{
			v = Upper.Apply( v );
			return v;
		}

		public static Vector3D UpperToPoincare( Vector3D v )
		{
			v = UpperInv.Apply( v );
			return v;
		}

		public static Vector3D PoincareToOrtho( Vector3D v )
		{
			// This may not be correct.
			// Should probably project to hyperboloid, then remove z coord.
			return SphericalModels.StereoToGnomonic( v );
		}

		public static Vector3D OrthoToPoincare( Vector3D v )
		{
			return SphericalModels.GnomonicToStereo( v );
		}

		public static Vector3D PoincareToBand( Vector3D v )
		{
			Complex z = v.ToComplex();
			z = 2 / Math.PI * Complex.Log( ( 1 + z ) / ( 1 - z ) );
			return Vector3D.FromComplex( z );
		}

		public static Vector3D BandToPoincare( Vector3D v )
		{
			Complex vc = v.ToComplex();
			vc = ( Complex.Exp( Math.PI * vc / 2 ) - 1 ) / ( Complex.Exp( Math.PI * vc / 2 ) + 1 );
			return Vector3D.FromComplex( vc );
		}

		/// <param name="a">The Euclidean period of the tiling in the band model.</param>
		public static Vector3D BandToRing( Vector3D v, double P, int k )
		{
			Complex vc = v.ToComplex();
			Complex i = new Complex( 0, 1 );
			vc = Complex.Exp( 2*Math.PI*i*(vc+i)/(k*P) );
			return Vector3D.FromComplex( vc );
		}

		/// <param name="a">The Euclidean period of the tiling in the band model.</param>
		public static Vector3D RingToBand( Vector3D v, double P, int k)
		{
			Complex vc = v.ToComplex();
			Complex i = new Complex( 0, 1 );
			vc = -P*k*i*Complex.Log( vc ) / (2*Math.PI) - i;
			return Vector3D.FromComplex( vc );
		}

		public static Vector3D RingToPoincare( Vector3D v, double P, int k)
		{
			Vector3D b = RingToBand( v, P, k );
			return BandToPoincare( b );
		}

		public static Vector3D JoukowskyToPoincare( Vector3D v, Vector3D cen )
		{
			Complex w = v.ToComplex();

			// Conformally map disk to ellipse with a > 1 and b = 1;
			// https://math.stackexchange.com/questions/1582608/conformally-mapping-an-ellipse-into-the-unit-circle
			// https://www.physicsforums.com/threads/conformal-mapping-unit-circle-ellipse.218014/
			double a = 1.0;
			double b = -cen.Abs();
			double alpha = ( a + b ) / 2;
			double beta = ( a - b ) / 2;

			// disk -> ellipse
			// Complex result = alpha * z + beta / z;

			double off = cen.Abs();
			System.Func<Complex, Complex> foil = z =>
			{
				//w *= 1 + Math.Sqrt( 2 );
				//Vector3D cen = new Vector3D( -off, -off );
				double rad = 1 + off;// cen.Dist( new Vector3D( 1, 0 ) );
				z *= rad;
				z += cen.ToComplex();
				return z;
			};

			// ellipse->disk
			Complex temp = Complex.Sqrt( w * w - 4 * alpha * beta );
			if( w.Real > 0 )
				temp *= -1;
			w = ( w + temp ) / ( 2 * alpha );
			return Vector3D.FromComplex( w );

			/*Complex r1 = ( w + Complex.Sqrt( w * w - alpha * beta ) ) / alpha;
			Complex r2 = ( w - Complex.Sqrt( w * w - alpha * beta ) ) / alpha;
			r1 = foil( r1 );
			r2 = foil( r2 );
			if( r1.Magnitude <= 1 ) // Pick the root that puts us inside the disk.
			{
				w = r1;
			}
			else
			{
				w = r2;
			}

			return Vector3D.FromComplex( w );*/
		}

		public static Vector3D EquidistantToPoincare( Vector3D p )
		{
			Vector3D result = p;
			result.Normalize();
			result *= EquidistantToPoincare( p.Abs() );
			return result;
		}

		private static double EquidistantToPoincare( double dist )
		{
			return DonHatch.h2eNorm( dist );
		}

		public static Vector3D EqualAreaToPoincare( Vector3D p )
		{
			Vector3D result = p;
			result.Normalize();
			result *= EqualAreaToPoincare( p.Abs() );
			return result;
		}

		private static double EqualAreaToPoincare( double dist )
		{
			double h = 2 * DonHatch.asinh( dist );
			return DonHatch.h2eNorm( h );
		}
	}
}
