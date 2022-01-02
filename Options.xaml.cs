using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SolarCell_DiodeModel_Fitting
{
    public partial class Options : Window
    {
        public string decimalSeparator { get; set; }
        public char newColumnSeparator { get; set; }
        public double temperature { get; set; }
        public UnitVoltage unitVoltage { get; set; }
        public UnitCurrent unitCurrent { get; set; }

        public Options(string decimalSeparator, char newColumnSeparator, UnitVoltage unitVoltage, UnitCurrent unitCurrent, double temperature)
        {
            InitializeComponent();

            if (decimalSeparator == ".")
                combobox_decimalSeparator.SelectedIndex = 0;
            else
                combobox_decimalSeparator.SelectedIndex = 1;

            if (newColumnSeparator == '\t')
                combobox_columnSeparator.SelectedIndex = 0;
            else if (newColumnSeparator == ',')
                combobox_columnSeparator.SelectedIndex = 1;
            else
                combobox_columnSeparator.SelectedIndex = 2;

            if (unitVoltage == UnitVoltage.V)
                combobox_voltageUnit.SelectedIndex = 0;
            else
                combobox_voltageUnit.SelectedIndex = 1;

            switch (unitCurrent)
            {
                case UnitCurrent.A:
                    combobox_currentUnit.SelectedIndex = 0;
                    break;
                case UnitCurrent.mA:
                    combobox_currentUnit.SelectedIndex = 1;
                    break;
                case UnitCurrent.Am2:
                    combobox_currentUnit.SelectedIndex = 2;
                    break;
                case UnitCurrent.mAcm2:
                    combobox_currentUnit.SelectedIndex = 3;
                    break;
            }

            textbox_temperature.Text = temperature.ToString();
        }

        private void Click_savePreferences(object sender, RoutedEventArgs e)
        {
            if (combobox_decimalSeparator.SelectedIndex == 0)
                decimalSeparator = ".";
            else
                decimalSeparator = ",";

            if (combobox_columnSeparator.SelectedIndex == 0)
                newColumnSeparator = '\t';
            else if (combobox_columnSeparator.SelectedIndex == 1)
                newColumnSeparator = ',';
            else
                newColumnSeparator = ';';

            if (combobox_voltageUnit.SelectedIndex == 0)
                unitVoltage = UnitVoltage.V;
            else
                unitVoltage = UnitVoltage.mV;

            switch (combobox_currentUnit.SelectedIndex)
            {
                case 0:
                    unitCurrent = UnitCurrent.A;
                    break;
                case 1:
                    unitCurrent = UnitCurrent.mA;
                    break;
                case 2:
                    unitCurrent = UnitCurrent.Am2;
                    break;
                case 3:
                    unitCurrent = UnitCurrent.mAcm2;
                    break;
            }

            try
            {
                temperature = double.Parse(textbox_temperature.Text);
            }
            catch
            {
                MessageBox.Show("Enter a numeric temperature.", "Parse exeption", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
