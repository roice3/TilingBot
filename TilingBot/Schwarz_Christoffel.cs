namespace TilingBot
{
	using System.Numerics;
	using Math = System.Math;

	/// <summary>
	/// Ported this code from Matthew Arcus at https://www.shadertoy.com/view/tsfyRj
	/// </summary>
	internal class Schwarz_Christoffel
	{
		private static double binomial( double a, int n )
		{
			double s = 1.0;
			for( int i = n; i >= 1; i--, a-- )
			{
				s *= a / i;
			}
			return s;
		}

		// The Lanczos approximation, should only be good for z >= 0.5,
		// but we get the right answers anyway.
		private static double gamma( double z )
		{
			double[] p = new double[8]
			{
			  676.5203681218851,
			  -1259.1392167224028,
			  771.32342877765313,
			  -176.61502916214059,
			  12.507343278686905,
			  -0.13857109526572012,
			  9.9843695780195716e-6,
			  1.5056327351493116e-7
			};
			z -= 1.0;
			double x = 0.99999999999980993; // Unnecessary precision
			for( int i = 0; i < 8; i++ )
			{
				double pval = p[i];
				x += pval / ( z + i + 1 );
			}
			double t = z + 8.0 - 0.5;
			return Math.Sqrt( 2.0 * Math.PI ) * Math.Pow( t, z + 0.5 ) * Math.Exp( -t ) * x;
		}

		// The Beta function
		private static double B( double a, double b )
		{
			return ( gamma( a ) * gamma( b ) ) / gamma( a + b );
		}

		// Original Octave/Matlab code for main function:
		// w=z(inZ).*( 1-cn(1)*h+(-cn(2)+(K+1)*cn(1)^2)*h.^2+
		// (-cn(3)+(3*K+2)*(cn(1)*cn(2)-(K+1)/2*cn(1)^3))*h.^3+
		// (-cn(4)+(2*K+1)*(2*cn(1)*cn(3)+cn(2)^2-(4*K+3)*(cn(1)^2*cn(2)-(K+1)/3*cn(1)^4)))*h.^4+
		// (-cn(5)+(5*K+2)*(cn(1)*cn(4)+cn(2)*cn(3)+(5*K+3)*(-.5*cn(1)^2*cn(3)-.5*cn(1)*cn(2)^2+
		//   (5*K+4)*(cn(1)^3*cn(2)/6-(K+1)*cn(1)^5/24))))*h.^5./(1+h/C^K) );

		public static Complex inversesc( Complex z, int K_ )
		{
			double K = K_;

			double[] cn = new double[6];
			for( int n = 1; n <= 5; n++ )
			{
				cn[n] = binomial( (double)n - 1.0 + 2.0 / K, n ) / ( 1 + n * K ); // Series Coefficients
			}
			double C = B( 1.0 / K, 1.0 - 2.0 / K ) / K; // Scale factor
			z *= C; // Scale polygon to have diameter 1
			Complex h = Complex.Pow( z, K );
			double T1 = -cn[1];
			double T2 = -cn[2] + ( K + 1 ) * Math.Pow( cn[1], 2.0 );
			double T3 = -cn[3] + ( 3 * K + 2 ) * ( cn[1] * cn[2] - ( K + 1 ) / 2.0 * Math.Pow( cn[1], 3.0 ) );
			double T4 = -cn[4] + ( 2 * K + 1 ) * ( 2.0 * cn[1] * cn[3] + Math.Pow( cn[2], 2.0 ) - ( 4 * K + 3 ) *
					( Math.Pow( cn[1], 2.0 ) * cn[2] - ( K + 1 ) / 3.0 * Math.Pow( cn[1], 4.0 ) ) );
			double T5 = -cn[5] + ( 5 * K + 2 ) * ( cn[1] * cn[4] + cn[2] * cn[3] + ( 5 * K + 3 ) *
					( -0.5 * Math.Pow( cn[1], 2.0 ) * cn[3] - 0.5 * cn[1] * Math.Pow( cn[2], 2.0 ) + ( 5 * K + 4 ) *
					( Math.Pow( cn[1], 3.0 ) * cn[2] / 6.0 - ( K + 1 ) * Math.Pow( cn[1], 5.0 ) / 24.0 ) ) );
			Complex X = new Complex( 1, 0 ) + h / Math.Pow( C, K );
			Complex w = Complex.Multiply( z, new Complex( 1, 0 ) + T1 * h + T2 * Complex.Pow( h, 2 ) + T3 * Complex.Pow( h, 3 ) + T4 * Complex.Pow( h, 4 ) + Complex.Divide( T5 * Complex.Pow( h, 5 ), X ) );
			return w;
		}
	}
}
