using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HedgeModManager.Uwp.Models;
using HedgeModManager.Uwp.Services;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.Gaming.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace HedgeModManager.Improved
{
    public static class Entry
    {
        public static void InitializeApplication(Application app) {
            // Xbox pointer policy belongs to application construction, before OnLaunched.
            try {if(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily=="Windows.Xbox")app.RequiresPointerMode=ApplicationRequiresPointerMode.WhenRequested;}
            catch(Exception ex) {System.Diagnostics.Debug.WriteLine("Pointer policy: "+ex);}
        }
        public static void Attach(Page host) {
            try{L.Select(ApplicationData.Current.LocalSettings.Values["InterfaceLanguage"] as string);}catch{L.Select("en");}
            try {host.Content=new ManagerPage();}
            catch(Exception ex) {
                var panel=new StackPanel {Margin=new Thickness(24),Spacing=16};
                panel.Children.Add(new TextBlock {Text=L.T("Não foi possível abrir o gerenciador"),FontSize=26,TextWrapping=TextWrapping.Wrap});
                panel.Children.Add(new TextBlock {Text=L.T("Os arquivos dos mods não foram alterados. Detalhes da falha:"),FontSize=20,TextWrapping=TextWrapping.Wrap});
                panel.Children.Add(new TextBox {Text=ex.ToString(),IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,FontSize=18});
                var retry=new Button {Content=L.T("Tentar novamente"),FontSize=20};retry.Click+=(s,e)=>Attach(host);panel.Children.Add(retry);
                host.Content=new ScrollViewer {Content=panel};
            }
        }
    }
    public sealed class ManagerPage : Page
    {
        readonly XboxModsDatabase database = new XboxModsDatabase();
        readonly QuickInstallService installer = new QuickInstallService();
        readonly ModUpdateService updater = new ModUpdateService();
        readonly List<ModEntry> mods = new List<ModEntry>();
        readonly Grid root = new Grid();
        readonly Grid body = new Grid();
        readonly TextBlock status = Text(L.T("Escolha a pasta do jogo ou conecte a unidade E:."),18);
        readonly TextBlock folderLabel = Text(L.T("Nenhuma pasta selecionada"),18);
        readonly TextBlock count = Text("",18);
        readonly ListView list = new ListView { SelectionMode=ListViewSelectionMode.Single,HorizontalContentAlignment=HorizontalAlignment.Stretch, IsItemClickEnabled=true,IsFocusEngagementEnabled=false };
        readonly TextBox search = new TextBox { PlaceholderText=L.T("Buscar mods…"), MinWidth=160,FontSize=20 };
        readonly Button save = new Button { Content=L.T("Salvar alterações"), MinHeight=44,FontSize=20 };
        readonly Button config = new Button { Content=L.T("X Configurar"), MinHeight=44,FontSize=20 };
        readonly Button toggle = new Button { Content=L.T("Y Ativar/desativar"),MinHeight=44,FontSize=20 };
        readonly Button up = new Button { Content=L.T("LT ↑ Prioridade"), MinHeight=44,FontSize=20 };
        readonly Button down = new Button { Content=L.T("RT ↓ Prioridade"), MinHeight=44,FontSize=20 };
        readonly List<Button> tabs=new List<Button>();
        Button languageButton;
        string installLinkText="",installModText="",installFileText="";
        CoreWindow inputWindow;
        readonly ShoulderNavigation shoulders=new ShoulderNavigation();
        readonly ActionButtonLatch actionButtons=new ActionButtonLatch();
        readonly DispatcherTimer controllerTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(50)};
        bool pollLeft,pollRight,pollX,pollY,windowActive=true;
        StorageFolder folder;
        bool dirty, busy, rendering;
        int tab;
        public ManagerPage() {
            RequestedTheme=ElementTheme.Dark;
            Language=L.Current;
            XYFocusKeyboardNavigation=Windows.UI.Xaml.Input.XYFocusKeyboardNavigationMode.Enabled;
            root.Background=new SolidColorBrush(Color.FromArgb(255,20,24,32)); root.Padding=new Thickness(16);
            root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height=new GridLength(1,GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            var header=new Grid();header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
            header.Children.Add(Text("HedgeModManager",28));
            var navigation=Row();navigation.Margin=new Thickness(0,0,0,8);
            tabs.Add(Action(L.T("Meus mods"),()=>{ShowMods(); return Task.CompletedTask;}));
            tabs.Add(Action(L.T("Instalar"),()=>{ShowInstall(); return Task.CompletedTask;}));
            tabs.Add(Action(L.T("Atualizações"),()=>{ShowUpdates(); return Task.CompletedTask;}));
            navigation.Children.Add(Text("LB",18));foreach(var button in tabs)navigation.Children.Add(button);navigation.Children.Add(Text("RB",18));
            languageButton=Action(L.Current.ToUpperInvariant()+" ▾",ChangeLanguage);languageButton.FontSize=18;navigation.Children.Add(languageButton);
            ToolTipService.SetToolTip(languageButton,L.T("Idioma"));AutomationProperties.SetName(languageButton,L.T("Idioma"));
            Grid.SetColumn(navigation,1);header.Children.Add(navigation);
            Place(header,0); Place(body,2);
            status.MaxLines=2;status.TextTrimming=TextTrimming.CharacterEllipsis;
            status.Margin=new Thickness(0,6,0,0);Place(status,3);
            Content=root;
            PreviewKeyDown+=PriorityPreviewDown;
            PreviewKeyUp+=PriorityPreviewUp;
            controllerTimer.Tick+=(s,e)=>PollController();
            save.Click += async (s,e)=>await Run(Save);
            config.Click += async (s,e)=>await Run(Configure);
            toggle.Click += (s,e)=>ToggleMod(Selected);
            up.Click += (s,e)=>Move(-1); down.Click += (s,e)=>Move(1);
            list.SelectionChanged += (s,e)=>Selection();
            list.ItemClick+=(s,e)=> {
                var item=e.ClickedItem as ListViewItem;
                var mod=item?.Tag as ModEntry;
                if(item!=null)list.SelectedItem=item;
                ToggleMod(mod);
            };
            search.TextChanged += (s,e)=>Render();
            Loaded += async(s,e)=> { await Run(async()=> {
                try {AttachInput();}catch(Exception ex){controllerTimer.Stop();status.Text=L.T("Entrada do controle indisponível: ")+ex.Message;}
                if(StorageApplicationPermissions.FutureAccessList.ContainsItem("mods"))
                    try { folder=await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("mods"); } catch { }
                if(folder==null) try { folder=await StorageFolder.GetFolderFromPathAsync(@"E:\Unleashed\mods"); } catch { }
                if(folder!=null) await Load();
            });};
            Unloaded+=(s,e)=>DetachInput();
            ShowMods();
        }
        static TextBlock Text(string value,int size=16) { return new TextBlock { Text=value,FontSize=size,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,4,0,4) }; }
        async Task ChangeLanguage(){
            var chooser=new ComboBox {ItemsSource=L.Names,SelectedIndex=Array.IndexOf(L.Codes,L.Current),MinWidth=280,FontSize=22};
            var dialog=new ContentDialog {Title=L.T("Idioma"),Content=chooser,PrimaryButtonText=L.T("Aplicar"),CloseButtonText=L.T("Cancelar"),RequestedTheme=ElementTheme.Dark};
            if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
            var code=L.Codes[chooser.SelectedIndex];
            ApplicationData.Current.LocalSettings.Values["InterfaceLanguage"]=code;L.Select(code);
            Language=L.Current;
            save.Content=L.T("Salvar alterações");config.Content=L.T("X Configurar");up.Content=L.T("LT ↑ Prioridade");down.Content=L.T("RT ↓ Prioridade");search.PlaceholderText=L.T("Buscar mods…");
            tabs[0].Content=L.T("Meus mods");tabs[1].Content=L.T("Instalar");tabs[2].Content=L.T("Atualizações");
            languageButton.Content=L.Current.ToUpperInvariant()+" ▾";ToolTipService.SetToolTip(languageButton,L.T("Idioma"));AutomationProperties.SetName(languageButton,L.T("Idioma"));
            if(tab==0)ShowMods();else if(tab==1)ShowInstall();else ShowUpdates();
            Selection();status.Text=dirty?L.T("Alterações pendentes. Use Salvar alterações para aplicar ao jogo."):L.T("Idioma alterado.");
        }
        static StackPanel Stack() { return new StackPanel { Spacing=8 }; }
        static StackPanel Row() { return new StackPanel { Orientation=Orientation.Horizontal,Spacing=8,Margin=new Thickness(0,4,0,4) }; }
        void AttachInput() {
            if(inputWindow!=null)return;inputWindow=Window.Current.CoreWindow;
            inputWindow.KeyDown+=WindowKeyDown;inputWindow.KeyUp+=WindowKeyUp;inputWindow.Activated+=WindowActivated;
            controllerTimer.Start();
        }
        void DetachInput() {
            controllerTimer.Stop();if(inputWindow==null)return;inputWindow.KeyDown-=WindowKeyDown;inputWindow.KeyUp-=WindowKeyUp;inputWindow.Activated-=WindowActivated;inputWindow=null;shoulders.Reset();actionButtons.Reset();pollLeft=pollRight=pollX=pollY=false;
        }
        void WindowActivated(CoreWindow sender,WindowActivatedEventArgs args) {windowActive=args.WindowActivationState!=CoreWindowActivationState.Deactivated;if(!windowActive){shoulders.Reset();actionButtons.Reset();pollLeft=pollRight=pollX=pollY=false;}}
        void PollController() {
            if(!windowActive)return;
            try {ReadController();}
            catch(Exception ex){controllerTimer.Stop();status.Text=L.T("Leitura direta do controle desativada: ")+ex.Message;}
        }
        void ReadController() {
            bool left=false,right=false,x=false,y=false;
            foreach(var controller in Gamepad.Gamepads) {try {var buttons=controller.GetCurrentReading().Buttons;left|=(buttons&GamepadButtons.LeftShoulder)!=0;right|=(buttons&GamepadButtons.RightShoulder)!=0;x|=(buttons&GamepadButtons.X)!=0;y|=(buttons&GamepadButtons.Y)!=0;}catch{}}
            if(left&&!pollLeft)TriggerShoulder(-1);if(right&&!pollRight)TriggerShoulder(1);
            if(!left&&pollLeft)shoulders.Release(-1);if(!right&&pollRight)shoulders.Release(1);
            if(x&&!pollX)_=TriggerAction(1);if(y&&!pollY)_=TriggerAction(2);
            if(!x&&pollX)actionButtons.Release(1);if(!y&&pollY)actionButtons.Release(2);
            // Priority is handled by tunneled XAML preview events, before native page scrolling.
            pollLeft=left;pollRight=right;pollX=x;pollY=y;
        }
        static int ModAction(VirtualKey key) {return key==VirtualKey.GamepadX||key==VirtualKey.F2?1:key==VirtualKey.GamepadY?2:key==VirtualKey.PageUp?3:key==VirtualKey.PageDown?4:0;}
        static int PriorityKey(VirtualKey key){return key==VirtualKey.GamepadLeftTrigger||key==VirtualKey.PageUp?3:key==VirtualKey.GamepadRightTrigger||key==VirtualKey.PageDown?4:0;}
        void PriorityPreviewDown(object sender,Windows.UI.Xaml.Input.KeyRoutedEventArgs args){
            int action=PriorityKey(args.Key);if(action==0||tab!=0||busy)return;
            if(Windows.UI.Xaml.Input.FocusManager.GetFocusedElement() is TextBox)return;
            args.Handled=true;if(!args.KeyStatus.WasKeyDown)_=TriggerAction(action);
        }
        void PriorityPreviewUp(object sender,Windows.UI.Xaml.Input.KeyRoutedEventArgs args){
            int action=PriorityKey(args.Key);if(action==0)return;actionButtons.Release(action);
            if(tab==0&&!busy)args.Handled=true;
        }
        bool CanUseModShortcut() {return tab==0&&!busy&&Selected!=null&&!(Windows.UI.Xaml.Input.FocusManager.GetFocusedElement() is TextBox);}
        async Task TriggerAction(int action) {
            if(!actionButtons.Press(action)||!CanUseModShortcut())return;
            if(action==1)await Run(Configure);else if(action==2)ToggleMod(Selected);else if(action==3)Move(-1);else if(action==4)Move(1);
        }
        void WindowKeyUp(CoreWindow sender,KeyEventArgs args) {if(Shoulder(args.VirtualKey)!=0)shoulders.Release(Shoulder(args.VirtualKey));if(ModAction(args.VirtualKey)!=0)actionButtons.Release(ModAction(args.VirtualKey));if(PriorityKey(args.VirtualKey)!=0)actionButtons.Release(PriorityKey(args.VirtualKey));}
        static int Shoulder(VirtualKey key) {return key==VirtualKey.GamepadRightShoulder?1:key==VirtualKey.GamepadLeftShoulder?-1:0;}
        void WindowKeyDown(CoreWindow sender,KeyEventArgs args) {
            // Leave priority keys to PreviewKeyDown. CoreWindow handling cannot suppress native list accelerators reliably.
            if(PriorityKey(args.VirtualKey)!=0)return;
            int action=ModAction(args.VirtualKey);
            if(action!=0) {if(CanUseModShortcut()){args.Handled=true;_=TriggerAction(action);}return;}
            int direction=Shoulder(args.VirtualKey);if(direction==0)return;
            args.Handled=true;
            TriggerShoulder(direction);
        }
        void TriggerShoulder(int direction) {
            if(!shoulders.Press(direction)||busy)return;
            int next=ShoulderNavigation.Next(tab,direction);
            if(next==0)ShowMods();else if(next==1)ShowInstall();else ShowUpdates();
            tabs[next].Focus(FocusState.Programmatic);
        }
        void HighlightTab() {for(int i=0;i<tabs.Count;i++){tabs[i].Background=new SolidColorBrush(i==tab?Color.FromArgb(255,30,85,130):Color.FromArgb(255,45,49,58));AutomationProperties.SetHelpText(tabs[i],i==tab?L.T("Aba atual"):L.T("Alternar aba"));}}
        void Place(FrameworkElement element,int row) { Grid.SetRow(element,row); root.Children.Add(element); }
        Button Action(string text,Func<Task> action) {
            var b=new Button {Content=text,MinHeight=44,FontSize=20}; b.Click+=async(s,e)=>await Run(action); return b;
        }
        async Task Run(Func<Task> action) {
            if(busy) return; busy=true;
            try { await action(); } catch(Exception ex) { status.Text=L.T("Não foi possível concluir: ")+ex.Message; }
            finally { busy=false; Selection(); }
        }
        async Task<bool> CanReload() {
            if(!dirty) return true;
            var d=new ContentDialog {Title=L.T("Há alterações não salvas"), Content=L.T("Salve a ativação e a prioridade dos mods antes de continuar."),PrimaryButtonText=L.T("Salvar e continuar"),SecondaryButtonText=L.T("Descartar"),CloseButtonText=L.T("Cancelar"),RequestedTheme=ElementTheme.Dark};
            var answer=await d.ShowAsync();
            if(answer==ContentDialogResult.Primary) { await Save(); return true; }
            return answer==ContentDialogResult.Secondary;
        }
        async Task Choose() {
            if(!await CanReload()) return;
            var picker=new FolderPicker {SuggestedStartLocation=PickerLocationId.ComputerFolder}; picker.FileTypeFilter.Add("*");
            var selected=await picker.PickSingleFolderAsync(); if(selected==null) return;
            var resolved=await database.ResolveModsFolder(selected);
            var previous=folder; folder=resolved;
            try { await Load(); StorageApplicationPermissions.FutureAccessList.AddOrReplace("mods",folder); }
            catch { folder=previous; throw; }
        }
        void ShowMods() {
            tab=0;HighlightTab(); body.Children.Clear(); body.RowDefinitions.Clear();
            for(int i=0;i<4;i++) body.RowDefinitions.Add(new RowDefinition {Height=i==2?new GridLength(1,GridUnitType.Star):GridLength.Auto});
            var saveParent=save.Parent as Panel;if(saveParent!=null)saveParent.Children.Remove(save);
            var tools=Row(); tools.Children.Add(Action(L.T("Pasta…"),Choose)); tools.Children.Add(Action(L.T("Recarregar"),async()=>{if(await CanReload()) await Load();})); tools.Children.Add(save);
            // Shared controls must be detached before this view is rebuilt.
            var oldParent=folderLabel.Parent as Panel; if(oldParent!=null) oldParent.Children.Remove(folderLabel);
            var toolbar=new Grid {Margin=new Thickness(0,0,0,6)};toolbar.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});toolbar.ColumnDefinitions.Add(new ColumnDefinition());toolbar.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
            toolbar.Children.Add(tools);
            oldParent=search.Parent as Panel; if(oldParent!=null) oldParent.Children.Remove(search);search.Margin=new Thickness(12,4,0,4);Grid.SetColumn(search,1);toolbar.Children.Add(search);
            oldParent=count.Parent as Panel; if(oldParent!=null) oldParent.Children.Remove(count); count.Margin=new Thickness(12,8,0,0);Grid.SetColumn(count,2);toolbar.Children.Add(count);
            Grid.SetRow(toolbar,0); body.Children.Add(toolbar); Grid.SetRow(list,2); body.Children.Add(list);
            var actions=Row(); foreach(var b in new[]{config,toggle,up,down}) { oldParent=b.Parent as Panel; if(oldParent!=null) oldParent.Children.Remove(b); actions.Children.Add(b); }
            Grid.SetRow(actions,3); body.Children.Add(actions); Render();
        }
        ModEntry Selected { get { return (list.SelectedItem as ListViewItem)?.Tag as ModEntry; } }
        void Selection() {
            var m=Selected; var index=m==null?-1:mods.IndexOf(m);
            config.IsEnabled=m!=null; up.IsEnabled=index>0; down.IsEnabled=index>=0 && index<mods.Count-1; save.IsEnabled=folder!=null && dirty;
            toggle.IsEnabled=m!=null;toggle.Content=m!=null&&m.Enabled?L.T("Y Desativar"):L.T("Y Ativar");
        }
        void Render(ModEntry selected=null) {
            if(tab!=0) return; selected=selected??Selected; rendering=true; list.Items.Clear();
            var q=(search.Text??"").Trim();
            for(int i=0;i<mods.Count;i++) {
                var m=mods[i]; if(q!="" && (m.Title+" "+m.Author+" "+m.FolderName).IndexOf(q,StringComparison.OrdinalIgnoreCase)<0) continue;
                var item=new ListViewItem {Tag=m,HorizontalContentAlignment=HorizontalAlignment.Stretch,Padding=new Thickness(4),MinHeight=92};
                // One focus target per row: the list item handles A/click. Nested controls must not consume input.
                var row=new Grid {Padding=new Thickness(8),IsHitTestVisible=false}; row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(64)}); row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
                row.Children.Add(Text((i+1).ToString("00"),28));
                var details=Stack(); details.Spacing=0;
                var title=Text(m.Title,26);title.FontWeight=Windows.UI.Text.FontWeights.SemiBold;title.Margin=new Thickness(0);
                var check=new CheckBox {Content=title,IsChecked=m.Enabled,MinHeight=44,IsTabStop=false,IsHitTestVisible=false};
                details.Children.Add(check); details.Children.Add(Text(m.Author+" • v"+m.Version,19)); Grid.SetColumn(details,1); row.Children.Add(details);
                var hint=Text(L.T("X Configurar"),20);hint.Margin=new Thickness(16,0,4,0);Grid.SetColumn(hint,2);row.Children.Add(hint);
                AutomationProperties.SetName(item,m.Title+(m.Enabled?L.T(", ativado"):L.T(", desativado")));
                AutomationProperties.SetHelpText(item,L.T("A ou clique: ativar/desativar. X: configurar. Y: ativar/desativar selecionado."));
                item.GotFocus+=(s,e)=>{if(!rendering)list.SelectedItem=item;};
                item.Content=row;list.Items.Add(item); if(m==selected) list.SelectedItem=item;
            }
            count.Text=mods.Count==0?L.T("Nenhum mod"):L.F("{0}/{1} • {2} ativos",list.Items.Count,mods.Count,mods.Count(m=>m.Enabled));
            if(list.SelectedItem==null&&list.Items.Count>0)list.SelectedIndex=0;
            folderLabel.Text=folder?.Path??L.T("Nenhuma pasta selecionada"); rendering=false; Selection();
        }
        void Changed() { dirty=true; status.Text=L.T("Alterações pendentes. Use Salvar alterações para aplicar ao jogo."); Selection(); count.Text=L.F("{0}/{1} • {2} ativos",list.Items.Count,mods.Count,mods.Count(m=>m.Enabled)); }
        void ToggleMod(ModEntry mod) {
            if(busy||mod==null||!mods.Contains(mod))return;
            mod.Enabled=!mod.Enabled;Changed();
            // Update the existing item, preserving focus and scroll while the button is released.
            foreach(var value in list.Items) {
                var item=value as ListViewItem;if(item?.Tag!=mod)continue;
                var row=item.Content as Grid;var details=row?.Children.OfType<StackPanel>().FirstOrDefault();
                var check=details?.Children.OfType<CheckBox>().FirstOrDefault();if(check!=null)check.IsChecked=mod.Enabled;
                list.SelectedItem=item;AutomationProperties.SetName(item,mod.Title+(mod.Enabled?L.T(", ativado"):L.T(", desativado")));break;
            }
            Selection();status.Text=mod.Title+(mod.Enabled?L.T(" ativado."):L.T(" desativado."))+L.T(" Use Salvar alterações para aplicar.");
        }
        void Move(int delta) {
            if(busy)return;
            var m=Selected; if(m==null||!ModOrder.TryMove(mods,m,delta))return;
            var viewport=FindScroll(list);double offset=viewport?.VerticalOffset??0;
            Changed(); Render(m);list.UpdateLayout();
            viewport=FindScroll(list);if(viewport!=null)viewport.ChangeView(null,offset,null,true);list.UpdateLayout();
            (list.SelectedItem as ListViewItem)?.Focus(FocusState.Programmatic);
            status.Text=L.F("{0} • prioridade {1}. Use Salvar alterações para aplicar.",m.Title,mods.IndexOf(m)+1);
        }
        static ScrollViewer FindScroll(DependencyObject node){
            if(node is ScrollViewer)return (ScrollViewer)node;
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var found=FindScroll(VisualTreeHelper.GetChild(node,i));if(found!=null)return found;}
            return null;
        }
        async Task Load() {
            if(folder==null) { status.Text=L.T("Escolha a pasta do jogo ou a pasta mods.");return; }
            var loaded=(await database.Load(folder)).ToList();
            var order=ModOrder.Read(new IniDocument(await Read(folder,"ModsDB.ini")));
            loaded=loaded.OrderBy(m=> {var n=order.FindIndex(p=>string.Equals(p,ModOrder.Normalize(m.ModIniPath),StringComparison.OrdinalIgnoreCase)); return n<0?int.MaxValue:n;}).ToList();
            mods.Clear();mods.AddRange(loaded);dirty=false; Render();status.Text=L.F("{0} mods • A/Y: ativação • X: configurar • LT/RT: subir/descer prioridade • Topo = maior prioridade",mods.Count);
        }
        async Task Save() {
            if(folder==null)throw new InvalidOperationException(L.T("Escolha a pasta de mods."));
            var states=mods.Select(m=>new ModState {Path=m.ModIniPath,Enabled=m.Enabled}).ToList();
            var text=ModOrder.Write(await Read(folder,"ModsDB.ini"),states);
            await Write(folder,"ModsDB.ini",text);
            var parent=await folder.GetParentAsync();
            if(parent!=null) {
                var cpk=new IniDocument(await Read(parent,"cpkredir.ini"));
                cpk.Set("CPKREDIR","Enabled","1"); cpk.Set("CPKREDIR","ModsDbIni","\""+folder.Path+"\\ModsDB.ini\"");
                if(cpk.Get("CPKREDIR","PlaceTocAtEnd",null)==null) cpk.Set("CPKREDIR","PlaceTocAtEnd","1");
                await Write(parent,"cpkredir.ini",cpk.ToString());
            }
            if(await Read(folder,"ModsDB.ini")!=text) throw new IOException(L.T("A verificação da gravação falhou."));
            dirty=false;status.Text=L.T("Ativação e prioridade salvas. Feche e abra o jogo para aplicar.");Selection();
        }
        internal static async Task<string> Read(StorageFolder f,string name) {
            var item=await f.TryGetItemAsync(name); if(item==null)return "";
            var file=item as StorageFile; if(file==null)throw new IOException(L.T("O caminho é uma pasta: ")+name);
            return await FileIO.ReadTextAsync(file);
        }
        internal static async Task Write(StorageFolder f,string name,string text) {
            var file=await f.TryGetItemAsync(name) as StorageFile;
            if(file!=null)await file.CopyAsync(f,name+".hmm-backup",NameCollisionOption.ReplaceExisting);
            var temp=await f.CreateFileAsync("hmm-"+Guid.NewGuid().ToString("N")+".tmp",CreationCollisionOption.FailIfExists);
            try {await FileIO.WriteTextAsync(temp,text); if(file!=null)await temp.MoveAndReplaceAsync(file);else await temp.RenameAsync(name,NameCollisionOption.FailIfExists);}
            catch {try{await temp.DeleteAsync();}catch{} throw;}
        }
        async Task Configure() { await Configure(Selected); }
        async Task Configure(ModEntry m) {if(m==null){status.Text=L.T("Selecione um mod na lista.");return;}await ModSettings.Show(m);status.Text=L.T("Configuração fechada: ")+m.Title;}
        void ShowInstall() {
            tab=1;HighlightTab(); body.Children.Clear();body.RowDefinitions.Clear();var panel=Stack();
            panel.Children.Add(Text(L.T("Instalar mods"),25));panel.Children.Add(Text(L.T("Cole um link de instalação do HedgeModManager ou informe os IDs do GameBanana. O download será instalado na pasta selecionada."),16));
            var link=new TextBox {Header=L.T("Link de instalação"),PlaceholderText="hedgemmswa:https://gamebanana.com/mmdl/…"};panel.Children.Add(link);
            var modId=new TextBox {Header=L.T("ID do mod (GameBanana)")};var fileId=new TextBox {Header=L.T("ID do arquivo (mmdl)")};panel.Children.Add(modId);panel.Children.Add(fileId);
            link.Text=installLinkText;modId.Text=installModText;fileId.Text=installFileText;
            link.TextChanged+=(s,e)=>installLinkText=link.Text;modId.TextChanged+=(s,e)=>installModText=modId.Text;fileId.TextChanged+=(s,e)=>installFileText=fileId.Text;
            panel.Children.Add(Action(L.T("Instalar"),async()=> {
                if(folder==null)throw new InvalidOperationException(L.T("Escolha a pasta em Meus mods primeiro."));
                QuickInstallEntry entry;string error; var input=link.Text.Trim();
                if(input=="") { if(!modId.Text.All(char.IsDigit)||modId.Text.Length==0||!fileId.Text.All(char.IsDigit)||fileId.Text.Length==0)throw new InvalidDataException(L.T("Informe os dois IDs numéricos.")); input="https://gamebanana.com/mmdl/"+fileId.Text+",Mod,"+modId.Text; }
                foreach(var prefix in new[]{"hedgemm://","hedgemm:","hedgemmswa://"}) if(input.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)) {input=input.Substring(prefix.Length);break;}
                if(!installer.TryParse(input,out entry,out error))throw new InvalidDataException(L.Service(error));
                if(!await CanReload())return;
                status.Text=L.T("Baixando e instalando…");var result=await installer.InstallAsync(entry,folder);await Load();status.Text=L.Service(result)+L.T(" Novos mods precisam ser ativados e salvos.");
            }));
            panel.Children.Add(Action(L.T("Explorar GameBanana"),()=> {ShowBrowser();return Task.CompletedTask;}));
            panel.Children.Add(Text(L.T("LB / RB alternam as telas. Use o direcional e A para navegar pelos botões."),14));
            body.Children.Add(new ScrollViewer {Content=panel});
        }
        void ShowBrowser() {
            body.Children.Clear();body.RowDefinitions.Clear();body.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});body.RowDefinitions.Add(new RowDefinition());
            var controls=Row();controls.Children.Add(Action(L.T("Voltar à instalação"),()=>{ShowInstall();return Task.CompletedTask;}));
            var web=new WebView();controls.Children.Add(Action(L.T("Voltar página"),()=>{if(web.CanGoBack)web.GoBack();return Task.CompletedTask;}));
            body.Children.Add(controls); Grid.SetRow(web,1);body.Children.Add(web);
            web.NavigationStarting+=(s,e)=>{if(e.Uri!=null && !(e.Uri.Scheme=="https"&&(e.Uri.Host=="gamebanana.com"||e.Uri.Host.EndsWith(".gamebanana.com")))) {e.Cancel=true;status.Text=L.T("Use o link de instalação na tela Instalar mods.");}};
            web.NavigationCompleted+=(s,e)=>{status.Text=e.IsSuccess?L.T("GameBanana aberto. Copie os IDs para instalar o mod."):L.T("Não foi possível abrir o GameBanana. Você ainda pode instalar usando um link ou IDs.");};
            web.Navigate(new Uri("https://gamebanana.com/mods/games/21975"));
        }
        void ShowUpdates() {
            tab=2;HighlightTab();body.Children.Clear();body.RowDefinitions.Clear();var panel=Stack();panel.Children.Add(Text(L.T("Atualizações dos mods"),25));
            var results=Stack();panel.Children.Add(Action(L.T("Verificar atualizações"),async()=>{
                if(folder==null)throw new InvalidOperationException(L.T("Escolha a pasta em Meus mods primeiro."));
                results.Children.Clear();int checkedMods=0;
                foreach(var m in mods.Where(m=>m.HasUpdateServer)) {
                    var update=await updater.CheckForUpdate(m);checkedMods++;
                    var card=Stack();card.Children.Add(Text(m.Title+" • "+L.Service(update.Status),18));card.Children.Add(Text(update.Changelog??"",14));
                    if(update.CanUpdate) card.Children.Add(Action(L.T("Atualizar ")+m.Title,async()=> {if(!await CanReload())return;status.Text=L.Service(await updater.ApplyUpdate(update));await Load();ShowUpdates();}));
                    results.Children.Add(card);
                }
                status.Text=checkedMods==0?L.T("Nenhum mod instalado informa um servidor de atualizações."):L.F("{0} mods verificados.",checkedMods);
            }));panel.Children.Add(results);body.Children.Add(new ScrollViewer {Content=panel});
        }
    }
}
