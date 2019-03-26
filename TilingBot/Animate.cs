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
			//settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2019-1-17_23-08-44.xml" ) );
			settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2019-3-18_21-49-38.xml" ) );
			// Make custom setting edits here.
			//settings.VertexWidth = 0.0;
			settings.P = 4;
			settings.Q = 8;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex1;
			Mobius mOrig = settings.Mobius;
			Program.StandardInputs( settings );

			int numFrames = Test.IsTesting ? 20 : 180;

			Vector3D pStart = new Vector3D();
			//Vector3D pEnd = new Vector3D( 0, OffsetInModel( settings, 2, 0, 0 ) );
			Vector3D pEnd = new Vector3D( OffsetInModel( settings, 0, 2, 0 ), 0 );
			//pEnd.RotateXY( Math.PI / settings.P );
			//pEnd.RotateXY( Math.PI / 2 );

			/*pStart = settings.Verts[0];
			pEnd = pStart;
			pEnd.RotateXY( 2 * Math.PI / settings.P );*/

			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
			Mobius t = settings.CenteringMobius();
			//pStart = t.Apply( pStart );
			//pEnd = t.Apply( pEnd );

			Vector3D[] points = TextureHelper.SubdivideSegmentInGeometry( pStart, pEnd, numFrames, settings.Geometry );

			/*
			// Array of Mobius transforms.

			System.Func<Vector3D, double, Vector3D> pointInDirAtDist = (dir, hDist) =>
			{
				dir.Normalize();
				dir *= DonHatch.h2eNorm( hDist );
				return dir;
			};

			List<double> distancesSmoothed = new List<double>();
			List<double> anglesSmoothed = new List<double>();
			for( int i=0; i<numFrames; i++ )
			{
				double frac = (double)i / numFrames;
				double smoothedFrac = Util.Smoothed( frac, 1.0 );
				distancesSmoothed.Add( smoothedFrac * DonHatch.e2hNorm( pEnd.Abs() ) );
				anglesSmoothed.Add( smoothedFrac * Math.PI / 2 );
			}
			Vector3D[] pointsSmoothed = distancesSmoothed.Select( d => pointInDirAtDist( pEnd, d ) ).ToArray();

			// Build up a set of Mobius transformations.
			Mobius trans = new Mobius(), rot = new Mobius(), runningStep = Mobius.Identity();
			trans.Geodesic( settings.Geometry, pStart.ToComplex(), pEnd.ToComplex() );
			rot.Isometry( Geometry.Euclidean, Math.PI / 2, new Complex() );
			List<Mobius> mList = new List<Mobius>();
			List<H3.Cell.Edge[]> globalEdges = new List<H3.Cell.Edge[]>();
			List<H3.Cell.Edge> completedEdges = new List<H3.Cell.Edge>();
			for( int s=0; s<=5; s++ )
			{
				for( int i = 0; i < numFrames * 2; i++ )
				{
					List<H3.Cell.Edge> frameEdges = new List<H3.Cell.Edge>();
					Mobius m = new Mobius();
					if( i < numFrames )
					{
						Vector3D pCurrent = pointsSmoothed[i];
						m.Geodesic( settings.Geometry, pStart.ToComplex(), pCurrent.ToComplex() );
						mList.Add( runningStep * m );
						frameEdges.Add( new H3.Cell.Edge( runningStep.Apply( pStart ), runningStep.Apply( pCurrent ) ) );
					}
					else
					{
						m.Isometry( Geometry.Euclidean, anglesSmoothed[i-numFrames], new Complex() );
						mList.Add( runningStep * trans * m );
						frameEdges.Add( new H3.Cell.Edge( runningStep.Apply( pStart ), runningStep.Apply( pEnd ) ) );
					}
					frameEdges.AddRange( completedEdges );
					globalEdges.Add( frameEdges.ToArray() );
				}
				completedEdges.Add( new H3.Cell.Edge( runningStep.Apply( pStart ), runningStep.Apply( pEnd ) ) );
				runningStep *= trans * rot;
			}
			*/

			double ew = settings.EdgeWidth;

			for( int i = 0; i < numFrames; i++ )
			{
				string numString = i.ToString();
				numString = numString.PadLeft( 3, '0' );
				settings.FileName = "frame" + numString + ".png";
				Console.WriteLine( settings.FileName );
				double frac = (double)i / numFrames;

				// Setup the Mobius.
				Vector3D pCurrent = points[i];
				Mobius m = Mobius.Identity(), mInitOff = Mobius.Identity(), mInitRot = Mobius.Identity(), mCentering = Mobius.Identity(), mModel = Mobius.Identity();
				//mInitRot = Mobius.CreateFromIsometry( Geometry.Euclidean, frac * 2 * Math.PI, new Complex() );
				//settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
				//mCentering = settings.CenteringMobius();
				double rot = frac * 2 * Math.PI / 2;
				settings.ImageRot = -rot;

				//mInitOff = OffsetMobius( settings );
				mInitRot = Mobius.CreateFromIsometry( Geometry.Euclidean, -Math.PI / 4, new Complex() );

				//m.Isometry( settings.Geometry, Math.PI / 4, pCurrent.ToComplex() );
				//m.Geodesic( settings.Geometry, pStart.ToComplex(), pCurrent.ToComplex() );
				//m = mCentering * m * mCentering.Inverse();

				// Rotation
				double xOff = OffsetInModel( settings, 0, 0, 1 );
				m = RotAboutPoint( settings.Geometry, new Vector3D( 0, 0 ), rot );
				//m = RotAboutPoint( settings.Geometry, new Vector3D( xOff, 0 ),/* -frac * Math.PI*/ -frac * 2 * Math.PI / settings.Q );
				//m = LimitRot( settings, 0*Math.PI/4, 2*frac );
				//m = RotAboutPoint( settings.Geometry, new Vector3D(0,0), frac * 2 * Math.PI / settings.P );
				//m = mList[i];

				settings.Anim = frac;// Util.Smoothed( frac, 1.0 );

				// Change edge width.
				//double fact = 1 + settings.Anim * 20;
				//settings.EdgeWidth = ew * fact;

				//settings.GlobalEdges = globalEdges[i];

				Console.WriteLine( Tweet.Format( settings ) + "\n" );
				string newPath = Path.Combine( Persistence.AnimDir, settings.FileName );
				if( File.Exists( newPath ) )
					continue;

				// Need to do this when animating where edges need recalc.
				settings.Init();

				//mModel = RotAboutPoint( Geometry.Spherical, new Vector3D( 1, 0 ), 2 * Math.PI * frac );
				//Mobius mZoom = Mobius.Scale( 1.0 / ( 1.0 - Util.Smoothed( 2*frac, 0.8 ) ) );
				//mModel *= mZoom;

				// We will control the Mobius transformation ourself.
				// NOTE: Needs to be done after Init call above, or it will get clobbered.
				settings.Mobius = m * mCentering * mInitOff * mInitRot * mModel;
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

		static public double OffsetInSpace(Tiler.Settings settings, double p = 0, double q = 0, double r = 1)
		{
			return
				p * Geometry2D.GetTrianglePSide( settings.P, settings.Q ) +
				q * Geometry2D.GetTriangleQSide( settings.P, settings.Q ) +
				r * Geometry2D.GetTriangleHypotenuse( settings.P, settings.Q );
		}

		static public double OffsetInModel(Tiler.Settings settings, double p = 0, double q = 0, double r = 1)
		{
			double off = OffsetInSpace( settings, p, q, r );
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
