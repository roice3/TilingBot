// Known Issues
// - Iterating to starting point doesn't always converge, which could cause missed tweets.
//
// Things to work on
// - Better colors. Why do reds seem to rarely get picked? More color functions. Same saturation/intensity for all color choices?
// - Animations
// - Leverage the list of reflections to color cosets (e.g. like in MagicTile).
// - Snubs, duals to uniforms, etc.
// - Display settings in console output.
// - Non-triangular domains (see FB thread with Tom).
// - Links to wiki pages (would require ensuring redirect links for wiki pages existed).
// - Refactor: publish R3.Core on Nuget: https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-visual-studio
//
// More ideas for variability, roughly prioritized
// - Cell, edge, or vertex centered.
// - Edge thickness, or even showing edges or not. Similar with vertices. (maybe if verts are smaller than edges, they could be subtracted out?)
// - Change displayed model. UHS for Euclidean.
// - Bounds
// - B&W
// - More than one uniform on a single tiling?
// - Other decorations (e.g. set of random points inside fundamental domain)
// - Include non-uniform choices (i.e. pick a random point in fundamental domain)
//
// Fun ideas
// - Pixellated.


namespace TilingBot
{
	using R3.Core;
	using R3.Geometry;
	using R3.Math;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.Serialization;
	using System.Xml;
	using TweetSharp;
	using Color = System.Drawing.Color;

	class Program
	{
		// Move these to a separate txt file excluded from .gitignore
		private static string ConsumerKey = string.Empty;
		private static string ConsumerKeySecret = string.Empty;
		private static string AccessToken = string.Empty;
		private static string AccessTokenSecret = string.Empty;

		static DateTime m_timestamp;

		static void Main( string[] args )
		{
			try
			{
				int batch = 1;
				for( int i = 0; i < batch; i++ )
					BotWork();
			}
			catch( Exception e )
			{
				Console.WriteLine( "TilingBot malfunction! " + e.Message );
			}
		}

		static void BotWork()
		{
			m_timestamp = DateTime.Now;

			// Generate the random settings.
			Tiler.Settings settings = GenSettings();
			String message = FormatTweet( settings );
			Console.WriteLine( message );
			Console.WriteLine( string.Empty );
			// ZZZ - output the settings.

			// Make the tiling.
			MakeTiling( settings );

			// Archive it.
			string imagePath = settings.FileName;
			string newPath = Path.Combine( Persistence.WorkingDir, imagePath );
			File.Move( imagePath, newPath );

			// Save settings for posterity.
			string settingsPath = FormatFileName() + ".xml";
			settingsPath = Path.Combine( Persistence.WorkingDir, settingsPath );
			Persistence.SaveSettings( settings, settingsPath );

			// Tweet it, but only if we aren't testing!
			ReadTwitterKeys();
			TwitterService service = new TwitterService( ConsumerKey, ConsumerKeySecret, AccessToken, AccessTokenSecret );
			if( !Test.IsTesting )
			{
				SendTweet( service, message, newPath );

				// Move to tweeted directory.
			}
		}

		static string FormatFileName()
		{
			return m_timestamp.ToString( "yyyy-M-dd_HH-mm-ss" );
		}

		private static void ReadTwitterKeys()
		{
			string keyFile = Path.Combine( Persistence.WorkingDir, "keys.txt" );
			string[] keys = File.ReadAllLines( keyFile );
			ConsumerKey = keys[0];
			ConsumerKeySecret = keys[1];
			AccessToken = keys[2];
			AccessTokenSecret = keys[3];
		}

		private static bool RandBool( Random rand )
		{
			return rand.NextDouble() > 0.5;
		}

		private static bool RandBoolWeighted( Random rand, double fractionTrue )
		{
			return rand.NextDouble() <= fractionTrue;
		}

		private static int RandIntWeighted( Random rand )
		{
			throw new System.NotImplementedException();
		}

		private static int RandPQ( Random rand )
		{
			// Weight smaller values more.
			double d = rand.NextDouble();
			int min = 3;
			return (int)(min + Math.Pow( d, 2.5 ) * 17);
		}

		private static double RandDouble( Random rand, double low, double high )
		{
			double r = rand.NextDouble();
			return low + r * (high - low);
		}

		private static Color RandColor( Random rand )
		{
			double hue = rand.NextDouble() * 360;
			double sat = RandDouble( rand, .2, 1.0 );
			double lum = RandDouble( rand, .2, 1.0 );
			Vector3D rgb = ColorUtil.CHSL2RGB( new Vector3D( hue, sat, lum ) );
			rgb *= 255;
			return Color.FromArgb( 255, (int)rgb.X, (int)rgb.Y, (int)rgb.Z );
		}

		internal static Mobius RandomMobius( Geometry g, Random rand )
		{
			// Pick a random point to move to origin.
			// Could be cool to make the geometry arbitrary here (i.e. a spherical transformation applied to a euclidean tiling looks sweet).
			Mobius m = new Mobius();
			Complex c = new Complex( rand.NextDouble() / 2, rand.NextDouble() / 2 );
			double a = g == Geometry.Euclidean ? // I don't really like how the rotations look in the Euclidean case.
				0 : rand.NextDouble() * Math.PI;
			m.Isometry( g, a, c );
			return m;
		}

		private static void RandomizeInputs( Tiler.Settings settings )
		{
			Random rand = new Random();

			int p = RandPQ( rand );
			int q = RandPQ( rand );
			if( q > 18 )
				q = -1;	// Make q infinite 10% of the time.

			// ZZZ - Pick certain geometries some percentage of the time.
			// Otherwise, we tend to overwhelmingly get hyperbolic tilings.

			settings.P = p;
			settings.Q = q;

			List<int> active = new List<int>();
			if( RandBool( rand ) ) active.Add( 0 );
			if( RandBool( rand ) ) active.Add( 1 );
			if( RandBool( rand ) ) active.Add( 2 );
			if( active.Count == 0 )
				active.Add( 0 );
			settings.Active = active.ToArray();

			settings.EdgeWidth = RandDouble( rand, 0, .05 );
			settings.VertexWidth = RandDouble( rand, 0, .1 );

			int centering = rand.Next( 1, 6 );
			switch( centering )
			{
			case 1:
				settings.Centering = Tiler.Centering.General;
				break;
			case 2:
				settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex1;
				break;
			case 3:
				settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
				break;
			case 4:
				settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
				break;
			case 5:
				settings.Centering = Tiler.Centering.Vertex;
				break;
			}

			settings.Mobius = RandomMobius( settings.Geometry, rand );

			// Random model.
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				{
					int model = rand.Next( 1, 3 );
					if( model == 2 )
						settings.SphericalModel = SphericalModel.Gnomonic;
					break;
				}
			case Geometry.Euclidean:
				{
					int model = rand.Next( 1, 4 );
					if( model == 2 )
						settings.EuclideanModel = EuclideanModel.Disk;
					if( model == 3 )
						settings.EuclideanModel = EuclideanModel.UpperHalfPlane;
					break;
				}
			case Geometry.Hyperbolic:
				{
					int model = rand.Next( 1, 6 );
					if( model == 2 )
						settings.HyperbolicModel = HyperbolicModel.Klein;
					if( model == 3 )
						settings.HyperbolicModel = HyperbolicModel.UpperHalfPlane;
					if( model == 4 )
						settings.HyperbolicModel = HyperbolicModel.Band;
					if( model == 5 )
						settings.HyperbolicModel = HyperbolicModel.Orthographic;
					break;
				}
			}

			settings.ShowCoxeter = RandBoolWeighted( rand, .7 );

			List<Color> colors = new List<Color>();
			for( int i=0; i<3; i++ )
				colors.Add( RandColor( rand ) );
			settings.Colors = colors.ToArray();
			settings.ColoringOption = RandBoolWeighted( rand, .8 ) ? 0 : 1;
		}

		private static Tiler.Settings GenSettings()
		{
			Tiler.Settings settings = new Tiler.Settings();
			RandomizeInputs( settings );
			Test.InputsTesting( ref settings );
			if( !Test.IsTesting )
			{
				string nextInQueue = Persistence.NextInQueue();
			}

			// Standard inputs.
			int size = Test.IsTesting ? 400 : 1200;
			settings.Antialias = true;
			settings.Width = size;
			settings.Height = size;
			settings.FileName = FormatFileName() + ".png";

			double diskBounds = 1.01;
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				settings.Bounds = settings.SphericalModel == SphericalModel.Sterographic ? 6 : 2;
				break;
			case Geometry.Euclidean:
				settings.Bounds = settings.EuclideanModel == EuclideanModel.Isometric ? 2 : diskBounds;
				break;
			case Geometry.Hyperbolic:
				settings.Bounds = settings.HyperbolicModel == HyperbolicModel.Orthographic ? 4 : diskBounds;
				if( settings.HyperbolicModel == HyperbolicModel.Band )
				{
					double factor = 1.5;
					settings.Height = (int)(settings.Height / factor);
					settings.Width = (int)(settings.Width * factor);
				}
				break;
			}
			settings.Init();

			return settings;
		}

		private static void MakeTiling( Tiler.Settings settings )
		{
			Tiler tiler = new Tiler();
			tiler.GenImage( settings );
		}

		private static string FormatTweet( Tiler.Settings settings )
		{
			string tilingType = string.Empty;
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				tilingType = "#Spherical";
				break;
			case Geometry.Euclidean:
				tilingType = "#Euclidean";
				break;
			case Geometry.Hyperbolic:
				tilingType = "#Hyperbolic";
				break;
			}

			/*
			From Tom Ruen...
			For right angle domains, you might consider these names for tilings. 
			{p,q} = {p,q}_100
			t{p,q} = {p,q}_110 (truncated)
			2t{p,q} = t{p,q} = {p,q}_011 (dual truncated or bitruncated)
			r{p,q} = {p,q}_010 (rectified)
			rr{p,q} = {p,q}_101 (double-rectified or cantellated)
			tr{p,q} = {p,q}_111 (Omnitruncated)
			s{p,q} = htr{p,qP = h{p,q}_111 (snub)
			*/

			string centeringString = CenteringString( settings );
			string modelString = ModelString( settings );
			string activeString = ActiveMirrorsString( settings );
			return string.Format( "{0} #tiling with [{1},{2}] #symmetry, shown{3} in {4} model. {5}",
				tilingType, InfinitySafe( settings.P ), InfinitySafe( settings.Q ), 
				centeringString, modelString, activeString );
		}

		private static string InfinitySafe( int i )
		{
			return i == -1 ? "∞" : i.ToString();
		}

		private static string CenteringString( Tiler.Settings settings )
		{
			// We may not be able to describe this well in all cases, so typically we just return nothing.

			if( settings.Centering == Tiler.Centering.General )
				return " uncentered";

			string vertexCentered = " vertex-centered";
			string edgeCentered = " edge-centered";
			string tileCentered = " tile-centered";

			if( settings.Centering == Tiler.Centering.Vertex )
				return vertexCentered;

			if( settings.Active.Length == 1 )
			{
				switch( settings.Active[0] )
				{
				case 0:
					{
						switch( settings.Centering )
						{
						case Tiler.Centering.Fundamental_Triangle_Vertex1:
							return vertexCentered;
						case Tiler.Centering.Fundamental_Triangle_Vertex2:
							return edgeCentered;
						case Tiler.Centering.Fundamental_Triangle_Vertex3:
							return tileCentered;
						}
						break;
					}
				case 2:
					{
						switch( settings.Centering )
						{
						case Tiler.Centering.Fundamental_Triangle_Vertex1:
							return tileCentered;
						case Tiler.Centering.Fundamental_Triangle_Vertex2:
							return edgeCentered;
						case Tiler.Centering.Fundamental_Triangle_Vertex3:
							return vertexCentered;
						}
						break;
					}
				}

			}

			return string.Empty;
		}

		private static string ModelString( Tiler.Settings settings )
		{
			string model = string.Empty;
			string prefix = "the ";
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				{
					switch( settings.SphericalModel )
					{
					case SphericalModel.Sterographic:
						model = "conformal (stereographic projection)";
						break;
					case SphericalModel.Gnomonic:
						model = "gnomonic";
						break;
					}
					break;
				}
			case Geometry.Euclidean:
				{
					switch( settings.EuclideanModel )
					{
					case EuclideanModel.Isometric:
						model = "plane";
						break;

					// These next two aren't well known and I should come up with better names.
					case EuclideanModel.Disk:
						prefix = "a ";
						model = "disk";
						break;
					case EuclideanModel.UpperHalfPlane:
						prefix = "an ";
						model = "upper half plane";
						break;
					}
					break;
				}
			case Geometry.Hyperbolic:
				{
					switch( settings.HyperbolicModel )
					{
					case HyperbolicModel.Poincare:
						model = "Poincaré ball";
						break;
					case HyperbolicModel.Klein:
						model = "Klein";
						break;
					case HyperbolicModel.UpperHalfPlane:
						model = "upper half plane";
						break;
					case HyperbolicModel.Band:
						model = "band";
						break;
					case HyperbolicModel.Orthographic:
						model = "orthographic";
						break;
					}
					break;
				}
			}

			return prefix + model;
		}

		private static string ActiveMirrorsString( Tiler.Settings settings )
		{
			List<string> mirrorDesc = new List<string>();
			foreach( int a in settings.Active )
			{
				switch( a )
				{
				case 0:
					mirrorDesc.Add( "first" );
					break;
				case 1:
					mirrorDesc.Add( "second" );
					break;
				case 2:
					mirrorDesc.Add( "third" );
					break;
				}
			}
			string temp = mirrorDesc[0];
			if( mirrorDesc.Count == 2 )
				temp += " and " + mirrorDesc[1];
			if( mirrorDesc.Count == 3 )
				temp = "All";

			// Make first char uppercase.
			temp = temp.First().ToString().ToUpper() + temp.Substring( 1 );

			string uniformDesc = UniformDesc( settings );
			string of = "of the fundamental triangle";
			string activeString = string.Format( "{0} mirror{1} active{2}.", temp,
				settings.Active.Length > 1 ? 
					"s " + of + " are" : 
					" "  + of + " is",
				uniformDesc );

			return activeString;
		}

		private static string UniformDesc( Tiler.Settings settings )
		{
			var m = settings.Active;

			string uniformDesc = string.Empty;

			if( m.Length == 1 )
			{
				if( m[0] == 1 )
				{
					uniformDesc = "rectification";
				}
				if( m[0] == 2 )
				{
					uniformDesc = "dual tiling";
				}
			}
			else if( m.Length == 2 )
			{
				int m1 = m[0], m2 = m[1];
				if( m1 == 0 && m2 == 1 )
				{
					uniformDesc = "truncation";
				}
				else if( m1 == 1 && m2 == 2 )
				{
					uniformDesc = "bitruncation";
				}
				else if( m1 == 0 && m2 == 2 )
				{
					uniformDesc = "cantellation";
				}
			}
			else if( m.Length == 3 )
			{
				uniformDesc = "omnitruncation";
			}

			if( !string.IsNullOrEmpty( uniformDesc ) )
				uniformDesc = " (" + uniformDesc + ")";

			return uniformDesc;
		}

		// Thanks to tutorial here: https://www.youtube.com/watch?v=n2FadWBTL9E
		private static void SendTweet( TwitterService service, string message, string imagePath )
		{
			using( FileStream stream = new FileStream( imagePath, FileMode.Open ) )
			{
				// I wasted a bunch of time trying to get the call with a callback to the response working.
				// There may be a bug in TweetSharp, because a low-level method didn't like that some of the 
				// SendTweetWithMediaOptions member variables were null, even though they are clearly nullable by design.
				// Compound this with the TweetSharp repo not being available (had to look through code on searchcode.com),
				// and I really should switch this out with some other tweeting code in the future.
				service.SendTweetWithMedia( new SendTweetWithMediaOptions()
				{
					Status = message,
					Images = new Dictionary<string, Stream> { { imagePath, stream } }
				} );
			}

			Console.WriteLine( "Tweet sent!" );
		}
	}
}
