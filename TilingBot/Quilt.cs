namespace TilingBot
{
	using R3.Drawing;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;

	internal class Quilt
	{
		public void Generate()
		{
			// Load image info.
			string quiltDir = @"C:\GitHub\TilingBot\TilingBot\working\anim\final";
			var images = LoadImages( quiltDir );

			//
			// Standard looking glass quilt (we can make this more flexible in the future)
			// 

			int rows = 8;
			int cols = 8;
			int size = 6400;


			ImageGrid.Settings settings = new ImageGrid.Settings()
			{
				Directory = quiltDir,
				Rows = rows,
				Columns = cols,
				Width = size,
				Height = size,
				hGap = 0,
				vGap = 0, 
				InputImages = images.Select( i => i.Value.Path ).ToArray(),
				FileName = "quilt.png"
			};

			ImageGrid grid = new ImageGrid();
			grid.Generate( settings );
		}

		private Dictionary<string, Mosaic.ImageInfo> LoadImages( string dir )
		{
			Dictionary<string, Mosaic.ImageInfo> images = new Dictionary<string, Mosaic.ImageInfo>();
			int count = 0;
			foreach( string file in Directory.EnumerateFiles( dir, "*.png", SearchOption.TopDirectoryOnly ).Take( 256 ) )	// ZZZ - only taking 66 because quilts want that.
			{
				if( count++ % 4 != 0 )
					continue;

				Mosaic.ImageInfo ii = Mosaic.LoadBitmap( file );
				images[ii.Path] = ii;
			}

			return images;
		}
	}
}
