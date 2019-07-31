namespace TilingBot
{
	using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using R3.Core;
	using R3.Drawing;
	using R3.Geometry;

	internal class Mosaic
	{
		public void Generate()
		{
			string mosaicDir = @"D:\GitHub\TilingBot\TilingBot\working\mosaic";
			string[] files = EnumFiles( new string[] { @"D:\GitHub\TilingBot\TilingBot\working\experiment graveyard\mosaic" } );
			//GenImages( mosaicDir, files );
			//return;

			// Load image info.
			mosaicDir = @"D:\GitHub\TilingBot\TilingBot\working\mosaic";
			var images = LoadImages( mosaicDir );
			//return;

			string sourceImage = @"D:\GitHub\TilingBot\TilingBot\working\tet_50.png";
			Bitmap source = new Bitmap( sourceImage );

			List<string> inputImages = new List<string>();
			for( int x = 0; x < source.Width; x++ )
			for( int y = 0; y < source.Height; y++ )
			{
				Color c = source.GetPixel( x, y );
				ImageInfo closest = FindClosest( c, images.Values );
				closest.Used = true;
				string fn = Path.GetFileName( closest.Path );
				inputImages.Add( fn );
				System.Diagnostics.Trace.WriteLine( fn + "\t" + c.GetBrightness() + "\t" + closest.AvgBrightness + "\t" + c.GetSaturation() + "\t" + closest.AvgSat );
			}

			// Create output image.
			int numTiles = source.Width;
			//numTiles = 10;
			int gridSpace = 30;
			int tileSize = 300;
			int size = numTiles * tileSize + ( numTiles + 1 ) * gridSpace;

			ImageGrid.Settings settings = new ImageGrid.Settings()
			{
				Directory = mosaicDir,
				Rows = numTiles,
				Columns = numTiles,
				Width = size,
				Height = size,
				hGap = gridSpace,
				vGap = gridSpace,
				InputImages = inputImages.ToArray(),
				FileName = "mosaic.png"
			};

			ImageGrid grid = new ImageGrid();
			grid.Generate( settings );
		}

		private ImageInfo FindClosest( Color c, IEnumerable<ImageInfo> images )
		{
			// Use brightness and saturation as a measure of closeness.
			double satScale = 0.4;
			double cBrightness = c.GetBrightness();
			double cSat = c.GetSaturation() * satScale;
			System.Func<double, double, double> distFn = ( brightness, sat ) =>
			  {
				  sat *= satScale;
				  return new Vector3D( cBrightness, cSat ).Dist( new Vector3D( brightness, sat ) );
			  };

			double dist = double.MaxValue;

			ImageInfo closest = null;
			foreach( ImageInfo image in images )
			{
				if( image.Used )
					continue;
				if( !File.Exists( image.Path ) )
					continue;

				double d = distFn( image.AvgBrightness, image.AvgSat );
				if( d < dist )
				{
					dist = d;
					closest = image;
				}
			}
			return closest;
		}

		private string[] EnumFiles( string[] directories )
		{
			List<string> list = new List<string>();
			foreach( string dir in directories )
				list.AddRange( Directory.EnumerateFiles( dir, "*.xml", SearchOption.AllDirectories ) );
			return list.ToArray();
		}

		private void GenImages( string mosaicDir, string[] files )
		{
			List<int[]> cData = new List<int[]>();
			//cData.Add( new int[] { 40, 4 } );
			cData.Add( new int[] { 30, 18 } );
			cData.Add( new int[] { 40, 36 } );
			cData.Add( new int[] { 20, 5 } );

			int count = 4000;//0;
			foreach( string file in files )
			{
				Tiler.Settings settings = Persistence.LoadSettings( file );
				if( settings.Geometry != Geometry.Hyperbolic )
					continue;

				//if( settings.ColoringOption != 3 )
				//	continue;
				//foreach( int[] data in cData )
				{
					//settings.ColoringData = new int[] { settings.ColoringData == null ? 0 : settings.ColoringData[0], data[0], data[1] };
					foreach( HyperbolicModel hModel in this.hModels )
					{
						settings.HyperbolicModel = hModel;
						Program.StandardInputs( settings );
						GenImage( mosaicDir, settings, count++ );
					}
				}
			}
		}

		private HyperbolicModel[] hModels
		{
			get
			{
				return new HyperbolicModel[]
				{
					HyperbolicModel.Poincare,
					HyperbolicModel.Klein,
					HyperbolicModel.Band,
					HyperbolicModel.UpperHalfPlane,
					HyperbolicModel.Orthographic,
					HyperbolicModel.Square,
					HyperbolicModel.InvertedPoincare,
					HyperbolicModel.Joukowsky,
					//Ring,
					HyperbolicModel.Azimuthal_Equidistant,
				};
			}
		}

		private void GenImage( string mosaicDir, Tiler.Settings settings, int count )
		{
			string numString = count.ToString();
			numString = numString.PadLeft( 4, '0' );
			settings.FileName = Path.Combine( mosaicDir, "" + numString + ".png" );
			if( File.Exists( settings.FileName ) )
				return;

			Program.MakeTiling( settings );
		}

		private Dictionary<string, ImageInfo> LoadImages( string dir )
		{
			HashSet<double> dupChecker = new HashSet<double>( new DoubleEqualityComparer( 0.0001 ) );
			Dictionary<string, ImageInfo> images = new Dictionary<string, ImageInfo>();
			string cached = Path.Combine( dir, "list.txt" );
			if( File.Exists( cached ) )
			{
				string[] lines = File.ReadAllLines( cached );
				foreach( string line in lines )
				{
					string[] split = line.Split( '\t' );
					ImageInfo ii = new ImageInfo()
					{
						Path = split[0],
						AvgBrightness = double.Parse( split[1] ),
						AvgSat = double.Parse( split[2] )
					};
					if( dupChecker.Add( ii.AvgBrightness ) )
						images[ii.Path] = ii;
				}
			}
			else
			{ 		
				foreach( string file in Directory.EnumerateFiles( dir, "*.png", SearchOption.TopDirectoryOnly ) )
				{
					ImageInfo ii = LoadBitmap( file );
					images[ii.Path] = ii;
				}
			}

			return images;
		}

		public class ImageInfo
		{
			public Bitmap Bitmap { get; set; }
			public string Path { get; set; }
			public double AvgBrightness { get; set; }
			public double AvgSat { get; set; }

			public bool Used = false;
		}

		public static ImageInfo LoadBitmap( string path )
		{
			double avgBrightness = 0, avgSat = 0;
			Bitmap bitmap = new Bitmap( path );
			for( int x = 0; x < bitmap.Width; x++ )
			for( int y = 0; y < bitmap.Height; y++ )
			{
				Color c = bitmap.GetPixel( x, y );
				avgBrightness += c.GetBrightness();
				avgSat += c.GetSaturation();
			}

			int numPixels = bitmap.Width * bitmap.Height;
			avgBrightness /= numPixels;
			avgSat /= numPixels;
			bitmap.Dispose();

			System.Diagnostics.Trace.WriteLine( path + "\t" + avgBrightness + "\t" + avgSat );
			return new ImageInfo()
			{
				//Bitmap = bitmap,
				Path = path,
				AvgBrightness = avgBrightness,
				AvgSat = avgSat
			};
		}
	}
}
