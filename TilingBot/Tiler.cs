namespace R3.Geometry
{
	using R3.Core;
	using R3.Math;
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Threading.Tasks;
	using Color = System.Drawing.Color;
	using Math = System.Math;

	public class Tiler
	{
		[DataContract( Namespace = "" )]
		public class Settings
		{
			public Settings() { Antialias = true; }

			[DataMember]
			public int P;

			[DataMember]
			public int Q;

			[DataMember]
			public int[] Active { get; set; }

			[DataMember]
			public Mobius Mobius { get; set; }

			public int Width { get; set; }
			public int Height { get; set; }
			public double Bounds { get; set; }
			public CircleNE[] Mirrors { get; set; }   // in conformal model
			public string FileName { get; set; }
			public bool Antialias { get; set; }
			public double ColorScaling { get; set; }    // Depth to cycle through one hexagon

			public Geometry Geometry
			{
				get
				{
					return Geometry2D.GetGeometry( P, Q );
				}
			}

			public double XOff
			{
				get
				{
					return Bounds * 2 / (Width - 1);
				}
			}

			public double YOff
			{
				get
				{
					return Bounds * 2 / (Height - 1);
				}
			}

			public EdgeInfo[] UniformEdges { get; set; }

			public void Init()
			{
				var verts = CalcMirrors();
				CalcEdges( verts );
			}

			private Vector3D[] CalcMirrors()
			{
				Geometry g = Geometry2D.GetGeometry( P, Q );

				TilingConfig config = new TilingConfig( P, Q, 1 );
				Tile baseTile = Tiling.CreateBaseTile( config );
				Segment seg = baseTile.Boundary.Segments[0];

				// Order needs to match mirrors.
				Vector3D[] verts = new Vector3D[]
				{
					new Vector3D(),
					seg.Midpoint,
					seg.P1,
				};

				// The unoriented mirrors.
				Circle[] circles = new Circle[]
				{
					seg.Type == SegmentType.Arc ? new Circle( seg.P1, seg.Midpoint, seg.P2 ) : new Circle( verts[1], verts[2] ),	// Arc or Line
					new Circle( verts[0], verts[2] ),	// Line
					new Circle( verts[0], verts[1] ),	// Line
				};

				// The oriented mirrors.
				// We can use the opposing vertex to determine the orientation.
				Mirrors = new CircleNE[]
				{
					new CircleNE( circles[0], verts[0] ),
					new CircleNE( circles[1], verts[1] ),
					new CircleNE( circles[2], verts[2] ),
				};

				for( int i=0; i<3; i++ )
					Mirrors[i].CenterNE = Mirrors[i].ReflectPoint( Mirrors[i].CenterNE );

				return verts;
			}

			private void CalcEdges( Vector3D[] verts )
			{
				Geometry g = Geometry2D.GetGeometry( P, Q );

				List<int[]> activeSet = new List<int[]>();
				//activeSet.Add( new int[] { 0 } );
				//activeSet.Add( new int[] { 1 } );
				//activeSet.Add( new int[] { 2 } );
				//activeSet.Add( new int[] { 0, 1 } );
				//activeSet.Add( new int[] { 1, 2 } );
				//activeSet.Add( new int[] { 0, 2 } );
				//activeSet.Add( new int[] { 0, 1, 2 } );
				activeSet.Add( Active );

				List<EdgeInfo> edges = new List<EdgeInfo>();
				foreach( int[] active in activeSet )
				{
					var starting = IterateToStartingPoint( g, Mirrors, verts, active );
					Vector3D startingPoint = starting.Item1;

					List<H3.Cell.Edge> startingEdges = new List<H3.Cell.Edge>();
					foreach( int a in active )
					{
						Vector3D reflected = Mirrors[a].ReflectPoint( startingPoint );
						startingEdges.Add( new H3.Cell.Edge( startingPoint, reflected, order: false ) );	// CAN'T ORDER HERE!
					}

					Vector3D color = starting.Item2;
					edges.Add( new EdgeInfo() { Edges = startingEdges.ToArray(), Color = MixColor( color ) } );
				}

				UniformEdges = edges.ToArray();
				foreach( EdgeInfo e in UniformEdges )
					e.PreCalc( g );
			}

			public Tuple<Vector3D,Vector3D> IterateToStartingPoint( Geometry g, CircleNE[] mirrors, Vector3D[] verts, int[] activeMirrors )
			{
				if( activeMirrors.Length == 1 )
				{
					Vector3D color = new Vector3D();
					color[activeMirrors[0]] = 1;
					return new Tuple<Vector3D, Vector3D>( verts[activeMirrors[0]], color );
				}

				// We'll assume spherical otherwise.
				bool hyperbolic = g == Geometry.Hyperbolic;

				// We are minimizing the output of this function, 
				// because we want all edge lengths to be as close as possible.
				// Input vector should be in the Ball Model.
				Func<Vector3D, double> diffFunc = v =>
				{
					List<double> lengths = new List<double>();
					for( int i = 0; i < activeMirrors.Length; i++ )
					{
						int m = activeMirrors[i];
						Vector3D reflected = mirrors[m].ReflectPoint( v );
						double dist = hyperbolic ?
							H3Models.Ball.HDist( v, reflected ) :
							Spherical2D.SDist( v, reflected );
						lengths.Add( dist );
					}

					double result = 0;
					double average = lengths.Average();
					foreach( double length in lengths )
						result += Math.Abs( length - average );
					return result;
				};

				// So that we can leverage Euclidean barycentric coordinates, we will first convert our simplex to the Klein model.
				// We will need to take care to properly convert back to the Ball as needed.
				Vector3D[] kleinVerts = verts.Select( v => hyperbolic ? 
					HyperbolicModels.PoincareToKlein( v ) : 
					SphericalModels.StereoToGnomonic( v ) ).ToArray();

				// Normalizing barycentric coords amounts to making sure the 4 coords add to 1.
				Func<Vector3D, Vector3D> baryNormalize = b =>
				{
					return b / (b.X + b.Y + b.Z);
				};

				// Bary Coords to Euclidean
				Func<Vector3D[], Vector3D, Vector3D> baryToEuclidean = ( kv, b ) =>
				{
					Vector3D result =
						kv[0] * b.X + kv[1] * b.Y + kv[2] * b.Z;
					return result;
				};

				Func<Vector3D[], Vector3D, Vector3D> toConformal = ( kv, b ) =>
				{
					Vector3D klein = baryToEuclidean( kv, b );
					if( g == Geometry.Hyperbolic )
						return HyperbolicModels.KleinToPoincare( klein );
					else
						return SphericalModels.GnomonicToStereo( klein );
				};

				// Our starting barycentric coords (halfway between all active mirrors).
				Vector3D bary = new Vector3D();
				foreach( int a in activeMirrors )
					bary[a] = 0.5;
				bary = baryNormalize( bary );

				// For each iteration, we'll shrink this search offset.
				// NOTE: I'm not actually sure that the starting offset and decrease factor I'm using
				// guarantee convergence, but it seems to be working pretty well (even when varying these parameters).
				double searchOffset = bary[activeMirrors[0]] / 1.3;

				double min = double.MaxValue;
				int iterations = 1000;
				for( int i = 0; i < iterations; i++ )
				{
					min = diffFunc( toConformal( kleinVerts, bary ) );
					foreach( int a in activeMirrors )
					{
						Vector3D baryTest1 = bary, baryTest2 = bary;
						baryTest1[a] += searchOffset;
						baryTest2[a] -= searchOffset;
						baryTest1 = baryNormalize( baryTest1 );
						baryTest2 = baryNormalize( baryTest2 );

						double t1 = diffFunc( toConformal( kleinVerts, baryTest1 ) );
						double t2 = diffFunc( toConformal( kleinVerts, baryTest2 ) );
						if( t1 < min && baryTest1[a] > 0 && baryTest1[a] < 1 )
						{
							min = t1;
							bary = baryTest1;
						}
						if( t2 < min && baryTest2[a] > 0 && baryTest2[a] < 1 )
						{
							min = t2;
							bary = baryTest2;
						}
					}

					if( Tolerance.Equal( min, 0.0, 1e-14 ) )
					{
						System.Console.WriteLine( string.Format( "Converged in {0} iterations.", i ) );
						break;
					}

					searchOffset /= 1.5;
				}

				if( !Tolerance.Equal( min, 0.0, 1e-14 ) )
				{
					System.Console.WriteLine( "Did not converge: " + min );
					System.Console.ReadKey( true );
					//throw new System.Exception( "Boo. We did not converge." );
				}

				return new Tuple<Vector3D,Vector3D>( toConformal( kleinVerts, bary ), bary );
			}
		}

		public class EdgeInfo
		{
			public H3.Cell.Edge[] Edges;
			public Color Color;

			// Mobius transforms that will take the first edge point to the origin.
			// This is to ease the calculation of distances to an edge.
			public Mobius[] Mobii;
			public double[] Angles;

			public void PreCalc( Geometry g )
			{
				List<Mobius> mobii = new List<Mobius>();
				List<double> angles = new List<double>();
				for( int i = 0; i < Edges.Length; i++ )
				{
					var edge = Edges[i];
					Mobius m = new Mobius();
					m.Isometry( g, 0, -edge.Start );
					mobii.Add( m );

					Vector3D end = m.Apply( edge.End );
					double a = Euclidean2D.AngleToCounterClock( new Vector3D( 1, 0 ), end );
					angles.Add( a );
				}
				Mobii = mobii.ToArray();
				Angles = angles.ToArray();
			}

			public bool SameSide( Geometry g, Vector3D p )
			{
				for( int i = 0; i < Edges.Length; i++ )
				{
					if( SameSide( g, p, Edges[i], Mobii[i], Angles[i] ) )
						return true;
				}

				return false;
			}

			public static bool SameSide( Geometry g, Vector3D p, H3.Cell.Edge e, Mobius m, double a )
			{
				p = m.Apply( p );
				p.RotateXY( -a );
				return p.Y > 0;
			}

			public bool PointWithinDist( Geometry g, Vector3D p )
			{
				for( int i = 0; i < Edges.Length; i++ )
				{
					if( PointWithinDist( g, p, Edges[i], Mobii[i], Angles[i] ) )
						return true;
				}

				return false;
			}

			public static bool PointWithinDist( Geometry g, Vector3D p, H3.Cell.Edge e, Mobius m, double a )
			{
				double cutoff = 0.025;

				p = m.Apply( p );

				// Dots near the starting point.
				//if( p.Abs() < cutoff * 4 )
				//return true;

				// Same side as endpoint?
				Vector3D end = m.Apply( e.End );
				if( p.AngleTo( end ) > Math.PI / 2 )
					return false;

				// Beyond the midpoint?
				double midAbs = g == Geometry.Hyperbolic ?
					DonHatch.h2eNorm( DonHatch.e2hNorm( end.Abs() ) / 2 ) :
					Spherical2D.s2eNorm( Spherical2D.e2sNorm( end.Abs() ) / 2 );
				//if( p.Abs() > midAbs )
				//	return false;

				p.RotateXY( -a );

				Vector3D cen;
				double d;
				H3Models.Ball.DupinCyclideSphere( p, cutoff, g, out cen, out d );
				return d > Math.Abs( cen.Y );
			}
		}

		Settings m_settings;

		public void GenImage( Settings settings )
		{
			m_settings = settings;
			int width = settings.Width;
			int height = settings.Height;
			Bitmap image = new Bitmap( width, height );

			// Cycle through all the pixels and calculate the color.
			int row = 0;
			double bounds = settings.Bounds;
			double xoff = settings.XOff;
			double yoff = settings.YOff;
			Parallel.For( 0, width, i =>
			//for( int i=0; i<width; i++ )
			{
				if( row++ % 20 == 0 )
					System.Console.WriteLine( string.Format( "Processing Line {0}", row ) );

				for( int j=0; j<height; j++ )
				{
					double x = -bounds + i * xoff;
					double y = -bounds + j * yoff;

					if( settings.Antialias )
					{
						const int div = 3;
						List<Color> colors = new List<Color>();
						for( int k=0; k<=div; k++ )
						for( int l=0; l<=div; l++ )
						{
							double xa = x /*- xoff/2*/ + k * xoff/div;
							double ya = y /*- yoff/2*/ + l * yoff/div;
							Vector3D v = new Vector3D( xa, ya );

							v = ApplyTransformation( settings.Mobius, v );
							Color color = CalcColor( settings, v );
							colors.Add( color );
						}

						lock( m_lock )
						{
							image.SetPixel( i, j, AvgColor( colors ) );
						}
					}
					else
					{
						lock( m_lock )
						{
							image.SetPixel( i, j, CalcColor( settings, new Vector3D( x, y ) ) );
						}
					}
				}
			} );

			image.Save( settings.FileName, ImageFormat.Png );
		}

		/// <summary>
		/// Using this to move the view around in interesting ways.
		/// </summary>
		private Vector3D ApplyTransformation( Mobius m, Vector3D v )
		{
			return m.Apply( v );
		}

		private readonly object m_lock = new object();

		private static Color AvgColor( List<Color> colors )
		{
			int a = (int)colors.Select( c => (double)c.A ).Average();
			int r = (int)colors.Select( c => (double)c.R ).Average();
			int g = (int)colors.Select( c => (double)c.G ).Average();
			int b = (int)colors.Select( c => (double)c.B ).Average();
			return Color.FromArgb( a, r, g, b );
		}

		private static Color AvgColorSquare( List<Color> colors )
		{
			int a = (int)Math.Sqrt( colors.Select( c => (double)c.A * c.A ).Average() );
			int r = (int)Math.Sqrt( colors.Select( c => (double)c.R * c.R ).Average() );
			int g = (int)Math.Sqrt( colors.Select( c => (double)c.G * c.G ).Average() );
			int b = (int)Math.Sqrt( colors.Select( c => (double)c.B * c.B ).Average() );
			return Color.FromArgb( a, r, g, b );
		}

		private static Color MixColor( Vector3D color )
		{
			Color red = ColorTranslator.FromHtml( "#CF3721" );
			Color green = ColorTranslator.FromHtml( "#258039" );
			//Color blue = ColorTranslator.FromHtml( "#1995AD" );
			Color blue = ColorTranslator.FromHtml( "#375E97" );

			return Color.FromArgb( 255,
				(int)(color.X * red.R + color.Y * green.R + color.Z * blue.R),
				(int)(color.X * red.G + color.Y * green.G + color.Z * blue.G),
				(int)(color.X * red.B + color.Y * green.B + color.Z * blue.B) );
		}

		private Color CalcColor( Settings settings, Vector3D v )
		{
			if( settings.Geometry == Geometry.Hyperbolic )
			{
				if( v.Abs() > 1.00133 )
					return Color.White;

				bool limitSet = v.Abs() >= 1;
				if( limitSet )
					return Color.Black;
			}

			int[] flips = new int[3];
			List<int> allFlips = new List<int>();
			if( !ReflectToFundamental( settings, ref v, ref flips, allFlips ) )
				return Color.White;

			//return Color.Black;
			return ColorFunc( settings, v, flips, allFlips, settings.P, settings.Q, settings.ColorScaling );
		}

		/// <summary>
		/// Somewhat based on http://commons.wikimedia.org/wiki/User:Tamfang/programs
		/// </summary>
		private bool ReflectToFundamental( Settings settings, ref Vector3D v, ref int[] flips, List<int> allFlips )
		{
			Vector3D original = v;

			int iterationCount = 0;
			flips = new int[3];
			while( true && iterationCount < m_maxIterations )
			{
				int idx1 = 0, idx2 = 1, idx3 = 2;

				CircleNE[] temp = new CircleNE[] { settings.Mirrors[idx1], settings.Mirrors[idx2] };
				int[] flipsTemp = new int[2];
				bool converged = ReflectAcrossMirrors( temp, ref v, ref allFlips, ref flipsTemp, ref iterationCount );
				flips[0] += flipsTemp[0];
				flips[1] += flipsTemp[1];

				if( !converged )
					continue;

				if( !ReflectAcrossMirror( settings.Mirrors[idx3], ref v ) )
				{
					flips[2]++;
					allFlips.Add( 2 );
					iterationCount++;
				}
				else
				{
					return true;
				}
			}

			System.Console.WriteLine( string.Format( "Did not converge at point {0}", original.ToString() ) );
			return false;
		}

		private int m_maxIterations = 4000;

		private bool ReflectAcrossMirror( CircleNE mirror, ref Vector3D v )
		{
			bool outsideFacet = mirror.IsPointInsideNE( v );
			if( outsideFacet )
			{
				v = mirror.ReflectPoint( v );
				return false;
			}

			return true;
		}

		private bool ReflectAcrossMirrors( CircleNE[] mirrors, ref Vector3D v, ref List<int> allFlips, ref int[] flips, ref int iterationCount )
		{
			int clean = 0;
			while( true && iterationCount < m_maxIterations )
			{
				for( int i=0; i<mirrors.Length; i++ )
				{
					CircleNE mirror = mirrors[i];
					if( !ReflectAcrossMirror( mirror, ref v ) )
					{
						flips[i]++;
						allFlips.Add( i );
						clean = 0;
					}
					else
					{
						clean++;
						if( clean >= mirrors.Length )
							return true;
					}
				}
				iterationCount++;
			}

			return false;
		}

		private static Color ColorFunc( Settings settings, Vector3D v, int[] flips, List<int> allFlips, int P, int Q, double colorScaling )
		{
			int reflections = flips.Sum();

			List<Color> colors = new List<Color>();
			bool within = false, sameSide = false;
			foreach( var edgeInfo in settings.UniformEdges )
			{
				within = edgeInfo.PointWithinDist( Geometry2D.GetGeometry( P, Q ), v );
				if( within )
				{
					//colors.Add( edgeInfo.Color );
					//colors.Add( Color.Blue );
					//colors.Add( ColorTranslator.FromHtml( "#375e97" ) );
					for( int i = 0; i < 2; i++ )
						colors.Add( ColorTranslator.FromHtml( "#2a3132" ) );
				}

				sameSide = edgeInfo.SameSide( Geometry2D.GetGeometry( P, Q ), v );
				if( sameSide )
					//colors.Add( Color.LightSeaGreen );
					//colors.Add( Color.CornflowerBlue );
					//colors.Add( ColorTranslator.FromHtml( "#763626" ) );
					colors.Add( ColorTranslator.FromHtml( "#a43820" ) );
				else
					//colors.Add( Color.LightSkyBlue );
					//colors.Add( Color.LightSkyBlue );
					colors.Add( ColorTranslator.FromHtml( "#90afc5" ) );
			}

			Color coxeter = reflections % 2 == 0 ? Color.White : ColorTranslator.FromHtml( "#3f681c" );
			int comp = sameSide ? 0 : 1;
			if( !within && reflections % 2 == comp )
				colors.Add( Color.White );

			return AvgColorSquare( colors );
		}
	}
}
