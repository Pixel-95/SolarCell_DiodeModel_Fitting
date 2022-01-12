## Program for Fitting experimental IV curves by Single-Diode equivalent-Circuit Model

### Installing
You only need to download the executable file `./bin/Debug/SolarCell_DiodeModel_Fitting.exe` within this repository and run it on your windows computer.
The `.NET Framework 4.8` will be required to run it.

### Main Window
This easy-to-use script reads experimentally measured current-voltage data by copy and paste and fits a single-diode equivalent-circuit model to this data via the implicit diode equation.
<p align="center">
  <img src="https://latex.codecogs.com/png.latex?%5Cdpi%7B120%7D%20%5Cfn_jvn%20%5Ccolor%7BDarkOrange%7D%20I%28V%29%20%3D%20-I_%5Ctext%7Bph%7D%20&plus;%20I_0%20%5Ccdot%20%5Cleft%28%20%5Cexp%5Cleft%28%20%5Cfrac%7Bq_e%20%5Ccdot%20%28V%20-%20I%28V%29%20%5Ccdot%20R_%5Ctext%7Bs%7D%29%7D%7Bn%20%5Ccdot%20k_%5Ctext%7BB%7D%20%5Ccdot%20T%7D%20%5Cright%29%20-1%20%5Cright%29%20&plus;%20%5Cfrac%7BV-%20I%28V%29%20%5Ccdot%20R_%5Ctext%7Bs%7D%7D%7BR_%5Ctext%7Bsh%7D%7D" alt="IV equation"/>
</p>
<!-- https://latex.codecogs.com/eqneditor/editor.php -->
<!-- \color{DarkOrange}
I(V) = -I_\text{ph} + I_0 \cdot \left( \exp\left( \frac{q_e \cdot (V - I(V) \cdot R_\text{s})}{n \cdot k_\text{B} \cdot T} \right) -1 \right) + \frac{V- I(V) \cdot R_\text{s}}{R_\text{sh}}-->

The main window of the program is devided into three column. On the left, experimental data can be read from file or copy-pasted from clipboard.
In the middle, fitting parameters can be automatically fitted or manually manipulated. Moreover, basic solar cell parameters as Voc, jsc, and FF are displayed.
On the right side, the experimental data and the fit are plotted.<br><br>
<img src="./screenshots/program.png" alt="screenshot of main program"/>

### Option Window
There are several optional preferences for the IV curve. First of all, decimal and column spearators can be set. Furthermore, the units of voltage and currents can be choosen. Finally, the correct temperature has to be set.<br><br>
<img src="./screenshots/options.png" alt="screenshot of option window" width="220"/>
