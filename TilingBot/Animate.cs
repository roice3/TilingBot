namespace TilingBot
{
	using LinqToTwitter;
	using R3.Core;
	using R3.Drawing;
	using R3.Geometry;
	using R3.Math;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Numerics;
	using System.Threading.Tasks;
	using Color = System.Drawing.Color;
	using Geometry = R3.Geometry.Geometry;

	public class Animate
	{
		public static void Gen()
		{
			Tiler.Settings settings = Program.GenSettings( tweeting: false );
			//settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2018-12-03_22-06-02.xml" ) );
			settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2019-1-14_14-54-14.xml" ) );
			//settings.P = 12;
			//settings.Q = 4;
			//settings.P = 3;
			//settings.ShowCoxeter = true;
			//settings.EuclideanModel = EuclideanModel.Isometric;
			//settings.HyperbolicModel = HyperbolicModel.Poincare;
			//settings.SphericalModel = SphericalModel.Sterographic;
			settings.Colors = new Color[] { Color.MidnightBlue };
			Program.StandardInputs( settings );

			int numFrames = Test.IsTesting ? 10 : 180;
			//numFrames = 6;

			Vector3D pStart = new Vector3D();
			Vector3D pEnd = new Vector3D( OffsetInModel( settings, 0, 2, 0 ), 0 );
			pEnd.RotateXY( Math.PI / settings.P );
			//pEnd.RotateXY( Math.PI / 2 );

			/*pStart = settings.Verts[0];
			pEnd = pStart;
			pEnd.RotateXY( 2 * Math.PI / settings.P );*/

			Vector3D[] points = TextureHelper.SubdivideSegmentInGeometry( pStart, pEnd, numFrames, settings.Geometry );

			for( int i = 0; i <= numFrames; i++ )
			{
				string numString = i.ToString();
				numString = numString.PadLeft( 3, '0' );
				settings.FileName = "frame" + numString + ".png";
				Console.WriteLine( settings.FileName );
				double frac = (double)i / numFrames;

				// Setup the Mobius.
				Vector3D pCurrent = points[i];
				Mobius m = Mobius.Identity(), mInitOff = Mobius.Identity(), mInitRot = Mobius.Identity(), mCentering = Mobius.Identity(), mModel = Mobius.Identity();
				settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
				//mCentering = settings.CenteringMobius();

				//mInitOff = OffsetMobius( settings );
				//mInitRot = Mobius.CreateFromIsometry( Geometry.Euclidean, -Math.PI / 4, new Complex() );

				//m.Isometry( settings.Geometry, Math.PI / 4, pCurrent.ToComplex() );
				//m.Geodesic( settings.Geometry, pStart.ToComplex(), pCurrent.ToComplex() );

				// Rotation
				double xOff = OffsetInModel( settings, 0, 0, 1 );
				//m = RotAboutPoint( settings, new Vector3D( xOff, 0 ), frac * 2 * Math.PI / settings.Q );
				//m = LimitRot( settings, Math.PI/4, 2*frac );
				m = RotAboutPoint( settings.Geometry, new Vector3D(0,1), frac * 2 * Math.PI );

				settings.Anim = Util.Smoothed( frac, 1.0 );

				Console.WriteLine( Tweet.Format( settings ) + "\n" );
				string newPath = Path.Combine( Persistence.AnimDir, settings.FileName );
				//if( File.Exists( newPath ) )
				//	continue;

				// Need to do this when animating where edges need recalc.
				settings.Init();
				
				//mModel = RotAboutPoint( Geometry.Spherical, new Vector3D( 1, 0 ), 2 * Math.PI * frac );
				//Mobius mZoom = Mobius.Scale( 1.0 / ( 1.0 - Util.Smoothed( 2*frac, 0.8 ) ) );
				//mModel *= mZoom;

				// We will control the Mobius transformation ourself.
				// NOTE: Needs to be done after Init call above, or it will get clobbered.
				settings.Mobius = mCentering * m * mInitOff * mInitRot * mModel;
				settings.Centering = Tiler.Centering.General;
				//settings.Mobius = RotAboutPoint( settings, new Vector3D(), Math.PI / 6 );

				Program.MakeTiling( settings );

				File.Delete( newPath );
				File.Move( settings.FileName, newPath );

				if( i == 0 )
				{
					string settingsPath = Path.Combine( Persistence.AnimDir, "settings.xml" );
					Persistence.SaveSettings( settings, settingsPath );
				}
			}
		}

		static public double OffsetInModel(Tiler.Settings settings, double p = 0, double q = 0, double r = 1)
		{
			double off =
				p * Geometry2D.GetTrianglePSide( settings.P, settings.Q ) +
				q * Geometry2D.GetTriangleQSide( settings.P, settings.Q ) +
				r * Geometry2D.GetTriangleHypotenuse( settings.P, settings.Q );
			switch( settings.Geometry )
			{
				case Geometry.Spherical:
					off = Spherical2D.s2eNorm( off );
					break;
				case Geometry.Hyperbolic:
					off = DonHatch.h2eNorm( off );
					break;
			}
			return off;
		}

		static public Mobius OffsetMobius(Tiler.Settings settings, double p = 0, double q = 0, double r = 1)
		{
			double off = OffsetInModel( settings, p, q, r );
			return Mobius.CreateFromIsometry( settings.Geometry, 0, new Complex( off, 0 ) );
		}

		/// <summary>
		/// initialRot is used to take a particular point to infinity. When 0, this point is (0,1) in the ball.
		/// off is used to do the limit rotation by doing a tranlation in the UHP.
		/// </summary>
		static public Mobius LimitRot(Tiler.Settings settings, double initialRot, double off)
		{
			Mobius mRot = new Mobius(), mOff = new Mobius();
			mRot.Isometry( Geometry.Euclidean, initialRot, new Complex() );
			mOff.Isometry( Geometry.Euclidean, 0, new Complex( off, 0 ) );
			return mRot.Inverse() * HyperbolicModels.UpperInv * mOff * HyperbolicModels.Upper * mRot;
		}

		static public Mobius RotAboutPoint(Geometry g, Vector3D p, double rot)
		{
			Mobius m1 = new Mobius(), m2 = new Mobius();
			m1.Isometry( g, 0, p );
			m2.Isometry( g, rot, new Complex() );
			return m1 * m2 * m1.Inverse();
		}
	}
}
