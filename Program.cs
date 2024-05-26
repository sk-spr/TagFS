using System;
using System.Text;
using TagFS.FileSystem;
using Tmds.Fuse;
using Tmds.Linux;
using static Tmds.Linux.LibC;

if (!Fuse.CheckDependencies())
{
    Console.WriteLine(Fuse.InstallationInstructions);
    return;
}
Console.WriteLine("Mounting");
using (var mount = Fuse.Mount(args[0], new TagFileSystem(args[1])))
{
    Console.WriteLine("Mounted.");
    await mount.WaitForUnmountAsync();
    Console.WriteLine("Unmounted.");
}
