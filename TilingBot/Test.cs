namespace TilingBot
{
	using R3.Geometry;
	using R3.Math;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using Color = System.Drawing.Color;

	/// <summary>
	/// This can be used to override the randomization in a tiling.
	/// Mainly used during development when adding new features.
	/// </summary>
	internal static class Test
	{
		public static bool IsTesting = true;

		public static void InputsTesting( ref Tiler.Settings settings )
		{

			return;

			settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2020-12-20_16-49-56.xml" ) );
			return;

			settings.P = 7;
			settings.Q = -1;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			settings.Active = new int[] { 0 };
			return;

			settings.P = settings.Q = 4;
			settings.EuclideanModel = EuclideanModel.Isometric;
			settings.ColoringOption = 1;
			settings.VertexWidth = settings.EdgeWidth = 0;
			settings.ShowCoxeter = true;
			settings.Active = new int[] { 0 };
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex1;
			settings.ImageRot = System.Math.PI / 4;
			settings.Bounds = 1.0;
			return;

			settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2020-12-20_16-49-56.xml" ) );
			settings.RingRepeats = 1;
			return;

			settings.P = 3;
			settings.Q = 7;
			settings.Active = new int[] { 0 };
			settings.HyperbolicModel = HyperbolicModel.Klein;
			return;

			
			settings.ColoringData = new int[] { 1 };
			settings.Colors = new Color[] { Color.MidnightBlue, Color.MidnightBlue, Color.MidnightBlue };
			return;

			settings.P = 4;
			settings.Q = 4;
			settings.EuclideanModel = EuclideanModel.Isometric;
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			settings.Active = new int[] { 0, 1, 2 };
			settings.Snub = true;
			settings.Dual = true;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			settings.ColoringOption = 0;
			return;

			settings.P = 4;
			settings.Q = 4;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			settings.EuclideanModel = EuclideanModel.Loxodromic;
			settings.Active = new int[] { 0 };
			return;

			//settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2020-5-31_22-24-37.xml" ) );
			//settings.P = 3;
			//settings.Q = -1;
			settings.HyperbolicModel = HyperbolicModel.UpperHalfPlane;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
			return;


			settings.P = settings.Q = -1;
			/*if( settings.Centering == Tiler.Centering.Fundamental_Triangle_Vertex1 ||
				settings.Centering == Tiler.Centering.Fundamental_Triangle_Vertex3 ||
				settings.Centering == Tiler.Centering.Vertex )*/
				settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
			Program.RandomModelAndMobius( settings, new System.Random() );
			settings.HyperbolicModel = HyperbolicModel.Square;
			settings.Active = new int[] { 0 };
			settings.ShowCoxeter = false;
			settings.DualCompound = true;
			return;

			
			settings.HyperbolicModel = HyperbolicModel.Band;
			//settings.ColoringData = 
			return;

			
			settings.Q = 9;
			settings.VertexWidth *= 1.5;
			return;

			settings.P = 3;
			settings.Q = 6;
			settings.EuclideanModel = EuclideanModel.Spiral;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			return;
			//if( !IsTesting )
			//return;

			//settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2020-5-31_22-24-37.xml" ) );
			settings.P = 6;
			settings.Q = 4;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			settings.ShowCoxeter = true;
			settings.ColoringOption = 1;
			settings.VertexWidth = settings.EdgeWidth = 0;
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			return;

			settings.ColoringOption = 3;
			System.Random rand = new System.Random();
			int r1 = (int)Program.RandDouble( rand, 5, 80 );
			int r2 = (int)Program.RandDouble( rand, 0, r1 );
			settings.ColoringData = new int[] { 1, r1, r2 };
		}

		public static void Sandbox()
		{
			for( int i=0; i<90; i++ )
			{
				string numString = i.ToString();
				numString = numString.PadLeft( 3, '0' );
				string fn1 = "frame" + numString + ".png";
				numString = ( 180 - i ).ToString();
				numString = numString.PadLeft( 3, '0' );
				string fn2 = "frame" + numString + ".png";

				string inDir = @"D:\GitHub\TilingBot\TilingBot\working\anim";
				string image = Path.Combine( inDir, fn1 );
				Bitmap original = new Bitmap( image );
				original.RotateFlip( RotateFlipType.RotateNoneFlipY );

				Size newSize = new Size( 512, 512 );
				Bitmap resized = new Bitmap( original, newSize );
				string outDir = @"D:\GitHub\TilingBot\TilingBot\working\anim\swirled_series";
				string path1 = Path.Combine( outDir, fn1 );
				string path2 = Path.Combine( outDir, fn2 );
				resized.Save( path1, ImageFormat.Png );
				resized.Save( path2, ImageFormat.Png );
			}



		}
	}
}
