using BepInEx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// A group of pieces relative to an anchor at the bottom centre, from a selection or a blueprint file.
/// </summary>
public sealed class Blueprint
{
    /// <summary>Display name.</summary>
    public string Name = string.Empty;

    /// <summary>Who made it.</summary>
    public string Creator = string.Empty;

    /// <summary>The file it was read from, or null.</summary>
    public string File;

    /// <summary>The pieces, relative to the anchor.</summary>
    public List<PieceSnapshot> Pieces = new();

    /// <summary>
    /// Makes a blueprint of placed pieces, anchored at their bottom centre.
    /// </summary>
    /// <param name="pieces">The pieces.</param>
    /// <returns>The blueprint.</returns>
    public static Blueprint FromPieces(IEnumerable<Piece> pieces)
    {
        Blueprint blueprint = new();
        blueprint.Pieces.AddRange(pieces.Where(piece => piece != null).Select(PieceSnapshot.Of));
        blueprint.Normalize();
        return blueprint;
    }

    /// <summary>
    /// Moves the pieces so the anchor is at the bottom centre of the group.
    /// </summary>
    public void Normalize()
    {
        if (Pieces.Count == 0)
        {
            return;
        }

        float minX = Pieces.Min(piece => piece.Position.x);
        float maxX = Pieces.Max(piece => piece.Position.x);
        float minZ = Pieces.Min(piece => piece.Position.z);
        float maxZ = Pieces.Max(piece => piece.Position.z);
        Vector3 anchor = new((minX + maxX) / 2f, Pieces.Min(piece => piece.Position.y), (minZ + maxZ) / 2f);
        foreach (PieceSnapshot piece in Pieces)
        {
            piece.Position -= anchor;
        }
    }
}

/// <summary>
/// Reads and writes blueprints in BepInEx/config/BrudvikWhiteHilt/blueprints/. Files are written in PlanBuild's
/// .blueprint format, so they can be shared; PlanBuild .blueprint and BuildShare .vbuild files are read.
/// </summary>
public static class BlueprintStore
{
    private const string BlueprintExtension = ".blueprint";
    private const string VBuildExtension = ".vbuild";

    /// <summary>The blueprint folder.</summary>
    public static string Folder => Path.Combine(Paths.ConfigPath, "BrudvikWhiteHilt", "blueprints");

    /// <summary>
    /// Reads every blueprint in the folder, sorted by name.
    /// </summary>
    /// <returns>The blueprints.</returns>
    public static List<Blueprint> LoadAll()
    {
        List<Blueprint> blueprints = new();
        if (!Directory.Exists(Folder))
        {
            return blueprints;
        }

        foreach (string file in Directory.GetFiles(Folder))
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension != BlueprintExtension && extension != VBuildExtension)
            {
                continue;
            }

            try
            {
                Blueprint blueprint = extension == VBuildExtension ? ReadVBuild(file) : ReadBlueprint(file);
                if (blueprint.Pieces.Count > 0)
                {
                    blueprints.Add(blueprint);
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Could not read blueprint {file}: {ex.Message}");
            }
        }

        return blueprints.OrderBy(blueprint => blueprint.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>
    /// Writes a blueprint as a .blueprint file named after it, and replaces the file it came from.
    /// </summary>
    /// <param name="blueprint">The blueprint.</param>
    public static void Save(Blueprint blueprint)
    {
        Directory.CreateDirectory(Folder);
        string file = UniqueFile(blueprint.Name, blueprint.File);
        File.WriteAllText(file, Format(blueprint));
        if (!string.IsNullOrEmpty(blueprint.File) && !string.Equals(blueprint.File, file, StringComparison.OrdinalIgnoreCase) && File.Exists(blueprint.File))
        {
            File.Delete(blueprint.File);
        }

        blueprint.File = file;
    }

    /// <summary>
    /// Writes a blueprint in PlanBuild's .blueprint format.
    /// </summary>
    /// <param name="blueprint">The blueprint.</param>
    /// <returns>The file's text.</returns>
    internal static string Format(Blueprint blueprint)
    {
        StringBuilder text = new();
        text.AppendLine("#Name:" + blueprint.Name.Replace('\n', ' '));
        text.AppendLine("#Creator:" + blueprint.Creator.Replace('\n', ' '));
        text.AppendLine("#Description:\"\"");
        text.AppendLine("#Category:Misc");
        text.AppendLine("#Pieces");
        foreach (PieceSnapshot piece in blueprint.Pieces)
        {
            text.AppendLine(string.Join(";", piece.Prefab, "Building",
                Number(piece.Position.x), Number(piece.Position.y), Number(piece.Position.z),
                Number(piece.Rotation.x), Number(piece.Rotation.y), Number(piece.Rotation.z), Number(piece.Rotation.w),
                Quote(piece.Text), "1", "1", "1"));
        }

        return text.ToString();
    }

    /// <summary>
    /// Deletes a blueprint's file.
    /// </summary>
    /// <param name="blueprint">The blueprint.</param>
    public static void Delete(Blueprint blueprint)
    {
        if (!string.IsNullOrEmpty(blueprint.File) && File.Exists(blueprint.File))
        {
            File.Delete(blueprint.File);
        }
    }

    private static Blueprint ReadBlueprint(string file)
    {
        Blueprint blueprint = ParseBlueprint(File.ReadAllLines(file), Path.GetFileNameWithoutExtension(file));
        blueprint.File = file;
        return blueprint;
    }

    /// <summary>
    /// Reads a PlanBuild .blueprint file's lines, with or without sections.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <param name="name">The name to use when the file gives none.</param>
    /// <returns>The blueprint, anchored at its bottom centre.</returns>
    internal static Blueprint ParseBlueprint(IEnumerable<string> lines, string name)
    {
        Blueprint blueprint = new() { Name = name };
        bool sawSection = false;
        bool inPieces = false;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("#"))
            {
                if (line.StartsWith("#Name:", StringComparison.OrdinalIgnoreCase))
                {
                    blueprint.Name = line.Substring(6).Trim();
                }
                else if (line.StartsWith("#Creator:", StringComparison.OrdinalIgnoreCase))
                {
                    blueprint.Creator = line.Substring(9).Trim();
                }
                else if (line.IndexOf(':') < 0)
                {
                    sawSection = true;
                    inPieces = line.Equals("#Pieces", StringComparison.OrdinalIgnoreCase);
                }

                continue;
            }

            // Old PlanBuild files have no sections: every line is a piece.
            if (inPieces || !sawSection)
            {
                string[] parts = line.Split(';');
                if (parts.Length >= 9)
                {
                    blueprint.Pieces.Add(new PieceSnapshot
                    {
                        Prefab = parts[0].Split('(')[0].Trim(),
                        Position = new Vector3(Parse(parts[2]), Parse(parts[3]), Parse(parts[4])),
                        Rotation = new Quaternion(Parse(parts[5]), Parse(parts[6]), Parse(parts[7]), Parse(parts[8])).normalized,
                        Text = parts.Length > 9 ? Unquote(parts[9]) : null
                    });
                }
            }
        }

        blueprint.Normalize();
        return blueprint;
    }

    private static Blueprint ReadVBuild(string file)
    {
        Blueprint blueprint = ParseVBuild(File.ReadAllLines(file), Path.GetFileNameWithoutExtension(file));
        blueprint.File = file;
        return blueprint;
    }

    /// <summary>
    /// Reads a BuildShare .vbuild file's lines.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <param name="name">The blueprint's name.</param>
    /// <returns>The blueprint, anchored at its bottom centre.</returns>
    internal static Blueprint ParseVBuild(IEnumerable<string> lines, string name)
    {
        Blueprint blueprint = new() { Name = name };
        foreach (string raw in lines)
        {
            string[] parts = raw.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
            {
                continue;
            }

            blueprint.Pieces.Add(new PieceSnapshot
            {
                Prefab = parts[0].Split('(')[0],
                Rotation = new Quaternion(Parse(parts[1]), Parse(parts[2]), Parse(parts[3]), Parse(parts[4])).normalized,
                Position = new Vector3(Parse(parts[5]), Parse(parts[6]), Parse(parts[7]))
            });
        }

        blueprint.Normalize();
        return blueprint;
    }

    private static string UniqueFile(string name, string current)
    {
        string safe = string.IsNullOrWhiteSpace(name) ? "blueprint" : name.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(invalid, '_');
        }

        string file = Path.Combine(Folder, safe + BlueprintExtension);
        for (int i = 2; File.Exists(file) && !string.Equals(file, current, StringComparison.OrdinalIgnoreCase); i++)
        {
            file = Path.Combine(Folder, $"{safe} ({i}){BlueprintExtension}");
        }

        return file;
    }

    // Some tools write a decimal comma; only numbers are read this way, so commas in a sign's text stay.
    private static float Parse(string text)
    {
        return float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
    }

    private static string Number(float value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Quote(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "\"\"";
        }

        string escaped = text.Replace(";", string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", string.Empty);
        return "\"" + escaped + "\"";
    }

    // Undoes Quote one escape at a time, so a backslash written before an n stays a backslash and an n.
    private static string Unquote(string text)
    {
        text = text.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
        {
            StringBuilder plain = new(text.Length);
            for (int i = 1; i < text.Length - 1; i++)
            {
                char c = text[i];
                if (c == '\\' && i + 1 < text.Length - 1)
                {
                    char next = text[++i];
                    plain.Append(next == 'n' ? '\n' : next);
                }
                else
                {
                    plain.Append(c);
                }
            }

            text = plain.ToString();
        }

        return string.IsNullOrEmpty(text) ? null : text;
    }
}
