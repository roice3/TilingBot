namespace TilingBot
{
	using R3.Geometry;
	using System.IO;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Xml;

	public class Persistence
	{
		public static string WorkingDir
		{
			get
			{
				if( !string.IsNullOrEmpty( m_working ) )
					return m_working;

				string working = "working";
				string current = Directory.GetCurrentDirectory();
				string dir = Path.Combine( current, working );
				if( Directory.Exists( dir ) )
					return dir;

				// Diff directory for development.
				string dev = Directory.GetParent( current ).Parent.FullName;
				return Path.Combine( dev, working );
			}
			set
			{
				m_working = value;
			}
		}
		private static string m_working = string.Empty;

		public static void Move( string fileSansExtension, string from, string to )
		{
			string[] files = new string[] { fileSansExtension + ".png", fileSansExtension + ".xml" };
			foreach( string file in files )
			{
				File.Move(
					Path.Combine( from, file ),
					Path.Combine( to, file ) );
			}
		}

		public static string AnimDir
		{
			get
			{
				string anim = "anim";
				return Path.Combine( WorkingDir, anim );
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

		public static string QueueFile
		{
			get
			{
				return Path.Combine( WorkingDir, "queue.txt" );
			}
		}

		public static string NextInQueue()
		{
			string[] queue = File.ReadAllLines( QueueFile );
			if( queue.Length > 0 )
				return queue[0];

			return RandomQueueFile();
		}

		public static string RandomQueueFile()
		{
			string[] files = System.IO.Directory.GetFiles( QueueDir, "*.xml", SearchOption.TopDirectoryOnly );
			if( files.Length == 0 )
				return string.Empty;

			System.Random rand = new System.Random();
			int idx = rand.Next( 0, files.Length );
			return Path.GetFileNameWithoutExtension( files[idx] );
		}

		public static void PopQueue()
		{
			string[] queue = File.ReadAllLines( QueueFile );
			File.WriteAllLines( QueueFile, queue.Skip( 1 ) );
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
