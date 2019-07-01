namespace TilingBot
{
	using System.Collections.Generic;
	using System.Drawing;
	using System.IO;
	using R3.Geometry;

	internal class Mosaic
	{
		public void Generate()
		{
			string mosaicDir = @"D:\GitHub\TilingBot\TilingBot\working\mosaic";
			string[] files = EnumFiles( new string[] { @"D:\Dropbox\TilingBot\tweeted", @"D:\TilingBot\working\queue" } );
			GenImages( mosaicDir, files );
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
			int count = 0;
			foreach( string file in files )
			{
				Tiler.Settings settings = Persistence.LoadSettings( file );
				if( settings.Geometry != Geometry.Hyperbolic )
					continue;

				foreach( HyperbolicModel hModel in this.hModels )
				{
					settings.HyperbolicModel = hModel;
					//Program.StandardInputs( settings );
					//GenImage( mosaicDir, settings, count++ );
					count++;
				}
			}
		}

		private SphericalModel[] sModels
		{
			get
			{
				return new SphericalModel[]
				{
					SphericalModel.Sterographic,
					SphericalModel.Gnomonic,
					SphericalModel.Azimuthal_Equidistant,
					SphericalModel.Equirectangular,
					SphericalModel.Mercator,
					SphericalModel.Orthographic,
					SphericalModel.Sinusoidal,
					SphericalModel.PeirceQuincuncial,
				};
			}
		}

		private EuclideanModel[] eModels
		{
			get
			{
				return new EuclideanModel[]
				{
					EuclideanModel.Isometric,
					EuclideanModel.Conformal,
					EuclideanModel.Disk,
					EuclideanModel.UpperHalfPlane,
					EuclideanModel.Spiral,
					EuclideanModel.Loxodromic,
				};
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

		public class BitmapInfo
		{
			Bitmap m_bitmap;
			int m_intensity;

		}

		public static BitmapInfo LoadBitmap( string path )
		{
			Bitmap bitmap = new Bitmap( path );
			//bitmap.
			throw new System.NotImplementedException();
		}
	}
}
