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
			Vertex,	// Centers on the generating vertex
		}

		[DataContract( Namespace = "" )]
		public class Settings
		{
			public Settings()
			{
				SetDefaults();
			}

			[OnDeserializing]
			private void OnDeserializing( StreamingContext context )
			{
				SetDefaults();
			}

			private void SetDefaults()
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
				RingRepeats = 5;
				SchwarzChristoffelPolygon = 3;
			}

			[DataMember]
			public int P;

			[DataMember]
			public int Q;

			/// <summary>
			/// Order of these... mirrors opposite vertex, edge, and tile
			/// </summary>
			[DataMember]
			public int[] Active { get; set; }

			[DataMember]
			public bool Dual { get; set; }

			[DataMember]
			public bool DualCompound { get; set; }

			[DataMember]
			public bool Snub { get; set; }

			/// <summary>
			/// If > 1, can be used to denote the number of recusive divisions for a "geodesic sphere" or "geodesic saddle".
			/// Only will apply if p=3.
			/// </summary>
			[DataMember]
			public int GeodesicLevels { get; set; }


			[DataMember]
			public int RingRepeats { get; set; }

			[DataMember]
			public int SchwarzChristoffelPolygon { get; set; }

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
			public bool CirclePacking { get; set; }

			[DataMember]
			public int ColoringOption { get; set; }

			[DataMember]
			public Color[] Colors { get; set; }

			/// <summary>
			/// Coloring functions can use this data however they please.
			/// </summary>
			[DataMember]
			public int[] ColoringData { get; set; }

			[DataMember]
			public double Bounds { get; set; }

			public int Width { get; set; }
			public int Height { get; set; }
			public CircleNE[] Mirrors { get; set; }   // in conformal model
			public Vector3D[] Verts { get; set; }
			public string FileName { get; set; }
			public bool Antialias { get; set; }

			/// <summary>
			/// A rotation/translation of the entire image.
			/// </summary>
			public double ImageRot { get; set; }
			public Vector3D ImageTrans { get; set; }

			public double PeriodBand
			{
				get
				{
					if( Geometry != Geometry.Hyperbolic )
						throw new System.NotImplementedException();

					Vector3D off = new Vector3D( DonHatch.h2eNorm( Animate.OffsetInSpace( this, 0, 2, 2 ) ), 0 );
					return HyperbolicModels.PoincareToBand( off ).X;
				}
			}

			/// <summary>
			/// Used for animations, in range [0,1]
			/// </summary>
			public double Anim { get; set; }

			public H3.Cell.Edge[] GlobalEdges { get; set; }

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

			public ElementInfo[] UniformEdges { get; set; }

			private void ResolveSettingDiscrepencies()
			{
				if( CirclePacking )
					Dual = false;
			}

			public void Init()
			{
				ResolveSettingDiscrepencies();
				CalcMirrors();

				List<ElementInfo> ei = new List<ElementInfo>();
				ei.AddRange( CalcEdges() );
				if( DualCompound )
				{
					Dual = !Dual;
					if( IsRegular )
					{
						if( Active[0] == 0 )
							Active[0] = 2;
						else
							Active[0] = 0;
					}
					/*EdgeInfo[] dualEdges = CalcEdges();
					List<H3.Cell.Edge> combined = new List<H3.Cell.Edge>();
					combined.AddRange( edges.First().Edges );
					combined.AddRange( dualEdges.First().Edges );
					edges.First().Edges = combined.ToArray();*/
					ei.AddRange( CalcEdges() );
				}

				UniformEdges = ei.ToArray();
				foreach( ElementInfo e in UniformEdges )
					e.PreCalc( this );

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

				Polygon baseTile = new Polygon();
				baseTile.CreateRegular( P, Q );
				Segment seg = baseTile.Segments[0];

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

			private ElementInfo[] CalcEdges()
			{
				Geometry g = Geometry2D.GetGeometry( P, Q );

				if( IsGeodesicDomeAnalogue )
					Active = new int[] { 0 };

				List<int[]> activeSet = new List<int[]>();
				activeSet.Add( Active );

				List<ElementInfo> edges = new List<ElementInfo>();
				foreach( int[] active in activeSet )
				{
					if( IsCatalanDual && !IsGoldberg && !Snub )
					{
						var starting = IterateToStartingPoint( g, Mirrors, Verts, active );
						StartingPoint = starting.Item1;
						Color color = MixColor( starting.Item2 );

						Func<int, int[]> OtherVerts = i =>
						{
							switch( i )
							{
								case 0:
									return new int[] { 1, 2 };
								case 1:
									return new int[] { 0, 2 };
								case 2:
									return new int[] { 0, 1 };
							}

							throw new System.ArgumentException();
						};

						// The edges are just the mirrors in this case.
						HashSet<Vector3D> vertsToDraw = new HashSet<Vector3D>();
						int edgesMeetingVert1 = 0;
						List<H3.Cell.Edge> startingEdges = new List<H3.Cell.Edge>();
						foreach( int a in active )
						{
							int[] other = OtherVerts( a );
							Vector3D start = Verts[other[0]];
							Vector3D end = Verts[other[1]];
							startingEdges.Add( new H3.Cell.Edge( start, end, order: false ) );

							// Don't add if at infinity...
							vertsToDraw.Add( start );
							vertsToDraw.Add( end );

							if( a == 0 || a == 2 )
								edgesMeetingVert1++;
						}

						// If we had only a single edge meeting vertex 1, we don't want to draw a vertex there.
						if( edgesMeetingVert1 == 1 )
						{
							vertsToDraw.Remove( Verts[1] );
						}

						// Don't include ideal verts in the hyperbolic case.
						if( g == Geometry.Hyperbolic )
							vertsToDraw.Remove( new Vector3D( 1, 0 ) );
						
						edges.Add( new ElementInfo()
						{
							Verts = vertsToDraw.ToArray(),
							Edges = startingEdges.ToArray(),
							Color = ColorUtil.Inverse( color )
						} );
					}
					else
					{
						var starting = IterateToStartingPoint( g, Mirrors, Verts, active );
						Vector3D startingPoint = starting.Item1;

						// Animation.
						int[] active_ = active;
						AnimateGeneratingVertex( g, ref startingPoint, ref active_ );
						//starting = new Tuple<Vector3D, Vector3D>( startingPoint, new Vector3D( 1.0 - this.Anim, 0, this.Anim ) );

						// Cache it. This is not global at the level of settings, so we may need to adjust in the future.
						StartingPoint = startingPoint;

						List<H3.Cell.Edge> startingEdges = new List<H3.Cell.Edge>();
						foreach( int a in active_ )
						{
							Vector3D reflected = Mirrors[a].ReflectPoint( startingPoint );
							startingEdges.Add( new H3.Cell.Edge( startingPoint, reflected, order: false ) );    // CAN'T ORDER HERE!
						}

						Vector3D color = starting.Item2;
						edges.Add( new ElementInfo()
						{
							Verts = new Vector3D[] { startingPoint },
							Edges = startingEdges.ToArray(),
							Color = MixColor( color )
						} );

						HandleGeodesicOrGoldberg( edges, color );
					}
				}

				if( IsSnub )
				{
					H3.Cell.Edge[] snubEdges = SnubEdges( this );
					ElementInfo ei = new ElementInfo() {
						Verts = snubEdges.Select( e => e.Start ).ToArray(),
						Edges = snubEdges.ToArray(),
						Color = edges[0].Color };
					edges.Clear();
					edges.Add( ei );
				}

				Mobius m = new Mobius();
				m.Isometry( g, 0, StartingPoint );
				StartingPointMobius = m;

				return edges.ToArray();
			}

			private void AnimateGeneratingVertex( Geometry g, ref Vector3D startingPoint, ref int[] active_ )
			{
				if( false )
				{
					// {i,i}
					Vector3D v1 = new Vector3D( -1, 0 );
					Vector3D v2 = v1 + new Vector3D( 2 * this.Anim, 0 );
					startingPoint = v2;
					active_ = new int[] { 0, 2 };
				}

				if( false )
				{
					Vector3D v0 = Verts[0];
					Vector3D v1 = Verts[1];
					Vector3D v2 = Verts[2];
					double hDist1 = H3Models.Ball.HDist( v0, v1 );
					double hDist2 = H3Models.Ball.HDist( v1, v2 );
					double hDist3 = H3Models.Ball.HDist( v2, v0 );
					double hDist = ( hDist1 + hDist2 + hDist3 );
					hDist *= this.Anim;

					startingPoint = new Vector3D( DonHatch.h2eNorm( hDist ), 0 );

					int[] flips = new int[4];
					List<int> allFlips = new List<int>();
					ReflectToFundamental( this, ref startingPoint, ref flips, allFlips );

					active_ = new int[] { 0, 1, 2 };
				}

				if( false )
				{
					Vector3D v1 = Verts[0];
					Vector3D v2 = Verts[2];
					double hDist = H3Models.Ball.HDist( v1, v2 );
					hDist *= this.Anim;

					Mobius m1 = new Mobius();
					m1.Isometry( this.Geometry, 0, -v1 );
					Vector3D v2_ = m1.Apply( v2 );
					double newD = DonHatch.h2eNorm( hDist );
					v2_.Normalize();
					v2_ *= newD;
					startingPoint = m1.Inverse().Apply( v2_ );

					if( Tolerance.GreaterThan( this.Anim, 0 ) )
					{
						if( Tolerance.Equal( this.Anim, 1 ) )
							active_ = new int[] { 2 };
						else
							active_ = new int[] { 0, 2 };
					}
				}
				if( false )
				{
					// Get the edge length. Assumes omnitruncation.
					Vector3D reflected = Mirrors[0].ReflectPoint( startingPoint );

					double hDist = H3Models.Ball.HDist( startingPoint, reflected ) / 2;

					Vector3D cen;
					double d;
					H3Models.Ball.DupinCyclideSphere( startingPoint, DonHatch.h2eNorm( hDist ), g, out cen, out d );

					double a = Math.PI * 2 * Anim;
					startingPoint = cen + new Vector3D( d * Math.Cos( a ), d * Math.Sin( a ) );
				}
			}

			private void HandleGeodesicOrGoldberg( List<ElementInfo> edges, Vector3D color )
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

				edges.Add( new ElementInfo()
				{
					Verts = gEdges.Select( e => e.Start ).ToArray(),
					Edges = gEdges.ToArray(),
					Color = MixColor( color ),
					WidthFactor = 0.25
				} );
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

			public Mobius CenteringMobius()
			{
				Mobius m = new Mobius();
				switch( Centering )
				{
				case Centering.General:
					return Mobius;
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
				return m;
			}

			private void CalcCentering()
			{
				Mobius = CenteringMobius();
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
				double factor = 1.2;
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
				ElementInfo ei = UniformEdges.Last();
				var edges = ei.Edges;

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

		public class ElementInfo
		{
			public Vector3D[] Verts;
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
				// Check verts first.
				double vCutoff = settings.VertexWidth * widthFactor;
				double vCutoffInGeometry = settings.DistInGeometry( new Vector3D( vCutoff, 0 ), new Vector3D() );
				foreach( Vector3D vert in Verts )
				{
					if( settings.DistInGeometry( p, vert ) < vCutoffInGeometry )
						return true;
				}

				H3.Cell.Edge e = Edges[edgeIdx];
				Mobius m = Mobii[edgeIdx];
				double a = Angles[edgeIdx];

				// Awkward that I made the width values the euclidean value at the origin :( Worth changing?
				double eCutoff = settings.EdgeWidth * widthFactor;
				double eCutoffInGeometry = settings.DistInGeometry( new Vector3D( eCutoff, 0 ), new Vector3D() );

				p = m.Apply( p );

				// Circle packing?
				if( settings.CirclePacking )
				{
					double eLength = settings.DistInGeometry( e.Start, e.End ) / 2;
					double compare = settings.DistInGeometry( p, new Vector3D() );
					if( Math.Abs( eLength - compare ) < eCutoffInGeometry )
						return true;
					return false;
				}

				// Same side as endpoint?
				Vector3D end = m.Apply( e.End );
				if( p.AngleTo( end ) > Math.PI / 2 )
					return false;

				// Beyond the endpoint?
				if( p.Abs() > end.Abs() && 
					settings.DistInGeometry( p, end ) > eCutoffInGeometry )
					return false;

				p.RotateXY( -a );

				Vector3D cen;
				double d;
				H3Models.Ball.DupinCyclideSphere( p, eCutoff, settings.Geometry, out cen, out d );
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
				// Track verts first.
				double dVerts = double.MaxValue;
				foreach( Vector3D vert in Verts )
				{
					dVerts = Math.Min( dVerts, settings.DistInGeometry( p, vert ) );
				}

				H3.Cell.Edge e = Edges[edgeIdx];
				Mobius m = Mobii[edgeIdx];
				double a = Angles[edgeIdx];

				p = m.Apply( p );

				// Circle packing?
				if( settings.CirclePacking )
				{
					double eLength = settings.DistInGeometry( e.Start, e.End ) / 2;
					double compare = settings.DistInGeometry( p, new Vector3D() );
					return Math.Abs( eLength - compare );
				}

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

				dVerts /= settings.VertexWidth;
				dEdge /= settings.EdgeWidth;
				return Math.Min( dVerts, dEdge );
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

		private void DrawCircle( Bitmap image, Settings settings )
		{
			double b = settings.Bounds;
			ImageSpace i = new ImageSpace( settings.Width, settings.Height );
			i.XMin = -b; i.XMax = b;
			i.YMin = -b; i.YMax = b;

			Vector3D cen = new Vector3D();
			cen = settings.Verts[0];
			cen.RotateXY( Math.PI / 2 );
			/*cen = settings.Mirrors[2].ReflectPoint( cen );
			cen.RotateXY( Math.PI );
			cen = settings.Mirrors[2].ReflectPoint( cen );
			cen.RotateXY( Math.PI / 4 );*/
			double r = ( cen.Y + 1 ) / 2;
			cen.Y = cen.Y - r;
			Circle circ = new Circle { Center = cen, Radius = r };

			float scale = 1;
			using( Graphics g = Graphics.FromImage( image ) )
			using( Pen p = new Pen( Color.Blue, scale * 3.0f ) )
			{
				DrawUtils.DrawCircle( circ, g, i, p );
			}
		}

		public void GenImage( Settings settings )
		{
			m_settings = settings;
			int width = settings.Width;
			int height = settings.Height;
			Bitmap image = new Bitmap( width, height );
			//DrawCircle( image, settings );

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

			//DrawCircle( image, settings );
			image.RotateFlip( RotateFlipType.RotateNoneFlipY );
			image.Save( settings.FileName, ImageFormat.Png );
		}

		private Vector3D ApplyAnimTransform( Vector3D v, double t )
		{
			Vector3D onSphere = Sterographic.PlaneToSphere( v );
			if( onSphere.Z >= t )
				return v * 10000;
			onSphere.Z += 1.0;
			v = onSphere.CentralProject( 1.0 + t );
			return v;
		}

		/// <summary>
		/// Using this to move the view around in interesting ways.
		/// NOTE: Transformations here are applied in reverse.
		/// </summary>
		private Vector3D ApplyTransformation( Vector3D v )
		{
			v.RotateXY( m_settings.ImageRot );
			v -= m_settings.ImageTrans;

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
					case SphericalModel.Sinusoidal:
						v = SphericalModels.SinusoidalToStereo( v );
						break;
					case SphericalModel.PeirceQuincuncial:
						v = Util.PeirceToStereo( v );
						break;
					}
					break;
				}
			case Geometry.Euclidean:
				{
					switch( m_settings.EuclideanModel )
					{
					case EuclideanModel.Isometric:
						break;
					case EuclideanModel.Conformal:
						break;
					case EuclideanModel.Disk:
						v = EuclideanModels.DiskToIsometric( v );
						break;
					case EuclideanModel.UpperHalfPlane:
						v = EuclideanModels.UpperHalfPlaneToIsometric( v );
						break;
					case EuclideanModel.Spiral:
						v = EuclideanModels.SpiralToIsometric( v, m_settings.P, 7, 3 );
						break;
					case EuclideanModel.Loxodromic:
						v = EuclideanModels.LoxodromicToIsometric( v, m_settings.P, 11, 3 );
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
							v = HyperbolicModels.BandToPoincare( v );
							break;

							/*
							// Animating Band -> Poincare.
							Complex w = vc;
							if( Tolerance.Zero( this.m_settings.Anim ) )
							{
								// Do nothing.
							}
							else if( Tolerance.Equal( this.m_settings.Anim, 1.0 ) )
							{ 
								w = bandToDisk( vc );
							}
							else
							{
								double factor = 1.0 / ( Math.Pow( this.m_settings.Anim, 0.5 ) );
								double post = bandToDisk( new Complex( 0, 1.0 / factor ) ).Magnitude;
								vc /= factor;
								w = bandToDisk( vc );
								w /= post;
							} */
						}
					case HyperbolicModel.Orthographic:
						v = HyperbolicModels.OrthoToPoincare( v );
						break;
					case HyperbolicModel.Square:
						v = Util.SquareToPoincare( v );
						break;
					case HyperbolicModel.InvertedPoincare:
						double mag = 1.0 / v.Abs();
						v.Normalize();
						v *= mag;
						break;
					case HyperbolicModel.Joukowsky:
						Vector3D cen = new Vector3D();
						v = HyperbolicModels.JoukowskyToPoincare( v, cen );
						break;
					case HyperbolicModel.Ring:
						v = HyperbolicModels.RingToPoincare( v, m_settings.PeriodBand, m_settings.RingRepeats );
						break;
					case HyperbolicModel.Azimuthal_Equidistant:
						v = HyperbolicModels.EquidistantToPoincare( v );
						break;
					case HyperbolicModel.Azimuthal_EqualArea:
						v = HyperbolicModels.EqualAreaToPoincare( v );
						break;
					case HyperbolicModel.Schwarz_Christoffel:
						v = Vector3D.FromComplex( 
							Schwarz_Christoffel.inversesc( v.ToComplex(), m_settings.SchwarzChristoffelPolygon ) );
						break;
						}
					break;
				}
			}

			// Branched cover?
			/*Complex vc = v.ToComplex();
			vc = Complex.Pow( vc, 3.0 );
			v = Vector3D.FromComplex( vc );*/

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
				if( settings.EuclideanModel == EuclideanModel.Disk ||
					settings.EuclideanModel == EuclideanModel.UpperHalfPlane )
					return true;
				else
					return false;
			}

			if( settings.Geometry == Geometry.Spherical )
				return false;

			if( settings.Geometry == Geometry.Hyperbolic )
			{ 
				if( settings.HyperbolicModel == HyperbolicModel.Orthographic ||
					settings.HyperbolicModel == HyperbolicModel.Joukowsky ||
					settings.HyperbolicModel == HyperbolicModel.Azimuthal_Equidistant ||
					settings.HyperbolicModel == HyperbolicModel.Azimuthal_EqualArea )
					return false;
				else
					return true;
			}

			throw new System.Exception( "Unhandled case in DrawLimit check." );
		}

		private static Color bgColor = Color.FromArgb( 0, 255, 255, 255 );

		private bool OutsideBoundary( Settings settings, Vector3D v, out Color color )
		{
			if( settings.Geometry == Geometry.Hyperbolic &&
				settings.HyperbolicModel == HyperbolicModel.Ring )
			{
				Vector3D v1 = HyperbolicModels.BandToRing( new Vector3D( 0, 1 ), settings.PeriodBand, settings.RingRepeats );
				Vector3D v2 = HyperbolicModels.BandToRing( new Vector3D( 0, -1 ), settings.PeriodBand, settings.RingRepeats );
				if( v.Abs() < v1.Abs() || v.Abs() > v2.Abs() )
				{
					color = bgColor;
					return true;
				}
			}

			if( settings.Geometry == Geometry.Hyperbolic &&
				settings.HyperbolicModel == HyperbolicModel.Schwarz_Christoffel )
			{
				Polygon poly = Polygon.CreateEuclidean( settings.SchwarzChristoffelPolygon );
				if( !poly.IsPointInside( v ) )
				{
					poly.Scale( 1.00133 );
					if( !poly.IsPointInside( v ) )
						color = bgColor;
					else
						color = Color.Black;
					return true;
				}
			}

			if( DrawLimit( settings ) )
			{
				v.RotateXY( settings.ImageRot );
				v -= settings.ImageTrans;
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
					{
						compare = Math.Max( Math.Abs( v.X ), Math.Abs( v.Y ) );
					}
					else if( settings.HyperbolicModel == HyperbolicModel.InvertedPoincare )
					{
						compare = 1.0 / v.Abs();
					}
					/*else if( settings.HyperbolicModel == HyperbolicModel.Joukowsky )
					{
						double a = 1.0;
						double b = m_settings.Anim;
						compare = Math.Abs( v.X* v.X / ( a * a ) + v.Y * v.Y / ( b * b ) );
					}*/
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
					//color = bgColor;
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

				if( settings.SphericalModel == SphericalModel.Sinusoidal )
				{
					if( Math.Cos( v.Y * Math.PI / 2 ) < Math.Abs( v.X ) )
					{
						color = bgColor;
						return true;
					}
				}
			}

			color = Color.White;
			return false;
		}

		public double Minkowski(Vector3D p1, Vector3D p2)
		{
			return - Math.Pow( p1.X - p2.X, 2 ) + Math.Pow( p1.Y - p2.Y, 2 );
		}

		private Color CalcColorMinkowski( Settings settings, Vector3D v )
		{
			double spacing = 0.5;
			v /= spacing;
			Vector3D vOrig = v;

			Vector3D e1 = new Vector3D( 1, 0 );
			Vector3D e2 = e1;
			e2.RotateXY( Math.PI / 3 );

			// Transform
			{
				double x = v.X, y = v.Y;
				double alpha = Euclidean2D.AngleToClock( e1 + 2 * e2, e1 );
				double B = Math.Tan( alpha );
				double eta = m_settings.Anim * DonHatch.atanh( B );
				v.X = x * Math.Cosh( eta ) - y * Math.Sinh( eta );
				v.Y = y * Math.Cosh( eta ) - x * Math.Sinh( eta );
			}

			int fx = (int)Math.Floor( ( v.X - v.Y * e2.X / e2.Y ) / e1.X );
			int fy = (int)Math.Floor( v.Y / e2.Y );

			double s2, d;
			int num = 3;
			for( int i = -num; i <= num; i++ )
				for( int j = -num; j <= num; j++ )
				{
					Vector3D compare = new Vector3D();
					compare = e1 * ( fx + i ) + e2 * ( fy + j );

					s2 = Minkowski( v, compare );
					d = Math.Sqrt( Math.Abs( s2 ) );
					//double intensity = Math.Exp( -100 * d * d );

					// Too close to the light ray causes issues.
					double ed = v.Dist( compare );

					if( ed < 0.9 )
						if( d < settings.VertexWidth )
							return Color.Black;
				}

			s2 = Minkowski( vOrig, new Vector3D() );
			d = Math.Sqrt( Math.Abs( s2 ) );
			double dr = Math.Round( d, 0 );
			if( Math.Abs( d - dr ) < 0.075 )
				return Color.DeepSkyBlue;

			return Color.White;
		}

		private bool GlobalDisk( double r, Vector3D v )
		{
			return v.Abs() < r;
		}

		private bool GlobalEdge( H3.Cell.Edge e, double r, Vector3D v )
		{
			Geometry g = m_settings.Geometry;
			if( e.Start == e.End )
				return false;

			Vector3D planeDual = Util.PlaneDualPoint( g, Util.EdgeToPlane( g, e ) );
			Vector3D hyperboloidPoint3D = Sterographic.PoincareBallToHyperboloid( v );
			double dist = Util.GeodesicPlaneHSDF( new Vector3D( hyperboloidPoint3D.X, hyperboloidPoint3D.Y, hyperboloidPoint3D.W ), planeDual );
			return Math.Abs( dist ) < r 
				&& H3Models.Ball.HDist( v, e.Start ) + H3Models.Ball.HDist( v, e.End ) - H3Models.Ball.HDist( e.Start, e.End ) < r ;
		}

		private Color? Globals( Vector3D v )
		{
			return null;
			foreach( H3.Cell.Edge e in m_settings.GlobalEdges )
				if( GlobalEdge( e, .03, v ) )
					return Color.White;

			if( GlobalDisk( .06, v ) )
				return Color.Gray;

			Mobius m = new Mobius();
			m.Isometry( Geometry.Hyperbolic, -Math.PI/4, new Complex( 0, 0.48586827175664565 ) );
			if( GlobalDisk( .04, m.Apply( v ) ) )
				return Color.Blue;

			m.Isometry( Geometry.Hyperbolic, -Math.PI / 4 - Math.PI/2, new Complex( 0, 0.48586827175664565 ) );
			if( GlobalDisk( .04, m.Apply( v ) ) )
				return Color.Red;

			return null;
		}

		private Color CalcColor( Settings settings, Vector3D v )
		{
			Color? gc = Globals( v );
			if( gc.HasValue )
				return gc.Value;

			int[] flips = new int[3];
			List<int> allFlips = new List<int>();
			if( !ReflectToFundamental( settings, ref v, ref flips, allFlips ) )
				return Color.FromArgb( 255, 128, 128, 128 );

			switch( settings.ColoringOption )
			{
			case 0:
				return ColorFunc0( settings, v, flips );
			case 1:
				return ColorFunc1( settings, v, flips, allFlips.ToArray() );
			case 2:
				return ColorFuncIntensity( settings, v, flips );
			case 3:
				return ColorFunc1( settings, v, flips, allFlips.ToArray(), hexagonColoring: true );
			case 4:
				return ColorFuncPicture( settings, v, flips, allFlips.ToArray() );
			}

			throw new System.ArgumentException( "Unknown Coloring Option." );
		}

		/// <summary>
		/// Somewhat based on http://commons.wikimedia.org/wiki/User:Tamfang/programs
		/// </summary>
		internal static bool ReflectToFundamental( Settings settings, ref Vector3D v, ref int[] flips, List<int> allFlips )
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

		private static int m_maxIterations = 4000;

		private static bool ReflectAcrossMirror( CircleNE mirror, ref Vector3D v )
		{
			if( mirror.IsPointOn( v ) )
				return true;

			bool outsideFacet = mirror.IsPointInsideNE( v );
			if( outsideFacet )
			{
				v = mirror.ReflectPoint( v );
				return false;
			}

			return true;
		}

		internal static Dictionary<double,int> GetTileCenters( Settings settings )
		{
			HashSet<Vector3D> completed = new HashSet<Vector3D>();
			completed.Add( new Vector3D() );
			ReflectRecursive( settings, new Vector3D[] { new Vector3D() }, completed );
			//return completed.Select( v => DonHatch.e2hNorm( v.Abs() ) ).Distinct( new DoubleEqualityComparer() ).OrderBy( d => d ).ToArray();

			Dictionary<double, int> result = new Dictionary<double, int>( new DoubleEqualityComparer() );
			foreach( Vector3D v in completed )
			{
				double abs = DonHatch.e2hNorm( v.Abs() );
				int count;
				if( !result.TryGetValue( abs, out count ) )
					count = 0;
				count++;
				result[abs] = count;
			}
			return result;
		}

		private static void ReflectRecursive( Settings settings, Vector3D[] centers, HashSet<Vector3D> completed )
		{
			int max = 1500000;
			if( 0 == centers.Length || completed.Count >= max )
				return;

			HashSet<Vector3D> newCenters = new HashSet<Vector3D>();

			foreach( Vector3D center in centers )
			for( int m = 0; m < settings.Mirrors.Length; m++ )
			{
				CircleNE mirror = settings.Mirrors[m];
				Vector3D v = mirror.ReflectPoint( center );

				if( completed.Add( v ) )
				{
					// Haven't seen this point yet, so 
					// we'll need to recurse on it.
					newCenters.Add( v );
				}
			}

			ReflectRecursive( settings, newCenters.ToArray(), completed );
		}

		private static bool ReflectAcrossMirrors( CircleNE[] mirrors, ref Vector3D v, ref List<int> allFlips, ref int[] flips, ref int iterationCount )
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

		private static void Darken( List<Color> c )
		{
			for( int i = 0; i < 2; i++ )
				c.Add( ColorTranslator.FromHtml( "#2a3132" ) );
		}

		private static void Lighten( List<Color> c )
		{
			c.Add( Color.White );
		}

		/// <summary>
		/// TODO: custom edge coloring
		/// </summary>
		private static Color ColorFunc0( Settings settings, Vector3D v, int[] flips )
		{
			int reflections = flips.Sum();

			bool lightenEdges = ColoringData( settings, 0 ) > 0;

			int parity = settings.Dual ? 0 : 1; // This is for better coloring on duals to snubs.
			Vector3D[] pointsToTry = GetTestPoints( settings, reflections + parity, v );

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
							Lighten( colors );
						else
							Darken( colors );

						within = true;
						break;
					}
				}
				if( within )
					break;
			}

			int idx;
			if( settings.IsSnub || settings.IsGoldberg || settings.IsGeodesicDomeAnalogue )
				idx = 0;
			else
				idx = settings.ColorIndexForPoint( v );
			colors.Add( settings.Colors[idx] );

			if( !within && reflections % 2 == 0 && settings.ShowCoxeter )
			{
				// Do the opposite of what we did for edges.
				if( lightenEdges )
					Darken( colors );
				else
					Lighten( colors );
			}

			return ColorUtil.AvgColorSquare( colors );
		}

		private static Color ColorFunc1( Settings settings, Vector3D v, int[] flips, int[] allFlips, bool hexagonColoring = false )
		{
			int reflections = flips.Sum();
			bool useStandardColors = ColoringData( settings, 0 ) == 0 || ( settings.IsGoldberg || settings.IsGeodesicDomeAnalogue );

			List<Color> colors = new List<Color>();
			foreach( var edgeInfo in settings.UniformEdges )
			foreach( Vector3D vTest in GetTestPoints( settings, reflections, v ) )
			{
				if( useStandardColors )
				{
					if( edgeInfo.PointWithinDist( settings, vTest, edgeInfo.WidthFactor ) )
					{
						colors.Add( edgeInfo.Color );
					}
				}
				else
				{
					for( int i = 0; i < edgeInfo.Edges.Length; i++ )
					{
						if( edgeInfo.PointWithinDist( settings, vTest, i, edgeInfo.WidthFactor ) )
							colors.Add( settings.Colors[i] );
					}
				}
			}

			Color whitish = ColorTranslator.FromHtml( "#F1F1F2" );
			Color darkish = ColorTranslator.FromHtml( "#BCBABE" );

			if( hexagonColoring )
			{
				int incrementsUntilRepeat = 40; //30;
				int offset = 4; //18;
				if( settings.ColoringData != null && settings.ColoringData.Length == 3 )
				{
					incrementsUntilRepeat = settings.ColoringData[1];
					offset = settings.ColoringData[2];
				}
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
			//return flips[0] % 2 == 0 ? Color.Black : Color.White;	// Checkerboard
		}

		private static void GetAnimParams( Settings settings, out int layer, out int mod, out double frac )
		{
			// To spend more time on early layers...
			//double a = Math.Pow( settings.Anim, 1.5 );
			double a = settings.Anim;

			// We'll do 15 frames.
			// Layer 1 is shorter.
			a *= 15;
			double layerDouble = ( a + 3 ) / 2;
			layer = (int)Math.Floor( layerDouble );
			if( layer == 1 )
				mod = 0;
			else
				mod = (int)( a + 3 ) % 2;

			frac = Util.Smoothed( layerDouble - layer, 1.0 );
		}

		private static Color ColorFuncIntensity( Settings settings, Vector3D v, int[] flips )
		{
			int reflections = flips.Sum();

			double dist = double.MaxValue;
			foreach( var edgeInfo in settings.UniformEdges )
			foreach( Vector3D vTest in GetTestPoints( settings, reflections, v ) )
			{
				double d = edgeInfo.DistTo( settings, vTest );
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

		private static Vector3D[] GetTestPoints( Tiler.Settings settings, int totalReflections, Vector3D v )
		{
			List<Vector3D> pointsToTry = new List<Vector3D>();
			if( totalReflections % 2 == 0 && settings.IsSnub )
			{
				pointsToTry.Add( settings.Mirrors[0].ReflectPoint( v ) );
				pointsToTry.Add( settings.Mirrors[1].ReflectPoint( v ) );
				pointsToTry.Add( settings.Mirrors[2].ReflectPoint( v ) );
			}
			else
				pointsToTry.Add( v );
			return pointsToTry.ToArray();
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

					string path = System.IO.Path.Combine( Persistence.WorkingDir, @"D:\temp\image.png" );
					m_texture = new Bitmap( path );
					//m_texture.RotateFlip( RotateFlipType.RotateNoneFlipY );
				}

				double w = m_texture.Width;
				double h = m_texture.Height;

				// Scale v so that the image will cover the f.d.
				double s = 1.0 / ((settings.Verts[0].Abs() + 1) / 2);
				//s *= 2.3;
				s *= 1.7;
				v *= s;

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
				return result;
				double lOff = 0.0;
				if( reflections % 2 == 0 )
					// result = ColorUtil.AvgColorSquare( new List<Color>( new Color[] { result, ColorTranslator.FromHtml( "#2a3132" ) } ) );
					result = ColorUtil.AdjustL( result, result.GetBrightness() - lOff );
				else
					result = ColorUtil.AdjustL( result, result.GetBrightness() + lOff );

				{
					int incrementsUntilRepeat = 5;
					int count = flips[2] % incrementsUntilRepeat;

					result = ColorUtil.AdjustS( result, 0.5 );
					result = ColorUtil.AdjustH( result, 360.0 * count / incrementsUntilRepeat );
				}

				return result;
			}
		}

		private Bitmap m_texture;
	}
}
