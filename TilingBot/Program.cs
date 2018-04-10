// Known Issues
// - Coloring doesn't work right for rectifieds (because only one mirror is active, but there can be two tile types).
//
// TODO
// - Better colors. Why do reds never show?
// - Add better helper random methods, e.g. RandBool( 80% chance of true )
//
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
			m_timestamp = DateTime.Now;

			// Gen the settings.
			Tiler.Settings settings = GenSettings();
			String message = FormatTweet( settings );
			Console.WriteLine( message );

			// Make the tiling.
			MakeTiling( settings );

			// Archive it.
			string imagePath = settings.FileName;
			string newPath = Path.Combine( WorkingDir, imagePath );
			File.Move( imagePath, newPath );

			// Save settings for posterity.
			string settingsPath = FormatFileName() + ".xml";
			settingsPath = Path.Combine( WorkingDir, settingsPath );
			SaveSettings( settings, settingsPath );

			// Tweet it, but only if we aren't testing!
			ReadTwitterKeys();
			TwitterService service = new TwitterService( ConsumerKey, ConsumerKeySecret, AccessToken, AccessTokenSecret );
			if( !Test.IsTesting )
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
			m.Isometry( settings.Geometry, a, c ); // Could be cool to make the geometry arbitrary here (i.e. a spherical transformation applied to a euclidean tiling looks sweet).
			settings.Mobius = m;

			// Random model.
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				break;
			case Geometry.Euclidean:
				break;
			case Geometry.Hyperbolic:
				{
					int model = rand.Next( 1, 3 );
					if( model == 2 )
						settings.HyperbolicModel = HyperbolicModel.Klein;
					if( model == 3 )
						settings.HyperbolicModel = HyperbolicModel.UpperHalfPlane;
					break;
				}
			}

			List<Color> colors = new List<Color>();
			for( int i=0; i<3; i++ )
				colors.Add( RandColor( rand ) );
			settings.Colors = colors.ToArray();

			settings.ShowCoxeter = RandBoolBiasTrue( rand, 3 );
		}

		private static Tiler.Settings GenSettings()
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
			Test.InputsTesting( settings );
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

			string modelString = ModelString( settings );
			string activeString = ActiveMirrorsString( settings );
			return string.Format( "{0} #tiling with {{{1},{2}}} #symmetry, shown in the {3} model. {4}",
				tilingType, InfinitySafe( settings.P ), InfinitySafe( settings.Q ), modelString, activeString );
		}

		private static string InfinitySafe( int i )
		{
			return i == -1 ? "∞" : i.ToString();
		}

		private static string ModelString( Tiler.Settings settings )
		{
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				return "conformal";
			case Geometry.Euclidean:
				return "plane";
			case Geometry.Hyperbolic:
				{
					switch( settings.HyperbolicModel )
					{
					case HyperbolicModel.Poincare:
						return "Poincaré ball";
					case HyperbolicModel.Klein:
						return "Klein";
					case HyperbolicModel.UpperHalfPlane:
						return "upper half plane";
					}
					break;
				}
			}

			throw new System.ArgumentException();
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
				temp += "and" + mirrorDesc[1];
			if( mirrorDesc.Count == 3 )
				temp += ", " + mirrorDesc[1] + ", and" + mirrorDesc[2];

			// Make first char uppercase.
			temp = temp.First().ToString().ToUpper() + temp.Substring( 1 );

			string of = "of the fundamental triangle";
			string activeString = string.Format( "{0} mirror{1} active.", temp,
				settings.Active.Length > 1 ? 
					"s " + of + " are" : 
					" "  + of + " is" );

			return activeString;
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
