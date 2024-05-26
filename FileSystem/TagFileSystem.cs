using System.Text;
using TagFS.Backend;
using TagFS.Backend.SQLiteTagger;
using Tmds.Fuse;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace TagFS.FileSystem;

public class TagFileSystem : FuseFileSystemBase
{
    private ITagProvider TagProvider;
    private string[] specialPaths = new[] { "/by-tag", "/by-class", "/by-alias" };
    private int[] specialPathNodes = new[] { 0, 0, 0 };
    public TagFileSystem(string dbPath)
    {
        TagProvider = new SQLiteTagProvider();
        ((SQLiteTagProvider)TagProvider).dbFileName = dbPath;
        TagProvider.Initialize();
        specialPathNodes[0] = TagProvider.GetAllTags().Count(t => t.ParentTag == null);
    }

    public override int GetAttr(ReadOnlySpan<byte> path, ref stat stat, FuseFileInfoRef fiRef)
    {
        if (path.SequenceEqual(RootPath))
        {
            stat.st_mode = S_IFDIR | 0b111_101_101;
            var rootTags = 
                from tag in TagProvider.GetAllTags()
                where tag.ParentTag == null 
                select tag;
            stat.st_nlink = (ulong) rootTags.Count() + 2;
            return 0;
        }

        var strP = Encoding.UTF8.GetString(path);
        if (specialPaths.Any(p => strP == p))
        {
            stat.st_mode = S_IFDIR | 0b111_101_101;
            stat.st_nlink = 2 + (uint)specialPathNodes[Array.IndexOf(specialPaths, strP)];
            return 0;
        }

        if (strP.StartsWith("/by-tag/"))
        {
            if (TagProvider.GetFullTag(strP.Remove(0, 8)) == null) return -ENOENT;
            stat.st_mode = S_IFDIR | 0b111_101_101;
            stat.st_nlink = 2; //+ (uint)TagProvider.GetChildTags(strP.Remove(0, 8)).Length;
            return 0;
        }
        return -ENOENT;
    }

    public override int ReadDir(ReadOnlySpan<byte> path, ulong offset, ReadDirFlags flags, DirectoryContent content, ref FuseFileInfo fi)
    {
        var utfPath = System.Text.Encoding.UTF8.GetString(path);
        Console.WriteLine(utfPath);
        content.AddEntry(".");
        content.AddEntry("..");
        if (path.SequenceEqual(RootPath))
        {
            content.AddEntry("by-tag");
            content.AddEntry("by-class");
            content.AddEntry("by-alias");
        } else if (utfPath.StartsWith("/by-tag"))
        {
            var tagFqn = utfPath.Remove(0, 7);
            if(tagFqn.EndsWith("/"))
                tagFqn = tagFqn.Remove(tagFqn.Length - 1);
            Console.WriteLine("Listing tags at \"" + tagFqn+"\"");
            if (tagFqn == "")
            {
                var allRootTags = TagProvider.GetAllTags();//.Where(t => t.ParentTag == null);
                Console.WriteLine($"Listing {allRootTags.Count()} root tags.");
                foreach (var rootTag in allRootTags)
                {
                    Console.WriteLine(rootTag.FullyQualifiedName);
                    content.AddEntry(rootTag.FullyQualifiedName);
                }

                return 0;
            }

            tagFqn = tagFqn.Remove(0, 1);
            Console.WriteLine("Finding child tags at " +tagFqn);
            var childTags = TagProvider.GetChildTags(tagFqn);
            Console.WriteLine($"Got {childTags.Length} child tags of {tagFqn}");
            foreach (var childTag in childTags)
            {
                Console.WriteLine(childTag.FullyQualifiedName);
                content.AddEntry(childTag.FullyQualifiedName.Split("/")[^1]);
            }

            return 0;
        }

        return 0;
    }

    public override int MkDir(ReadOnlySpan<byte> path, mode_t mode)
    {
        Console.WriteLine("MKDIR CALLED");
        var utfPath = Encoding.UTF8.GetString(path);
        Console.WriteLine("trying to make " + utfPath);
        Console.WriteLine(mode);
        if (utfPath.StartsWith("/by-tag/")) // create a new tag
        {
            var tagPath = utfPath.Remove(0, 8);
            if (TagProvider.GetFullTag(tagPath) != null)
                return -ENOENT;
            var pathParts = tagPath.Split("/");
            var success = TagProvider.RegisterTag(tagPath,
                pathParts.Length > 1 ? TagProvider.GetFullTag(GetParentPath(pathParts)) : null, Array.Empty<string>());
            if (success) return 0;
            return -ENOENT;
        }
        return -ENOENT;
    }

    static string GetParentPath(string[] parts)
    {
        var allButLast = parts.Take(parts.Length - 1).ToArray();
        return String.Join("/", allButLast);
    }

    public override int OpenDir(ReadOnlySpan<byte> path, ref FuseFileInfo fi)
    {
        var utfPath = Encoding.UTF8.GetString(path);
        if (path.SequenceEqual(RootPath))
            return 0;
        Console.WriteLine("OPENDIR CALLED!" + utfPath);
        if (utfPath.StartsWith("/by-tag"))
        {
            var tagPath = utfPath.Remove(0, 7);
            Console.WriteLine("tag path "+tagPath);
            return 0;
        }

        return -ENOENT;
    }
}