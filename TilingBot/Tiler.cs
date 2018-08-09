namespace TilingBot
{
	using R3.Core;
	using R3.Drawing;
	using R3.Geometry;
	using R3.Math;
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.Serialization;
	using System.Threading.Tasks;
	using Color = System.Drawing.Color;
	using Math = System.Math;

	public class Tiler
	{
		public Tiler()
		{
		}

		public enum Centering
		{
			General, // General position
			Fundamental_Triangle_Vertex1,
			Fundamental_Triangle_Vertex2,
			Fundamental_Triangle_Vertex3,
			Vertex,	// For a vertex-transitive tiling
		}

		[DataContract( Namespace = "" )]
		public class Settings
		{
			public Settings()
			{
				Antialias = true;

				EdgeWidth = 0.025;
				VertexWidth = EdgeWidth;
				Centering = Centering.General;
				EuclideanModel = EuclideanModel.Isometric;
				SphericalModel = SphericalModel.Sterographic;
				HyperbolicModel = HyperbolicModel.Poincare;
				ColoringOption = 0;
				GeodesicLevels = 0;
			}

			[DataMember]
			public int P;

			[DataMember]
			public int Q;

			[DataMember]
			public int[] Active { get; set; }

			[DataMember]
			public bool Dual { get; set; }

			[DataMember]
			public bool Snub { get; set; }

			/// <summary>
			/// If > 1, can be used to denote the number of recusive divisions for a "geodesic sphere" or "geodesic saddle".
			/// Only will apply if p=3.
			/// </summary>
			[DataMember]
			public int GeodesicLevels { get; set; }

			[DataMember]
			public double EdgeWidth { get; set; }

			[DataMember]
			public double VertexWidth { get; set; }

			[DataMember]
			public Centering Centering { get; set; }

			[DataMember]
			public Mobius Mobius { get; set; }

			[DataMember]
			public EuclideanModel EuclideanModel { get; set; }

			[DataMember]
			public SphericalModel SphericalModel { get; set; }

			[DataMember]
			public HyperbolicModel HyperbolicModel { get; set; }

			[DataMember]
			public bool ShowCoxeter { get; set; }

			[DataMember]
			public int ColoringOption { get; set; }

			[DataMember]
			public Color[] Colors { get; set; }

			/// <summary>
			/// Coloring functions can use this data however they please.
			/// </summary>
			[DataMember]
			public int[] ColoringData { get; set; }

			public int Width { get; set; }
			public int Height { get; set; }
			public double Bounds { get; set; }
			public CircleNE[] Mirrors { get; set; }   // in conformal model
			public Vector3D[] Verts { get; set; }
			public string FileName { get; set; }
			public bool Antialias { get; set; }

			/// <summary>
			/// Used for animations, in range [0,1]
			/// </summary>
			public double Anim { get; set; }

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
					return Bounds * 2 / Width;
				}
			}

			public double YOff
			{
				get
				{
					return Bounds * 2 / Height;
				}
			}

			public Vector3D StartingPoint { get; set; }
			public Mobius StartingPointMobius { get; set; }

			public EdgeInfo[] UniformEdges { get; set; }

			public void Init()
			{
				CalcMirrors();
				CalcEdges();
				CalcCentering();
			}

			internal bool iiMirrors()
			{
				if( P != -1 || Q != -1 )
					return false;

				Vector3D p1 = new Vector3D( 1, 0 );
				Vector3D p2 = new Vector3D( 0, 1 - Math.Sqrt( 2 ) );
				Vector3D p3 = new Vector3D( -1, 0 );

				Circle3D arcCircle;
				H3Models.Ball.OrthogonalCircleInterior( p1, p2, out arcCircle );
				Segment seg1 = Segment.Arc( p1, p2, arcCircle.Center, clockwise: true );
				H3Models.Ball.OrthogonalCircleInterior( p2, p3, out arcCircle );
				Segment seg2 = Segment.Arc( p2, p3, arcCircle.Center, clockwise: true );

				Vector3D[] verts = new Vector3D[]
				{
					p1,
					p2,
					p3,
				};

				Circle[] circles = new Circle[]
				{
					new Circle( seg2.P1, seg2.Midpoint, seg2.P2 ),
					new Circle( verts[0], verts[2] ),
					new Circle( seg1.P1, seg1.Midpoint, seg1.P2 ),
				};

				// The oriented mirrors.
				// We can use the opposing vertex to determine the orientation.
				Mirrors = new CircleNE[]
				{
					new CircleNE( circles[0], verts[0] ),
					new CircleNE( circles[1], verts[1] ),
					new CircleNE( circles[2], verts[2] ),
				};
				for( int i = 0; i < 3; i++ )
					Mirrors[i].CenterNE = Mirrors[i].ReflectPoint( Mirrors[i].CenterNE );

				Verts = verts;
				return true;
			}

			internal void CalcMirrors()
			{
				Geometry g = Geometry2D.GetGeometry( P, Q );

				if( iiMirrors() )
					return;

				TilingConfig config = new TilingConfig( P, Q, 1 );
				Tile baseTile = Tiling.CreateBaseTile( config );
				Segment seg = baseTile.Boundary.Segments[0];

				// Order needs to match mirrors.
				// Order is mirrors opposite: vertex, edge, center.
				Vector3D[] verts = new Vector3D[]
				{
					seg.P1,
					seg.Midpoint,
					new Vector3D(),
				};

				// The unoriented mirrors.
				Circle[] circles = new Circle[]
				{
					new Circle( verts[2], verts[1] ),	// Line
					new Circle( verts[2], verts[0] ),	// Line
					seg.Type == SegmentType.Arc ? new Circle( seg.P1, seg.Midpoint, seg.P2 ) : new Circle( verts[1], verts[0] ),    // Arc or Line
				};

				// The oriented mirrors.
				// We can use the opposing vertex to determine the orientation.
				Mirrors = new CircleNE[]
				{
					new CircleNE( circles[0], verts[0] ),
					new CircleNE( circles[1], verts[1] ),
					new CircleNE( circles[2], verts[2] ),
				};
				for( int i = 0; i < 3; i++ )
					Mirrors[i].CenterNE = Mirrors[i].ReflectPoint( Mirrors[i].CenterNE );

				Verts = verts;
			}

			private int[] OtherVerts( int i )
			{
				switch( i )
				{
				case 0:
					return new int[] { 1, 2 };
				case 1:
					return new int[] { 2, 0 };	// The weird order here is because the first vertex may be at infinity, which can cause issues.
				case 2:
					return new int[] { 1, 0 };
				}

				throw new System.ArgumentException();
			}

			public bool IsRegular
			{
				get
				{
					return Active.Length == 1 && Active[0] != 1;
				}
			}

			public bool IsRegularDual
			{
				get
				{
					// NOTE! Our code does not pay attention to the "Dual" flag for regular tilings.
					return Active.Length == 1 && Active[0] == 2;
				}
			}

			public bool IsCatalanDual
			{
				get
				{
					return Dual && !IsRegular;
				}
			}

			private bool IsGeodesicOrGoldberg
			{
				get
				{
					return
						P == 3 &&
						Geometry != Geometry.Euclidean &&   // Not supported yet.
						GeodesicLevels > 0;
				}
			}

			public bool IsGeodesicDomeAnalogue
			{
				get
				{
					return IsGeodesicOrGoldberg && !Dual;
				}
			}

			public bool IsGoldberg
			{
				get
				{
					return IsGeodesicOrGoldberg && Dual;
				}
			}

			public bool IsOmnitruncated
			{
				get
				{
					return Active.Length == 3;
				}
			}

			public bool IsSnub
			{
				get
				{
					return Snub && IsOmnitruncated && !IsGeodesicDomeAnalogue;
				}
			}

			private void CalcEdges()
			{
				Geometry g = Geometry2D.GetGeometry( P, Q );

				if( IsGeodesicDomeAnalogue )
					Active = new int[] { 0 };

				List<int[]> activeSet = new List<int[]>();
				activeSet.Add( Active );

				List<EdgeInfo> edges = new List<EdgeInfo>();
				foreach( int[] active in activeSet )
				{
					if( IsCatalanDual && !IsGoldberg && !Snub )
					{
						var starting = IterateToStartingPoint( g, Mirrors, Verts, active );
						StartingPoint = starting.Item1;
						Color color = MixColor( starting.Item2 );

						// The edges are just the mirrors in this case.
						List<H3.Cell.Edge> startingEdges = new List<H3.Cell.Edge>();
						foreach( int a in active )
						{
							int[] other = OtherVerts( a );
							startingEdges.Add( new H3.Cell.Edge( Verts[other[0]], Verts[other[1]], order: false ) );
						}

						edges.Add( new EdgeInfo() { Edges = startingEdges.ToArray(), Color = ColorUtil.Inverse( color ) } );
					}
					else
					{
						var starting = IterateToStartingPoint( g, Mirrors, Verts, active );
						Vector3D startingPoint = starting.Item1;

						// Cache it. This is not global at the level of settings, so we may need to adjust in the future.
						StartingPoint = startingPoint;

						List<H3.Cell.Edge> startingEdges = new List<H3.Cell.Edge>();
						foreach( int a in active )
						{
							Vector3D reflected = Mirrors[a].ReflectPoint( startingPoint );
							startingEdges.Add( new H3.Cell.Edge( startingPoint, reflected, order: false ) );    // CAN'T ORDER HERE!
						}

						Vector3D color = starting.Item2;
						edges.Add( new EdgeInfo() { Edges = startingEdges.ToArray(), Color = MixColor( color ) } );

						HandleGeodesicOrGoldberg( edges, color );
					}
				}

				if( IsSnub )
				{
					H3.Cell.Edge[] snubEdges = SnubEdges( this );
					EdgeInfo ei = new EdgeInfo() { Edges = snubEdges.ToArray(), Color = edges[0].Color };
					edges.Clear();
					edges.Add( ei );
				}

				Mobius m = new Mobius();
				m.Isometry( g, 0, StartingPoint );
				StartingPointMobius = m;

				UniformEdges = edges.ToArray();
				foreach( EdgeInfo e in UniformEdges )
					e.PreCalc( this );
			}

			private void HandleGeodesicOrGoldberg( List<EdgeInfo> edges, Vector3D color )
			{
				if( !IsGeodesicOrGoldberg )
					return;

				Geometry g = Geometry;

				Vector3D p1 = Verts[0];
				InfiniteQHack( ref p1 );
				Vector3D p2 = p1;
				p2.RotateXY( -2 * Math.PI / 3 );    // CW to make the vertices calculated by the texture helper nicer
				Vector3D p3 = p1;
				p3.RotateXY( -4 * Math.PI / 3 );
				int divisions = (int)Math.Pow( 2, GeodesicLevels );
				TextureHelper.SetLevels( GeodesicLevels );  // Ugh, clean up how the LOD is passed around. So bad.
				Vector3D[] coords = TextureHelper.CalcViaProjections( p1, p2, p3, divisions, Geometry );
				int[] elems = TextureHelper.TextureElements( 1, GeodesicLevels );
				HashSet<H3.Cell.Edge> gEdges = new HashSet<H3.Cell.Edge>( new H3.Cell.EdgeEqualityComparer() );

				// Due to symmetry, keep only edges incident with the first sextant.
				System.Func<Vector3D, bool> inSextant = v =>
				{
					double angle = Euclidean2D.AngleToCounterClock( new Vector3D( 1, 0 ), v );
					return (Tolerance.GreaterThanOrEqual( angle, 0 ) && Tolerance.LessThanOrEqual( angle, Math.PI / 3 ))
						|| Tolerance.Equal( angle, 2 * Math.PI );
				};

				System.Func<int, Vector3D[]> elemVerts = i =>
				{
					int idx1 = i * 3;
					int idx2 = i * 3 + 1;
					int idx3 = i * 3 + 2;
					return new Vector3D[]
					{
						coords[elems[idx1]],
						coords[elems[idx2]],
						coords[elems[idx3]]
					};
				};

				if( IsGoldberg )
				{
					Dictionary<int, Vector3D> centers = new Dictionary<int, Vector3D>();
					for( int i = 0; i < elems.Length / 3; i++ )
						centers[i] = Util.Centroid( g, elemVerts( i ) );

					var graph = TextureHelper.ElementGraph( 1, GeodesicLevels );
					for( int i = 0; i < elems.Length / 3; i++ )
					{
						Vector3D start = centers[i];
						int[] neighbors = graph[i];
						foreach( int n in neighbors )
						{
							Vector3D end = new Vector3D();
							if( n == -1 )
								end = Mirrors[2].ReflectPoint( start );
							else
								end = centers[n];

							if( inSextant( start ) || inSextant( end ) )
								gEdges.Add( new H3.Cell.Edge( start, end ) );
						}
					}

					edges.Clear();
				}
				else
				{
					for( int i = 0; i < elems.Length / 3; i++ )
					{
						Vector3D[] verts = elemVerts( i );
						Vector3D v1 = verts[0], v2 = verts[1], v3 = verts[2];
						if( inSextant( v1 ) )
						{
							gEdges.Add( new H3.Cell.Edge( v1, v2 ) );
							gEdges.Add( new H3.Cell.Edge( v1, v3 ) );
						}
						if( inSextant( v2 ) || inSextant( v3 ) )
							gEdges.Add( new H3.Cell.Edge( v2, v3 ) );
					}
				}

				edges.Add( new EdgeInfo() { Edges = gEdges.ToArray(), Color = MixColor( color ), WidthFactor = 0.25 } );
			}

			private static H3.Cell.Edge[] SnubEdges( Settings settings )
			{
				Vector3D startingPoint = settings.StartingPoint;
				CircleNE[] mirrors = settings.Mirrors;

				System.Func<int, int, Vector3D> end = ( i1, i2 ) =>
				{
					Vector3D e = mirrors[i1].ReflectPoint( startingPoint );
					e = mirrors[i2].ReflectPoint( e );
					return e;
				};

				List<Vector3D> endingPoints = new List<Vector3D>();
				endingPoints.Add( end( 0, 1 ) );
				endingPoints.Add( end( 2, 1 ) );
				endingPoints.Add( end( 1, 2 ) );
				endingPoints.Add( end( 0, 2 ) );	// == 2, 0
				endingPoints.Add( end( 1, 0 ) );

				List<H3.Cell.Edge> snubEdges = new List<H3.Cell.Edge>();
				if( settings.Dual )
				{
					Vector3D p1 = Util.Centroid( settings.Geometry, new Vector3D[] { startingPoint, endingPoints[0], endingPoints[1] } );
					Vector3D p2 = Util.Centroid( settings.Geometry, new Vector3D[] { startingPoint, endingPoints[2], endingPoints[3] } );
					Vector3D p3 = Util.Centroid( settings.Geometry, new Vector3D[] { startingPoint, endingPoints[3], endingPoints[4] } );
					bool order = false;
					snubEdges.Add( new H3.Cell.Edge( settings.Verts[2], p1, order ) );
					snubEdges.Add( new H3.Cell.Edge( p1, settings.Verts[0], order ) );
					snubEdges.Add( new H3.Cell.Edge( settings.Verts[0], p2, order ) );
					snubEdges.Add( new H3.Cell.Edge( p2, p3, order ) );
					snubEdges.Add( new H3.Cell.Edge( p3, settings.Verts[2], order ) );
				}
				else
				{
					foreach( Vector3D v in endingPoints )
						snubEdges.Add( new H3.Cell.Edge( startingPoint, v, order: false ) );
				}
				
				return snubEdges.ToArray();
			}

			public void InfiniteQHack( ref Vector3D v )
			{
				if( Q != -1 )
					return;

				v *= 0.999;
			}

			private void CalcCentering()
			{
				Mobius m = new Mobius();
				switch( Centering )
				{
				case Centering.General:
					return;
				case Centering.Fundamental_Triangle_Vertex1:
					m.Isometry( Geometry, 0, Verts[0] );
					break;
				case Centering.Fundamental_Triangle_Vertex2:
					m.Isometry( Geometry, Math.PI/P, Verts[1] );
					break;
				case Centering.Fundamental_Triangle_Vertex3:
					m.Isometry( Geometry, 0, Verts[2] );
					break;
				case Centering.Vertex:
					m = StartingPointMobius;
					break;
				}
				Mobius = m;
			}

			public Tuple<Vector3D, Vector3D> IterateToStartingPoint( Geometry g, CircleNE[] mirrors, Vector3D[] verts, int[] activeMirrors )
			{
				if( activeMirrors.Length == 1 )
				{
					Vector3D color = new Vector3D();
					color[activeMirrors[0]] = 1;
					return new Tuple<Vector3D, Vector3D>( verts[activeMirrors[0]], color );
				}

				List<int[]> edgeDefs = new List<int[]>();
				if( IsSnub )
				{
					edgeDefs = new List<int[]>
					{
						new int[] { 0, 1 },
						new int[] { 0, 2 },
						new int[] { 1, 0 },
						new int[] { 1, 2 },
						new int[] { 2, 0 },
						new int[] { 2, 1 },
					};
				}
				else
				{
					foreach( int m in activeMirrors )
						edgeDefs.Add( new int[] { m } );
				}

				// We are minimizing the output of this function, 
				// because we want all edge lengths to be as close as possible.
				// Input vector should be in the Ball Model.
				Func<Vector3D, double> diffFunc = v =>
				{
					List<double> lengths = new List<double>();
					for( int i = 0; i < edgeDefs.Count; i++ )
					{
						Vector3D reflected = v;
						foreach( int m in edgeDefs[i] )
							reflected = mirrors[m].ReflectPoint( reflected );

						double dist = 0;
						switch( g )
						{
						case Geometry.Spherical:
							dist = Spherical2D.SDist( v, reflected );
							break;
						case Geometry.Euclidean:
							dist = v.Dist( reflected );
							break;
						case Geometry.Hyperbolic:
							dist = H3Models.Ball.HDist( v, reflected );
							break;
						}

						lengths.Add( dist );
					}

					double result = 0;
					double average = lengths.Average();
					foreach( double length in lengths )
						result += Math.Abs( length - average );
					return result;
				};

				Vector3D[] kleinVerts = Util.KleinVerts( g, verts );

				// Our starting barycentric coords (halfway between all active mirrors).
				Vector3D bary = new Vector3D();
				foreach( int a in activeMirrors )
					bary[a] = 0.5;
				bary = Util.BaryNormalize( bary );

				// For each iteration, we'll shrink this search offset.
				// NOTE: I'm not sure that the starting offset and decrease factor I'm using
				// guarantee convergence, but it seems to be working pretty well (even when varying these parameters).
				double factor = 1.3;
				double searchOffset = bary[activeMirrors[0]] / factor;

				double min = double.MaxValue;
				int iterations = 1000;
				for( int i = 0; i < iterations; i++ )
				{
					min = diffFunc( Util.ToConformal( g, kleinVerts, bary ) );
					foreach( int a in activeMirrors )
					{
						Vector3D baryTest1 = bary, baryTest2 = bary;
						baryTest1[a] += searchOffset;
						baryTest2[a] -= searchOffset;
						baryTest1 = Util.BaryNormalize( baryTest1 );
						baryTest2 = Util.BaryNormalize( baryTest2 );

						double t1 = diffFunc( Util.ToConformal( g, kleinVerts, baryTest1 ) );
						double t2 = diffFunc( Util.ToConformal( g, kleinVerts, baryTest2 ) );
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

					searchOffset /= factor;
				}

				if( !Tolerance.Equal( min, 0.0, 1e-14 ) )
				{
					System.Console.WriteLine( "Did not converge: " + min );
					System.Console.WriteLine( TilingBot.Tweet.Format( this ) );
					System.Console.ReadKey( true );
					//throw new System.Exception( "Boo. We did not converge." );
				}

				return new Tuple<Vector3D, Vector3D>( Util.ToConformal( g, kleinVerts, bary ), bary );
			}

			public double DistInGeometry( Vector3D v1, Vector3D v2 )
			{
				double dist = double.MaxValue;
				switch( this.Geometry )
				{
				case Geometry.Spherical:
					dist = Spherical2D.SDist( v1, v2 );
					break;
				case Geometry.Euclidean:
					dist = v1.Dist( v2 );
					break;
				case Geometry.Hyperbolic:
					dist = H3Models.Ball.HDist( v1, v2 );
					break;
				}

				return dist;
			}

			/// <summary>
			/// NOTE: Only searches active vertices.
			/// </summary>
			public int ClosestVertIndex( Vector3D test )
			{
				double min = double.MaxValue;
				int result = 0;
				foreach( int idx in Active )
				{
					Vector3D testV = Verts[idx];
					double dist = DistInGeometry( test, testV );
					if( dist < min )
					{
						min = dist;
						result = idx;
					}
				}

				return result;
			}

			/// <summary>
			/// The goal of this method is to give a unique color for every face.
			/// Unfortunately, it isn't working for rectified tilings right now.
			/// </summary>
			public int ColorIndexForPoint( Vector3D test )
			{
				EdgeInfo ei = UniformEdges[0];
				var edges = UniformEdges[0].Edges;

				// We need to go through the edges in CCW order.
				// ZZZ - Performance: should be moved to an outer loop.
				List<int> testOrder = new List<int>();
				for( int i = 0; i < edges.Length; i++ )
					testOrder.Add( i );
				testOrder = testOrder.OrderBy( i => ei.Angles[i] ).ToList();

				for( int i=0; i<testOrder.Count; i++ )
				{
					double a = ei.AngleTo( Geometry, test, testOrder[i] );
					if( Tolerance.LessThanOrEqual( a, 0 ) )
						return testOrder[i];
				}

				return testOrder[0];
				//return testOrder.Count == 3 ? testOrder[0] : testOrder.Count;	// Thought this fixed, but it breaks bitruncations.
			}
		}

		public class EdgeInfo
		{
			public H3.Cell.Edge[] Edges;
			public Color Color;
			public double WidthFactor = 1.0;

			// Mobius transforms that will take the first edge point to the origin.
			// This is to ease the calculation of distances to an edge.
			// ZZZ - may be able to simplify (will the same for all edges for a given set of active mirrors)
			public Mobius[] Mobii;
			public double[] Angles;

			public void PreCalc( Settings settings )
			{
				Geometry g = settings.Geometry;
				List<Mobius> mobii = new List<Mobius>();
				List<double> angles = new List<double>();
				for( int i = 0; i < Edges.Length; i++ )
				{
					var edge = Edges[i];
					Mobius m = new Mobius();
					Vector3D start = edge.Start;
					settings.InfiniteQHack( ref start );
					m.Isometry( g, 0, -start );
					mobii.Add( m );

					Vector3D end = m.Apply( edge.End );
					double a = Euclidean2D.AngleToCounterClock( new Vector3D( 1, 0 ), end );
					angles.Add( a );
				}
				Mobii = mobii.ToArray();
				Angles = angles.ToArray();
			}

			public bool PointWithinDist( Settings settings, Vector3D p, double widthFactor )
			{
				for( int i = 0; i < Edges.Length; i++ )
				{
					if( PointWithinDist( settings, p, i, widthFactor ) )
						return true;
				}

				return false;
			}

			public bool PointWithinDist( Settings settings, Vector3D p, int edgeIdx, double widthFactor )
			{
				H3.Cell.Edge e = Edges[edgeIdx];
				Mobius m = Mobii[edgeIdx];
				double a = Angles[edgeIdx];

				// Awkward that I made the width values the euclidean value at the origin :( Worth changing?
				double cutoff = settings.EdgeWidth * widthFactor;
				double cutoffInGeometry = settings.DistInGeometry( new Vector3D( cutoff, 0 ), new Vector3D() );

				p = m.Apply( p );

				// Dots near the starting point.
				if( p.Abs() < settings.VertexWidth * widthFactor )
					return true;

				// Same side as endpoint?
				Vector3D end = m.Apply( e.End );
				if( p.AngleTo( end ) > Math.PI / 2 )
					return false;

				// Beyond the endpoint?
				if( p.Abs() > end.Abs() && 
					settings.DistInGeometry( p, end ) > cutoffInGeometry )
					return false;

				p.RotateXY( -a );

				Vector3D cen;
				double d;
				H3Models.Ball.DupinCyclideSphere( p, cutoff, settings.Geometry, out cen, out d );
				return d > Math.Abs( cen.Y );
			}

			public double DistTo( Settings settings, Vector3D p )
			{
				double min = double.MaxValue;
				for( int i = 0; i < Edges.Length; i++ )
				{
					double d = DistTo( settings, p, i );
					min = Math.Min( d, min );
				}

				return min;
			}

			public double DistTo( Settings settings, Vector3D p, int edgeIdx )
			{
				H3.Cell.Edge e = Edges[edgeIdx];
				Mobius m = Mobii[edgeIdx];
				double a = Angles[edgeIdx];

				p = m.Apply( p );

				double dOrigin = settings.DistInGeometry( p, new Vector3D() );

				// Same side as endpoint?
				double dEdge;
				Vector3D end = m.Apply( e.End );
				if( p.AngleTo( end ) > Math.PI / 2 )
					dEdge = double.PositiveInfinity;
				// Beyond the endpoint?
				else if( p.Abs() > end.Abs() )
				{
					dEdge = settings.DistInGeometry( p, end );
				}
				else
				{
					p.RotateXY( -a );
					double distToP = dOrigin;
					double angleToP = new Vector3D( 1, 0 ).AngleTo( p );

					// Use hyperbolic law of sines.
					dEdge = DonHatch.asinh( DonHatch.sinh( distToP ) * Math.Sin( angleToP ) / Math.Sin( Math.PI / 2 ) );
				}

				dOrigin /= settings.VertexWidth;
				dEdge /= settings.EdgeWidth;
				return Math.Min( dOrigin, dEdge );
			}

			public double AngleTo( Geometry g, Vector3D p, int edgeIdx )
			{
				H3.Cell.Edge e = Edges[edgeIdx];
				Mobius m = Mobii[edgeIdx];
				double a = Angles[edgeIdx];

				p = m.Apply( p );

				double pAngle = Euclidean2D.AngleToCounterClock( new Vector3D( 1, 0 ), p );
				return pAngle - a;
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

				for( int j = 0; j < height; j++ )
				{
					double x = -bounds + xoff / 2 + i * xoff;
					double y = -bounds + yoff / 2 + j * yoff;

					if( settings.Antialias )
					{
						const double perc = 0.99;
						const int div = 3;
						List<Color> colors = new List<Color>();
						for( int k = 0; k <= div; k++ )
						for( int l = 0; l <= div; l++ )
						{
							double xa = x - xoff * perc / 2 + k * xoff * perc / div;
							double ya = y - yoff * perc / 2 + l * yoff * perc / div;
							Vector3D v = new Vector3D( xa, ya );

							Color color;
							//v = ApplyTransformation( v );
							if( !OutsideBoundary( settings, v, out color ) )
							{
								v = ApplyTransformation( v );
								color = CalcColor( settings, v );
							}
							colors.Add( color );
						}

						lock( m_lock )
						{
							image.SetPixel( i, j, ColorUtil.AvgColor( colors ) );
						}
					}
					else
					{
						lock( m_lock )
						{
							Vector3D v = new Vector3D( x, y );
							Color color;
							//v = ApplyTransformation( v );
							if( !OutsideBoundary( settings, v, out color ) )
							{
								v = ApplyTransformation( v );
								color = CalcColor( settings, v );
							}
							image.SetPixel( i, j, color );
						}
					}
				}
			} );

			image.RotateFlip( RotateFlipType.RotateNoneFlipY );
			image.Save( settings.FileName, ImageFormat.Png );
		}

		/// <summary>
		/// Using this to move the view around in interesting ways.
		/// NOTE: Transformations here are applied in reverse.
		/// </summary>
		private Vector3D ApplyTransformation( Vector3D v )
		{
			// Now, apply the model, which depends on the geometry.
			switch( m_settings.Geometry )
			{
			case Geometry.Spherical:
				{
					switch( m_settings.SphericalModel )
					{
					case SphericalModel.Sterographic:
						break;
					case SphericalModel.Gnomonic:
						v = SphericalModels.GnomonicToStereo( v );
						break;
					case SphericalModel.Azimuthal_Equidistant:
						v = SphericalModels.EquidistantToStereo( v );
						break;
					case SphericalModel.Azimuthal_EqualArea:
						v = SphericalModels.EqualAreaToStereo( v );
						break;
					case SphericalModel.Equirectangular:
						v = SphericalModels.EquirectangularToStereo( v );
						break;
					case SphericalModel.Mercator:
						v = SphericalModels.MercatorToStereo( v );
						break;
					case SphericalModel.Orthographic:
						v = SphericalModels.OrthographicToStereo( v );
						break;
					}
					break;
				}
			case Geometry.Euclidean:
				{
					switch( m_settings.EuclideanModel )
					{
					case EuclideanModel.Isometric:
					case EuclideanModel.Conformal:
						break;
					case EuclideanModel.Disk:
						v = EuclideanModels.DiskToIsometric( v );
						break;
					case EuclideanModel.UpperHalfPlane:
						v = EuclideanModels.UpperHalfPlaneToIsometric( v );
						break;
					}
					break;
				}
			case Geometry.Hyperbolic:
				{
					switch( m_settings.HyperbolicModel )
					{
					case HyperbolicModel.Poincare:
						break;
					case HyperbolicModel.Klein:
						v = HyperbolicModels.KleinToPoincare( v );
						/*double mag = HyperbolicModels.KleinToPoincare( v.Dot( v ));
						double mag1 = v.Abs();
						double mag2 = mag1 * mag;
						double magc = mag1 + (mag2 - mag1) * m_settings.Anim;
						v.Normalize();
						v *= magc;*/
						break;
					case HyperbolicModel.UpperHalfPlane:
						{
							v = HyperbolicModels.UpperToPoincare( v );
							break;
						}
					case HyperbolicModel.Band:
						{
							v.X *= Math.Pow( 1.5, 2 );  // Magic number here must match height/width value in Program.cs.
							Complex vc = v.ToComplex();
							Complex result = (Complex.Exp( Math.PI * vc / 2 ) - 1) / (Complex.Exp( Math.PI * vc / 2 ) + 1);
							v = Vector3D.FromComplex( result );
							break;
						}
					case HyperbolicModel.Orthographic:
						v = HyperbolicModels.OrthoToPoincare( v );
						break;
					case HyperbolicModel.Square:
						v = Util.SquareToPoincare( v );
						break;
					}
					break;
				}
			}

			// Apply the centering one.
			Mobius m = m_settings.Mobius;
			v = m.Apply( v );

			return v;
		}

		private readonly object m_lock = new object();

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

		private bool DrawLimit( Settings settings )
		{
			if( settings.Geometry == Geometry.Euclidean )
			{
				if( settings.EuclideanModel == EuclideanModel.Isometric ||
					settings.EuclideanModel == EuclideanModel.Conformal )
					return false;
				else
					return true;
			}

			if( settings.Geometry == Geometry.Spherical )
				return false;

			if( settings.Geometry == Geometry.Hyperbolic )
			{ 
				if( settings.HyperbolicModel == HyperbolicModel.Orthographic )
					return false;
				else
					return true;
			}

			throw new System.Exception( "Unhandled case in DrawLimit check." );
		}

		private bool OutsideBoundary( Settings settings, Vector3D v, out Color color )
		{
			Color bgColor = Color.FromArgb( 0, 255, 255, 255 );
			if( DrawLimit( settings ) )
			{
				double compare = v.Abs();
				if( settings.Geometry == Geometry.Euclidean &&
					settings.EuclideanModel == EuclideanModel.UpperHalfPlane )
					compare = -v.Y;
				else if( settings.Geometry == Geometry.Hyperbolic )
				{
					if( settings.HyperbolicModel == HyperbolicModel.UpperHalfPlane )
						compare = -v.Y;
					else if( settings.HyperbolicModel == HyperbolicModel.Band )
						compare = Math.Abs( v.Y );
					else if( settings.HyperbolicModel == HyperbolicModel.Square )
						compare = Math.Max( Math.Abs( v.X ), Math.Abs( v.Y ) );
				}

				if( compare > 1.00133 )
				{
					color = bgColor;
					return true;
				}

				bool limitSet = compare >= 1;
				if( limitSet )
				{
					color = Color.Black;
					return true;
				}
			}

			if( settings.Geometry == Geometry.Spherical )
			{
				if( settings.SphericalModel == SphericalModel.Azimuthal_Equidistant ||
					settings.SphericalModel == SphericalModel.Azimuthal_EqualArea || 
					settings.SphericalModel == SphericalModel.Orthographic )
				{
					if( v.Abs() > 1 )
					{
						color = bgColor;
						return true;
					}
				}
			}

			color = Color.White;
			return false;
		}

		private Color CalcColor( Settings settings, Vector3D v )
		{
			int[] flips = new int[3];
			List<int> allFlips = new List<int>();
			if( !ReflectToFundamental( settings, ref v, ref flips, allFlips ) )
				return Color.FromArgb( 255, 128, 128, 128 );

			switch( settings.ColoringOption )
			{
			case 0:
				return ColorFunc0( settings, v, flips );
			case 1:
				return ColorFunc1( settings, v, flips );
			case 2:
				return ColorFuncIntensity( settings, v, flips );
			case 3:
				return ColorFunc1( settings, v, flips, hexagonColoring: true );
			case 4:
				return ColorFuncPicture( settings, v, flips, allFlips.ToArray() );
			}

			throw new System.ArgumentException( "Unknown Coloring Option." );
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

			//System.Console.WriteLine( string.Format( "Did not converge at point {0}", original.ToString() ) );
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
				for( int i = 0; i < mirrors.Length; i++ )
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

		private void ReflectSequence( CircleNE[] mirrors, int[] reflections, ref Vector3D v )
		{
			foreach( int reflection in reflections )
				v = mirrors[reflection].ReflectPoint( v );
		}

		private static int ColoringData( Settings settings, int idx )
		{
			if( settings.ColoringData == null ||
				settings.ColoringData.Length < idx - 1 )
				return 0;

			return settings.ColoringData[idx];
		}

		/// <summary>
		/// TODO: custom edge coloring
		/// </summary>
		private static Color ColorFunc0( Settings settings, Vector3D v, int[] flips )
		{
			int reflections = flips.Sum();

			bool lightenEdges = ColoringData( settings, 0 ) > 0;
			System.Action<List<Color>> darken = new System.Action<List<Color>>( c =>
			{
				for( int i = 0; i < 2; i++ )
					c.Add( ColorTranslator.FromHtml( "#2a3132" ) );
			} );
			System.Action<List<Color>> lighten = new System.Action<List<Color>>( c =>
			{
				c.Add( Color.White );
			} );

			List<Vector3D> pointsToTry = new List<Vector3D>();
			int compare = settings.Dual ? 0 : 1; // This is for better coloring on duals to snubs.
			if( reflections % 2 == compare && settings.IsSnub )
			{
				pointsToTry.Add( settings.Mirrors[0].ReflectPoint( v ) );
				pointsToTry.Add( settings.Mirrors[1].ReflectPoint( v ) );
				pointsToTry.Add( settings.Mirrors[2].ReflectPoint( v ) );
			}
			else
				pointsToTry.Add( v );

			List<Color> colors = new List<Color>();
			bool within = false;
			foreach( Vector3D testV in pointsToTry )
			{ 
				foreach( var edgeInfo in settings.UniformEdges )
				{
					bool tWithin = edgeInfo.PointWithinDist( settings, testV, edgeInfo.WidthFactor );
					if( tWithin )
					{
						if( lightenEdges )
							lighten( colors );
						else
							darken( colors );

						within = true;
						break;
					}
				}
				if( within )
					break;
			}

			int idx;
			if( settings.IsSnub || settings.IsGoldberg )
				idx = 0;
			else
				idx = settings.ColorIndexForPoint( v );
			colors.Add( settings.Colors[idx] );

			if( !within && reflections % 2 == 0 && settings.ShowCoxeter )
			{
				// Do the opposite of what we did for edges.
				if( lightenEdges )
					darken( colors );
				else
					lighten( colors );
			}

			return ColorUtil.AvgColorSquare( colors );
		}

		/// <summary>
		/// TODO: support snubs
		/// </summary>
		private static Color ColorFunc1( Settings settings, Vector3D v, int[] flips, bool hexagonColoring = false )
		{
			int reflections = flips.Sum();
			bool useStandardColors = ColoringData( settings, 0 ) == 0;

			List<Color> colors = new List<Color>();
			foreach( var edgeInfo in settings.UniformEdges )
			{
				if( useStandardColors )
				{
					if( edgeInfo.PointWithinDist( settings, v, edgeInfo.WidthFactor ) )
						colors.Add( edgeInfo.Color );
				}
				else
				{
					for( int i = 0; i < edgeInfo.Edges.Length; i++ )
					{
						if( edgeInfo.PointWithinDist( settings, v, i, edgeInfo.WidthFactor ) )
							colors.Add( settings.Colors[i] );
					}
				}
			}

			Color whitish = ColorTranslator.FromHtml( "#F1F1F2" );
			//Color whitish = Color.FromArgb( 0, 0, 0, 0 );
			Color darkish = ColorTranslator.FromHtml( "#BCBABE" );
			if( hexagonColoring )
			{
				int incrementsUntilRepeat = 30;
				int offset = 18;
				int count = settings.ShowCoxeter ? reflections : flips[2];
				whitish = ColorUtil.ColorAlongHexagon( incrementsUntilRepeat, count + offset );
				/*Color edgeColor = ColorUtil.Inverse( whitish );
				if( colors.Count > 0 )
					return edgeColor;
				return whitish;*/
			}

			if( colors.Count > 0 )
				return ColorUtil.AvgColor( colors );

			if( !settings.ShowCoxeter )
				return whitish;

			return reflections % 2 == 0 ? whitish : darkish;
		}

		/// <summary>
		/// TODO: support snubs
		/// </summary>
		private static Color ColorFuncIntensity( Settings settings, Vector3D v, int[] flips )
		{
			int reflections = flips.Sum();

			double dist = double.MaxValue;
			foreach( var edgeInfo in settings.UniformEdges )
			{
				double d = edgeInfo.DistTo( settings, v );
				dist = Math.Min( d, dist );
			}

			double low = 0;
			if( settings.ShowCoxeter && reflections % 2 != 0 )
			{
				low += 0.5;
				if( low > 1 )
					low = 1;
			}

			double newVal = Math.Exp( -.2 * dist * dist );
			newVal = low + newVal * (1.0 - low);

			// I tried adjusting saturation and hue as well, but didn't really like the results.
			bool inverse = ColoringData( settings, 0 ) > 0;
			if( inverse )
				newVal = 1.0 - newVal;

			Color c = settings.Colors[0];
			return ColorUtil.AdjustL( c, newVal );
		}

		private Color ColorFuncPicture( Settings settings, Vector3D v, int[] flips, int[] allFlips )
		{
			foreach( var edgeInfo in settings.UniformEdges )
			{
				bool tWithin = edgeInfo.PointWithinDist( settings, v, edgeInfo.WidthFactor );
				if( tWithin )
					return Color.Black;
			}

			int reflections = flips.Sum();

			// Reflect back so that we take up the entire polygon {p}.
			int[] sequence = allFlips.Reverse().Where( m => m != 2 ).ToArray();
			//int[] sequence = allFlips.Reverse().Where( m => m != 1 ).ToArray();
			//v = settings.Mobius.Inverse().Apply( v );
			ReflectSequence( settings.Mirrors, sequence, ref v );
			//v = settings.Mobius.Apply( v );

			lock( m_lock )
			{
				if( m_texture == null )
				{

					string path = System.IO.Path.Combine( Persistence.WorkingDir, "gold.JPG" );
					m_texture = new Bitmap( path );
					m_texture.RotateFlip( RotateFlipType.RotateNoneFlipY );
				}

				double w = m_texture.Width;
				double h = m_texture.Height;

				// Scale v so that the image will cover the f.d.
				double s = 1.0 / ((settings.Verts[0].Abs() + 1) / 2);
				//s *= 2.3;
				//v *= s;

				// Get the pixel coords.
				double size = w < h ? w : h;
				double x = (v.X + 1) / 2 * size;
				double y = (v.Y + 1) / 2 * size;
				/*y += 175;
				x += 15;
				y -= 0;
				//x -= 25;
				if( x < 0 )
					x = Math.Abs( x );
				if( x >= w )
					x = w - 1 - (x-w);
				if( y < 0 )
					y += h;
				if( y >= h )
					y -= h;*/

				if( x < 0 || y < 0 ||
					x >= w || y >= h )
				//if( v.Abs() > 1.13 )
					return Color.FromArgb( 255, 0, 0, 0 );

				Color result = m_texture.GetPixel( (int)x, (int)y );
				double lOff = 0.1;
				if( reflections % 2 == 0 )
					// result = ColorUtil.AvgColorSquare( new List<Color>( new Color[] { result, ColorTranslator.FromHtml( "#2a3132" ) } ) );
					result = ColorUtil.AdjustL( result, result.GetBrightness() - lOff );
				else
					result = ColorUtil.AdjustL( result, result.GetBrightness() + lOff );
				return result;
			}
		}

		private Bitmap m_texture;
	}
}
