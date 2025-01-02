using System.ComponentModel.Composition;
using System.Windows;

namespace BillNash.NINA.GpsdLocationPlugin {
    [Export(typeof(ResourceDictionary))]
    partial class Options : ResourceDictionary {

        public Options() {
            InitializeComponent();
        }
    }
}