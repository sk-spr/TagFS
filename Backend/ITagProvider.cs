using TagFS.Types;

namespace TagFS.Backend;


public interface ITagProvider
{
    //Control
    public bool Initialize();
    public bool AddTestData();
    
    //Getters
    /// <summary>
    /// Find the tag an alias references
    /// </summary>
    /// <param name="alias">Alias to look up</param>
    /// <returns>Tag if found, null otherwise</returns>
    public TagomatTag? GetTagByAlias(string alias);

    /// <summary>
    /// Find the full tag object for a given fully qualified tag name
    /// </summary>
    /// <param name="qualifiedTagName">Tag to retrieve</param>
    /// <returns>Tag if found, null otherwise</returns>
    public TagomatTag? GetFullTag(string qualifiedTagName);

    /// <summary>
    /// Get all registered tags
    /// </summary>
    /// <returns>Array of available tags</returns>
    public TagomatTag[] GetAllTags();

    /// <summary>
    /// Get all tags associated with a file
    /// </summary>
    /// <param name="file">File to search</param>
    /// <returns>Array of tags assigned to file</returns>
    public TagomatTag[] GetTagsByFile(TagomatFile file);

    /// <summary>
    /// Get all registered classes
    /// </summary>
    /// <returns></returns>
    public TagomatClass[] GetAllClasses();
    
    /// <summary>
    /// Get all files associated with the tags with the specified match mode
    /// </summary>
    /// <param name="tags">A list of tags to look for</param>
    /// <param name="mode">Match mode</param>
    /// <returns>Array of files found</returns>
    public TagomatFile[] GetFilesByTags(TagomatTag[] tags, SearchType mode);
    
    /// <summary>
    /// Get all files associated with a file class with the specified match mode
    /// </summary>
    /// <param name="fileClass">Class to enumerate</param>
    /// <returns>Array of files found</returns>
    public TagomatFile[] GetFilesByClass(string fileClass);

    public TagomatTag[] GetChildTags(string qualifiedName);
    
    //setters
    /// <summary>
    /// Register a File in the tagging system
    /// </summary>
    /// <param name="path">URI of the file or resource to register</param>
    /// <param name="dname">Display name for the file</param>
    /// <param name="description">Description of the file</param>
    /// <returns>bool, whether the file was successfully registered</returns>
    public bool RegisterFile(Uri path, string dname, string description);

    /// <summary>
    /// Register a new tag in the tagging system
    /// </summary>
    /// <param name="fullyQualifiedName">Fully qualified tag name using "/" to delimit inheritance</param>
    /// <param name="parent">Parent tag, can be null</param>
    /// <param name="aliases">Aliases for the tag, may be empty</param>
    /// <returns>bool, whether the tag was successfully registered</returns>
    public bool RegisterTag(string fullyQualifiedName, TagomatTag? parent, string[] aliases);
    /// <summary>
    /// Assign a tag to a file
    /// </summary>
    /// <param name="file">File to tag</param>
    /// <param name="qualifiedTag">Fully qualified tag name using "/" to delimit inheritance</param>
    /// <returns>bool, whether the tag was successfully assigned</returns>
    public bool AssignTag(TagomatFile file, string qualifiedTag);
    /// <summary>
    /// Remove a tag from a file
    /// </summary>
    /// <param name="file">File to untag</param>
    /// <param name="qualifiedTag">Tag to remove</param>
    /// <returns>bool, whether the tag was successfully removed</returns>
    public bool UnassignTag(TagomatFile file, string qualifiedTag);
    /// <summary>
    /// Register a new alias for an existing tag
    /// </summary>
    /// <param name="fullyQualifiedName">Fully qualified tag name using "/" to delimit inheritance</param>
    /// <param name="aliases">Alias(es) to add to the tag</param>
    /// <returns>bool, whether the alias was successfully registered</returns>
    public bool RegisterAlias(string fullyQualifiedName, string[] aliases);
    /// <summary>
    /// Remove an alias association
    /// </summary>
    /// <param name="alias">Alias to remove</param>
    /// <returns>bool, whether the alias was successfully removed</returns>
    public bool RemoveAlias(string alias);

}