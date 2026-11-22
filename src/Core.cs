using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace HedgeModManager.Improved
{
    public sealed class ActionButtonLatch
    {
        readonly HashSet<int> held=new HashSet<int>();
        public bool Press(int button) {return button>0&&held.Add(button);}
        public void Release(int button) {held.Remove(button);}
        public void Reset() {held.Clear();}
    }
    public sealed class ShoulderNavigation
    {
        readonly HashSet<int> held=new HashSet<int>();
        public bool Press(int direction) {return (direction==1||direction==-1)&&held.Add(direction);}
        public void Release(int direction) {held.Remove(direction);}
        public void Reset() {held.Clear();}
        public static int Next(int tab,int direction) {return (tab+(direction>0?1:2))%3;}
    }
    public static class SchemaText
    {
        // PC schemas accept JSON comments and trailing commas. Keep quoted paths and URLs intact.
        public static string Normalize(string source) {
            var text=(source??"").TrimStart('\uFEFF');var output=new System.Text.StringBuilder();
            bool quoted=false,escape=false;
            for(int i=0;i<text.Length;i++) {
                char c=text[i];
                if(quoted) {output.Append(c);if(escape)escape=false;else if(c=='\\')escape=true;else if(c=='"')quoted=false;continue;}
                if(c=='"'){quoted=true;output.Append(c);continue;}
                if(c=='/'&&i+1<text.Length&&text[i+1]=='/') {while(i<text.Length&&text[i]!='\n')i++;output.Append('\n');continue;}
                if(c=='/'&&i+1<text.Length&&text[i+1]=='*') {
                    i+=2;while(i+1<text.Length&&!(text[i]=='*'&&text[i+1]=='/'))i++;
                    if(i+1>=text.Length)throw new InvalidDataException(L.T("Comentário JSON não terminado."));i++;output.Append(' ');continue;
                }
                output.Append(c);
            }
            text=output.ToString();output.Clear();quoted=false;escape=false;
            for(int i=0;i<text.Length;i++) {
                char c=text[i];
                if(quoted){output.Append(c);if(escape)escape=false;else if(c=='\\')escape=true;else if(c=='"')quoted=false;continue;}
                if(c=='"')quoted=true;
                if(c==','){int j=i+1;while(j<text.Length&&char.IsWhiteSpace(text[j]))j++;if(j<text.Length&&(text[j]==']'||text[j]=='}'))continue;}
                output.Append(c);
            }
            return output.ToString();
        }
        public static string Resolve(string root,string relative) {
            if(string.IsNullOrWhiteSpace(relative)||Path.IsPathRooted(relative)||relative.Contains(":"))throw new InvalidDataException(L.T("O arquivo deve ficar dentro da pasta do mod."));
            var full=Path.GetFullPath(Path.Combine(root,relative.Replace('/','\\')));
            if(!full.StartsWith(root.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(L.T("O arquivo está fora da pasta do mod."));
            return full;
        }
    }
    // Edits only the requested keys; comments, unknown sections and code selections survive.
    public sealed class IniDocument
    {
        readonly List<string> lines;
        public IniDocument(string text) { lines = (text ?? "").Replace("\r\n", "\n").Split('\n').ToList(); }
        static bool Section(string line, out string name) {
            var t = line.Trim(); name = "";
            if (!t.StartsWith("[") || !t.EndsWith("]")) return false;
            name = t.Substring(1, t.Length - 2).Trim(); return true;
        }
        static bool Key(string line, out string key) {
            key = ""; var t = line.Trim(); var i = t.IndexOf('=');
            if (i < 1 || t.StartsWith(";") || t.StartsWith("#")) return false;
            key = t.Substring(0, i).Trim(); return true;
        }
        public IEnumerable<KeyValuePair<string,string>> Entries(string group) {
            var section = "";
            foreach(var line in lines) {
                string s, k;
                if (Section(line, out s)) section = s;
                else if (string.Equals(section, group, StringComparison.OrdinalIgnoreCase) && Key(line, out k))
                    yield return new KeyValuePair<string,string>(k, line.Substring(line.IndexOf('=') + 1).Trim().Trim('"'));
            }
        }
        public string Get(string group, string key, string fallback = "") {
            var matches = Entries(group).Where(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)).ToList();
            return matches.Count == 0 ? fallback : matches.Last().Value;
        }
        public void Remove(string group, Func<string,bool> predicate) {
            var section = "";
            for (int i = 0; i < lines.Count; i++) {
                string s, k;
                if (Section(lines[i], out s)) section = s;
                else if (string.Equals(section, group, StringComparison.OrdinalIgnoreCase) && Key(lines[i], out k) && predicate(k)) lines.RemoveAt(i--);
            }
        }
        public void Set(string group, string key, string value) {
            if (key.IndexOfAny(new[]{'\r','\n','='}) >= 0 || group.IndexOfAny(new[]{'\r','\n',']'}) >= 0 || value.IndexOfAny(new[]{'\r','\n'}) >= 0)
                throw new InvalidDataException(L.T("Nome ou valor INI inválido."));
            Remove(group, k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            int index = -1;
            for(int i=0; i<lines.Count; i++) { string s; if(Section(lines[i],out s)) {
                if(index >= 0) break;
                if(string.Equals(s,group,StringComparison.OrdinalIgnoreCase)) { index = i+1; while(index<lines.Count && !Section(lines[index],out s)) index++; break; }
            }}
            if(index < 0) { lines.Add("["+group+"]"); index=lines.Count; }
            lines.Insert(index,key+"="+value);
        }
        public override string ToString() { return string.Join("\r\n",lines); }
    }
    public sealed class ModState { public string Path; public bool Enabled; }
    public static class ModOrder
    {
        public static bool TryMove<T>(IList<T> items,T selected,int delta) {
            if(delta!=-1&&delta!=1)return false;
            int index=items.IndexOf(selected),target=index+delta;
            if(index<0||target<0||target>=items.Count)return false;
            items.RemoveAt(index);items.Insert(target,selected);return true;
        }
        public static string Normalize(string p) { return (p??"").Trim('"').Replace('/','\\'); }
        public static bool Indexed(string key, string prefix) { int n; return key.StartsWith(prefix,StringComparison.OrdinalIgnoreCase) && int.TryParse(key.Substring(prefix.Length),out n) && n>=0; }
        public static List<string> Read(IniDocument db) {
            var all = db.Entries("HMMUwpOrder").Where(x=>Indexed(x.Key,"Mod")).OrderBy(x=>int.Parse(x.Key.Substring(3))).Select(x=>Normalize(x.Value)).ToList();
            var map = db.Entries("Mods").GroupBy(x=>x.Key,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.Last().Value,StringComparer.OrdinalIgnoreCase);
            var active = db.Entries("Main").Where(x=>Indexed(x.Key,"ActiveMod")).OrderBy(x=>int.Parse(x.Key.Substring(9))).Select(x=>map.ContainsKey(x.Value)?Normalize(map[x.Value]):"").Where(x=>x!="").ToList();
            // Active ordering may have been edited by the PC manager since our last save.
            if(all.Count == 0) return active;
            int a=0;
            for(int i=0;i<all.Count;i++) if(active.Contains(all[i],StringComparer.OrdinalIgnoreCase) && a<active.Count) all[i]=active[a++];
            return all.Concat(active).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        public static string Write(string existing, IList<ModState> mods) {
            var db = new IniDocument(existing);
            var old = db.Entries("Mods").GroupBy(x=>Normalize(x.Value),StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.First().Key,StringComparer.OrdinalIgnoreCase);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            db.Remove("Main", k=>Indexed(k,"ActiveMod") || k.Equals("ActiveMod",StringComparison.OrdinalIgnoreCase));
            db.Remove("HMMUwpOrder", k=>Indexed(k,"Mod"));
            db.Set("Main","ManifestVersion","1.1"); db.Set("Main","ReverseLoadOrder","0");
            db.Set("Main","ActiveModCount",mods.Count(x=>x.Enabled).ToString(CultureInfo.InvariantCulture));
            int active=0;
            for(int i=0;i<mods.Count;i++) {
                string id; var path=Normalize(mods[i].Path);
                if(!old.TryGetValue(path,out id) || !ids.Add(id)) { id=Guid.NewGuid().ToString(); ids.Add(id); }
                db.Set("Mods",id,"\""+path+"\""); db.Set("HMMUwpOrder","Mod"+i,"\""+path+"\"");
                if(mods[i].Enabled) db.Set("Main","ActiveMod"+(active++),"\""+id+"\"");
            }
            if(db.Get("Codes","CodeCount",null)==null) db.Set("Codes","CodeCount","0");
            return db.ToString();
        }
        public static string ValidateNumber(string text, bool integer, double? min, double? max) {
            double value;
            if(!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,out value) || double.IsNaN(value) || double.IsInfinity(value) || (integer && (value != Math.Truncate(value) || value<int.MinValue || value>int.MaxValue)))
                throw new InvalidDataException(integer?L.T("Informe um número inteiro válido."):L.T("Informe um número válido (use ponto para decimais)."));
            if((min.HasValue && value<min) || (max.HasValue && value>max)) throw new InvalidDataException(L.T("Valor fora do intervalo permitido."));
            return value.ToString("R",CultureInfo.InvariantCulture);
        }
    }
}
