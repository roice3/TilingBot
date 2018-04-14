namespace TilingBot
{
	using R3.Geometry;
	using R3.Math;
	using System.Drawing;
	using Color = System.Drawing.Color;

	/// <summary>
	/// This can be used to override the randomization in a tiling.
	/// Mainly used during development when adding new features.
	/// </summary>
	internal static class Test
	{
		public static bool IsTesting = false;

		public static void InputsTesting( Tiler.Settings settings )
		{
			//if( !IsTesting )
			//	return;

			//FirstTweet( settings );
			//HyperbolicModels( settings );
			//SphericalModels( settings );
			//NewColorFunc( settings );
			//Euclidean( settings );
			//Infinities( settings );
		}

		private static void FirstTweet( Tiler.Settings settings )
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

		private static void HyperbolicModels( Tiler.Settings settings )
		{
			settings.P = 4;
			settings.Q = 6;
			settings.Active = new int[] { 1 };
			settings.ShowCoxeter = true;
			settings.Mobius = Mobius.Identity();
			settings.HyperbolicModel = HyperbolicModel.UpperHalfPlane;
			settings.Colors = new Color[] {
				Color.FromArgb( unchecked((int)4282022368) ),
				Color.White, Color.White };
		}

		private static void SphericalModels( Tiler.Settings settings )
		{
			settings.P = 3;
			settings.Q = 5;
			settings.Active = new int[] { 0, 2 };
			settings.ShowCoxeter = true;
			settings.Mobius = Program.RandomMobius( settings.Geometry, new System.Random() );
			settings.SphericalModel = SphericalModel.Gnomonic;
			settings.VertexWidth = settings.EdgeWidth = .01;
			settings.ColoringOption = 0;
			settings.Colors = new Color[] { Color.MediumTurquoise, Color.MediumSlateBlue, Color.MediumSeaGreen };
		}

		private static void NewColorFunc( Tiler.Settings settings )
		{
			settings.P = 3;
			settings.Q = 9;
			settings.Active = new int[] { 0, 1, 2 };
			settings.ShowCoxeter = true;
			settings.Mobius = Program.RandomMobius( settings.Geometry, new System.Random() );
			settings.HyperbolicModel = HyperbolicModel.Klein;
			settings.ColoringOption = 1;
		}

		private static void Euclidean( Tiler.Settings settings )
		{
			settings.P = 3;
			settings.Q = 6;
			settings.Active = new int[] { 0, 1 };
			settings.ShowCoxeter = true;
			settings.Mobius = Program.RandomMobius( settings.Geometry, new System.Random() );
			settings.ColoringOption = 0;
		}

		private static void Infinities( Tiler.Settings settings )
		{
			settings.P = 4;
			settings.Q = -1;
			settings.Active = new int[] { 2 };
			settings.ShowCoxeter = false;
			settings.Mobius = Program.RandomMobius( settings.Geometry, new System.Random() );
			settings.HyperbolicModel = HyperbolicModel.Poincare;
			settings.VertexWidth = settings.EdgeWidth * 4;
			settings.ColoringOption = 1;
		}
	}
}
