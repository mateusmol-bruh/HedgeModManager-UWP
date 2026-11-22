using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HedgeModManager.Uwp.Models;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HedgeModManager.Improved
{
    internal static class ModSettings
    {
        static IJsonValue Find(JsonObject obj,string name) { foreach(var p in obj)if(p.Key.Equals(name,StringComparison.OrdinalIgnoreCase))return p.Value;return null; }
        static string Scalar(IJsonValue val,string fallback="") {
            if(val==null||val.ValueType==JsonValueType.Null)return fallback;
            if(val.ValueType==JsonValueType.String)return val.GetString();
            if(val.ValueType==JsonValueType.Boolean)return val.GetBoolean()?"true":"false";
            if(val.ValueType==JsonValueType.Number)return val.GetNumber().ToString("R",CultureInfo.InvariantCulture);
            return fallback;
        }
        static string Str(JsonObject obj,string key,string fallback="") {return Scalar(Find(obj,key),fallback);}
        static JsonArray Array(JsonObject obj,string key) {var v=Find(obj,key);return v!=null&&v.ValueType==JsonValueType.Array?v.GetArray():new JsonArray();}
        static string Description(JsonObject obj) {var v=Find(obj,"Description");return v!=null&&v.ValueType==JsonValueType.Array?string.Join("\n",v.GetArray().Select(x=>Scalar(x))):Scalar(v);}
        static double? Limit(JsonObject obj,string key) {double n;return double.TryParse(Str(obj,key),NumberStyles.Float,CultureInfo.InvariantCulture,out n)?(double?)n:null;}
        static string SafeRelative(string root,string path) {return SchemaText.Resolve(root,path);}
        public static async Task Show(ModEntry mod) {
            var ini=new IniDocument(await ManagerPage.Read(mod.Folder,"mod.ini"));
            var schemaName=ini.Get("Main","ConfigSchemaFile");
            if(schemaName!="") {
                var schemaPath=SafeRelative(mod.Folder.Path,schemaName);
                var schemaFolder=await GetTarget(mod.Folder,schemaPath,false);
                if(schemaFolder==null)throw new FileNotFoundException(L.T("Pasta do esquema não encontrada: ")+schemaName);
                var schemaFile=await schemaFolder.TryGetItemAsync(Path.GetFileName(schemaPath)) as StorageFile;
                if(schemaFile==null)throw new FileNotFoundException(L.T("O mod declara um esquema que não foi encontrado: ")+schemaName);
                JsonObject schema;
                if(!JsonObject.TryParse(SchemaText.Normalize(await FileIO.ReadTextAsync(schemaFile)),out schema))throw new InvalidDataException(L.T("O esquema de configuração não contém JSON válido: ")+schemaName);
                await SchemaEditor(mod,schema);return;
            }
            var files=(await mod.Folder.GetFilesAsync()).Where(f=>f.FileType.Equals(".ini",StringComparison.OrdinalIgnoreCase)&&!f.Name.Equals("mod.ini",StringComparison.OrdinalIgnoreCase)).ToList();
            if(files.Count==0) {await new ContentDialog {Title=mod.Title,Content=L.T("Este mod não fornece um esquema de configuração nem um arquivo INI editável. Mods sem opções próprias não têm configurações para ajustar."),CloseButtonText=L.T("Fechar"),RequestedTheme=ElementTheme.Dark}.ShowAsync();return;}
            StorageFile target=files[0];
            if(files.Count>1) {
                var chooser=new ComboBox {Header=L.T("Arquivo de configuração"),ItemsSource=files.Select(f=>f.Name).ToList(),SelectedIndex=0,MinWidth=280};
                if(await new ContentDialog {Title=L.T("Configurar ")+mod.Title,Content=chooser,PrimaryButtonText=L.T("Abrir"),CloseButtonText=L.T("Cancelar"),RequestedTheme=ElementTheme.Dark}.ShowAsync()!=ContentDialogResult.Primary)return;
                target=files[chooser.SelectedIndex];
            }
            await IniEditor(mod,target);
        }
        static ContentDialog Dialog(string title,StackPanel panel,TextBlock error) {
            panel.Children.Add(error);
            return new ContentDialog {Title=title,FontSize=20,Content=new ScrollViewer {Content=panel,MaxHeight=Math.Max(180,Window.Current.Bounds.Height-190),HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},PrimaryButtonText=L.T("Salvar configurações"),CloseButtonText=L.T("Cancelar"),RequestedTheme=ElementTheme.Dark};
        }
        static async Task SchemaEditor(ModEntry mod,JsonObject schema) {
            var path=SafeRelative(mod.Folder.Path,Str(schema,"IniFile","config.ini"));
            var target=await GetTarget(mod.Folder,path,false);
            var original=target==null?"":await ManagerPage.Read(target,Path.GetFileName(path));
            var document=new IniDocument(original);
            var panel=new StackPanel {Spacing=12,MinWidth=280};
            panel.Children.Add(new TextBlock {Text=L.T("Ajustes de ")+mod.Title+" • "+Path.GetFileName(path),TextWrapping=TextWrapping.Wrap});
            var error=new TextBlock {TextWrapping=TextWrapping.Wrap};
            var commits=new List<Action>();
            var enumsValue=Find(schema,"Enums");var enums=enumsValue!=null&&enumsValue.ValueType==JsonValueType.Object?enumsValue.GetObject():new JsonObject();
            foreach(var groupValue in Array(schema,"Groups")) {
                var group=groupValue.GetObject();var groupName=Str(group,"Name");
                panel.Children.Add(new TextBlock {Text=Str(group,"DisplayName",groupName),FontSize=20,Margin=new Thickness(0,12,0,0)});
                foreach(var elementValue in Array(group,"Elements")) {
                    var element=elementValue.GetObject();var name=Str(element,"Name");var label=Str(element,"DisplayName",name);var type=Str(element,"Type","string");
                    var fallback=Scalar(Find(element,"DefaultValue"),Scalar(Find(element,"Value")));
                    var current=document.Get(groupName,name,fallback);
                    var enumValue=Find(enums,type);
                    if(type.Equals("bool",StringComparison.OrdinalIgnoreCase)) {
                        var toggle=new ToggleSwitch {Header=label,FontSize=20,IsOn=current.Equals("true",StringComparison.OrdinalIgnoreCase)||current=="1",OnContent=L.T("Ativado"),OffContent=L.T("Desativado")};panel.Children.Add(toggle);
                        commits.Add(()=>document.Set(groupName,name,toggle.IsOn?"true":"false"));
                    } else if(enumValue!=null&&enumValue.ValueType==JsonValueType.Array) {
                        var choices=enumValue.GetArray().Select(v=>v.GetObject()).ToList();
                        var options=choices.Select(v=>Str(v,"DisplayName",Str(v,"Value"))).ToList();
                        int selected=choices.FindIndex(v=>Str(v,"Value")==current);
                        // Preserve custom values absent from an older schema until explicitly changed.
                        if(selected<0){choices.Add(null);options.Add(L.T("Valor atual: ")+current);selected=options.Count-1;}
                        var combo=new ComboBox {Header=label,FontSize=20,ItemsSource=options,SelectedIndex=selected,HorizontalAlignment=HorizontalAlignment.Stretch};panel.Children.Add(combo);
                        commits.Add(()=>document.Set(groupName,name,choices[combo.SelectedIndex]==null?current:Str(choices[combo.SelectedIndex],"Value")));
                    } else {
                        var min=Limit(element,"MinValue");var max=Limit(element,"MaxValue");
                        var numeric=type.Equals("int",StringComparison.OrdinalIgnoreCase)||type.Equals("float",StringComparison.OrdinalIgnoreCase)||type.Equals("double",StringComparison.OrdinalIgnoreCase);
                        var box=new TextBox {Header=label,Text=current,FontSize=20};panel.Children.Add(box);
                        if(numeric&&(min.HasValue||max.HasValue))panel.Children.Add(new TextBlock {Text=L.T("Intervalo: ")+(min?.ToString(CultureInfo.InvariantCulture)??L.T("sem mínimo"))+L.T(" a ")+(max?.ToString(CultureInfo.InvariantCulture)??L.T("sem máximo")),FontSize=13});
                        commits.Add(()=> {try {document.Set(groupName,name,numeric?ModOrder.ValidateNumber(box.Text,type.Equals("int",StringComparison.OrdinalIgnoreCase),min,max):box.Text);}catch(Exception ex){throw new InvalidDataException(label+": "+ex.Message);}});
                    }
                    var desc=Description(element);if(desc!="")panel.Children.Add(new TextBlock {Text=desc,TextWrapping=TextWrapping.Wrap,FontSize=17});
                }
            }
            if(commits.Count==0)throw new InvalidDataException(L.T("O esquema não fornece opções editáveis."));
            var dialog=Dialog(L.T("Configurar mod"),panel,error);
            dialog.PrimaryButtonClick+=async(s,e)=> {
                var deferral=e.GetDeferral();
                try {
                    foreach(var commit in commits)commit();
                    var output=document.ToString();
                    // Check external edits before changing anything on disk.
                    var currentTarget=await GetTarget(mod.Folder,path,false);
                    var now=currentTarget==null?"":await ManagerPage.Read(currentTarget,Path.GetFileName(path));
                    if(now!=original)throw new IOException(L.T("O arquivo mudou desde que foi aberto. Cancele e abra a configuração novamente."));
                    var destination=await GetTarget(mod.Folder,path,true);await ManagerPage.Write(destination,Path.GetFileName(path),output);
                }catch(Exception ex){e.Cancel=true;error.Text=ex.Message;}finally{deferral.Complete();}
            };
            await dialog.ShowAsync();
        }
        static async Task<StorageFolder> GetTarget(StorageFolder root,string path,bool create) {
            var relative=Path.GetDirectoryName(path).Substring(root.Path.Length).Trim('\\');var result=root;
            foreach(var part in relative.Split(new[]{'\\'},StringSplitOptions.RemoveEmptyEntries)) {
                if(create)result=await result.CreateFolderAsync(part,CreationCollisionOption.OpenIfExists);
                else {var item=await result.TryGetItemAsync(part);if(item==null)return null;result=item as StorageFolder;if(result==null)throw new IOException(L.T("Esperava uma pasta: ")+part);}
            }
            return result;
        }
        static async Task IniEditor(ModEntry mod,StorageFile file) {
            var original=await FileIO.ReadTextAsync(file);var panel=new StackPanel {Spacing=10,MinWidth=280};
            panel.Children.Add(new TextBlock {Text=L.F("Editor avançado de {0}. Preserve os nomes das seções e opções fornecidas pelo autor.",file.Name),TextWrapping=TextWrapping.Wrap});
            var editor=new TextBox {Text=original,AcceptsReturn=true,TextWrapping=TextWrapping.NoWrap,MinHeight=220,MaxHeight=400};panel.Children.Add(editor);
            var error=new TextBlock {TextWrapping=TextWrapping.Wrap};var dialog=Dialog(L.T("Configurar ")+mod.Title,panel,error);
            dialog.PrimaryButtonClick+=async(s,e)=>{var d=e.GetDeferral();try {
                if(await FileIO.ReadTextAsync(file)!=original)throw new IOException(L.T("O arquivo mudou. Cancele e abra novamente."));
                await ManagerPage.Write(await file.GetParentAsync(),file.Name,editor.Text);
            }catch(Exception ex){e.Cancel=true;error.Text=ex.Message;}finally{d.Complete();}};
            await dialog.ShowAsync();
        }
    }
}
