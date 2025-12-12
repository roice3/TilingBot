namespace TilingBot
{
	using R3.Geometry;
	using R3.Math;
	using System.Drawing;
	using System.IO;
	using Color = System.Drawing.Color;

	/// <summary>
	/// This can be used to override the randomization in a tiling.
	/// Mainly used during development when adding new features.
	/// </summary>
	internal static class Test
	{
		public static bool IsTesting = false;

		public static void InputsTesting( ref Tiler.Settings settings )
		{
			// 
			settings.P = 3;
			settings.Q = 8;
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			settings.ShowCoxeter = false;
			settings.Active = new int[] { 0 };
			settings.Dual = false;
			settings.EdgeWidth = 0.0;
			settings.VertexWidth = 0.0;
			settings.ColoringData = new int[] { 1, 10, 6 };
			settings.ColoringOption = 3;
			settings.Colors = new Color[] { Color.Black, Color.Black, Color.Black };
			return;

			settings.P = 7;
			settings.Q = 3;
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			settings.ShowCoxeter = false;
			settings.Active = new int[] { 1 };
			settings.Dual = true;
			settings.EdgeWidth = .01;
			settings.VertexWidth = 0.0;
			settings.ColoringData = new int[] { 1, 10, 6 };
			settings.ColoringOption = 1;
			settings.Colors = new Color[] { Color.Black, Color.Black, Color.Black };
			return;

			settings.P = 3;
			settings.Q = 12;
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex3;
			settings.ShowCoxeter = true;
			settings.Active = new int[] { 0 };
			settings.Dual = false;
			settings.EdgeWidth = .03;
			settings.VertexWidth = 0.15;
			settings.ColoringData = new int[] { 1, 10, 6 };
			settings.ColoringOption = 3;
			settings.Colors = new Color[] { Color.Black, Color.Black, Color.Black };
			return;


			settings.P = 7;
            settings.Q = 3;
            settings.HyperbolicModel = HyperbolicModel.Parabola;
            settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
            settings.ShowCoxeter = true;
            settings.Active = new int[] { 0 };
            settings.Dual = false;
            settings.EdgeWidth = .01;
            settings.VertexWidth = 0.05;
            settings.ColoringData = new int[] { 1, 10, 6 };
            settings.ColoringOption = 3;
			settings.Colors = new Color[] { Color.Black, Color.Black, Color.Black };
			return;

            // Final post
            settings.P = -1;
			settings.Q = -1;
			settings.Active = new int[] { 0 };
			settings.Dual = false;
			settings.HyperbolicModel = HyperbolicModel.Square;
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			settings.ShowCoxeter = true;
			settings.ColoringData = new int[] { 1, 10, 6 };
			settings.ColoringOption = 3;

			System.Random rand = new System.Random();
			Program.RandomizeCentering( settings, rand );
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
			settings.Centering = Tiler.Centering.General;
			//settings.Mobius = Program.RandomMobius( settings, rand );
			settings.Mobius = Animate.FinalAnim( settings, 0 );
			//settings.Mobius = Mobius.Identity();
			return;

			// For Baez
			settings.P = 3;
			settings.Q = -1;
			settings.HyperbolicModel = HyperbolicModel.UpperHalfPlane;
			settings.ColoringOption = 1;
			settings.Centering = Tiler.Centering.Fundamental_Triangle_Vertex2;
			settings.ShowCoxeter = true;
			settings.Active = new int[] { 0 };
			settings.Dual = false;
			settings.EdgeWidth = 0;
			settings.VertexWidth = 0;
			return;

			// Do we have a custom settings file?
			string customFilename = Path.Combine( Persistence.WorkingDir, "custom_tiling.xml" );
			if( File.Exists( customFilename ) )
			{
				settings = Persistence.LoadSettings( customFilename );
				settings.Active = new int[] { 0, 1, 2 };
				settings.Colors = new Color[] { Color.Red, Color.Green, Color.Blue };
				settings.ColoringOption = 1;
				return;
			}

			if( !IsTesting )
				return;

			//settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2018-4-08_23-53-01.xml" ) );
		}
	}
}
