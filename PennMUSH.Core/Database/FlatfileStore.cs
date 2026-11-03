namespace PennMUSH.Core.Database;

using System;
using System.IO;
using System.Linq;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public static class FlatfileStore
{
    public static void Save(ObjectDatabase db, TextWriter writer)
    {
        writer.WriteLine("+V5");
        writer.WriteLine("+flags WIZARD ROYALTY DARK QUIET ANSI INHERIT NO_COMMAND");
        writer.WriteLine(db.Count);

        for (int i = 0; i < db.Count; i++)
        {
            var obj = db.GetObject(new Dbref(i));
            if (obj == null)
            {
                writer.WriteLine($"#{i}");
                writer.WriteLine("Garbage");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("1");
                writer.WriteLine("0");
                writer.WriteLine("6"); // Garbage
                writer.WriteLine("");
                writer.WriteLine("");
                writer.WriteLine("<");
                continue;
            }

            writer.WriteLine($"#{obj.Id.Value}");
            writer.WriteLine(obj.Name);
            writer.WriteLine(obj.Location.Value.ToString());
            writer.WriteLine(obj.Contents.Value.ToString());
            writer.WriteLine(obj.Exits.Value.ToString());
            writer.WriteLine(obj.Next.Value.ToString());
            writer.WriteLine(obj.Parents.Count > 0 ? obj.Parents[0].Value.ToString() : "-1");
            writer.WriteLine(obj.Zones.Count > 0 ? obj.Zones.First().Value.ToString() : "-1");
            writer.WriteLine(obj.Owner.Value.ToString());
            writer.WriteLine(obj.Pennies.ToString());
            writer.WriteLine(((int)obj.Type).ToString());
            writer.WriteLine(string.Join(" ", obj.Flags));
            writer.WriteLine(string.Join(" ", obj.Powers));

            foreach (var attr in obj.Attributes)
            {
                writer.WriteLine($"> {attr.Name}");
                writer.WriteLine(attr.Value);
            }

            writer.WriteLine("<");
        }

        writer.WriteLine("***END OF DUMP***");
    }

    public static void Load(ObjectDatabase db, TextReader reader)
    {
        db.Clear();

        string? line = reader.ReadLine();
        if (line == null || !line.StartsWith("+V"))
        {
            throw new FormatException("Invalid flatfile header format.");
        }

        // Skip header lines until object count
        while ((line = reader.ReadLine()) != null)
        {
            if (int.TryParse(line.Trim(), out _))
            {
                break; // Found object count
            }
        }

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line == "***END OF DUMP***") break;
            if (!line.StartsWith('#')) continue;

            if (!int.TryParse(line.Substring(1), out int idVal)) continue;
            var id = new Dbref(idVal);

            string name = reader.ReadLine() ?? string.Empty;
            int loc = int.Parse(reader.ReadLine() ?? "-1");
            int con = int.Parse(reader.ReadLine() ?? "-1");
            int ex = int.Parse(reader.ReadLine() ?? "-1");
            int next = int.Parse(reader.ReadLine() ?? "-1");
            int parent = int.Parse(reader.ReadLine() ?? "-1");
            int zone = int.Parse(reader.ReadLine() ?? "-1");
            int owner = int.Parse(reader.ReadLine() ?? "1");
            int pennies = int.Parse(reader.ReadLine() ?? "0");
            int typeVal = int.Parse(reader.ReadLine() ?? "0");
            string flagsStr = reader.ReadLine() ?? string.Empty;
            string powersStr = reader.ReadLine() ?? string.Empty;

            var obj = new MUSHObject(id, name, (ObjectType)typeVal, new Dbref(owner))
            {
                Location = new Dbref(loc),
                Contents = new Dbref(con),
                Exits = new Dbref(ex),
                Next = new Dbref(next),
                Pennies = pennies
            };

            if (parent >= 0) obj.AddParent(new Dbref(parent));
            if (zone >= 0) obj.AddZone(new Dbref(zone));

            foreach (var f in flagsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                obj.AddFlag(f);

            foreach (var p in powersStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                obj.AddPower(p);

            // Read attributes until '<'
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == "<") break;

                if (line.StartsWith("> "))
                {
                    string attrName = line.Substring(2).Trim();
                    string attrVal = reader.ReadLine() ?? string.Empty;
                    obj.SetAttribute(attrName, attrVal, new Dbref(owner));
                }
            }

            db.SetObject(obj);
        }
    }
}
