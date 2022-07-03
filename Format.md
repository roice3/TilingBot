# Format

<dl>
<dt>P and Q</dt>
<dd>
Determines the [P,Q] symmetry group. For a regular {P,Q} tiling, this means we have P-gons, with Q meeting at each vertex.

The choice of this will determine the geometry, depending on the value of (P-2)*(Q-2).
</dd>

<dt>SphericalModel</dt>
<dd>
For spherical tilings, the following options are possible:  

Sterographic  
Gnomonic  
Azimuthal_Equidistant  
Azimuthal_EqualArea  
Equirectangular  
Mercator  
Orthographic  
Sinusoidal  
PeirceQuincuncial  
</dd>

<dt>EuclideanModel</dt>
<dd>
For euclidean tilings, the following options are possible:  

Isometric  
Conformal  
Disk  
UpperHalfPlane  
Spiral  
Loxodromic  
</dd>

<dt>HyperbolicModel</dt>
<dd>
For hyperbolic tilings, the following options are possible:  

Poincare  
Klein  
Pseudosphere  
Hyperboloid  
Band  
UpperHalfPlane  
Orthographic  
Square  
InvertedPoincare  
Joukowsky  
Ring  
Azimuthal_Equidistant  
Azimuthal_EqualArea  
Schwarz_Christoffel  
</dd>

<dt>GeodesicLevels</dt>
<dd>
If > 1, can be used to denote the number of recusive divisions for a "geodesic sphere" or "geodesic saddle".

NOTES!
* This setting will only apply if P = 3.
* Not currently supported for Euclidean tilings.
</dd>

</dl>