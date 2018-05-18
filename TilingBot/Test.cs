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
			if( !IsTesting )
				return;

			settings = Persistence.LoadSettings( Path.Combine( Persistence.WorkingDir, "2018-5-17_22-45-53.xml" ) );
		}
	}
}
