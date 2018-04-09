namespace TilingBot
{
	using R3.Core;
	using R3.Geometry;
	using R3.Math;
	using System;
	using System.Collections.Generic;
	using System.IO;
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
			m_timestamp = DateTime.Now;
			
			// Make the tiling.
			Tiler.Settings settings = MakeTiling();

			// Archive it.
			string imagePath = settings.FileName;
			string newPath = Path.Combine( WorkingDir, imagePath );
			File.Move( imagePath, newPath );

			// Save settings for posterity.
			string settingsPath = FormatFileName() + ".xml";
			settingsPath = Path.Combine( WorkingDir, settingsPath );
			SaveSettings( settings, settingsPath );

			// Tweet it.
			ReadTwitterKeys();
			TwitterService service = new TwitterService( ConsumerKey, ConsumerKeySecret, AccessToken, AccessTokenSecret );
			String message = FormatTweet( settings );
			SendTweet( service, message, newPath );

			//Console.Read();
		}

		static string FormatFileName()
		{
			return m_timestamp.ToString( "yyyy-M-dd_HH-mm-ss" );
		}

		static void SaveSettings( Tiler.Settings settings, string path )
		{
			XmlWriterSettings writerSettings = new XmlWriterSettings();
			writerSettings.OmitXmlDeclaration = true;
			writerSettings.Indent = true;
			using( var writer = XmlWriter.Create( path, writerSettings ) )
			{
				DataContractSerializer dcs = new DataContractSerializer( settings.GetType() );
				dcs.WriteObject( writer, settings );
			}
		}

		public static string WorkingDir
		{
			get
			{
				string working = "working";
				string current = Directory.GetCurrentDirectory();
				string dir = Path.Combine( current, working );
				if( Directory.Exists( dir ) )
					return dir;

				// Diff directory for development.
				string dev = Directory.GetParent( current ).Parent.FullName;
				return Path.Combine( dev, working );
			}
		}

		private static void ReadTwitterKeys()
		{
			string keyFile = Path.Combine( WorkingDir, "keys.txt" );
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

		private static bool RandBoolBiasTrue( Random rand, double factor )
		{
			return rand.NextDouble() * factor > 0.5;
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
			double sat = RandDouble( rand, .4, .9 );
			double lum = RandDouble( rand, .2, .9 );
			Vector3D rgb = ColorUtil.CHSL2RGB( new Vector3D( hue, sat, lum ) );
			rgb *= 255;
			return Color.FromArgb( 255, (int)rgb.X, (int)rgb.Y, (int)rgb.Z );
		}

		private static void InputsTesting( Tiler.Settings settings )
		{
			settings.P = 7;
			settings.Q = 3;
			settings.Active = new int[] { 0, 1 };
			settings.ShowCoxeter = true;
			settings.Mobius = Mobius.Identity();
			settings.Colors = new Color[] {
				Color.FromArgb( unchecked((int)4288984284) ),
				Color.FromArgb( unchecked((int)4283970203) ),
				Color.FromArgb( unchecked((int)4279345479) ) };
		}

		private static void RandomizeInputs( Tiler.Settings settings )
		{
			Random rand = new Random();

			int p = RandPQ( rand );
			int q = RandPQ( rand );

			settings.P = p;
			settings.Q = q;

			List<int> active = new List<int>();
			if( RandBool( rand ) ) active.Add( 0 );
			if( RandBool( rand ) ) active.Add( 1 );
			if( RandBool( rand ) ) active.Add( 2 );
			if( active.Count == 0 )
				active.Add( 0 );
			settings.Active = active.ToArray();

			// Pick a random point to move to origin.
			Mobius m = new Mobius();
			Complex c = new Complex( rand.NextDouble() / 2, rand.NextDouble() / 2 );
			double a = rand.NextDouble() * Math.PI;
			m.Isometry( settings.Geometry, a, c );
			settings.Mobius = m;

			List<Color> colors = new List<Color>();
			for( int i=0; i<3; i++ )
				colors.Add( RandColor( rand ) );
			settings.Colors = colors.ToArray();

			settings.ShowCoxeter = RandBoolBiasTrue( rand, 3 );

			// More ideas for variability, roughly prioritized
			// - Show dots for vertices
			// - Edge thickness, or even showing edges or not
			// - Change displayed model
			// - Ideal tilings
			// - B&W
			// - Other decorations (e.g. set of random points inside fundamental domain)
			// - Include non-uniform choices (i.e. pick a random point in fundamental domain)
			// - More than one uniform on a single tiling?
			// - Duals to uniforms
		}

		private static Tiler.Settings MakeTiling()
		{
			// Standard inputs.
			int size = 1200;
			Tiler.Settings settings = new Tiler.Settings()
			{
				Width = size,
				Height = size,
				FileName = FormatFileName() + ".png"
			};

			RandomizeInputs( settings );
			//InputsTesting( settings );	// Uncomment to set some standard values.
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				settings.Bounds = 6;
				break;
			case Geometry.Euclidean:
				settings.Bounds = 2;
				break;
			case Geometry.Hyperbolic:
				settings.Bounds = 1.01;
				break;
			}
			settings.Init();

			Tiler tiler = new Tiler();
			tiler.GenImage( settings );
			return settings;
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

			string activeString = string.Format( "Mirror{0} {1} {2} active.",
				settings.Active.Length > 1 ? "s" : string.Empty,
				string.Join( ",", settings.Active ),
				settings.Active.Length > 1 ? "are" : "is" );

			return string.Format( "{0} #tiling with {{{1},{2}}} #symmetry. {3}",
				tilingType, settings.P, settings.Q , activeString );
		}

		// Thanks to tutorial here: https://www.youtube.com/watch?v=n2FadWBTL9E
		private static void SendTweet( TwitterService service, string message, string imagePath )
		{
			using( FileStream stream = new FileStream( imagePath, FileMode.Open ) )
			{
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
