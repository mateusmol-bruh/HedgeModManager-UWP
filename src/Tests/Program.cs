using HedgeModManager.Improved;
using System.Globalization;
var passed=0;
void Check(bool b,string label){if(!b)throw new Exception(label);passed++;Console.WriteLine("PASS "+label);}
void Reject(Action a,string label){try{a();}catch(InvalidDataException){Check(true,label);return;}throw new Exception(label);}
var input="; comentário\r\n[Main]\r\nManifestVersion=1.1\r\nActiveModCount=2\r\nActiveMod0=\"b\"\r\nActiveMod1=\"a\"\r\nFavoriteModCount=1\r\nFavoriteMod0=\"a\"\r\nCustom=keep\r\n[Mods]\r\na=\"E:\\mods\\A\\mod.ini\"\r\nb=\"E:\\mods\\B\\mod.ini\"\r\n[Codes]\r\nCodeCount=1\r\nCode0=\"TestCode\"\r\n[Other]\r\nUntouched=yes\r\n";
var states=new List<ModState>{new(){Path=@"E:\mods\A\mod.ini",Enabled=true},new(){Path=@"E:\mods\Off\mod.ini",Enabled=false},new(){Path=@"E:\mods\B\mod.ini",Enabled=true}};
Check(ModOrder.Read(new IniDocument(input))[0]==states[2].Path,"restore existing loader order");
var output=ModOrder.Write(input,states);var ini=new IniDocument(output);
Check(ini.Get("Main","ActiveMod0")=="a"&&ini.Get("Main","ActiveMod1")=="b","priority changes loader ActiveMod indices");
Check(ini.Get("Main","ActiveModCount")=="2","disabled mods excluded");
Check(ModOrder.Read(ini).SequenceEqual(states.Select(s=>s.Path)),"all priorities survive reload including inactive mods");
Check(ini.Get("Codes","Code0")=="TestCode"&&ini.Get("Codes","CodeCount")=="1","code selections preserved");
Check(ini.Get("Main","FavoriteMod0")=="a"&&ini.Get("Main","Custom")=="keep","favorites and custom keys preserved");
Check(output.Contains("; comentário")&&ini.Get("Other","Untouched")=="yes","comments and unknown sections preserved");
ini.Set("Main","ActiveMod0","\"b\"");ini.Set("Main","ActiveMod1","\"a\"");
Check(ModOrder.Read(ini).Where(p=>p!=states[1].Path).SequenceEqual(new[]{states[2].Path,states[0].Path}),"external PC priority edits respected");
var zero=new IniDocument(ModOrder.Write(output,new List<ModState>()));
Check(zero.Get("Main","ActiveModCount")=="0"&&!zero.Entries("Main").Any(x=>ModOrder.Indexed(x.Key,"ActiveMod")),"empty list removes stale active entries");
var doc=new IniDocument("[Config]\r\nSpeed=1\r\nUnknown=yes\r\n; leave me\r\n[Else]\r\nValue=x");doc.Set("Config","Speed","2");
Check(doc.Get("Config","Speed")=="2"&&doc.Get("Config","Unknown")=="yes"&&doc.ToString().Contains("; leave me"),"individual config edits preserve unrelated values");
Reject(()=>doc.Set("Config","X","a\nInjected=true"),"reject line injection");
CultureInfo.CurrentCulture=new CultureInfo("pt-BR");Check(ModOrder.ValidateNumber("1.25",false,0,2)=="1.25","decimal serialization independent of locale");
Reject(()=>ModOrder.ValidateNumber("2.2",true,null,null),"reject fractional integer");Reject(()=>ModOrder.ValidateNumber("99999999999",true,null,null),"reject integer overflow");
Reject(()=>ModOrder.ValidateNumber("NaN",false,null,null),"reject NaN");Reject(()=>ModOrder.ValidateNumber("3",false,0,2),"reject out of range");
var duplicates=new IniDocument("[Config]\r\nSpeed=1\r\nSpeed=2");duplicates.Set("Config","Speed","3");Check(duplicates.Entries("Config").Count()==1,"replace duplicate keys without ambiguity");
var navigation=new ShoulderNavigation();
Check(ShoulderNavigation.Next(0,1)==1&&ShoulderNavigation.Next(1,1)==2&&ShoulderNavigation.Next(2,1)==0,"RB cycles all tabs including wraparound");
Check(ShoulderNavigation.Next(0,-1)==2&&ShoulderNavigation.Next(2,-1)==1&&ShoulderNavigation.Next(1,-1)==0,"LB cycles all tabs in reverse");
Check(navigation.Press(1)&&!navigation.Press(1),"held shoulder changes tab once");navigation.Release(1);Check(navigation.Press(1),"shoulder works again after release");
navigation.Reset();Check(navigation.Press(1)&&navigation.Press(-1),"activation reset and both shoulder directions");
var permissive="\uFEFF{\"Url\":\"https://example.com/a,b\", // comment\n \"Path\":\"C:\\\\mods\\\\test\",\"Groups\":[{\"Name\":\"main\",},], /* tail */ }";
using(var json=System.Text.Json.JsonDocument.Parse(SchemaText.Normalize(permissive))) {
    Check(json.RootElement.GetProperty("Url").GetString()=="https://example.com/a,b","schema comments preserve URLs and commas inside strings");
    Check(json.RootElement.GetProperty("Groups")[0].GetProperty("Name").GetString()=="main","schema trailing commas and BOM match PC compatibility");
    Check(json.RootElement.GetProperty("Path").GetString()==@"C:\mods\test","schema escaped backslashes preserved");
}
Reject(()=>SchemaText.Normalize("{/* not closed"),"reject unterminated JSON comment");
Check(SchemaText.Resolve(@"C:\mods\Example","mod.ini")==@"C:\mods\Example\mod.ini","schema target mod.ini is supported");
Reject(()=>SchemaText.Resolve(@"C:\mods\Example",@"..\Other\mod.ini"),"schema cannot escape mod folder");
Reject(()=>SchemaText.Resolve(@"C:\mods\Example",@"C:\other\mod.ini"),"schema cannot target absolute paths");
// Optional real-world regression fixture supplied at test time; user files are only read.
if(args.Length==2) {
    using var schema=System.Text.Json.JsonDocument.Parse(SchemaText.Normalize(File.ReadAllText(args[0])));
    var modText=File.ReadAllText(args[1]);var actual=new IniDocument(modText);
    var iniFile=schema.RootElement.GetProperty("IniFile").GetString();
    Check(iniFile=="mod.ini","Riley schema edits mod.ini rather than a separate config.ini");
    var group=schema.RootElement.GetProperty("Groups")[0];var groupName=group.GetProperty("Name").GetString();
    Check(groupName.Equals("Main",StringComparison.OrdinalIgnoreCase)&&group.GetProperty("Elements").GetArrayLength()==10,"Riley exposes all ten character options");
    var element=group.GetProperty("Elements")[0];var key=element.GetProperty("Name").GetString();
    var enums=schema.RootElement.GetProperty("Enums").GetProperty(element.GetProperty("Type").GetString());
    var replacement=enums[1].GetProperty("Value").GetString();actual.Set(groupName,key,replacement);
    Check(actual.Get("Main",key)==replacement,"Riley selected enum is written into loader IncludeDir entry");
    var changed=new IniDocument(actual.ToString());
    Check(changed.Get("Main",key)==replacement&&changed.Get("Main","IncludeDirCount")=="10","Riley selection survives readback and include count is preserved");
    var before=new IniDocument(modText);
    Check(before.Entries("Main").Where(p=>!p.Key.Equals(key,StringComparison.OrdinalIgnoreCase)).All(p=>changed.Get("Main",p.Key)==p.Value),"Riley all other Main options preserved");
    Check(before.Entries("Desc").All(p=>changed.Get("Desc",p.Key)==p.Value),"Riley description and author metadata preserved");
}
var actions=new ActionButtonLatch();
Check(actions.Press(1)&&!actions.Press(1),"X is not dispatched twice by window and controller polling");
actions.Release(1);Check(actions.Press(1),"X can open another configuration after release");
Check(actions.Press(2)&&!actions.Press(2),"held Y toggles only once");actions.Release(2);Check(actions.Press(2),"Y toggles again after release");
actions.Reset();Check(actions.Press(1)&&actions.Press(2),"focus reset releases X and Y independently");
var activationStates=new List<ModState>{new(){Path=@"E:\mods\First\mod.ini",Enabled=false},new(){Path=@"E:\mods\Second\mod.ini",Enabled=true}};
activationStates[0].Enabled=!activationStates[0].Enabled;
var activated=new IniDocument(ModOrder.Write(input,activationStates));
Check(activated.Get("Main","ActiveModCount")=="2"&&activated.Get("Mods",activated.Get("Main","ActiveMod0"))==activationStates[0].Path,"activating a row persists in first priority position");
activationStates[0].Enabled=!activationStates[0].Enabled;
var deactivated=new IniDocument(ModOrder.Write(activated.ToString(),activationStates));
Check(deactivated.Get("Main","ActiveModCount")=="1"&&deactivated.Get("Mods",deactivated.Get("Main","ActiveMod0"))==activationStates[1].Path,"deactivating a row removes it from game active list");
Check(ModOrder.Read(deactivated).SequenceEqual(activationStates.Select(m=>m.Path))&&deactivated.Get("Codes","Code0")=="TestCode","activation changes preserve priority and code selections");
var priorityStates=new List<ModState>{new(){Path=@"E:\mods\A\mod.ini",Enabled=true},new(){Path=@"E:\mods\B\mod.ini",Enabled=false},new(){Path=@"E:\mods\C\mod.ini",Enabled=true}};
var chosen=priorityStates[2];
Check(!ModOrder.TryMove(priorityStates,priorityStates[0],-1)&&!ModOrder.TryMove(priorityStates,chosen,1),"priority shortcuts respect top and bottom boundaries");
Check(ModOrder.TryMove(priorityStates,chosen,-1)&&ReferenceEquals(priorityStates[1],chosen),"LT raises the same selected mod by one position");
Check(ModOrder.TryMove(priorityStates,chosen,1)&&ReferenceEquals(priorityStates[2],chosen),"RT lowers the same selected mod by one position");
Check(!ModOrder.TryMove(priorityStates,new ModState(),-1),"missing selection cannot move a different mod");
ModOrder.TryMove(priorityStates,chosen,-1);
var reordered=new IniDocument(ModOrder.Write("",priorityStates));
Check(ModOrder.Read(reordered).SequenceEqual(priorityStates.Select(m=>m.Path)),"shortcut priority survives save and reload including inactive mods");
Check(reordered.Get("Main","ActiveModCount")=="2"&&reordered.Get("Mods",reordered.Get("Main","ActiveMod1"))==chosen.Path,"priority changes preserve activation and game loader order");
Check(L.Current=="en"&&L.T("Salvar alterações")=="Save changes","English is the default application language");
Check(L.Codes.SequenceEqual(new[]{"en","es","pt-BR","it","fr"}),"all five requested languages are available");
Check(L.Catalog.Count>=100&&L.Catalog.Values.All(v=>v.Length==5&&v.All(s=>!string.IsNullOrWhiteSpace(s))),"every application message has five nonempty translations");
bool formatsValid=true;
foreach(var code in L.Codes) {
    L.Select(code);
    foreach(var entry in L.Catalog) {
        try {var rendered=L.F(entry.Key,1,2,3,4,5,6,7,8);if(rendered.Length==0)formatsValid=false;}catch{formatsValid=false;}
        var expected=System.Text.RegularExpressions.Regex.Matches(entry.Key,@"\{\d+\}").Select(m=>m.Value).OrderBy(x=>x);
        var actual=System.Text.RegularExpressions.Regex.Matches(L.T(entry.Key),@"\{\d+\}").Select(m=>m.Value).OrderBy(x=>x);
        if(!expected.SequenceEqual(actual))formatsValid=false;
    }
}
Check(formatsValid,"translated templates preserve arguments and format correctly in every language");
L.Select("es");Check(L.T("Meus mods")=="Mis mods","runtime language switching updates interface labels");
L.Select("fr");Check(L.Service(@"Installed to E:\mods\My Mod")==@"Installé dans E:\mods\My Mod","translated service messages preserve file paths");
Check(L.T("Riley's Character Edits")=="Riley's Character Edits","mod authors' content is not translated or modified");
L.Select("invalid");Check(L.Current=="en","invalid saved language falls back to English");
L.Select(null);Check(L.Current=="en","missing saved language starts in English");
Console.WriteLine($"{passed} checks passed.");
