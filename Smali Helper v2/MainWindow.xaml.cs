using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;
using System.Xml;

namespace Smali_Helper_v2
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists("HelpFiles"))
            {
                MessageBox.Show("Help files not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            List();
            DisplayFile("HelpFiles/general help/SmaliInfo.xaml");
        }

        private void List()
        {
            DirectoryInfo[] categoryDirs = new DirectoryInfo("HelpFiles").GetDirectories();
            var treeViewItems = new List<TreeViewItem>();

            foreach (DirectoryInfo categoryDir in categoryDirs)
            {
                var categoryItem = new TreeViewItem();
                categoryItem.Header = categoryDir.Name;

                var treeViewSubItems = new List<TreeViewItem>();

                foreach (FileInfo helpFile in categoryDir.GetFiles())
                {
                    var subItem = new TreeViewItem();
                    subItem.Header = helpFile.Name.Replace("#", "/");
                    subItem.Tag = helpFile.FullName;

                    treeViewSubItems.Add(subItem);
                }

                foreach (TreeViewItem treeViewSubItem in treeViewSubItems.OrderBy(node => node.Header))
                    categoryItem.Items.Add(treeViewSubItem);

                treeViewItems.Add(categoryItem);
            }

            foreach (TreeViewItem currentItem in treeViewItems.OrderBy(a => a.Header))
                treeView.Items.Add(currentItem);
        }

        private async void DisplayFile(string file)
        {
            string rawXamlText;
            using (StreamReader streamReader = File.OpenText(file))
            {
                rawXamlText = streamReader.ReadToEnd();
            }

            var flowDocument = XamlReader.Load(new XmlTextReader(new StringReader(rawXamlText))) as FlowDocument;
            DocumentPaginator paginator = ((IDocumentPaginatorSource) flowDocument).DocumentPaginator;
            Package package = Package.Open(new MemoryStream(), FileMode.Create, FileAccess.ReadWrite);
            var packUri = new Uri("pack://temp.xps");

            PackageStore.RemovePackage(packUri);
            PackageStore.AddPackage(packUri, package);

            var xps = new XpsDocument(package, CompressionOption.NotCompressed, packUri.ToString());
            XpsDocument.CreateXpsDocumentWriter(xps).Write(paginator);

            FixedDocument fixedDocument = xps.GetFixedDocumentSequence().References[0].GetDocument(true);

            await Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    documentViewer.Document = fixedDocument;
                    documentViewer.Zoom = 125;
                }));
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = e.NewValue as TreeViewItem;
            if (item?.Tag == null)
                return;

            DisplayFile(item.Tag.ToString());
        }
    }
}