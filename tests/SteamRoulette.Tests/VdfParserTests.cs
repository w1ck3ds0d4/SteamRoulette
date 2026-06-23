using SteamRoulette.Core.Steam;

namespace SteamRoulette.Tests;

public class VdfParserTests
{
    [Fact]
    public void Parses_LibraryFolders_With_Nested_Paths()
    {
        const string vdf = """
        "libraryfolders"
        {
            "0"
            {
                "path"        "C:\\Program Files (x86)\\Steam"
                "label"        ""
                "apps"
                {
                    "440"        "123456"
                }
            }
            "1"
            {
                "path"        "D:\\SteamLibrary"
            }
        }
        """;

        var root = VdfParser.Parse(vdf)["libraryfolders"];

        Assert.NotNull(root);
        Assert.Equal(@"C:\Program Files (x86)\Steam", root!["0"]!["path"]!.Value);
        Assert.Equal(@"D:\SteamLibrary", root["1"]!["path"]!.Value);
        Assert.Equal("123456", root["0"]!["apps"]!["440"]!.Value);
    }

    [Fact]
    public void Parses_AppManifest_Fields()
    {
        const string acf = """
        "AppState"
        {
            "appid"        "620"
            "name"        "Portal 2"
            "installdir"        "Portal 2"
            "LastPlayed"        "1700000000"
            "SizeOnDisk"        "13000000000"
        }
        """;

        var state = VdfParser.Parse(acf)["AppState"];

        Assert.NotNull(state);
        Assert.Equal("620", state!["appid"]!.Value);
        Assert.Equal("Portal 2", state["name"]!.Value);
        Assert.Equal("13000000000", state["SizeOnDisk"]!.Value);
    }

    [Fact]
    public void Lookup_Is_Case_Insensitive_And_Missing_Keys_Are_Null()
    {
        var state = VdfParser.Parse("\"AppState\" { \"AppId\" \"10\" }")["appstate"];

        Assert.Equal("10", state!["appid"]!.Value);
        Assert.Null(state["nonexistent"]);
    }
}
