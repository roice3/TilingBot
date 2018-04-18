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

	public class Persistence
	{
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

		public static string QueueDir
		{
			get
			{
				string queue = "queue";
				return Path.Combine( WorkingDir, queue );
			}
		}

		public static string NextInQueue()
		{
			DirectoryInfo di = new DirectoryInfo( QueueDir );
			FileInfo fi = di.GetFiles().OrderBy( f => f.Name ).FirstOrDefault();
			if( fi == null )
				return string.Empty;
			return fi.Name;
		}

		public static void SaveSettings( Tiler.Settings settings, string path )
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

		public static Tiler.Settings LoadSettings( string path )
		{
			XmlReaderSettings readingSettings = new XmlReaderSettings();
			readingSettings.IgnoreWhitespace = true;
			using( var reader = XmlReader.Create( path, readingSettings ) )
			{
				DataContractSerializer dcs = new DataContractSerializer( typeof( Tiler.Settings ) );
				return (Tiler.Settings)dcs.ReadObject( reader, verifyObjectName: false );
			}
		}
	}
}
