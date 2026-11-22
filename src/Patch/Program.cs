using Mono.Cecil;
using Mono.Cecil.Cil;

if(args.Length!=3)throw new ArgumentException("original.exe improved.dll output.exe");
var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
using var original=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters {AssemblyResolver=resolver});
using var improved=AssemblyDefinition.ReadAssembly(args[1]);
var page=original.MainModule.GetType("HedgeModManager.Uwp.MainPage");
var ctor=page.Methods.Single(m=>m.IsConstructor&&!m.IsStatic&&m.Parameters.Count==0);
var baseCtor=(MethodReference)ctor.Body.Instructions.First(i=>i.OpCode==OpCodes.Call && i.Operand is MethodReference m && m.Name==".ctor" && m.DeclaringType.FullName=="Windows.UI.Xaml.Controls.Page").Operand;
var attach=improved.MainModule.GetType("HedgeModManager.Improved.Entry").Methods.Single(m=>m.Name=="Attach");
ctor.Body=new MethodBody(ctor);var il=ctor.Body.GetILProcessor();
il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Call,baseCtor);il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Call,original.MainModule.ImportReference(attach));il.Emit(OpCodes.Ret);
var app=original.MainModule.GetType("HedgeModManager.Uwp.App");
var appCtor=app.Methods.Single(m=>m.IsConstructor&&!m.IsStatic&&m.Parameters.Count==0);
var initialize=improved.MainModule.GetType("HedgeModManager.Improved.Entry").Methods.Single(m=>m.Name=="InitializeApplication");
var appIl=appCtor.Body.GetILProcessor();
foreach(var ret in appCtor.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToList()) {
    appIl.InsertBefore(ret,appIl.Create(OpCodes.Ldarg_0));
    appIl.InsertBefore(ret,appIl.Create(OpCodes.Call,original.MainModule.ImportReference(initialize)));
}
original.Write(args[2]);
Console.WriteLine("MainPage: interface atualizada. App: política de entrada configurada no construtor, antes do lançamento.");
