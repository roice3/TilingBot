# Format Field Descriptions

<dl>

<dt><b>P and Q</b></dt>
<dd>
Determines the [P,Q] symmetry group. For a regular {P,Q} tiling, this means we have P-gons, with Q meeting at each vertex.

The choice of this will determine the geometry, depending on the value of (P-2)*(Q-2).
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
<br/><br/>
Sterographic<br/>
Gnomonic<br/>
Azimuthal_Equidistant<br/>
Azimuthal_EqualArea<br/>
Equirectangular<br/>
Mercator<br/>
Orthographic<br/>
Sinusoidal<br/>
PeirceQuincuncial<br/>
</dd>
<br/>

<dt><b>EuclideanModel</b></dt>
<dd>
For euclidean tilings, the following options are possible:  
<br/><br/>
Isometric<br/>
Conformal<br/>
Disk<br/>
UpperHalfPlane<br/>
Spiral<br/>
Loxodromic<br/>
</dd>
<br/>

<dt><b>HyperbolicModel</b></dt>
<dd>
For hyperbolic tilings, the following options are possible:  
<br/><br/>
Poincare<br/>
Klein<br/>
Pseudosphere<br/>
Hyperboloid<br/>
Band<br/>
UpperHalfPlane<br/>
Orthographic<br/>
Square<br/>
InvertedPoincare<br/>
Joukowsky<br/>
Ring<br/>
Azimuthal_Equidistant<br/>
Azimuthal_EqualArea<br/>
Schwarz_Christoffel<br/>
</dd>
<br/>

<dt><b>GeodesicLevels</b></dt>
<dd>
If > 1, can be used to denote the number of recusive divisions for a "geodesic sphere" or "geodesic saddle".

NOTES!
* This setting will only apply if P = 3.
* Not currently supported for Euclidean tilings.
</dd>

</dl>