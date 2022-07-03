# Format Field Descriptions

<dl>

<dt><b>P and Q</b></dt>
<dd>
Determines the [P,Q] symmetry group. For a regular {P,Q} tiling, this means we have P-gons, with Q meeting at each vertex.

The choice of this will determine the geometry, depending on the value of (P-2)*(Q-2).
</dd>

<dt><b>Centering</b></dt>
<dd>
Controls how the tiling is centered.<br />
<ul>
<li>General (controlled by the Mobius property)</li>
<li>Fundamental_Triangle_Vertex1</li>
<li>Fundamental_Triangle_Vertex2</li>
<li>Fundamental_Triangle_Vertex3</li>
<li>Vertex (centers on the generating vertex)</li>
</ul>
</dd>

<dt><b>ColoringOption</b></dt>
<dd>
An integer, which determines some main approaches for coloring.<br />
<ul>
<li>0: Coloring assigned to polygon types.</li>
<li>1: Edges colored according to where vertex lies in fundamental triangle.</li>
<li>2: Color assigned to tiling and intensity of edges fade.</li>
<li>3: Coloring altered by number of reflections.</li>
<li>4: Used for importing pictures as the background. Not user friendly at all.</li>
</ul>
</dd>

<dt><b>Colors</b></dt>
<dd>
An array of colors. The meaning of these depends on the ColoringOption setting.

The format of a color comes from the DataContractSerializer for the C# System.Drawing.Color class. The easiest way to configure will be with [known colors](https://docs.microsoft.com/en-us/dotnet/api/system.drawing.knowncolor?view=net-6.0)
</dd>

<dt><b>SphericalModel</b></dt>
<dd>
For spherical tilings, the following options are possible:  
<ul>
<li>Sterographic</li>
<li>Gnomonic</li>
<li>Azimuthal_Equidistant</li>
<li>Azimuthal_EqualArea</li>
<li>Equirectangular</li>
<li>Mercator</li>
<li>Orthographic</li>
<li>Sinusoidal</li>
<li>PeirceQuincuncial</li>
</ul>

<dt><b>EuclideanModel</b></dt>
<dd>
For euclidean tilings, the following options are possible:  
<ul>
<li>Isometric</li>
<li>Conformal</li>
<li>Disk</li>
<li>UpperHalfPlane</li>
<li>Spiral</li>
<li>Loxodromic</li>
</ul>

<dt><b>HyperbolicModel</b></dt>
<dd>
For hyperbolic tilings, the following options are possible:  
<ul>
<li>Poincare</li>
<li>Klein</li>
<li>Pseudosphere</li>
<li>Hyperboloid</li>
<li>Band</li>
<li>UpperHalfPlane</li>
<li>Orthographic</li>
<li>Square</li>
<li>InvertedPoincare</li>
<li>Joukowsky</li>
<li>Ring</li>
<li>Azimuthal_Equidistant</li>
<li>Azimuthal_EqualArea</li>
<li>Schwarz_Christoffel</li>
</ul>

<dt><b>GeodesicLevels</b></dt>
<dd>
If > 1, can be used to denote the number of recusive divisions for a "geodesic sphere" or "geodesic saddle".

NOTES!
* This setting will only apply if P = 3.
* Not currently supported for Euclidean tilings.
</dd>

</dl>