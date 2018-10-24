namespace TilingBot
{
	using LinqToTwitter;
	using R3.Core;
	using R3.Geometry;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using Geometry = R3.Geometry.Geometry;

	public class Tweet
	{
		// Move these to a separate txt file excluded from .gitignore
		private static string ConsumerKey = string.Empty;
		private static string ConsumerKeySecret = string.Empty;
		private static string AccessToken = string.Empty;
		private static string AccessTokenSecret = string.Empty;

		public static string Format( Tiler.Settings settings )
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

			string centeringString = string.Empty; //CenteringString( settings );
			string modelString = ModelString( settings );
			string additionalInfo = string.Empty;
			string link = string.Empty;
			if( settings.DualCompound )
			{
				additionalInfo = DualCompoundString( settings );
			}
			else if( settings.IsGeodesicDomeAnalogue )
			{
				additionalInfo = GeodesicString( settings );
				link = @"https://en.wikipedia.org/wiki/Geodesic_polyhedron";
			}
			else if( settings.IsGoldberg )
			{
				additionalInfo = GoldbergString( settings );
				link = @"https://en.wikipedia.org/wiki/Goldberg_polyhedron";
			}
			else if( settings.IsCatalanDual )
			{
				additionalInfo = CatalanDualString( settings );
			}
			else
			{
				additionalInfo = Capitalize( ShortDesc( settings ) ) + ".";
			}

			// Consider adding links for projections as well.
			// https://en.wikipedia.org/wiki/Sinusoidal_projection

			if( settings.IsSnub && !settings.Dual )
				link = @"https://en.wikipedia.org/wiki/Snub_(geometry)";
			if( !string.IsNullOrEmpty( link ) )
				link = " " + link;

			return string.Format( "{0} #tiling shown{1} in {2}. {3}{4}",
				tilingType, centeringString, modelString, additionalInfo, link );
		}

		private static string SymmetryDesc( Tiler.Settings settings )
		{
			return string.Format( "[{0},{1}] #symmetry",
				InfinitySafe( settings.P ), InfinitySafe( settings.Q ) );
		}

		private static string ShortDesc( Tiler.Settings settings )
		{
			string p = InfinitySafe( settings.P );
			string q = InfinitySafe( settings.Q );
			if( settings.IsRegularDual )
				Utils.Swap( ref p, ref q );

			string uniformDesc = UniformDesc( settings, false );
			return string.Format( "{0} {{{1},{2}}}", uniformDesc, p, q );
		}

		private static string DualCompoundString( Tiler.Settings settings )
		{
			string symmetry = SymmetryDesc( settings );
			return string.Format( "Dual compound with {0}.", symmetry );
		}

		private static string GeodesicString( Tiler.Settings settings )
		{
			string desc = settings.Geometry == Geometry.Spherical ? "dome" : "saddle";
			string symmetry = SymmetryDesc( settings );
			return "Geodesic " + desc + string.Format( " with {0} and {1}-frequency subdivision.", 
				symmetry, Math.Pow( 2, settings.GeodesicLevels ) );
		}

		private static string GoldbergString( Tiler.Settings settings )
		{
			string desc = settings.Geometry == Geometry.Spherical ? "polyhedron" : "\"polyhedron\"";
			return "Goldberg " + desc + string.Format( " with {0} and {1} steps.", 
				SymmetryDesc( settings ), Math.Pow( 2, settings.GeodesicLevels ) );
		}

		private static string CatalanDualString( Tiler.Settings settings )
		{
			if( !settings.IsCatalanDual )
				return string.Empty;

			return "Catalan tiling dual to " + ShortDesc( settings ) + ".";
		}

		private static string InfinitySafe( int i )
		{
			return i == -1 ? "∞" : i.ToString();
		}

		private static string Capitalize( string input )
		{
			if( string.IsNullOrEmpty( input ) )
				return input;

			return input.First().ToString().ToUpper() + input.Substring( 1 );
		}

		private static string CenteringString( Tiler.Settings settings )
		{
			//
			// We may not be able to describe this well in all cases, so typically we just return nothing.
			//

			if( settings.Geometry == Geometry.Spherical &&
				settings.SphericalModel == SphericalModel.Mercator )
				return string.Empty;

			if( settings.Geometry == Geometry.Euclidean &&
				settings.EuclideanModel == EuclideanModel.UpperHalfPlane )
				return string.Empty;

			if( settings.Geometry == Geometry.Hyperbolic &&
				settings.HyperbolicModel == HyperbolicModel.UpperHalfPlane )
				return string.Empty;

			if( settings.Centering == Tiler.Centering.General )
				return string.Empty;

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
			string postfix = " projection";
			switch( settings.Geometry )
			{
			case Geometry.Spherical:
				{
					switch( settings.SphericalModel )
					{
					case SphericalModel.Sterographic:
						prefix = string.Empty;
						model = "stereographic";
						break;
					case SphericalModel.Gnomonic:
						model = "gnomonic";
						break;
					case SphericalModel.Azimuthal_Equidistant:
						model = "equidistant azimuthal";
						break;
					case SphericalModel.Azimuthal_EqualArea:
						model = "equal area azimuthal";
						break;
					case SphericalModel.Equirectangular:
						model = "equirectangular";
						break;
					case SphericalModel.Mercator:
						model = "Mercator";
						break;
					case SphericalModel.Orthographic:
						model = "orthographic";
						break;
					case SphericalModel.Sinusoidal:
						model = "sinusoidal";
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
						postfix = " model";
						break;
					case EuclideanModel.Conformal:
						prefix = "a ";
						model = "conformal";
						break;
					// These next two aren't well known and I should come up with better names.
					case EuclideanModel.Disk:
						prefix = "a ";
						model = "fisheye";
						postfix = " view";
						break;
					case EuclideanModel.UpperHalfPlane:
						prefix = "an ";
						model = "upper half plane";
						break;
					case EuclideanModel.Spiral:
						prefix = "a ";
						model = "spiral";
						break;
					case EuclideanModel.Loxodromic:
						prefix = "a ";
						model = "loxodromic";
						break;
					}
					break;
				}
			case Geometry.Hyperbolic:
				{
					switch( settings.HyperbolicModel )
					{
					case HyperbolicModel.Poincare:
						model = "Poincaré disk";
						postfix = " model";
						break;
					case HyperbolicModel.Klein:
						model = "Klein";
						postfix = " model";
						break;
					case HyperbolicModel.UpperHalfPlane:
						model = "upper half plane";
						postfix = " model";
						break;
					case HyperbolicModel.Band:
						model = "band";
						postfix = " model";
						break;
					case HyperbolicModel.Orthographic:
						model = "orthographic";
						break;
					case HyperbolicModel.Square:
						model = "conformal square";
						break;
					case HyperbolicModel.InvertedPoincare:
						model = "inverted Poincaré disk";
						break;
					case HyperbolicModel.Joukowsky:
						model = "Joukowsky";
						break;
					}
					break;
				}
			}

			return prefix + model + postfix;
		}

		private static string UniformDesc( Tiler.Settings settings, bool addParenthesis = true )
		{
			var m = settings.Active;

			string uniformDesc = string.Empty;

			if( m.Length == 1 )
			{
				if( m[0] == 0 )
				{
					uniformDesc = "regular";
				}
				else if( m[0] == 1 )
				{
					uniformDesc = "rectified";
				}
				else if( m[0] == 2 )
				{
					// We'll handle duals by switching p and q in the description.
					uniformDesc = "regular";
				}
			}
			else if( m.Length == 2 )
			{
				int m1 = m[0], m2 = m[1];
				if( m1 == 0 && m2 == 1 )
				{
					uniformDesc = "truncated";
				}
				else if( m1 == 1 && m2 == 2 )
				{
					uniformDesc = "bitruncated";
				}
				else if( m1 == 0 && m2 == 2 )
				{
					uniformDesc = "cantellated";
				}
			}
			else if( m.Length == 3 )
			{
				uniformDesc = settings.IsSnub ? "snub" : "omnitruncated";
			}

			if( addParenthesis && !string.IsNullOrEmpty( uniformDesc ) )
				uniformDesc = " (" + uniformDesc + ")";

			return uniformDesc;
		}

		public static void ReadKeys()
		{
			string keyFile = Path.Combine( Persistence.WorkingDir, "keys.txt" );
			string[] keys = File.ReadAllLines( keyFile );
			ConsumerKey = keys[0];
			ConsumerKeySecret = keys[1];
			AccessToken = keys[2];
			AccessTokenSecret = keys[3];
		}

		public static TwitterContext TwitterContext()
		{
			var auth = new SingleUserAuthorizer
			{
				CredentialStore = new SingleUserInMemoryCredentialStore
				{
					ConsumerKey = ConsumerKey,
					ConsumerSecret = ConsumerKeySecret,
					AccessToken = AccessToken,
					AccessTokenSecret = AccessTokenSecret
				}
			};
			var twitterCtx = new TwitterContext( auth );
			return twitterCtx;
		}

		// https://github.com/JoeMayo/LinqToTwitter/wiki/Tweeting-with-Media
		public static async Task Send( string status, string imagePath )
		{
			TwitterContext twitterCtx = TwitterContext();

			Media media = await twitterCtx.UploadMediaAsync( File.ReadAllBytes( imagePath ), "image/png", "tweet_image" );
			Status tweet = await twitterCtx.TweetAsync( status, new ulong[] { media.MediaID } );
			if( tweet != null )
				Console.WriteLine( $"Tweet sent: {tweet.Text}" );
		}

		public static async Task Reply( ulong tweetID, string status, string imagePath )
		{
			TwitterContext twitterCtx = TwitterContext();

			Media media = await twitterCtx.UploadMediaAsync( File.ReadAllBytes( imagePath ), "image/png", "tweet_image" );
			Status tweet = await twitterCtx.ReplyAsync( tweetID, status, new ulong[] { media.MediaID } );
			if( tweet != null )
				Console.WriteLine( $"Reply sent: {tweet.Text}" );
		}
	}
}
