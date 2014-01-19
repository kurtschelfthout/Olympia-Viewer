using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Document;
using System.IO;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Threading;
using System.ComponentModel;

namespace WpfOly
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Nobles nobles;
        private GatesLayer gateLayer;
        private TradeRoutesLayer tradeRoutesLayer;
        private GarrisonLayer garrisonLayer;
        private Map provinia;
        private Map undercity;
        private Map faery;
        private Map hades;
        private Map cloud;

        private ZoomableCanvas ProviniaCanvas;
        private Point ProviniaLastMousePosition;
        
        private ZoomableCanvas TunnelCanvas;
        private Point TunnelLastMousePosition;

        private ZoomableCanvas FaeryCanvas;
        private Point FaeryLastMousePosition;

        private ZoomableCanvas HadesCanvas;
        private Point HadesLastMousePosition;

        private ZoomableCanvas CloudCanvas;
        private Point CloudLastMousePosition;

        private string Filename { get; set; }
        private CompletionWindow completionWindow;
        //private InsightWindow insightWindow;
        //private OverloadInsightWindow overloadInsightWindow;
        private IList<ICompletionData> orderCompletionData = new List<ICompletionData>();
        private ArgumentCompletions argumentCompletions = new ArgumentCompletions(Tables.Skills, Tables.Items);
        
        public MainWindow()
        {
            InitializeComponent();

            provinia = new Map(Map.Region.Provinia);
            undercity = new Map(Map.Region.Undercity);
            faery = new Map(Map.Region.Faery);
            hades = new Map(Map.Region.Hades);
            cloud = new Map(Map.Region.Cloud);

            nobles = new Nobles(new[] { provinia, undercity, faery, hades, cloud });
            gateLayer = new GatesLayer(provinia);
            tradeRoutesLayer = new TradeRoutesLayer(provinia);
            garrisonLayer = new GarrisonLayer(provinia);

            Provinces.ItemsSource = provinia;
            Tunnels.ItemsSource = undercity;
            Faery.ItemsSource = faery;
            Hades.ItemsSource = hades;
            Cloud.ItemsSource = cloud;

            GatesLayer.ItemsSource = gateLayer.Gates;
            GateHitsLayer.ItemsSource = gateLayer.GateHitsAndDetections;
            TradeRoutesLayer.ItemsSource = tradeRoutesLayer.TradeRoutes;
            GarrisonLayer.ItemsSource = garrisonLayer.Garrisons;
            search.DataContext = provinia;

            Filename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "olyviewer-temp.txt");

            textEditor.SyntaxHighlighting = LoadHighlightingDefinition("oly.xshd");

            textEditor.TextChanged += new EventHandler(textEditor_TextChanged);
            textEditor.TextArea.TextEntered += new TextCompositionEventHandler(TextArea_TextEntered);
            textEditor.TextArea.TextEntering += new TextCompositionEventHandler(TextArea_TextEntering);
            textEditor.MouseHover += new MouseEventHandler(textEditor_MouseHover);
            textEditor.MouseHoverStopped += new MouseEventHandler(textEditor_MouseHoverStopped);
            textEditor.TextArea.DefaultInputHandler.InputBindings.Add(new InputBinding(new MoveCommand(textEditor,"w"), new KeyGesture(Key.Left, ModifierKeys.Control)));
            textEditor.TextArea.DefaultInputHandler.InputBindings.Add(new InputBinding(new MoveCommand(textEditor,"n"), new KeyGesture(Key.Up, ModifierKeys.Control)));
            textEditor.TextArea.DefaultInputHandler.InputBindings.Add(new InputBinding(new MoveCommand(textEditor,"e"), new KeyGesture(Key.Right, ModifierKeys.Control)));
            textEditor.TextArea.DefaultInputHandler.InputBindings.Add(new InputBinding(new MoveCommand(textEditor,"s"), new KeyGesture(Key.Down, ModifierKeys.Control)));

            foreach (var order in Orders.All)
            {
                var o = new CompletionData(order.Name, order.Help);
                orderCompletionData.Add(o);
            }


            
        }

        

        public static IHighlightingDefinition LoadHighlightingDefinition(string resourceName)
        {
            var type = typeof(MainWindow);
            var fullName = type.Namespace + ".Resources." + resourceName;
            
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            {
                using (var reader = new XmlTextReader(stream))
                { 
                    return HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
        }

       
        private void ProviniaCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            // Store the canvas in a local variable since x:Name doesn't work.
            ProviniaCanvas = (ZoomableCanvas)sender;
            ProviniaCanvas.Offset = new Point(6000, 3000);

            // Set the canvas as the DataContext so our overlays can bind to it.
            DataContext = ProviniaCanvas;
        }

        private void Provinces_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            ProviniaCanvas.Scale *= x;

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)e.GetPosition(Provinces);
            ProviniaCanvas.Offset = (Point)((Vector)
                (ProviniaCanvas.Offset + position) * x - position);

            e.Handled = true;
        }

        private void Provinces_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(Provinces);
            if (e.LeftButton == MouseButtonState.Pressed)
                //&& !(e.OriginalSource is Thumb)) // Don't block the scrollbars.
            {
                ((UIElement) sender).CaptureMouse();
                ProviniaCanvas.Offset -= position - ProviniaLastMousePosition;
                e.Handled = true;
            }
            else
            {
                ((UIElement)sender).ReleaseMouseCapture();
            }
            ProviniaLastMousePosition = position;
        }

        private void TunnelCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            // Store the canvas in a local variable since x:Name doesn't work.
            TunnelCanvas = (ZoomableCanvas)sender;
        }

        private void Tunnels_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            TunnelCanvas.Scale *= x;

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)e.GetPosition(Tunnels);
            TunnelCanvas.Offset = (Point)((Vector)
                (TunnelCanvas.Offset + position) * x - position);

            e.Handled = true;
        }

        private void Tunnels_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(Tunnels);
            if (e.LeftButton == MouseButtonState.Pressed)
            //&& !(e.OriginalSource is Thumb)) // Don't block the scrollbars.
            {
                ((UIElement)sender).CaptureMouse();
                TunnelCanvas.Offset -= position - TunnelLastMousePosition;
                e.Handled = true;
            }
            else
            {
                ((UIElement)sender).ReleaseMouseCapture();
            }
            TunnelLastMousePosition = position;
        }

        private void FaeryCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            // Store the canvas in a local variable since x:Name doesn't work.
            FaeryCanvas = (ZoomableCanvas)sender;
        }

        private void Faery_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            FaeryCanvas.Scale *= x;

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)e.GetPosition(Faery);
            FaeryCanvas.Offset = (Point)((Vector)
                (FaeryCanvas.Offset + position) * x - position);

            e.Handled = true;
        }

        private void Faery_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(Faery);
            if (e.LeftButton == MouseButtonState.Pressed)
            //&& !(e.OriginalSource is Thumb)) // Don't block the scrollbars.
            {
                ((UIElement)sender).CaptureMouse();
                FaeryCanvas.Offset -= position - FaeryLastMousePosition;
                e.Handled = true;
            }
            else
            {
                ((UIElement)sender).ReleaseMouseCapture();
            }
            FaeryLastMousePosition = position;
        }

        private void HadesCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            // Store the canvas in a local variable since x:Name doesn't work.
            HadesCanvas = (ZoomableCanvas)sender;
        }

        private void Hades_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            HadesCanvas.Scale *= x;

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)e.GetPosition(Hades);
            HadesCanvas.Offset = (Point)((Vector)
                (HadesCanvas.Offset + position) * x - position);

            e.Handled = true;
        }

        private void Hades_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(Hades);
            if (e.LeftButton == MouseButtonState.Pressed)
            //&& !(e.OriginalSource is Thumb)) // Don't block the scrollbars.
            {
                ((UIElement)sender).CaptureMouse();
                HadesCanvas.Offset -= position - HadesLastMousePosition;
                e.Handled = true;
            }
            else
            {
                ((UIElement)sender).ReleaseMouseCapture();
            }
            HadesLastMousePosition = position;
        }

        private void CloudCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            // Store the canvas in a local variable since x:Name doesn't work.
            CloudCanvas = (ZoomableCanvas)sender;
        }

        private void Cloud_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            CloudCanvas.Scale *= x;

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)e.GetPosition(Hades);
            CloudCanvas.Offset = (Point)((Vector)
                (CloudCanvas.Offset + position) * x - position);

            e.Handled = true;
        }

        private void Cloud_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(Cloud);
            if (e.LeftButton == MouseButtonState.Pressed)
            //&& !(e.OriginalSource is Thumb)) // Don't block the scrollbars.
            {
                ((UIElement)sender).CaptureMouse();
                CloudCanvas.Offset -= position - CloudLastMousePosition;
                e.Handled = true;
            }
            else
            {
                ((UIElement)sender).ReleaseMouseCapture();
            }
            CloudLastMousePosition = position;
        }

        private void goto_Click(object sender, RoutedEventArgs e)
        {
            Location loc;
            if (provinia.LocationsById.TryGetValue(search.Text, out loc))
            {
                FocusCanvasOn(loc);
            }
            else
            {
                var result = provinia.LocationsById.Values.FirstOrDefault(v => v.Name.Equals(search.Text));
                if (result != null)
                    FocusCanvasOn(result);
            }
        }

        private Storyboard myStoryboard;

        private void FocusCanvasOn(Location loc)
        {
            var size = ProviniaCanvas.RenderSize;
            var toOffset = new Point((loc.X + Map.length) /* * ProviniaCanvas.Scale */ - size.Width / 2, (loc.Y + Map.length) /* * ProviniaCanvas.Scale */- size.Height / 2);

            //----- pan the canvas to the postion ----
            PointAnimation pan = new PointAnimation(toOffset, Duration.Automatic);
           
            DoubleAnimation zoom = new DoubleAnimation(1.0, Duration.Automatic);

            myStoryboard = new Storyboard();

            myStoryboard.Children.Add(pan);
            Storyboard.SetTargetProperty(pan, new PropertyPath(ZoomableCanvas.OffsetProperty));
            myStoryboard.FillBehavior = FillBehavior.Stop;
            myStoryboard.Completed += new EventHandler((s, e) =>
                {
                    ProviniaCanvas.Offset = toOffset;
                });

            myStoryboard.Children.Add(zoom);
            Storyboard.SetTargetProperty(zoom, new PropertyPath(ZoomableCanvas.ScaleProperty));
            myStoryboard.FillBehavior = FillBehavior.Stop;
            myStoryboard.Completed += new EventHandler((s, e) =>
            {
                ProviniaCanvas.Scale = 1.0;
            });
            myStoryboard.Begin(ProviniaCanvas);
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "turn xx"; // Default file name
            dialog.DefaultExt = ".txt"; // Default file extension
            dialog.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
            dialog.CheckFileExists = false;
            // Show save file dialog box
            Nullable<bool> result = dialog.ShowDialog(this);

            // Process save file dialog box results
            if (result == true)
            {
                // Load document
                this.Filename = dialog.FileName;
                if (File.Exists(Filename))
                {
                    textEditor.Document.Text = File.ReadAllText(Filename);
                }
                else
                {
                    File.WriteAllText(Filename,textEditor.Document.Text);
                }
            }
        }

        void textEditor_TextChanged(object sender, EventArgs e)
        {
            File.WriteAllText(Filename, textEditor.Document.Text);
        }

        void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            //if we're typing a letter, check that it is the first word of the line. If so, auto-complete on orders.
            if (completionWindow == null && Char.IsLetterOrDigit(e.Text[0]))
            {
                var line = textEditor.Document.GetText(textEditor.Document.GetLineByOffset(textEditor.CaretOffset));
                var orderInProgress = line.TrimStart(new char[] { '\t', ' ' });
                if (!orderInProgress.Contains(" "))
                {
                    completionWindow = new CompletionWindow(textEditor.TextArea);
                    completionWindow.StartOffset = textEditor.CaretOffset - orderInProgress.Length;
                    foreach (var order in orderCompletionData)
                    {
                        completionWindow.CompletionList.CompletionData.Add(order);
                    }
                    completionWindow.CompletionList.SelectItem(orderInProgress);
                    completionWindow.Show();
                    completionWindow.Closed += delegate
                    {
                        completionWindow = null;
                    };
                }
                else //there is an order on this line. Try to complete arguments.
                {
                    var word = GetFirstWord(orderInProgress);
                    Order order = null;
                    Orders.ByName.TryGetValue(word, out order);
                    if (order != null)
                    {
                        var completions = order.GetCompletions(argumentCompletions,orderInProgress.Count(ch => ch.Equals(' ')) - 1);
                        if (completions.Count() > 0)
                        {
                            completionWindow = new CompletionWindow(textEditor.TextArea);
                            completionWindow.StartOffset = textEditor.CaretOffset - e.Text.Length;
                            foreach (var completion in completions)
                            {
                                completionWindow.CompletionList.CompletionData.Add(completion);
                            }
                            completionWindow.Show();
                            completionWindow.Closed += delegate
                            {
                                completionWindow = null;
                            };
                        }
                    }
                }
            }
        }

        void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (completionWindow != null && (e.Text.Length == 0 || !char.IsLetterOrDigit(e.Text[0])))
            {
                // Whenever a non-letter is typed while the completion window is open,
                // insert the currently selected element.
                completionWindow.CompletionList.RequestInsertion(e);                    
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }


        private ToolTip toolTip = new ToolTip();

        private static string GetFirstWord(string line)
        {
            var withoutStart = line.TrimStart(new[] { '\t', ' ' });
            return new String(withoutStart.TakeWhile(c => c != ' ').ToArray());
        }

        void textEditor_MouseHover(object sender, MouseEventArgs e)
        {
            var pos = textEditor.GetPositionFromPoint(e.GetPosition(textEditor));

            if (pos != null)
            {
                var word = GetFirstWord(textEditor.Document.GetText(textEditor.Document.GetLineByNumber(pos.Value.Line)));
                Order order = null;
                Orders.ByName.TryGetValue(word, out order);
                if (order != null)
                {
                    toolTip.PlacementTarget = this; // required for property inheritance
                    toolTip.Content = order.Help;
                    toolTip.IsOpen = true;
                    e.Handled = true;
                }
            }
        }

        void textEditor_MouseHoverStopped(object sender, MouseEventArgs e)
        {
            toolTip.IsOpen = false;
        }

        private static Lazy<LoadAnalyze> loadAnalyze = new Lazy<LoadAnalyze>(LazyThreadSafetyMode.ExecutionAndPublication);
        private static bool busy = false;

        static MainWindow()
        {
            //get started with loading the script in the background during startup.
            ThreadPool.QueueUserWorkItem(state =>
                {
                    var result = loadAnalyze.Value;
                });
        }

        //private void download_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!busy)
        //    {
        //        busy = true;
        //        download.IsEnabled = false;
        //        parse.IsEnabled = false;
        //        download.Content = "Downloading";
        //        parse.Content = "Downloading";
        //        var bg = new BackgroundWorker();
        //        bg.DoWork += (s, ee) =>
        //        {
        //            var l = loadAnalyze.Value;
        //            var output = clojure.lang.RT.var("analyze", "save-reports");
        //            //var loc = @"C:\Users\Kurt\Projects\oly\reports";
        //            var loc = @"reports";
        //            var dirs = Directory.GetDirectories(loc);
        //            var latestTurn = dirs.Max(name => Int32.Parse(name.Split('\\').Last()));
        //            ee.Result = output.invoke(latestTurn + 1);
        //        };
        //        bg.RunWorkerCompleted += (s, ee) =>
        //            {
        //                MessageBox.Show("Downloaded " + ee.Result.ToString() + " turn reports.");
        //                busy = false;
        //                download.IsEnabled = true;
        //                parse.IsEnabled = true;
        //                download.Content = "Download";
        //                parse.Content = "Parse";
        //            };
        //        bg.RunWorkerAsync();
        //    }
        //}


        //private void parse_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!busy)
        //    {
        //        busy = true;
        //        download.IsEnabled = false;
        //        parse.IsEnabled = false;
        //        download.Content = "Parsing";
        //        parse.Content = "Parsing";
        //        var bg = new BackgroundWorker();
        //        bg.DoWork += (s, ee) =>
        //        {
        //            var l = loadAnalyze.Value;
        //            var output = clojure.lang.RT.var("analyze", "make-reports-xml");
        //            ee.Result = output.invoke();
        //        };
        //        bg.RunWorkerCompleted += (s, ee) =>
        //        {
        //            MessageBox.Show("Done parsing. Now Restart.");
        //            busy = false;
        //            download.IsEnabled = true;
        //            parse.IsEnabled = true;
        //            download.Content = "Download";
        //            parse.Content = "Parse";
        //        };
        //        bg.RunWorkerAsync();
        //    }
        //}

        private void province_textBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Province province = ((FrameworkElement) sender).DataContext as Province;
                //var reports = @"C:\Users\Kurt\Projects\oly\reports";
                var reports = @"reports";
                if (province.Visited)
                {
                    var reportToShow = (from visitedBy in province.Visits[province.TurnLastVisited]
                                        let path = System.IO.Path.Combine(reports, province.TurnLastVisited.ToString(), visitedBy + ".htm")
                                        where File.Exists(path)
                                        select path).First();
                    Window w = new Window();

                    var frame = new TextBox();
                    frame.FontFamily = new FontFamily("Consolas");
                    frame.Text = File.ReadAllText(reportToShow);
                    

                    //var frame = new WebBrowser();
                    //frame.Navigate(reportToShow);

                    w.Content = frame;
                    w.Show();

                    var lines = File.ReadLines(reportToShow);
                    int scrollTo = 0;
                    string search = " [" + province.Id + "]\"><font size=+1>"; //<name="Plain [ax54]"><font size=+1>
                    foreach (var line in lines)
                    {
                        if (line.Contains(search)) 
                            break;
                        scrollTo++;
                    }
                    frame.ScrollToLine(scrollTo);
               }
            }
             
        }
    }

    class LoadAnalyze
    {
        public LoadAnalyze()
        {
            //System.Environment.SetEnvironmentVariable("clojure.load.path", @"C:\Users\Kurt\Projects\oly\OlyViewer\Clojure", EnvironmentVariableTarget.Process);
            clojure.lang.RT.load("analyze");
        }
    }
}
