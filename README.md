## Program for Fitting experimental IV curves by Single-Diode equivalent-Circuit Model

This script reads experimentally measured current-voltage data and fits a single-diode equivalent-circuit model to this data via the implicit diode equation.
<p align="center">
  <img src="https://latex.codecogs.com/png.latex?%5Cdpi%7B120%7D%20%5Cfn_jvn%20%5Ccolor%7BDarkOrange%7D%20I%28V%29%20%3D%20I_%5Ctext%7Bph%7D%20&plus;%20I_0%20%5Ccdot%20%5Cleft%28%5Cexp%5Cleft%28%5Cfrac%7Bq_e%20%5Ccdot%20%5Cleft%28V%20-%20I%28V%29%29%20R_%5Ctext%7Bs%7D%5Cright%29%7D%7Bn%20k_%5Ctext%7BB%7D%20T%7D%5Cright%29%20-%201%5Cright%29%20&plus;%20%5Cfrac%7BV%20-%20I%28V%29%20R_%5Ctext%7Bs%7D%7D%7BR_%5Ctext%7Bshunt%7D%7D" alt="IV equation"/>
</p>

![screenshot of main program](./screenshots/program.png)
![screenshots of option window](./screenshots/options.png)
