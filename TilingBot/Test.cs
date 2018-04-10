namespace TilingBot
{
	using R3.Geometry;
	using R3.Math;
	using System.Drawing;
	using Color = System.Drawing.Color;

	/// <summary>
	/// This can be used to override the randomization in a tiling.
	/// Mainly used during development.
	/// </summary>
	internal static class Test
	{
		public static bool IsTesting = false;

		public static void InputsTesting( Tiler.Settings settings )
		{
			if( !IsTesting )
				return;

			//FirstTweet( settings );
			InfinityAndHyperbolicModels( settings );
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

		private static void InfinityAndHyperbolicModels( Tiler.Settings settings )
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
	}
}
