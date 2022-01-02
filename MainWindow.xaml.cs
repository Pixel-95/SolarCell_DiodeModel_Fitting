using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AtomicusChart.Interface.CameraView;
using AtomicusChart.Interface.Data;
using AtomicusChart.Interface.DataReaders;
using AtomicusChart.Interface.PresentationData;
using AtomicusChart.Interface.PresentationData.BaseTypes;
using AtomicusChart.Interface.PresentationData.Primitives;
using AtomicusChart.Interface.UtilityTypes;
using Extreme.Mathematics.SignalProcessing;
using Microsoft.Win32;

namespace SolarCell_DiodeModel_Fitting
{
    public enum UnitVoltage { V, mV }
    public enum UnitCurrent { A, mA, Am2, mAcm2 }

    public partial class MainWindow : Window
    {
        private static MainWindow mainWindowInstance;
        public static string decimalSeparator = ".";
        public static char newColumnSeparator = '\t';
        public static double temperature = 300;
        public UnitVoltage unitVoltage = UnitVoltage.V;
        public UnitCurrent unitCurrent = UnitCurrent.A;

        public static readonly double e = 1.602176634e-19;
        public static readonly double kB = 1.380649e-23;
        public static readonly double eps0 = 8.854187e-12;
        public static readonly double mu0 = 4 * Math.PI * 1e-7;
        public static readonly double c = 299792458;
        public static readonly double h = 6.62607015e-34;

        public static int significatDigits = 8;

        CharacteristicCurve curveExp;

        public MainWindow()
        {
            InitializeComponent();
            SetupGraphsForPlotting();
            mainWindowInstance = this;

            // First time running Savitsky Golay results in error with Constura.Fody
            try { Smoothing.SavitskyGolay(Extreme.Mathematics.Vector.Create(new double[] { 1, 2, 3, 4 }), 9, 3); } catch { }
        }

        // Button interaction ██████████████████████████████████████████████████████████████████████████████████████████████████████████
        private void Click_GetInitGuess(object sender, RoutedEventArgs e)
        {
            if (!textbox_console.Text.Equals(string.Empty))
                ConsoleWriteLine();
            Task.Run(() =>
            {
                if (!ExecuteTaskWithTimeLimit(1000, () => { return ReadDataToCharacteristicCurve(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return GetInitGuess(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return PlotData(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return WriteToGUI(); })) return;
            });
        }
        private void Click_FitFromValues(object sender, RoutedEventArgs e)
        {
            if (!textbox_console.Text.Equals(string.Empty))
                ConsoleWriteLine();
            Task.Run(() =>
            {
                if (!ExecuteTaskWithTimeLimit(1000, () => { return ReadDataToCharacteristicCurve(); })) return;
                if (!ExecuteTaskWithTimeLimit(3000, () => { return FitFromValues(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return PlotData(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return WriteToGUI(); })) return;
            });
        }
        private void Click_Plot(object sender, RoutedEventArgs e)
        {
            if (!textbox_console.Text.Equals(string.Empty))
                ConsoleWriteLine();
            Task.Run(() =>
            {
                ExecuteTaskWithTimeLimit(1000, () => { return ReadDataToCharacteristicCurve(); });
                if (!ExecuteTaskWithTimeLimit(1000, () => { return PlotData(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return WriteToGUI(); })) return;
            });
        }
        private void Click_Fit(object sender, RoutedEventArgs e)
        {
            if (!textbox_console.Text.Equals(string.Empty))
                ConsoleWriteLine();
            checkbox_Iph.IsChecked = true;
            checkbox_I0.IsChecked = true;
            checkbox_n.IsChecked = true;
            checkbox_Rs.IsChecked = true;
            checkbox_Rsh.IsChecked = true;
            Task.Run(() =>
            {
                if (!ExecuteTaskWithTimeLimit(1000, () => { return ReadDataToCharacteristicCurve(); })) return;
                if (!ExecuteTaskWithTimeLimit(3000, () => { return Fit(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return PlotData(); })) return;
                if (!ExecuteTaskWithTimeLimit(1000, () => { return WriteToGUI(); })) return;
            });
        }

        // Menu band interaction ███████████████████████████████████████████████████████████████████████████████████████████████████████
        private void OpenOptions(object sender, RoutedEventArgs e)
        {
            var options = new Options(decimalSeparator, newColumnSeparator, unitVoltage, unitCurrent, temperature);
            if (options.ShowDialog() ?? false)
            {
                decimalSeparator = options.decimalSeparator;
                newColumnSeparator = options.newColumnSeparator;
                temperature = options.temperature;
                unitVoltage = options.unitVoltage;
                unitCurrent = options.unitCurrent;

                if (decimalSeparator.Equals("."))
                {
                    textbox_Iph.Text = textbox_Iph.Text.Replace(",", ".");
                    textbox_I0.Text = textbox_I0.Text.Replace(",", ".");
                    textbox_n.Text = textbox_n.Text.Replace(",", ".");
                    textbox_Rs.Text = textbox_Rs.Text.Replace(",", ".");
                    textbox_Rsh.Text = textbox_Rsh.Text.Replace(",", ".");
                    textbox_Voc.Text = textbox_Voc.Text.Replace(",", ".");
                    textbox_Isc.Text = textbox_Isc.Text.Replace(",", ".");
                    textbox_Vmpp.Text = textbox_Vmpp.Text.Replace(",", ".");
                    textbox_Impp.Text = textbox_Impp.Text.Replace(",", ".");
                    textbox_Pmpp.Text = textbox_Pmpp.Text.Replace(",", ".");
                    textbox_FF.Text = textbox_FF.Text.Replace(",", ".");
                    textbox_Rsquared.Text = textbox_Rsquared.Text.Replace(",", ".");
                }
                else
                {
                    textbox_Iph.Text = textbox_Iph.Text.Replace(".", ",");
                    textbox_I0.Text = textbox_I0.Text.Replace(".", ",");
                    textbox_n.Text = textbox_n.Text.Replace(".", ",");
                    textbox_Rs.Text = textbox_Rs.Text.Replace(".", ",");
                    textbox_Rsh.Text = textbox_Rsh.Text.Replace(".", ",");
                    textbox_Voc.Text = textbox_Voc.Text.Replace(".", ",");
                    textbox_Isc.Text = textbox_Isc.Text.Replace(".", ",");
                    textbox_Vmpp.Text = textbox_Vmpp.Text.Replace(".", ",");
                    textbox_Impp.Text = textbox_Impp.Text.Replace(".", ",");
                    textbox_Pmpp.Text = textbox_Pmpp.Text.Replace(".", ",");
                    textbox_FF.Text = textbox_FF.Text.Replace(".", ",");
                    textbox_Rsquared.Text = textbox_Rsquared.Text.Replace(".", ",");
                }

                SetUnits();
            }
        }
        private void SetUnits()
        {
            switch (unitCurrent)
            {
                case UnitCurrent.A:
                    textbox_unit_Iph.Text = "A";
                    textbox_unit_I0.Text = "A";
                    textbox_unit_Isc.Text = "A";
                    textbox_unit_Impp.Text = "A";
                    chart.AxesSettings.Axes2D.Y.Title = "current  /  A";
                    break;
                case UnitCurrent.mA:
                    textbox_unit_Iph.Text = "mA";
                    textbox_unit_I0.Text = "mA";
                    textbox_unit_Isc.Text = "mA";
                    textbox_unit_Impp.Text = "mA";
                    chart.AxesSettings.Axes2D.Y.Title = "current  /  mA";
                    break;
                case UnitCurrent.Am2:
                    textbox_unit_Iph.Text = "A/m²";
                    textbox_unit_I0.Text = "A/m²";
                    textbox_unit_Isc.Text = "A/m²";
                    textbox_unit_Impp.Text = "A/m²";
                    chart.AxesSettings.Axes2D.Y.Title = "current  /  A/m²";
                    break;
                case UnitCurrent.mAcm2:
                    textbox_unit_Iph.Text = "mA/cm²";
                    textbox_unit_I0.Text = "mA/cm²";
                    textbox_unit_Isc.Text = "mA/cm²";
                    textbox_unit_Impp.Text = "mA/cm²";
                    chart.AxesSettings.Axes2D.Y.Title = "current  /  mA/cm²";
                    break;
            }

            if (unitVoltage == UnitVoltage.V)
            {
                textbox_unit_Voc.Text = "V";
                textbox_unit_Vmpp.Text = "V";
                chart.AxesSettings.Axes2D.X.Title = "voltage  /  V";
            }
            else
            {
                textbox_unit_Voc.Text = "mV";
                textbox_unit_Vmpp.Text = "mV";
                chart.AxesSettings.Axes2D.X.Title = "voltage  /  mV";
            }

            if (unitVoltage == UnitVoltage.V)
            {
                switch (unitCurrent)
                {
                    case UnitCurrent.A:
                        textbox_unit_Rs.Text = "Ω";
                        textbox_unit_Rsh.Text = "Ω";
                        textbox_unit_Pmpp.Text = "W";
                        break;
                    case UnitCurrent.mA:
                        textbox_unit_Rs.Text = "kΩ";
                        textbox_unit_Rsh.Text = "kΩ";
                        textbox_unit_Pmpp.Text = "mW";
                        break;
                    case UnitCurrent.Am2:
                        textbox_unit_Rs.Text = "Ωm²";
                        textbox_unit_Rsh.Text = "Ωm²";
                        textbox_unit_Pmpp.Text = "W/m²";
                        break;
                    case UnitCurrent.mAcm2:
                        textbox_unit_Rs.Text = "kΩcm²";
                        textbox_unit_Rsh.Text = "kΩcm²";
                        textbox_unit_Pmpp.Text = "mW/cm²";
                        break;
                }
            }
            else
            {
                switch (unitCurrent)
                {
                    case UnitCurrent.A:
                        textbox_unit_Rs.Text = "mΩ";
                        textbox_unit_Rsh.Text = "mΩ";
                        textbox_unit_Pmpp.Text = "mW";
                        break;
                    case UnitCurrent.mA:
                        textbox_unit_Rs.Text = "Ω";
                        textbox_unit_Rsh.Text = "Ω";
                        textbox_unit_Pmpp.Text = "µW";
                        break;
                    case UnitCurrent.Am2:
                        textbox_unit_Rs.Text = "mΩm²";
                        textbox_unit_Rsh.Text = "mΩm²";
                        textbox_unit_Pmpp.Text = "mW/m²";
                        break;
                    case UnitCurrent.mAcm2:
                        textbox_unit_Rs.Text = "Ωcm²";
                        textbox_unit_Rsh.Text = "Ωcm²";
                        textbox_unit_Pmpp.Text = "µW/cm²";
                        break;
                }
            }
        }
        private void Click_DataFromFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Load IV file";
            openFileDialog.Filter = "dat or txt files |*.dat;*.txt|All files (*.*)|*.*";
            //openFileDialog.InitialDirectory = Path.GetFullPath();
            if (openFileDialog.ShowDialog() == true)
            {
                var lines = ReadInputFile(openFileDialog.FileName);

                int amountHeaderLines = 0;
                while (amountHeaderLines <= lines.Length)
                    try
                    { ToDoubleWithArbitrarySeparator(lines[amountHeaderLines].Split(newColumnSeparator).First().Trim()); break; }
                    catch
                    { amountHeaderLines++; }

                lines = lines.Skip(amountHeaderLines).ToArray();

                textbox_data.Text = lines.FirstOrDefault();
                for (int i = 1; i < lines.Length; i++)
                    textbox_data.Text += "\n" + lines[i];
            }
        }
        private void Click_DemoData(object sender, RoutedEventArgs e)
        {
            List<(double voltage, double current)> data = new List<(double voltage, double current)>()
            {
                (0, -20.08155112),
                (0.02, -19.73654886),
                (0.04, -19.65226528),
                (0.06, -20.14102151),
                (0.08, -19.58979377),
                (0.1, -19.67772819),
                (0.12, -19.71407653),
                (0.14, -20.13684966),
                (0.16, -19.82242645),
                (0.18, -19.67418816),
                (0.2, -19.88336856),
                (0.22, -19.83996447),
                (0.24, -19.28758228),
                (0.26, -20.03111477),
                (0.28, -19.70630289),
                (0.3, -19.21808441),
                (0.32, -19.52599423),
                (0.34, -19.29277213),
                (0.36, -19.49426382),
                (0.38, -19.32364647),
                (0.4, -19.11362417),
                (0.42, -19.38136068),
                (0.44, -19.8899443),
                (0.46, -19.69992217),
                (0.48, -19.80520419),
                (0.5, -19.14004235),
                (0.52, -19.27141421),
                (0.54, -19.85508284),
                (0.56, -19.5590254),
                (0.58, -19.72961102),
                (0.6, -19.07565787),
                (0.62, -18.97435721),
                (0.64, -19.04745545),
                (0.66, -18.90112745),
                (0.68, -18.39707849),
                (0.7, -18.08679109),
                (0.72, -17.98740535),
                (0.74, -16.17250582),
                (0.76, -16.62531177),
                (0.78, -13.99280052),
                (0.8, -12.31607057),
                (0.82, -11.49172509),
                (0.84, -6.663128675),
                (0.86, -4.816087681),
                (0.88, -1.076303278),
                (0.9, 2.128781281),
                (0.92, 5.180580596),
                (0.94, 10.04428947),
                (0.96, 12.26549289),
                (0.98, 16.84039106),
                (1, 26.04892466),
            };

            textbox_data.Text = ToStringWithSeparator(data[0].voltage) + newColumnSeparator + ToStringWithSeparator(data[0].current);
            for (int i = 1; i < data.Count; i++)
                textbox_data.Text += "\n" + ToStringWithSeparator(data[i].voltage) + newColumnSeparator + ToStringWithSeparator(data[i].current);
        }
        private void OpenImprint(object sender, RoutedEventArgs e)
        {
            string message = "Version 2.0.211230\nProgram by Mario Zinßer (2021)\nQuestions and remarks to mariozinsser@freenet.de";
            string title = "Imprint";
            MessageBox.Show(message, title);
        }

        // Calculation █████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        private bool ReadDataToCharacteristicCurve()
        {
            // read file to list
            List<string> allLines = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (textbox_data.Text.Equals(string.Empty))
                {
                    ConsoleWriteLine("Insert experimental data to the textbox.");
                    return;
                }
                string text = textbox_data.Text;
                int amountComma = text.Count(c => c == ',');
                int amountPoint = text.Count(c => c == '.');
                if ((decimalSeparator.Equals(".") && amountComma > amountPoint) || (decimalSeparator.Equals(",") && amountPoint > amountComma))
                    ConsoleWriteLine("Make sure to have the correct decimal separator. It can be selected via Preferences > Options.");
                allLines = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            });
            if (allLines == null)
                return false;

            for (int counter = 0; counter < allLines.Count; counter++)
            {
                // remove multi-line comments
                if (allLines[counter].StartsWith("/*"))
                {
                    int start = counter;
                    int stop;
                    for (int length = 0; true; length++)
                        if (allLines[counter + length].Contains("*/"))
                        {
                            stop = counter + length;
                            break;
                        }
                    allLines.RemoveRange(start, stop - start + 1);

                    counter -= counter - (stop - start + 1);
                }
            }
            for (int counter = 0; counter < allLines.Count; counter++)
            {
                // remove single-line comments
                allLines[counter] = allLines[counter].Replace("//", "╰").Split('╰')[0];

                // remove all empty lines (completely empty or only tab and space)
                if (allLines[counter].Replace("\t", "").Replace(" ", "") == "")
                {
                    allLines.RemoveAt(counter);
                    counter--;
                }
            }

            var data = ReadLinesTo2DArray(allLines.ToArray(), newColumnSeparator);

            if (data.GetLength(0) < 5)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConsoleWriteLine("Please provide more than 5 data points.");
                });
                return false;
            }

            List<(double voltage, double current, double power, double area, double efficiency)> experimentalData = new List<(double voltage, double current, double power, double area, double efficiency)>();
            for (int i = 0; i < data.GetLength(0); i++)
                experimentalData.Add((data[i, 0], data[i, 1], 0, 0, 0));

            curveExp = new CharacteristicCurve(temperature, experimentalData);

            Application.Current.Dispatcher.Invoke(() =>
            {
                ConsoleWriteLine(data.GetLength(0) + " data points read.");
            });

            return true;
        }
        private bool GetInitGuess()
        {
            if (curveExp == null)
                return false;

            var diodeParameters = curveExp.GetInitGuess();
            Application.Current.Dispatcher.Invoke(() =>
            {
                textbox_Iph.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[0], significatDigits));
                textbox_I0.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[1], significatDigits));
                textbox_n.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[2], significatDigits));
                textbox_Rs.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[3], significatDigits));
                textbox_Rsh.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[4], significatDigits));
            
                ConsoleWriteLine("Initial guess calculated.");
            });

            return true;
        }
        private bool FitFromValues()
        {
            if (curveExp == null)
            {
                ConsoleWriteLine("Insert experimental data to the textbox.");
                return false;
            }

            double[] initGuess = new double[5];
            bool[] fitThisParameter = new bool[5];

            Application.Current.Dispatcher.Invoke(() =>
            {
                initGuess[0] = ToDoubleWithArbitrarySeparator(textbox_Iph.Text);
                initGuess[1] = ToDoubleWithArbitrarySeparator(textbox_I0.Text);
                initGuess[2] = ToDoubleWithArbitrarySeparator(textbox_n.Text);
                initGuess[3] = ToDoubleWithArbitrarySeparator(textbox_Rs.Text);
                initGuess[4] = ToDoubleWithArbitrarySeparator(textbox_Rsh.Text);

                fitThisParameter[0] = checkbox_Iph.IsChecked ?? false;
                fitThisParameter[1] = checkbox_I0.IsChecked ?? false;
                fitThisParameter[2] = checkbox_n.IsChecked ?? false;
                fitThisParameter[3] = checkbox_Rs.IsChecked ?? false;
                fitThisParameter[4] = checkbox_Rsh.IsChecked ?? false;
            });

            var diodeParameters = curveExp.FitWithGivenParameters(initGuess, fitThisParameter);

            Application.Current.Dispatcher.Invoke(() =>
            {
                textbox_Iph.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[0], significatDigits));
                textbox_I0.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[1], significatDigits));
                textbox_n.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[2], significatDigits));
                textbox_Rs.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[3], significatDigits));
                textbox_Rsh.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[4], significatDigits));

                ConsoleWriteLine("Fit converged.");
            });

            return true;
        }
        private bool Fit()
        {
            if (curveExp == null)
            {
                ConsoleWriteLine("Insert experimental data to the textbox.");
                return false;
            }

            var diodeParameters = curveExp.ExecuteFit();

            Application.Current.Dispatcher.Invoke(() =>
            {
                textbox_Iph.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[0], significatDigits));
                textbox_I0.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[1], significatDigits));
                textbox_n.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[2], significatDigits));
                textbox_Rs.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[3], significatDigits));
                textbox_Rsh.Text = ToStringWithSeparator(RoundToSignificantDigits(diodeParameters[4], significatDigits));

                ConsoleWriteLine("Initial guess calculated and fit converged.");
            });

            return true;
        }
        private bool PlotData()
        {
            var plotData = new List<RenderData>();
            float minCurrent = 0;
            float maxCurrent = 0;
            (double voltage, double current, double power, double fillfactor) MPP = (0, 0, 0, 0);

            if (curveExp == null)
            {
                // fit
                (double Iph, double I0, double n, double Rs, double Rsh) diodeParameters = (double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    diodeParameters = ReadTextBoxes();
                });
                if (!double.IsNaN(diodeParameters.Iph) && !double.IsNaN(diodeParameters.I0) && !double.IsNaN(diodeParameters.n))
                {
                    CharacteristicCurve curveFit = new CharacteristicCurve(temperature, diodeParameters.Iph, diodeParameters.I0, diodeParameters.n, diodeParameters.Rs, diodeParameters.Rsh);
                    double start = 0;
                    double step = curveFit.GetDataSetOpenCircuit().voltage / 1000;
                    double Isc = -curveFit.GetDataSetShortCircuit().current;
                    List<Vector3F> fit = new List<Vector3F>();
                    for (int i = 0; i < 10000; i++)
                    {
                        double voltage = start + i * step;
                        double current = curveFit.GetCurrentAtVoltage(voltage);
                        fit.Add(new Vector3F((float)voltage, (float)current, 0));

                        if (current > 2 * Isc)
                            break;
                    }
                    minCurrent = Math.Min(minCurrent, fit.Min(e => e.Y));
                    maxCurrent = Math.Max(maxCurrent, fit.Max(e => e.Y));
                    MPP = curveFit.GetDataSetMaximumPowerPoint();
                    plotData.Add(PlotPoints("fit", true, fit.ToArray(), 2, new Color4(150, 0, 0), MarkerStyle.None));
                }
            }
            else
            {
                // experimental data
                if (curveExp.experimentalData.Count > 0)
                    plotData.Add(PlotPoints("data points", true, curveExp.experimentalData.Select(e => new Vector3F((float)e.voltage, (float)e.current, 0)).ToArray(), 0, null, MarkerStyle.Circle, 5, new Color4(0, 0, 0)));
                minCurrent = Math.Min(minCurrent, (float)curveExp.experimentalData.Min(e => e.current));
                maxCurrent = Math.Max(maxCurrent, (float)curveExp.experimentalData.Max(e => e.current));

                // fit
                (double Iph, double I0, double n, double Rs, double Rsh) diodeParameters = (double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    diodeParameters = ReadTextBoxes();
                });
                if (!double.IsNaN(diodeParameters.Iph) && !double.IsNaN(diodeParameters.I0) && !double.IsNaN(diodeParameters.n))
                {
                    CharacteristicCurve curveFit = new CharacteristicCurve(temperature, diodeParameters.Iph, diodeParameters.I0, diodeParameters.n, diodeParameters.Rs, diodeParameters.Rsh);
                    double min = curveExp.experimentalData.Min(e => e.voltage);
                    double max = curveExp.experimentalData.Max(e => e.voltage);
                    int amount = 1001;
                    Vector3F[] fit = new Vector3F[amount];
                    for (int i = 0; i < amount; i++)
                    {
                        double voltage = min + (max - min) * (double)i / (double)(amount - 1);
                        fit[i] = new Vector3F((float)voltage, (float)curveFit.GetCurrentAtVoltage(voltage), 0);
                    }
                    minCurrent = Math.Min(minCurrent, fit.Min(e => e.Y));
                    maxCurrent = Math.Max(maxCurrent, fit.Max(e => e.Y));
                    MPP = curveFit.GetDataSetMaximumPowerPoint();
                    plotData.Add(PlotPoints("fit", true, fit, 2, new Color4(150, 0, 0), MarkerStyle.None));
                }
            }

            // boundaries
            minCurrent -= (maxCurrent - minCurrent) * 0.05f;
            maxCurrent += (maxCurrent - minCurrent) * 0.05f;
            plotData.Add(PlotPoints("boundaries", true, new Vector3F[] { new Vector3F(0, minCurrent, 0), new Vector3F(0, maxCurrent, 0)}, 0, markerStyle: MarkerStyle.None));

            // MPP
            List<Vector2F> points = new List<Vector2F>();
            points.Add(new Vector2F(0, 0));
            points.Add(new Vector2F((float)MPP.voltage, 0));
            points.Add(new Vector2F((float)MPP.voltage, (float)MPP.current));
            points.Add(new Vector2F(0, (float)MPP.current));
            points.Add(new Vector2F(0, 0));
            Prism prism = new Prism
            {
                Side = points.ToArray(),
                BottomToTopVector = new Vector3F(0, 0, 1),
                Material = new RenderMaterial(0.7f, 0.1f, 0f, 0.3f, 0f),
                Color = new Color4(150, 0, 0, 10),
                Transform = Matrix4F.Translation(0, 0, -2),
            };
            plotData.Add(prism);

            Application.Current.Dispatcher.Invoke(() =>
            {
                chart.DataSource = plotData;
                ConsoleWriteLine("Data plotted.");
            });

            return true;
        }
        private bool WriteToGUI()
        {
            if (curveExp == null)
                return false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var diodeParameters = ReadTextBoxes();
                if (!double.IsNaN(diodeParameters.Iph) && !double.IsNaN(diodeParameters.I0) && !double.IsNaN(diodeParameters.n))
                {
                    textbox_Voc.Text = ToStringWithSeparator(RoundToSignificantDigits(curveExp.GetDataSetOpenCircuit().voltage, significatDigits));
                    textbox_Isc.Text = ToStringWithSeparator(RoundToSignificantDigits(-curveExp.GetDataSetShortCircuit().current, significatDigits));
                    textbox_Vmpp.Text = ToStringWithSeparator(RoundToSignificantDigits(curveExp.GetDataSetMaximumPowerPoint().voltage, significatDigits));
                    textbox_Impp.Text = ToStringWithSeparator(RoundToSignificantDigits(-curveExp.GetDataSetMaximumPowerPoint().current, significatDigits));
                    textbox_Pmpp.Text = ToStringWithSeparator(RoundToSignificantDigits(-curveExp.GetDataSetMaximumPowerPoint().power, significatDigits));
                    textbox_FF.Text = ToStringWithSeparator(RoundToSignificantDigits(curveExp.GetDataSetMaximumPowerPoint().fillfactor, significatDigits));
                    textbox_Rsquared.Text = ToStringWithSeparator(RoundToSignificantDigits(curveExp.coefficientOfDetermination, significatDigits));

                    ConsoleWriteLine("Solar cell parameters calculated.");
                }
            });
            
            return true;
        }
        private (double Iph, double I0, double n, double Rs, double Rsh) ReadTextBoxes()
        {
            try
            {
                double Iph = ToDoubleWithArbitrarySeparator(textbox_Iph.Text);
                double I0 = ToDoubleWithArbitrarySeparator(textbox_I0.Text);
                double n = ToDoubleWithArbitrarySeparator(textbox_n.Text);
                double Rs = ToDoubleWithArbitrarySeparator(textbox_Rs.Text);
                double Rsh = ToDoubleWithArbitrarySeparator(textbox_Rsh.Text);

                return (Iph, I0, n, Rs, Rsh);
            }
            catch
            {
                return (double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            }
        }

        // GUI output ██████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        public static void ConsoleWrite(char outputchar)
        {
            if (mainWindowInstance != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    mainWindowInstance.textbox_console.Text += outputchar;
                    mainWindowInstance.textbox_console.ScrollToEnd();
                });
            }
        }
        public static void ConsoleWrite(string outputstring)
        {
            if (mainWindowInstance != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    mainWindowInstance.textbox_console.Text += outputstring;
                    mainWindowInstance.textbox_console.ScrollToEnd();
                });
            }
        }
        public static void ConsoleWriteLine(string outputstring)
        {
            if (mainWindowInstance != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    mainWindowInstance.textbox_console.Text += outputstring + "\n";
                    mainWindowInstance.textbox_console.ScrollToEnd();
                });
            }
        }
        public static void ConsoleWriteLine()
        {
            if (mainWindowInstance != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    mainWindowInstance.textbox_console.Text += "\n";
                    mainWindowInstance.textbox_console.ScrollToEnd();
                });
            }
        }
        private void SetupGraphsForPlotting()
        {
            chart.IsLegendVisible = false;
            chart.View.Mode2D = true;
            chart.View.Camera2D.Projection = Projection2DTypes.XY;
            chart.AxesSettings.Axes2D.CartesianSettings.IsGridStripeVisible = false;
            chart.AxesSettings.Axes3D.IsVisible = true;
            chart.AxesSettings.Axes2D.X.Title = "voltage  /  V";
            chart.AxesSettings.Axes2D.Y.Title = "current  /  A";
        }

        // Support methods █████████████████████████████████████████████████████████████████████████████████████████████████████████████
        public static Series PlotPoints(string name, bool visible, Vector3F[] dataXYZ, int linethickness = 2, Color4? lineColor = null, MarkerStyle markerStyle = MarkerStyle.Circle, int markerSize = 5, Color4? markerColor = null)
        {
            Color4 lineColorUsed = lineColor ?? new Color4(0, 0, 0);
            Color4 markerColorUsed = markerColor ?? new Color4(0, 0, 0);

            var reader = new DefaultPositionMaskDataReader(dataXYZ);

            Series Points = new Series
            {
                Name = name,
                Color = lineColorUsed,
                Thickness = linethickness,
                PatternStyle = PatternStyle.Solid,
                Reader = reader,
                MarkerStyle = markerStyle,
                MarkerSize = markerSize,
                MarkerColor = markerColorUsed,
                IsVisible = visible,
            };

            return Points;
        }
        public static bool ExecuteTaskWithTimeLimit(int timeLimit_milliseconds, Func<bool> codeBlock)
        {
            Thread thread = null;
            Exception exception = null;

            // do work
            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                thread = Thread.CurrentThread;
                try
                {
                    return codeBlock();
                }
                catch (Exception e)
                {
                    exception = e;
                    return false;
                }
            });

            // wait for the task to complete or the maximum allowed time
            task.Wait(timeLimit_milliseconds);

            // if task is canceled: show message and return false
            if (!task.IsCompleted && thread != null && thread.IsAlive)
            {
                ConsoleWriteLine("Task cancled because it timed out after " + timeLimit_milliseconds + "ms.");
                thread.Abort();
                return false;
            }

            // if error occured: show message and return false
            if (exception != null)
            {
                MessageBox.Show(exception.Message, "Exeption", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // if task has no error and is not canceled: return its return value
            return task.Result;
        }
        public static double LambertW(double x)
        {
            // LambertW is not defined in this section
            if (x < -Math.Exp(-1))
                return double.MaxValue;

            // computes the first branch for real values only

            // amount of iterations (empirically found)
            int amountOfIterations = Math.Max(4, (int)Math.Ceiling(Math.Log10(x) / 3));

            // initial guess is based on 0 < ln(a) < 3
            double w = 3 * Math.Log(x + 1) / 4;

            // Halley's method via eqn (5.9) in Corless et al (1996)
            double exp;
            for (int i = 0; i < amountOfIterations; i++)
            {
                exp = Math.Exp(w);
                w = w - (w * exp - x) / (exp * (w + 1) - (w + 2) * (w * exp - x) / (2 * w + 2));
            }

            return w;
        }
        public static double FindRootViaNewtonMethod(Func<double, double> function, Func<double, double> derivation, double initialGuess, double tolerance = 1e-16)
        {
            double x1 = initialGuess, x2;

            while (true)
            {
                x2 = x1 - function(x1) / derivation(x1);

                if (Math.Abs(x2 - x1) <= tolerance)
                    return x2;

                x1 = x2;
            }
        }
        public static double FindRootViaNewtonMethod(Func<double, double> function, double initialGuess, double tolerance = 1e-10)
        {
            double x1 = initialGuess, x2;

            for (int i = 0; i < 10000; i++)
            {
                double derivation = (function(x1 + tolerance) - function(x1 - tolerance)) / (2 * tolerance);
                x2 = x1 - function(x1) / derivation;

                if (Math.Abs(x2 - x1) <= tolerance)
                    return x2;

                x1 = x2;
            }

            return x1;
        }
        public static int GetLineOfStringInArray(string[] array, string search, int startline = 0, bool directionFromBottom = false)
        {
            //int index = Array.IndexOf(array, search, 0);

            if (!directionFromBottom) // top to bottom
            {
                for (int i = startline; i < array.Length; i++)
                    if (array[i].Contains(search))
                        return i;
                return -1;
            }
            else // bottom to top  
            {
                for (int i = startline; i >= 0; i--)
                    if (array[i].Contains(search))
                        return i;
                return -1;
            }
        }
        public static string ToStringWithSeparator(double number)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = decimalSeparator;
            return number.ToString(nfi);
        }
        public static double ToDoubleWithArbitrarySeparator(string numberString)
        {
            if (decimalSeparator == ",")
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            else
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            double value = double.Parse(numberString, NumberStyles.Any, CultureInfo.CurrentCulture);
            return value;

            if (CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator == ",")
                return Convert.ToDouble(numberString.Replace(".", ","));
            else
                return Convert.ToDouble(numberString.Replace(",", "."));
        }
        public static int ToIntWithArbitrarySeparator(string numberString)
        {
            if (CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator == ",")
                return Convert.ToInt32(numberString.Replace(".", ","));
            else
                return Convert.ToInt32(numberString.Replace(",", "."));
        }
        public static double RoundToSignificantDigits(double nubmerToRound, int significantDigits)
        {
            if (double.IsNaN(nubmerToRound))
                return double.NaN;

            int signum = Math.Sign(nubmerToRound);

            if (signum < 0)
                nubmerToRound *= -1;

            if (nubmerToRound == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(nubmerToRound))) + 1);
            return signum * scale * Math.Round(nubmerToRound / scale, significantDigits);
        }
        public static string[] ReadInputFile(string filepath)
        {
            // Only if file exists
            if (File.Exists(filepath))
            {
                // read file to list
                List<string> allLines = File.ReadAllLines(filepath).ToList();

                for (int counter = 0; counter < allLines.Count; counter++)
                {
                    // remove multi-line comments
                    if (allLines[counter].StartsWith("/*"))
                    {
                        int start = counter;
                        int stop;
                        for (int length = 0; true; length++)
                            if (allLines[counter + length].Contains("*/"))
                            {
                                stop = counter + length;
                                break;
                            }
                        allLines.RemoveRange(start, stop - start + 1);

                        counter -= counter - (stop - start + 1);
                    }
                }
                for (int counter = 0; counter < allLines.Count; counter++)
                {
                    // remove single-line comments
                    allLines[counter] = allLines[counter].Replace("//", "╰").Split('╰')[0];

                    // remove all empty lines (completely empty or only tab and space)
                    if (allLines[counter].Replace("\t", "").Replace(" ", "") == "")
                    {
                        allLines.RemoveAt(counter);
                        counter--;
                    }
                }

                return allLines.ToArray();
            }
            else
            {
                throw new Exception("File >>>" + filepath + "<<< does not exist!");
            }
        }
        public static double[,] ReadLinesTo2DArray(string[] lines, char delimiterColumn = '\t')
        {
            // Get the amount of leading header lines by checking if first elements in rows are parsable
            int amountHeaderLines = 0;
            while (true)
                try
                { ToDoubleWithArbitrarySeparator(lines[amountHeaderLines].Split(delimiterColumn).First().Trim()); break; }
                catch
                { amountHeaderLines++; }

            // cut away the header lines
            lines = lines.Skip(amountHeaderLines).ToArray();

            // Amount of row-delimiters determines the first dimension
            int amountLines = lines.Where(l => !l.Trim().Equals(string.Empty)).Count();
            // Amount of column-delimiters IN THE FIRST LINE determines the second dimension
            int amountRows = lines.First().Split(delimiterColumn).Where(s => !s.Trim().Equals(string.Empty)).Count();

            // Create 2D Array
            double[,] matrix = new double[amountLines, amountRows];

            // Interation variables
            int i = 0, j;

            // Iterate through rows
            foreach (var row in lines)
            {
                // Iterate through columns
                j = 0;
                foreach (var element in row.Split(delimiterColumn))
                {
                    // Try to parse number
                    double number = 0;
                    try { number = ToDoubleWithArbitrarySeparator(element.Trim().Trim()); }
                    catch { /* Element i,j could not be parsed. */ }

                    // Try to set number
                    try { matrix[i, j] = number; }
                    catch { /* Out of range error: element i,j does not exist in lines x rows matrix. */ }

                    // Go to next column
                    j++;
                }

                // Go to next row
                i++;
            }

            // Return Array
            return matrix;
        }
    }
}
