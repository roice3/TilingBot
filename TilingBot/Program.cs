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

	class Program
	{
		static DateTime m_timestamp;

		static void Main( string[] args )
		{
			try
			{
				bool tweet = false;
				if( args.Length == 1 )
				{
					string arg = args[0];
					if( arg == "-Tweet" )
					{ 
						tweet = true;
					}
					else if( arg == "-TestQueue" )
					{
						TestCurrentQueue();
						return;
					}
					else if( arg == "-Animate" )
					{
						Animate.Gen();
						return;
					}
					else if( arg == "-Request" )
					{
						CheckRequests().Wait();
						return;
					}
				}

				int batch = 1;
				for( int i = 0; i < batch; i++ )
					BotWork( tweet );
			}
			catch( Exception e )
			{
				Console.WriteLine( "TilingBot malfunction! " + e.Message + "\n" + e.StackTrace );
				throw;
			}
		}

		static void BotWork( bool tweet )
		{
			m_timestamp = DateTime.Now;
			Tiler.Settings settings;

			string existingImage = "";
			string newPath = Path.Combine( Persistence.WorkingDir, existingImage + ".png" );
			if( !string.IsNullOrEmpty( existingImage ) && !Test.IsTesting )
			{
				settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, existingImage + ".xml" ) );
				Console.WriteLine( Tweet.Format( settings ) + "\n" );
			}
			else
			{
				// Generate the random settings.
				settings = GenSettings( tweet );
				Console.WriteLine( Tweet.Format( settings ) + "\n" );

				MakeTiling( settings );
				newPath = ArchiveToWorking( settings );
			}

			// Tweet it, but only if we aren't testing!
			if( tweet && !Test.IsTesting )
			{
				String message = Tweet.Format( settings );
				System.Console.WriteLine( "Attempting tweet." );
				Tweet.ReadKeys();
				Tweet.Send( message, newPath ).Wait();

				// Move to tweeted directory.
			}
		}

		public static string ArchiveToWorking( Tiler.Settings settings )
		{
			string imagePath = settings.FileName;
			string newPath = Path.Combine( Persistence.WorkingDir, imagePath );
			File.Move( imagePath, newPath );

			// Save settings for posterity.
			string settingsPath = FormatFileName() + ".xml";
			settingsPath = Path.Combine( Persistence.WorkingDir, settingsPath );
			Persistence.SaveSettings( settings, settingsPath );
			return newPath;
		}

		static void TestCurrentQueue()
		{
			Test.IsTesting = true;
			string localWorkingDir = Persistence.WorkingDir;
			string testDir = Path.Combine( localWorkingDir, "queueTest" );
			DirectoryInfo di = new DirectoryInfo( testDir );
			foreach( FileInfo file in di.GetFiles() )
				file.Delete();

			string workingDir = @"D:\GitHub\TilingBot\TilingBot\working";
			workingDir = @"D:\TilingBot\working";
			Persistence.WorkingDir = workingDir;
			string[] queue = File.ReadAllLines( Persistence.QueueFile );
			//string[] queue = Directory.GetFiles( Path.Combine( workingDir, "queue" ), "*.xml" ).Select( p => Path.GetFileNameWithoutExtension( p ) ).ToArray();

			List<string> tweetStrings = new List<string>();
			foreach( string qi in queue )
			{
				string fullPath = Path.Combine( Persistence.QueueDir, qi + ".xml" );
				Tiler.Settings settings = Persistence.LoadSettings( fullPath );
				StandardInputs( settings );

				string tweetString = Tweet.Format( settings );
				Console.WriteLine( tweetString + "\n" );
				tweetStrings.Add( tweetString );
			}
			File.WriteAllLines( Path.Combine( testDir, "tweetStrings.txt" ), tweetStrings.ToArray() );

			foreach( string qi in queue )
			{
				m_timestamp = DateTime.Now;
				string fullPath = Path.Combine( Persistence.QueueDir, qi + ".xml" );
				Tiler.Settings settings = Persistence.LoadSettings( fullPath );
				StandardInputs( settings );
				settings.FileName = FormatFileName() + ".png";

				string tweetString = Tweet.Format( settings );
				Console.WriteLine( tweetString + "\n" );
				tweetStrings.Add( tweetString );
				MakeTiling( settings );

				string imagePath = settings.FileName;
				string newPath = Path.Combine( testDir, imagePath );
				File.Move( imagePath, newPath );
			}
		}

		static async Task CheckRequests()
		{
			Tweet.ReadKeys();
			TwitterContext twitterCtx = Tweet.TwitterContext();

			// Mentions, but don't include normal replies.
			var tweets = await
			  ( from tweet in twitterCtx.Status
				where tweet.Type == StatusType.Mentions &&
					tweet.ScreenName == "Tiling Bot" &&
					tweet.ExcludeReplies == true &&
					tweet.SinceID == 1024808727394713601
				select tweet ).ToListAsync();

			tweets.ForEach(
				mention => Console.WriteLine(
					"Name: {0}, Tweet[{1}]: {2}\n",
					mention.User.Name, mention.StatusID, mention.Text ) );
		}

		static string FormatFileName()
		{
			return m_timestamp.ToString( "yyyy-M-dd_HH-mm-ss" );
		}

		private static bool RandBool( Random rand )
		{
			return rand.NextDouble() > 0.5;
		}

		private static bool RandBoolWeighted( Random rand, double fractionTrue )
		{
			return rand.NextDouble() <= fractionTrue;
		}

		/// <summary>
		/// 0-indexed
		/// </summary>
		private static int RandIntWeighted( Random rand, int[] weights )
		{
			int weightsSum = weights.Sum();
			int rnd = rand.Next( weightsSum );
			for( int i = 0; i < weights.Length; i++ )
			{
				if( rnd < weights[i] )
					return i;
				rnd -= weights[i];
			}

			throw new System.Exception( "bug" );
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
			return ColorUtil.FromRGB( rgb );
		}

		/// <summary>
		/// This needs to be called after the model is set.
		/// </summary>
		internal static Mobius RandomMobius( Tiler.Settings settings, Random rand )
		{
			Geometry g = settings.Geometry;

			// Pick a random point to move to origin.
			// Could be cool to make the geometry arbitrary here (i.e. a spherical transformation applied to a euclidean tiling looks sweet).
			Mobius m = new Mobius();
			Complex c = new Complex( rand.NextDouble() / 2, rand.NextDouble() / 2 );
			double a = rand.NextDouble() * Math.PI;

			if( g == Geometry.Euclidean )
			{
				if( settings.EuclideanModel == EuclideanModel.Isometric ||
					settings.EuclideanModel == EuclideanModel.Loxodromic )
				{
					// I don't really like how the rotations look in the plane.
					// For Loxodromic, things won't line up if we allow this.
					a = 0;
				}
				else if( settings.EuclideanModel == EuclideanModel.Conformal )
				{
					g = Geometry.Spherical;
					c = new Complex( rand.NextDouble() * 4, rand.NextDouble() * 4 );
				}
			}

			m.Isometry( g, a, c );
			return m;
		}

		private static void RandomizeInputs( Tiler.Settings settings )
		{
			Random rand = new Random();

			int p = RandPQ( rand );
			int q = RandPQ( rand );
			if( q > 18 )
				q = -1; // Make q infinite 10% of the time.

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
			settings.Dual = RandBoolWeighted( rand, .2 );

			settings.EdgeWidth = RandDouble( rand, 0, .05 );
			settings.VertexWidth = RandDouble( rand, 0, .075 );

			int centering = 1 + RandIntWeighted( rand, new int[] { 40, 10, 10, 10, 10 } );
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

			settings.ShowCoxeter = RandBoolWeighted( rand, .7 );
			RandomizeColors( settings, rand );
			RandomModelAndMobius( settings, rand );
		}

		internal static void RandomModelAndMobius( Tiler.Settings settings, Random rand )
		{
			// Random model.
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				{
					int model = 1 + RandIntWeighted( rand, new int[] { 30, 20, 5, 15, 5, 20, 15, 15, 15 } );
					if( model == 2 )
						settings.SphericalModel = SphericalModel.Gnomonic;
					if( model == 3 )
						settings.SphericalModel = SphericalModel.Azimuthal_Equidistant;
					if( model == 4 )
						settings.SphericalModel = SphericalModel.Azimuthal_EqualArea;
					if( model == 5 )
						settings.SphericalModel = SphericalModel.Equirectangular;
					if( model == 6 )
						settings.SphericalModel = SphericalModel.Mercator;
					if( model == 7 )
						settings.SphericalModel = SphericalModel.Orthographic;
					if( model == 8 )
						settings.SphericalModel = SphericalModel.Sinusoidal;
					if( model == 9 )
						settings.SphericalModel = SphericalModel.PierceQuincuncial;
					break;
				}
			case Geometry.Euclidean:
				{
					int model = 1 + RandIntWeighted( rand, new int[] { 30, 10, 10, 10 } );
					if( model == 2 )
						settings.EuclideanModel = EuclideanModel.Conformal;
					if( model == 3 )
						settings.EuclideanModel = EuclideanModel.Disk;
					if( model == 4 )
						settings.EuclideanModel = EuclideanModel.UpperHalfPlane;
					break;
				}
			case Geometry.Hyperbolic:
				{
					int model = 1 + RandIntWeighted( rand, new int[] { 30, 20, 20, 20, 10, 20, 15, 15 } );
					if( model == 2 )
						settings.HyperbolicModel = HyperbolicModel.Klein;
					if( model == 3 )
						settings.HyperbolicModel = HyperbolicModel.UpperHalfPlane;
					if( model == 4 )
						settings.HyperbolicModel = HyperbolicModel.Band;
					if( model == 5 )
						settings.HyperbolicModel = HyperbolicModel.Orthographic;
					if( model == 6 )
						settings.HyperbolicModel = HyperbolicModel.Square;
					if( model == 7 )
						settings.HyperbolicModel = HyperbolicModel.InvertedPoincare;
					if( model == 8 )
						settings.HyperbolicModel = HyperbolicModel.Joukowsky;
					break;
				}
			}

			settings.Mobius = RandomMobius( settings, rand );
		}

		public static void RandomizeColors( Tiler.Settings settings, Random rand )
		{
			List<Color> colors = new List<Color>();
			for( int i = 0; i < 5; i++ )
				colors.Add( RandColor( rand ) );
			settings.Colors = colors.ToArray();
			settings.ColoringOption = RandIntWeighted( rand, new int[] { 10, 20, 30, 10 } );
			if( RandBool( rand ) )
				settings.ColoringData = new int[] { 1 };
		}

		internal static void StandardInputs( Tiler.Settings settings )
		{
			settings.Antialias = Test.IsTesting ? false : true;
			int size = Test.IsTesting ? 900 : 1200;// 4800;
			settings.Width = size;
			settings.Height = size;

			double diskBounds = 1.01;
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				if( settings.SphericalModel == SphericalModel.Sterographic )
					settings.Bounds = 6.5;
				else if( settings.SphericalModel == SphericalModel.Equirectangular ||
						 settings.SphericalModel == SphericalModel.Sinusoidal )
				{
					settings.Bounds = 1;
					if( settings.SphericalModel == SphericalModel.Sinusoidal )
						settings.Bounds = 1.01;
					settings.Height = (int)(settings.Height * 2.0 / 3);
					settings.Width = (int)(settings.Width * 4.0 / 3);
				}
				else if( settings.SphericalModel == SphericalModel.Mercator )
				{
					settings.Bounds = 1;
				}
				else if( settings.SphericalModel == SphericalModel.Azimuthal_Equidistant ||
					settings.SphericalModel == SphericalModel.Azimuthal_EqualArea ||
					settings.SphericalModel == SphericalModel.Orthographic )
				{
					settings.Bounds = diskBounds;
				}
				else if( settings.SphericalModel == SphericalModel.PierceQuincuncial )
					settings.Bounds = 4;
				else
					settings.Bounds = 2;
				break;
			case Geometry.Euclidean:
				if( settings.EuclideanModel == EuclideanModel.Isometric ||
					settings.EuclideanModel == EuclideanModel.Spiral ||
					settings.EuclideanModel == EuclideanModel.Loxodromic )
					settings.Bounds = 2;
				else
					settings.Bounds = diskBounds;
				break;
			case Geometry.Hyperbolic:
				settings.Bounds =  diskBounds;
				if( settings.HyperbolicModel == HyperbolicModel.Band )
				{
					double factor = 1.5;
					settings.Height = (int)(settings.Height / factor);
					settings.Width = (int)(settings.Width * factor);
				}
				else if( settings.HyperbolicModel == HyperbolicModel.Orthographic ||
					settings.HyperbolicModel == HyperbolicModel.InvertedPoincare )
					settings.Bounds = 5;
				else if( settings.HyperbolicModel == HyperbolicModel.Joukowsky )
					settings.Bounds = 1.5;
				break;
			}

			settings.Init();
		}

		internal static Tiler.Settings GenSettings( bool tweeting )
		{
			Tiler.Settings settings = new Tiler.Settings();
			RandomizeInputs( settings );
			Test.InputsTesting( ref settings );
			if( tweeting && !Test.IsTesting )
			{
				string next = Persistence.NextInQueue();
				string queueDir = Persistence.QueueDir;
				string fullPath = Path.Combine( queueDir, next + ".xml" );
				if( File.Exists( fullPath ) )
				{
					settings = Persistence.LoadSettings( fullPath );
					Persistence.Move( next, queueDir, Path.Combine( queueDir, "done" ) );
				}
				Persistence.PopQueue();
			}

			settings.FileName = FormatFileName() + ".png";
			StandardInputs( settings );
			return settings;
		}

		internal static void MakeTiling( Tiler.Settings settings )
		{
			Tiler tiler = new Tiler();
			tiler.GenImage( settings );
		}
	}
}
