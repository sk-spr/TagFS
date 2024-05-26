using System.Data.SQLite;
using TagFS.Types;

namespace TagFS.Backend.SQLiteTagger;

public class SQLiteTagProvider : ITagProvider
{
    public string dbFileName;
    private SQLiteConnection? _sqlConn;
    public bool Initialize()
    {
        var isFirstTime = !File.Exists(dbFileName);
        _sqlConn = new($"Data Source={dbFileName};Version=3;{
            (isFirstTime ? "New=True;" : "")}Compress=True;");
        try
        {
            _sqlConn.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error opening SQLite Connection: " + e.Message);
            return false;
        }

        if (!isFirstTime)
            return true;
        
        Console.WriteLine("Performing first time setup...");
        try
        {
            Setup("db_setup.sql");
        }
        catch (IOException e)
        {
            Console.WriteLine("Failed to perform first time setup! Failed with: " + e);
            return false;
        }
        return true;
    }

    public bool AddTestData()
    {
        return Setup("testdata_setup.sql");
    }

    private bool Setup(string filename)
    {
        //set up command
        var command = _sqlConn?.CreateCommand();
        if (command is null)
            throw new IOException("Could not get SQL console!");
        //read in source sql
        using var reader = File.Open(Path.Combine("SQL", filename), FileMode.Open);
        var fullText = new byte[reader.Length];
        int n = reader.Read(fullText);
        if (n < reader.Length)
            throw new IOException("SQL File reader for " + filename + " did not read the entire file! Should read " 
                                  + reader.Length + " bytes, did read " + n);
        //and run it
        command.CommandText = System.Text.Encoding.UTF8.GetString(fullText);
        command.ExecuteNonQuery();
        return true;
    }

    public bool Stop()
    {
        throw new NotImplementedException();
    }

    public TagomatTag? GetTagByAlias(string alias)
    {
        var comm = _sqlConn?.CreateCommand();
        if (comm == null)
            return null;
        comm.CommandText =
            $"SELECT t.tagname FROM tags t JOIN tagaliases ta ON ta.tagid = t.tagid WHERE ta.alias = '{alias}';";
        var result = comm.ExecuteScalar();
        if (result == null)
            return null;
        var tagName = (string)result;
        return GetFullTag(tagName);
    }

    public TagomatTag? GetFullTag(string qualifiedTagName)
    {
        var comm = _sqlConn.CreateCommand();
        if (comm == null) return null;
        comm.CommandText =
            $"SELECT t.tagname, t.parent, t.ismeta\nFROM tags t\nWHERE t.tagname = '{qualifiedTagName}';";
        var reader = comm.ExecuteReader();
        if (!reader.Read())
            return null;
        var fqName = reader.GetString(0);
        var meta = reader.GetBoolean(2);
        var hasPar = !reader.IsDBNull(1);
        var parId = hasPar ? reader.GetInt32(1) : 0;
        reader.Dispose();
        string? parent = null;
        if (hasPar)
        {
            comm.CommandText = $"SELECT t.tagname FROM tags t WHERE t.tagid = {parId};"; 
            var result = comm.ExecuteScalar();
            if (result != null)
                parent = (string)result;
        }
        return new()
        {
            FullyQualifiedName = fqName,
            ParentTag = parent,
            IsMeta = meta
        };
    }

    public TagomatTag[] GetAllTags()
    {
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $"SELECT t1.tagname, t2.tagname, t1.ismeta FROM tags t1 LEFT JOIN tags t2 ON t2.tagid = t1.parent;";
        var reader = comm.ExecuteReader();
        List<TagomatTag> tags = new();
        while (reader.Read())
        {
            tags.Add(new()
            {
                FullyQualifiedName = reader.GetString(0),
                ParentTag = reader.IsDBNull(1)? null : reader.GetString(1),
                IsMeta = reader.GetBoolean(2)
            });
        }

        return tags.ToArray();
    }

    public TagomatTag[] GetTagsByFile(TagomatFile file)
    {
        var comm = _sqlConn.CreateCommand();
        comm.CommandText =
            $@"SELECT t.tagname, t2.tagname, t.ismeta
                FROM files f 
                LEFT JOIN filetags f2 ON f2.fileid = f.fileid 
                LEFT JOIN tags t ON t.tagid = f2.tagid
                LEFT JOIN tags t2 ON t2.tagid = t.parent
                WHERE f.fileid='{file.Id}' AND t.tagname IS NOT NULL;";
        var reader = comm.ExecuteReader();
        List<TagomatTag> tags = new();
        while (reader.Read())
            tags.Add(new()
            {
                FullyQualifiedName = reader.GetString(0),
                ParentTag = reader.IsDBNull(1) ? null : reader.GetString(1),
                IsMeta = reader.GetBoolean(2)
            });
        return tags.ToArray();
    }

    public TagomatClass[] GetAllClasses()
    {
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $"SELECT classid, name, description FROM classes;";
        var classIdReader = comm.ExecuteReader();
        List<(Int64 id, string name, string description)> classes = new();
        while(classIdReader.Read())
            classes.Add((classIdReader.GetInt64(0), classIdReader.GetString(1), classIdReader.GetString(2)));
        classIdReader.Close();
        List<TagomatClass> result = new();
        foreach (var tClass in classes)
        {
            comm.CommandText = $"SELECT e.extension FROM extclasses ec JOIN extensions e ON ec.extension = e.extid WHERE ec.classid={tClass.id};";
            var extReader = comm.ExecuteReader();
            List<string> extensions = new();
            while(extReader.Read())
                extensions.Add(extReader.GetString(0));
            result.Add(new()
            {
                Name = tClass.name,
                Description = tClass.description,
                Extensions=extensions.ToArray()
            });
            extReader.Close();
        }

        return result.ToArray();
    }

    public TagomatFile[] GetFilesByTags(TagomatTag[] tags, SearchType mode)
    {
        var comm = _sqlConn.CreateCommand();
        var logicalConj = mode switch
        {
            SearchType.MatchAll => "AND",
            SearchType.MatchSome => "OR",
            _ => "AND"
        };
        var nullCheck = mode switch
        {
            SearchType.MatchAll => "IS NOT NULL",
            SearchType.MatchSome => "IS NOT NULL",
            _ => "IS NULL"
        };

        if (tags.Length == 0)
        {
            comm.CommandText = $"SELECT DISTINCT f.fileid, f.dname, f.description, f.fpath FROM files f;";
            var allFileReader = comm.ExecuteReader();
            List<TagomatFile> files = new();
            while (allFileReader.Read())
            {
                files.Add(new()
                {
                    Id = Guid.Parse(allFileReader.GetString(0)),
                    Dname = allFileReader.GetString(1),
                    Description = allFileReader.GetString(2),
                    FilePath = new(allFileReader.GetString(3))
                });
            }
            return files.ToArray();
        }
        
        var innerQueryGen = (string tag) =>
            $"(SELECT t.tagid FROM tags t JOIN filetags ft ON ft.tagid = t.tagid WHERE t.tagname = '{tag}' AND ft.fileid = f.fileid) {nullCheck} ";
        var innerQueries = tags.Select(t => innerQueryGen(t.FullyQualifiedName));
        var whereClause = innerQueries.Aggregate((a, b) => $"{a} {logicalConj} {b}");
        comm.CommandText = $"SELECT DISTINCT f.fileid, f.dname, f.description, f.fpath FROM files f WHERE {whereClause}";
        var reader = comm.ExecuteReader();
        List<TagomatFile> foundFiles = new();
        while (reader.Read())
        {
            foundFiles.Add(new()
            {
                Id = Guid.Parse(reader.GetString(0)),
                Dname = reader.GetString(1),
                Description = reader.GetString(2),
                FilePath = new(reader.GetString(3))
            });
        }

        return foundFiles.ToArray();
    }

    public TagomatFile[] GetFilesByClass(string fileClass)
    {
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $@"SELECT DISTINCT f.fileid, f.dname, f.description, f.fpath FROM files f 
            JOIN extclasses ec ON ec.extension = f.extension 
            JOIN classes c ON c.classid = ec.classid
            WHERE c.name = '{fileClass}';";
        var reader = comm.ExecuteReader();
        List<TagomatFile> files = new();
        while (reader.Read())
        {
            files.Add(new()
            {
                Id = Guid.Parse(reader.GetString(0)),
                Dname = reader.GetString(1),
                Description = reader.GetString(2),
                FilePath = new(reader.GetString(3))
            });
        }

        return files.ToArray();
    }

    public TagomatTag[] GetChildTags(string qualifiedName)
    {
        Console.WriteLine("GetChildTags at "+ qualifiedName);
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $"SELECT t1.tagname, t2.tagname, t1.ismeta FROM tags t1 INNER JOIN tags t2 ON t2.tagid = t1.parent WHERE t2.tagname='{qualifiedName}';";
        var reader = comm.ExecuteReader();
        List<TagomatTag> tags = new();
        while (reader.Read())
        {
            tags.Add(new()
            {
                FullyQualifiedName = reader.GetString(0),
                ParentTag = reader.IsDBNull(1)? null : reader.GetString(1),
                IsMeta = reader.GetBoolean(2)
            });
        }
        Console.WriteLine("Done");
        return tags.ToArray();
    }

    public bool RegisterFile(Uri path, string dname, string description)
    {
        string extension = "none";
        if (path.IsFile)
            extension = path.ToString().Split(".")[^1];
        var comm = _sqlConn.CreateCommand();
        // check if extension is known, else create it
        comm.CommandText = $"SELECT description FROM extensions WHERE extension='{extension}';";
        if (comm.ExecuteScalar() == null)
        {
            Console.Write($"Extension \"{extension}\" not known, enter description: ");
            var newDescription = Console.ReadLine();
            Console.WriteLine("Registering extension...");
            comm.CommandText = @$"BEGIN;
                INSERT INTO extensions (extension, description) 
                VALUES ('{extension}', '{newDescription}');
                COMMIT;";
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not register extension! Error: " + e.Message);
            }

            comm.CommandText = $"SELECT extid FROM extensions WHERE extension='{extension}'";
            var extResult = comm.ExecuteScalar();
            if (extResult == null)
            {
                Console.WriteLine("Creating extension seems to have failed...");
                return false;
            }
            var extid = (Int64)extResult;
            
            Console.Write($"Add extension {extension} to any classes?\nEnter classes: ");
            var classes = Console.ReadLine().Split(" ");
            foreach (var fileClass in classes)
            {
                comm.CommandText = $"SELECT * FROM classes WHERE name='{fileClass}';";
                bool canAssociate = true;
                if (comm.ExecuteScalar() == null)
                {
                    Console.Write($"Class name {fileClass} not known, enter description: ");
                    var classDesc = Console.ReadLine();
                    comm.CommandText =
                        $@" BEGIN;
                            INSERT INTO classes(name, description)
                            VALUES ('{fileClass}', '{classDesc}');
                            COMMIT;";
                    try
                    {
                        comm.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error creating class {fileClass}! Error was: " + e.Message);
                        canAssociate = false;
                    }
                    
                }

                comm.CommandText =
                    $@"BEGIN;
                        INSERT INTO extclasses (extension, classid)
                        VALUES ({extid}, 
                                (SELECT classid FROM classes WHERE name='{fileClass}'));
                        COMMIT;";
                try
                {
                    comm.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error associating {fileClass} with .{extension}! Error was: " + e.Message);
                    return false;
                }
            }
        }

        comm.CommandText =
            $@"BEGIN;
                INSERT INTO files (fileid, fpath, dname, description, extension)
                VALUES (
                        '{Guid.NewGuid()}', '{path}', '{dname}', '{description}', 
                        (SELECT extid FROM extensions WHERE extension='{extension}')
                );
                COMMIT;";
        try
        {
            comm.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error adding the file! Error was: " + e.Message);
            return false;
        }
        return true;
    }

    public bool RegisterTag(string fullyQualifiedName, TagomatTag? parent, string[] aliases)
    {
        //TODO: hierarchy
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $"SELECT tagid FROM tags WHERE tagname='{fullyQualifiedName}';";
        if (comm.ExecuteScalar() != null)
        {
            Console.WriteLine("Tag is already present.");
            return false;
        }

        comm.CommandText =
            @$"BEGIN;
            INSERT INTO tags (tagname, parent, ismeta)
            VALUES ('{fullyQualifiedName}', (SELECT tagid FROM tags WHERE tagname='{(parent.HasValue ? parent.GetValueOrDefault().FullyQualifiedName : "NULL")}'), FALSE);
            COMMIT;";
        try
        {
            comm.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error during tag creation: " + e.Message);
            return false;
        }

        return true;
    }

    public bool AssignTag(TagomatFile file, string qualifiedTag)
    {
        var comm = _sqlConn.CreateCommand();
        if (comm == null)
            return false;
        comm.CommandText = $"SELECT tagid FROM tags WHERE tagname = '{qualifiedTag}'";
        var result = comm.ExecuteScalar();
        if (result == null)
        {
            Console.WriteLine($"Tag {qualifiedTag} not found in database!");
            return false;
        }

        var tagid = (Int64)result;

        comm.CommandText = $"SELECT * FROM filetags WHERE tagid={tagid} AND fileid='{file.Id}'";
        if (comm.ExecuteScalar() != null)
        {
            return true;
        }
        
        comm.CommandText = @$"BEGIN; 
            INSERT INTO filetags (fileid, tagid)
            VALUES ('{file.Id}', {tagid});
            COMMIT;";
        comm.ExecuteNonQuery();
        return true;
    }

    public bool UnassignTag(TagomatFile file, string qualifiedTag)
    {
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $@"SELECT ft.tagid 
                        FROM filetags ft 
                        JOIN tags t ON t.tagid = ft.tagid 
                        WHERE t.tagname = '{qualifiedTag}' AND ft.fileid = '{file.Id}'";
        var result = comm.ExecuteScalar();
        if (result == null)
            return true;
        var tagid = (Int64)result;
        comm.CommandText =
            $@"BEGIN;
            DELETE FROM filetags WHERE tagid={tagid} AND fileid='{file.Id}';
            COMMIT;";
        try
        {
            comm.ExecuteNonQuery();
        }
        catch (SQLiteException e)
        {
            Console.WriteLine("Could not remove tag association! Error was: " + e.Message);
            return false;
        }

        return true;
    }

    public bool RegisterAlias(string fullyQualifiedName, string[] aliases)
    {
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $"SELECT tagid FROM tags WHERE tagname = '{fullyQualifiedName}';";
        var result = comm.ExecuteScalar();
        if (result == null)
            return false;
        var tagid = (Int64)result;
        foreach (var alias in aliases)
        {
            comm.CommandText = $"SELECT aliasid FROM tagaliases WHERE tagid={tagid} AND alias='{alias}'";
            if (comm.ExecuteScalar() != null)
                continue;
            comm.CommandText =
                $@"BEGIN;
                INSERT INTO tagaliases (tagid, aliasid, alias)
                VALUES ({tagid}, (SELECT COALESCE(MAX(aliasid),0) FROM tagaliases) + 1, '{alias}');
                COMMIT;";
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not register alias {alias}: {e.Message}");
                continue;
            }
        }

        return true;
    }

    public bool RemoveAlias(string alias)
    {
        var comm = _sqlConn.CreateCommand();
        comm.CommandText = $"BEGIN;DELETE FROM tagaliases WHERE alias='{alias}';COMMIT;";
        try
        {
            return comm.ExecuteNonQuery() > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Could not remove alias {alias}: {e.Message}");
            return false;
        }
    }
}