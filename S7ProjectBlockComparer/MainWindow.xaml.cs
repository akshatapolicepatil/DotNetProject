﻿using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

using DiffMatchPatch;
using DotNetSiemensPLCToolBoxLibrary.DataTypes;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.AWL.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders.Step7V5;

using ICSharpCode.AvalonEdit.Highlighting;

using WPFToolboxForSiemensPLCs.AvalonEdit;

namespace S7ProjectBlockComparer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BlocksOfflineFolder fld1;
        private BlocksOfflineFolder fld2;

        private S7ConvertingOptions convOpt;
        
        public string CurrentBlock
        {
            get { return (string)GetValue(CurrentBlockProperty); }
            set { SetValue(CurrentBlockProperty, value); }
        }

        public static readonly DependencyProperty CurrentBlockProperty =
            DependencyProperty.Register("CurrentBlock", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        
        public MainWindow()
        {

            convOpt = new S7ConvertingOptions(MnemonicLanguage.German) {GenerateCallsfromUCs = true, };

            this.DataContext = this;

            InitializeComponent();

            string highlighterFile = "";
            highlighterFile = "S7ProjectBlockComparer.AvalonEdit.AWL_Step5_Highlighting.xshd";
            IHighlightingDefinition customHighlighting;
            using (Stream s = typeof(MainWindow).Assembly.GetManifestResourceStream(highlighterFile))
            {
                if (s == null)
                    throw new InvalidOperationException("Could not find embedded resource");
                using (XmlReader reader = new XmlTextReader(s))
                {
                    customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                        HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
            HighlightingManager.Instance.RegisterHighlighting("Custom Highlighting", new string[] { ".cool" }, customHighlighting);
            txtResult.SyntaxHighlighting = customHighlighting;

            //txtResult.TextArea.Options.EnableImeSupport = false;
        }

        private void cmdPrj1_Click(object sender, RoutedEventArgs e)
        {
            fld1 = DotNetSiemensPLCToolBoxLibrary.Projectfiles.SelectProjectPart.SelectBlocksOfflineFolder();
            if (fld1 != null) prj1.Text = fld1.ToString();
        }

        private void cmdPrj2_Click(object sender, RoutedEventArgs e)
        {
            fld2 = DotNetSiemensPLCToolBoxLibrary.Projectfiles.SelectProjectPart.SelectBlocksOfflineFolder();
            if (fld2 != null) prj2.Text = fld2.ToString();
        }


        private void cmdComp_Click(object sender, RoutedEventArgs e)
        {
            lstBlocks.Items.Clear();

            if (fld1 != null && fld2 != null)
            {
                busy.IsBusy = true;

                Task.Factory.StartNew(() =>
                    {
                        foreach (var projectBlockInfo in fld1.BlockInfos)
                        {
                            if (projectBlockInfo.BlockType == DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.FB || projectBlockInfo.BlockType == DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.FC || projectBlockInfo.BlockType == DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.OB)
                            {
                                var blk1 = fld1.GetBlock(projectBlockInfo, convOpt);

                                Dispatcher.Invoke(new Action(() => this.akBlock.Text = blk1.BlockName));

                                var blk2 = fld2.GetBlock(blk1.BlockName, convOpt);

                                if (blk2 != null)
                                {
                                    var b1 = ((S7FunctionBlock)blk1).ToString(false);
                                    var b2 = ((S7FunctionBlock)blk2).ToString(false);

                                    if (b1 != b2)
                                    {
                                        Dispatcher.Invoke(new Action(() => lstBlocks.Items.Add(blk1.BlockName)));
                                    }
                                }
                                else
                                {
                                    Dispatcher.Invoke(new Action(() => lstBlocks.Items.Add("-" + blk1.BlockName)));
                                }
                            }
                        }

                        foreach (var projectBlockInfo in fld2.BlockInfos)
                        {
                            if (projectBlockInfo.BlockType == DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.FB || projectBlockInfo.BlockType == DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.FC || projectBlockInfo.BlockType == DotNetSiemensPLCToolBoxLibrary.DataTypes.PLCBlockType.OB)
                            {
                                if (!fld1.BlockInfos.Any(x => x.Name == projectBlockInfo.Name))
                                {
                                    Dispatcher.Invoke(new Action(() => lstBlocks.Items.Add("+" + projectBlockInfo.Name)));
                                }                                
                            }
                        }

                        Dispatcher.Invoke(new Action(() => busy.IsBusy = false));
                    });
            }
        }

        private void lstBlocks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstBlocks.SelectedItem != null)
            {
                if (lstBlocks.SelectedItem.ToString().StartsWith("+"))
                {
                    var bk=((S7FunctionBlock)fld2.GetBlock(lstBlocks.SelectedItem.ToString().Substring(1), convOpt)).ToString(false);
                    txtResult.Document.Text = bk;
                }
                else if (lstBlocks.SelectedItem.ToString().StartsWith("-"))
                {
                    var bk = ((S7FunctionBlock)fld1.GetBlock(lstBlocks.SelectedItem.ToString().Substring(1), convOpt)).ToString(false);
                    txtResult.Document.Text = bk;
                }
                else
                {
                    var blk1 =
                        ((S7FunctionBlock) fld1.GetBlock(lstBlocks.SelectedItem.ToString(), convOpt)).ToString(false);
                    var blk2 =
                        ((S7FunctionBlock) fld2.GetBlock(lstBlocks.SelectedItem.ToString(), convOpt)).ToString(false);

                    txtResult.TextArea.TextView.LineTransformers.Clear();

                    var txt = "";
                    diff_match_patch comparer = new diff_match_patch();
                    var result = comparer.diff_main(blk1, blk2);

                    txtResult.Document.Text = "";
                    foreach (var diff in result)
                    {
                        if (diff.operation == Operation.INSERT)
                        {
                            var st = txt.Length;
                            txt += diff.text;
                            var stp = txt.Length;

                            txtResult.TextArea.TextView.LineTransformers.Add(new TextColorizer(st, stp, Brushes.Green));
                        }
                        else if (diff.operation == Operation.DELETE)
                        {
                            var st = txt.Length;
                            txt += diff.text;
                            var stp = txt.Length;

                            txtResult.TextArea.TextView.LineTransformers.Add(new TextColorizer(st, stp, Brushes.Red));
                        }
                        else
                        {
                            txt += diff.text;
                        }

                    }
                    txtResult.Document.Text = txt;
                }

            }
        }
    }
}
