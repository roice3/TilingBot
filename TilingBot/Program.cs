namespace TilingBot
{
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

		private static TwitterService service = new TwitterService( ConsumerKey, ConsumerKeySecret, AccessToken, AccessTokenSecret );

		static DateTime m_timestamp;

		static void Main( string[] args )
		{
			m_timestamp = DateTime.Now;
			ReadTwitterKeys();

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
			String message = FormatTweet( settings );
			//SendTweet( message, newPath );

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

		private static int RandPQ( Random rand )
		{
			// Weight smaller values more.
			double d = rand.NextDouble();
			int min = 3;
			return (int)(min + Math.Pow( d, 3 ) * 13);
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

			// More ideas for variability, roughly prioritized
			// - Random colors
			// - Including Coxeter complex or not
			// - Include non-uniform choices (i.e. pick a random point in fundamental domain)
			// - Ideal tilings
			// - Duals to uniforms

			settings.FileName = FormatFileName() + ".png";
		}

		private static Tiler.Settings MakeTiling()
		{
			// Standard inputs.
			int size = 200;
			Tiler.Settings settings = new Tiler.Settings()
			{
				Width = size,
				Height = size,
			};

			RandomizeInputs( settings );
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				settings.Bounds = 8;
				break;
			case Geometry.Euclidean:
				settings.Bounds = 4;
				break;
			case Geometry.Hyperbolic:
				settings.Bounds = 1.1;
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
				tilingType = "#Hyperbolic";
				break;
			case Geometry.Euclidean:
				tilingType = "#Euclidean";
				break;
			case Geometry.Hyperbolic:
				tilingType = "#Spherical";
				break;
			}

			string activeString = string.Format( "Mirror{0} {1} {2} active.",
				settings.Active.Length > 1 ? "s" : string.Empty,
				string.Join( ",", settings.Active ),
				settings.Active.Length > 1 ? "are" : "is" );

			return string.Format( "{0} tiling with {{{1},{2}}} symmetry. {3}",
				tilingType, settings.P, settings.Q , activeString );
		}

		private static void SendTweet( string message, string imagePath )
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
